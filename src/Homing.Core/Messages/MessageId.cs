namespace Homing.Core.Messages;

public readonly record struct MessageId(string Account, string Folder, uint UidValidity, uint Uid);