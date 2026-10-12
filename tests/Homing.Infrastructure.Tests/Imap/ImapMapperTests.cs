using Homing.Core.Messages;
using Homing.Infrastructure.Imap;
using MailKit;
using MimeKit;
using ImapFlags = MailKit.MessageFlags;
using MessageFlags = Homing.Core.Messages.MessageFlags;
using ImapSummary = MailKit.MessageSummary;
using MessageSummary = Homing.Core.Messages.MessageSummary;

namespace Homing.Infrastructure.Tests.Imap;

public class ImapMapperTests
{
    private const string Account = "other_mail@mail.ru";
    private const string Folder = "INBOX/Work";
    private const uint UidValidity = 1370234464;
    private const uint Uid = 42;
    private static readonly DateTimeOffset ReceivedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SentAt = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ImapFlags.None, MessageFlags.None)]
    [InlineData(ImapFlags.Seen, MessageFlags.Seen)]
    [InlineData(ImapFlags.Answered, MessageFlags.Answered)]
    [InlineData(ImapFlags.Flagged, MessageFlags.Flagged)]
    [InlineData(ImapFlags.Deleted, MessageFlags.Deleted)]
    [InlineData(ImapFlags.Draft, MessageFlags.Draft)]
    public void ToMessageFlagsSingleFlagMapsToSameDomainFlag(ImapFlags input, MessageFlags expected)
    {
        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToMessageFlagsSeveralFlagsMapsAllOfThem()
    {
        var input = ImapFlags.Seen | ImapFlags.Flagged;

        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(MessageFlags.Seen | MessageFlags.Flagged, result);
    }

    [Fact]
    public void ToMessageFlagsUndefinedFlagsMeansNone()
    {
        var input = ImapFlags.UserDefined | ImapFlags.Recent;

        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(MessageFlags.None, result);
    }

    [Theory]
    [InlineData("", "some_address@mail.ru")]
    [InlineData("   ", "some_address2@mail.ru")]
    public void ToEmailAddressEmptyNameMeansNull(string name, string address)
    {
        var input = new MailboxAddress(name, address);

        var result = ImapMapper.ToEmailAddress(input);

        Assert.Equal(address, result.Address);
        Assert.Null(result.Name);
    }

    [Fact]
    public void ToEmailAddressSomeNameMeansOk()
    {
        var input = new MailboxAddress("SomeName", "some_address@mail.ru");

        var result = ImapMapper.ToEmailAddress(input);

        Assert.Equal("some_address@mail.ru", result.Address);
        Assert.Equal("SomeName", result.Name);
    }

    [Fact]
    public void ToMessageSummaryValidSummaryMapsAllFields()
    {
        var imap = CreateValidImapSummary();

        var result = ImapMapper.ToMessageSummary(imap, Account, Folder, UidValidity);

        Assert.Equal(new MessageId(Account, Folder, UidValidity, Uid), result.Id);
        Assert.Equal(ReceivedAt, result.ReceivedAt);
        Assert.Equal(MessageFlags.Seen, result.Flags);
        Assert.True(result.IsRead);
        Assert.Equal(new EmailAddress("some_from_mail@mail.ru", "SomeFromName"), result.From);
        Assert.Equal("Hello", result.Subject);
        Assert.Equal(["work"], result.Tags);
        Assert.True(result.HasAttachments);
    }

    [Fact]
    public void ToMessageSummaryNullFrom()
    {
        var imap = CreateValidImapSummary();
        imap.Envelope!.From.Clear();

        var result = ImapMapper.ToMessageSummary(imap, Account, Folder, UidValidity);

        Assert.Null(result.From);
    }

    [Fact]
    public void ToMessageSummaryNoEnvelope()
    {
        var imap = CreateValidImapSummary();
        imap.Envelope = null;

        Assert.Throws<InvalidOperationException>(() => ImapMapper.ToMessageSummary(imap, Account, Folder, UidValidity));
    }

    [Fact]
    public void ToMessageDetailsMapsAllFields()
    {
        var imap = CreateValidImapSummary();
        var summary = CreateDomainSummary();

        var result = ImapMapper.ToMessageDetails(imap, summary, "plain text", "<p>html text</p>");

        Assert.Same(summary, result.Summary);
        Assert.Equal([new EmailAddress("some_to_mail@mail.ru", "SomeToName")], result.To);
        Assert.Equal([new EmailAddress("some_cc_mail@mail.ru", "SomeCcName")], result.Cc);
        Assert.Equal([new EmailAddress("some_reply_to_mail@mail.ru", "SomeReplyToName")], result.ReplyTo);
        Assert.Equal(SentAt, result.SentAt);
        Assert.Equal("msg-1@mail.ru", result.MessageIdHeader);
        Assert.Equal("parent@mail.ru", result.InReplyTo);
        Assert.Equal(["root@mail.ru", "parent@mail.ru"], result.References);
        Assert.Equal("plain text", result.TextBody);
        Assert.Equal("<p>html text</p>", result.HtmlBody);
        Assert.Equal([new AttachmentInfo("2", 1024, "application/pdf", "file.pdf")], result.Attachments);
        Assert.Empty(result.Bcc);
    }

    [Fact]
    public void ToMessageDetailsReferencesNotFetchedThrows()
    {
        var imap = CreateValidImapSummary();
        imap.References = null;
        var summary = CreateDomainSummary();

        Assert.Throws<InvalidOperationException>(() =>
            ImapMapper.ToMessageDetails(imap, summary, "plain text", "<p>html text</p>"));
    }

    [Fact]
    public void ToMessageDetailsEmptyGroupInToIsSkipped()
    {
        var imap = CreateValidImapSummary();
        imap.Envelope!.To.Add(new GroupAddress("undisclosed-recipients"));
        var summary = CreateDomainSummary();

        var result = ImapMapper.ToMessageDetails(imap, summary, "plain text", "<p>html text</p>");

        Assert.Equal([new EmailAddress("some_to_mail@mail.ru", "SomeToName")], result.To);
    }

    private static MessageSummary CreateDomainSummary() =>
        new() { Id = new MessageId(Account, Folder, UidValidity, Uid), ReceivedAt = ReceivedAt };

    private static ImapSummary CreateValidImapSummary()
    {
        var envelope = new Envelope { Subject = "Hello" };
        envelope.From.Add(new MailboxAddress("SomeFromName", "some_from_mail@mail.ru"));
        envelope.Cc.Add(new MailboxAddress("SomeCcName", "some_cc_mail@mail.ru"));
        envelope.To.Add(new MailboxAddress("SomeToName", "some_to_mail@mail.ru"));
        envelope.ReplyTo.Add(new MailboxAddress("SomeReplyToName", "some_reply_to_mail@mail.ru"));
        envelope.Date = SentAt;
        envelope.MessageId = "msg-1@mail.ru";
        envelope.InReplyTo = "parent@mail.ru";

        var body = new BodyPartMultipart(new ContentType("multipart", "mixed"), "");
        body.BodyParts.Add(new BodyPartText(new ContentType("text", "plain"), "1"));
        body.BodyParts.Add(new BodyPartBasic(new ContentType("application", "pdf"), "2")
        {
            Octets = 1024,
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment) { FileName = "file.pdf" },
        });

        return new ImapSummary(0)
        {
            UniqueId = new UniqueId(Uid),
            Flags = ImapFlags.Seen,
            InternalDate = ReceivedAt,
            Envelope = envelope,
            Body = body,
            Keywords = new HashSet<string> { "work" },
            References = new MessageIdList { "root@mail.ru", "parent@mail.ru" },
        };
    }
}