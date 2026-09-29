namespace Homing.Core.Messages;

public record AttachmentInfo(string PartId, long Size, string ContentType, string? Name);