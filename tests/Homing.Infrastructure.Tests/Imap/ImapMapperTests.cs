using Homing.Core.Messages;
using Homing.Infrastructure.Imap;
using MailKit;
using MimeKit;
using ImapFlags = MailKit.MessageFlags;
using MessageFlags = Homing.Core.Messages.MessageFlags;
using ImapSummary = MailKit.MessageSummary;

namespace Homing.Infrastructure.Tests.Imap;

public class ImapMapperTests
{
    private const string Account = "other_mail@mail.ru";
    private const string Folder = "INBOX/Work";
    private const uint UidValidity = 1370234464;
    private const uint Uid = 42;
    private static readonly DateTimeOffset ReceivedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

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
        Assert.Equal(new EmailAddress("some_mail@mail.ru", "SomeName"), result.From);
        Assert.Equal("Hello", result.Subject);
        Assert.Equal(["work"], result.Tags);
        Assert.False(result.HasAttachments);
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

    private static ImapSummary CreateValidImapSummary()
    {
        var envelope = new Envelope { Subject = "Hello" };
        envelope.From.Add(new MailboxAddress("SomeName", "some_mail@mail.ru"));
        var body = new BodyPartText(new ContentType("text", "plain"), "1");

        return new ImapSummary(0)
        {
            UniqueId = new UniqueId(Uid),
            Flags = ImapFlags.Seen,
            InternalDate = ReceivedAt,
            Envelope = envelope,
            Body = body,
            Keywords = new HashSet<string> { "work" },
        };
    }
}