using Homing.Core.Messages;
using MailKit;
using MimeKit;
using ImapFlags = MailKit.MessageFlags;
using MessageFlags = Homing.Core.Messages.MessageFlags;
using MessageSummary = Homing.Core.Messages.MessageSummary;

namespace Homing.Infrastructure.Imap;

internal static class ImapMapper
{
    public static EmailAddress ToEmailAddress(MailboxAddress address) =>
        new EmailAddress(address.Address, string.IsNullOrWhiteSpace(address.Name) ? null : address.Name);

    public static MessageFlags ToMessageFlags(ImapFlags flags)
    {
        var result = MessageFlags.None;

        if (flags.HasFlag(ImapFlags.Seen))
            result |= MessageFlags.Seen;
        if (flags.HasFlag(ImapFlags.Answered))
            result |= MessageFlags.Answered;
        if (flags.HasFlag(ImapFlags.Flagged))
            result |= MessageFlags.Flagged;
        if (flags.HasFlag(ImapFlags.Deleted))
            result |= MessageFlags.Deleted;
        if (flags.HasFlag(ImapFlags.Draft))
            result |= MessageFlags.Draft;

        return result;
    }

    public static MessageSummary ToMessageSummary(IMessageSummary summary, string account, string folderPath,
        uint uidValidity)
    {
        var envelope = summary.Envelope ?? throw new InvalidOperationException("Envelope was not fetched.");
        var flags = summary.Flags ?? throw new InvalidOperationException("Flags were not fetched.");
        var date = summary.InternalDate ?? throw new InvalidOperationException("InternalDate was not fetched.");
        var body = summary.Body ?? throw new InvalidOperationException("Body was not fetched.");
        var from = envelope.From.Mailboxes.FirstOrDefault();

        return new MessageSummary
        {
            Id = new MessageId(account, folderPath, uidValidity, summary.UniqueId.Id),
            ReceivedAt = date,
            Flags = ToMessageFlags(flags),
            From = from is null ? null : ToEmailAddress(from),
            HasAttachments = summary.Attachments.Any(),
            Subject = envelope.Subject,
            Tags = summary.Keywords.ToList()
        };
    }

    public static MessageDetails ToMessageDetails(IMessageSummary imapSummary, MessageSummary domainSummary,
        string? textBody, string? htmlBody)
    {
        var envelope = imapSummary.Envelope ?? throw new InvalidOperationException("Envelope was not fetched.");
        var refs = imapSummary.References?.ToList() ??
                   throw new InvalidOperationException("References was not fetched.");
        var to = ToEmailAddresses(envelope.To);
        var cc = ToEmailAddresses(envelope.Cc);
        var bcc = ToEmailAddresses(envelope.Bcc);
        var attachments = ToAttachmentInfos(imapSummary.Attachments);
        var replyTo = ToEmailAddresses(envelope.ReplyTo);

        return new MessageDetails
        {
            Summary = domainSummary,
            HtmlBody = htmlBody,
            TextBody = textBody,
            To = to,
            Cc = cc,
            Bcc = bcc,
            Attachments = attachments,
            InReplyTo = envelope.InReplyTo,
            SentAt = envelope.Date,
            MessageIdHeader = envelope.MessageId,
            ReplyTo = replyTo,
            References = refs,
        };
    }

    private static List<EmailAddress> ToEmailAddresses(InternetAddressList list)
    {
        return (from MailboxAddress? address in list.Mailboxes select ToEmailAddress(address)).ToList();
    }

    private static List<AttachmentInfo> ToAttachmentInfos(IEnumerable<BodyPartBasic> infos)
    {
        return (from BodyPartBasic? body in infos
                select ToAttachmentInfo(body))
            .ToList();
    }

    private static AttachmentInfo ToAttachmentInfo(BodyPartBasic part)
    {
        return new AttachmentInfo(part.PartSpecifier, part.Octets, part.ContentType.MimeType, part.FileName);
    }
}