using Homing.Core.Messages;
using Homing.Infrastructure.Imap;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var mailAcc = MailAccountSettings.ForMailRu(
    secrets["Mail:Username"] ?? throw new InvalidOperationException(),
    secrets["Mail:Password"] ?? throw new InvalidOperationException());

var reader = new ImapMailboxReader(mailAcc);

Console.WriteLine($"First page");

var firstPage = await reader.GetMessagesAsync("INBOX", null, 10);
foreach (var m in firstPage)
{
    Console.WriteLine($"{m.Id.Uid} {m.ReceivedAt} {m.From?.Address} {m.Subject} {m.IsRead} {m.HasAttachments}");
}

Console.WriteLine($"Second page");

foreach (var m in await reader.GetMessagesAsync("INBOX", firstPage.ToList().Last().Id, 10))
{
    Console.WriteLine($"{m.Id.Uid} {m.ReceivedAt} {m.From?.Address} {m.Subject} {m.IsRead} {m.HasAttachments}");
}

var Id = firstPage[1].Id;

var details = await reader.GetMessageAsync(Id);
if(details is null) Console.WriteLine("Message is null");
else
{
    Console.WriteLine(details.Summary);
    Console.WriteLine(details.TextBody);
    Console.WriteLine(details.HtmlBody);
    Console.WriteLine(details.To);
    Console.WriteLine(details.Cc);
    Console.WriteLine(details.Attachments);
}