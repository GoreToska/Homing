namespace Homing.Core.Exceptions;

public class MailboxException : Exception
{
    public MailboxException() : base()
    {
    }

    public MailboxException(string message) : base(message)
    {
    }

    public MailboxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}