using System.Diagnostics.CodeAnalysis;

namespace Homing.Core.Messages;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Имя повторяет терминологию IMAP (message flags) и MailKit.")]
[Flags]
public enum MessageFlags
{
    None = 0,
    Seen = 1,
    Flagged = 2,
    Answered = 4,
    Deleted = 8,
    Draft = 16,
}