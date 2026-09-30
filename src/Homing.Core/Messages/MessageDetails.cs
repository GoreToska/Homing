namespace Homing.Core.Messages;

public record MessageDetails
{
    public required MessageSummary Summary { get; init; }
    public IReadOnlyList<EmailAddress> To { get; init; } = [];
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];
    public IReadOnlyList<EmailAddress> ReplyTo { get; init; } = [];
    public DateTimeOffset? SentAt { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public IReadOnlyList<AttachmentInfo> Attachments { get; init; } = [];
    public string? MessageIdHeader { get; init; }
    public string? InReplyTo { get; init; }
    public IReadOnlyList<string> References { get; init; } = [];
}