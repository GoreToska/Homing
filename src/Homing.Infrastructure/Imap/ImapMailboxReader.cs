using Homing.Core.Abstractions;
using Homing.Core.Exceptions;
using Homing.Core.Messages;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MessageSummary = Homing.Core.Messages.MessageSummary;

namespace Homing.Infrastructure.Imap;

public sealed class ImapMailboxReader : IMailboxReader
{
    private readonly MailAccountSettings _settings;

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

        using var client = await ConnectClientAsync(cancellationToken);
        var foundFolder = await client.GetFolderAsync(folder, cancellationToken);
        await foundFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        if (olderThan is not null && foundFolder.UidValidity != olderThan.Value.UidValidity)
            throw new UidValidityChangedException(folder, olderThan.Value.UidValidity, foundFolder.UidValidity);

        if (olderThan is not null && olderThan.Value.Uid <= 1) return [];

        var query = olderThan is null
            ? SearchQuery.All
            : SearchQuery.Uids(new UniqueIdRange(UniqueId.MinValue, new UniqueId(olderThan.Value.Uid - 1)));

        var searchResult = await foundFolder.SearchAsync(query, cancellationToken);

        var pageUids = searchResult.OrderByDescending(u => u.Id).Take(count).ToList();

        if (pageUids.Count == 0) return [];

        var items = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags |
                    MessageSummaryItems.InternalDate | MessageSummaryItems.BodyStructure;

        var fetched = await foundFolder.FetchAsync(pageUids, items, cancellationToken);
        var result = fetched.Select(s =>
                ImapMapper.ToMessageSummary(s, _settings.Username, foundFolder.FullName, foundFolder.UidValidity))
            .OrderByDescending(s => s.Id.Uid).ToList();

        await client.DisconnectAsync(true, cancellationToken);
        
        return result;
    }

    public Task<MessageDetails?> GetMessageAsync(MessageId messageId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
}