using Homing.Infrastructure.Imap;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

var test = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var mailAcc = MailAccountSettings.ForMailRu(
    test["Mail:Username"] ?? throw new InvalidOperationException(),
    test["Mail:Password"] ?? throw new InvalidOperationException());
Console.WriteLine(mailAcc);

using var client = new ImapClient();

await client.ConnectAsync(mailAcc.Host, mailAcc.Port, SecureSocketOptions.SslOnConnect);

await client.AuthenticateAsync(mailAcc.Username, mailAcc.Password);

var listOfFolders = await client.GetFoldersAsync(client.PersonalNamespaces[0]);

foreach (var folder in listOfFolders)
{
    Console.WriteLine(folder.FullName);
}

var folderAccess = await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

Console.WriteLine($"Messages {client.Inbox.Count}");
Console.WriteLine($"UIDVALIDITY {client.Inbox.UidValidity}");

await client.DisconnectAsync(true);