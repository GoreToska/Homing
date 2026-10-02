namespace Homing.Core.Exceptions;

public sealed class UidValidityChangedException(string folder, uint expected, uint actual)
    : MailboxException($"Folder '{folder}': UID validity changed from '{expected}' to '{actual}'.")
{
    public string Folder { get; } = folder;
    public uint ExpectedUidValidity { get; } = expected;
    public uint ActualUidValidity { get; } = actual;
}