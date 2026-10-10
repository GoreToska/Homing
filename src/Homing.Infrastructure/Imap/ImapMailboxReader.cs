using System.Net.Sockets;
using Homing.Core.Abstractions;
using Homing.Core.Exceptions;
using Homing.Core.Messages;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MessageSummary = Homing.Core.Messages.MessageSummary;

namespace Homing.Infrastructure.Imap;

public sealed class ImapMailboxReader : IMailboxReader
{
    private readonly MailAccountSettings _settings;

    private const MessageSummaryItems SummaryItems =
        MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags |
        MessageSummaryItems.InternalDate | MessageSummaryItems.BodyStructure;

    public ImapMailboxReader(MailAccountSettings settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(string folder, MessageId? olderThan, int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));

        if (olderThan is not null && olderThan.Value.Folder != folder)
            throw new ArgumentException("Cursor belongs to a different folder.", nameof(olderThan));

        try
        {
            using var client = await ConnectClientAsync(cancellationToken);
            var foundFolder = await OpenFolderAsync(client, folder, olderThan?.UidValidity, cancellationToken);

            if (olderThan is not null && olderThan.Value.Uid <= 1) return [];

            var query = olderThan is null
                ? SearchQuery.All
                : SearchQuery.Uids(new UniqueIdRange(UniqueId.MinValue, new UniqueId(olderThan.Value.Uid - 1)));

            var searchResult = await foundFolder.SearchAsync(query, cancellationToken);

            var pageUids = searchResult.OrderByDescending(u => u.Id).Take(count).ToList();

            if (pageUids.Count == 0) return [];

            var fetched = await foundFolder.FetchAsync(pageUids, SummaryItems, cancellationToken);
            return fetched.Select(s =>
                    ImapMapper.ToMessageSummary(s, _settings.Username, foundFolder.FullName, foundFolder.UidValidity))
                .OrderByDescending(s => s.Id.Uid).ToList();
        }
        catch (Exception e) when (IsMailError(e))
        {
            throw new MailboxException($"Failed to get messages from folder '{folder}'.", e);
        }
    }

    public async Task<MessageDetails?> GetMessageAsync(MessageId messageId,
        CancellationToken cancellationToken = default)
    {
        if (messageId.Account != _settings.Username)
            throw new ArgumentException("Message belongs to a different account.", nameof(messageId));

        try
        {
            using var client = await ConnectClientAsync(cancellationToken);
            var foundFolder = await OpenFolderAsync(client, messageId.Folder, messageId.UidValidity, cancellationToken);

            var messageSummaries = await foundFolder.FetchAsync([new UniqueId(messageId.Uid)],
                SummaryItems | MessageSummaryItems.References, cancellationToken);

            var summary = messageSummaries.FirstOrDefault();

            if (summary is null)
                return null;

            var textBody = await DownloadTextAsync(foundFolder, summary.UniqueId, summary.TextBody, cancellationToken);
            var htmlBody = await DownloadTextAsync(foundFolder, summary.UniqueId, summary.HtmlBody, cancellationToken);

            var messageSummary = ImapMapper.ToMessageSummary(summary, _settings.Username, foundFolder.FullName,
                foundFolder.UidValidity);

            return ImapMapper.ToMessageDetails(summary, messageSummary, textBody, htmlBody);
        }
        catch (Exception e) when (IsMailError(e))
        {
            throw new MailboxException($"Failed to get message {messageId.Uid} from folder '{messageId.Folder}'.", e);
        }
    }

    private async Task<ImapClient> ConnectClientAsync(CancellationToken cancellationToken)
    {
        var client = new ImapClient();

        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None, cancellationToken);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            return client;
        }
        catch (Exception)
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task<IMailFolder> OpenFolderAsync(ImapClient client, string folder, uint? expectedValidity,
        CancellationToken cancellationToken)
    {
        var foundFolder = await client.GetFolderAsync(folder, cancellationToken);
        await foundFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        if (expectedValidity is not null && foundFolder.UidValidity != expectedValidity)
            throw new UidValidityChangedException(folder, expectedValidity.Value, foundFolder.UidValidity);

        return foundFolder;
    }

    private static async Task<string?> DownloadTextAsync(IMailFolder folder, UniqueId uid, BodyPartText? part,
        CancellationToken cancellationToken)
    {
        if (part is null) return null;

        var body = await folder.GetBodyPartAsync(uid, part, cancellationToken);

        if (body is TextPart textPart)
            return textPart.Text;

        return null;
    }

    private static bool IsMailError(Exception e)
    {
        return e is CommandException or ProtocolException or FolderNotFoundException or AuthenticationException
            or SslHandshakeException or IOException or SocketException or ParseException;
    }
}