namespace Homing.Core.Messages;

public record MessageSummary
{
    public required MessageId Id { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public MessageFlags Flags { get; init; }
    public bool HasAttachments { get; init; }
    public EmailAddress? From { get; init; }
    public string? Subject { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool IsRead => Flags.HasFlag(MessageFlags.Seen);
}