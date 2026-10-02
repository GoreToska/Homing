using System.Text;

namespace Homing.Infrastructure.Imap;

public record MailAccountSettings(
    string Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl = true
)
{
    public static MailAccountSettings ForMailRu(string username, string password)
    {
        return new MailAccountSettings("imap.mail.ru", 993, username, password, true);
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Host = ").Append(Host);
        builder.Append(", Port = ").Append(Port);
        builder.Append(", Username = ").Append(Username);
        builder.Append(", Password = ").Append("*****");
        builder.Append(", UseSsl = ").Append(UseSsl);

        return true;
    }
}