using Homing.Core.Exceptions;
using Homing.Core.Messages;

namespace Homing.Core.Abstractions;

/// <summary>
/// Reads messages from a single mail account.
/// </summary>
/// <remarks>
/// An instance is bound to one account; methods do not take the account as a parameter.
/// </remarks>
public interface IMailboxReader
{
    /// <summary>
    /// Gets a page of message summaries from a folder, ordered from newest to oldest.
    /// </summary>
    /// <remarks>
    /// Uses keyset pagination: to get the next page, pass the <see cref="MessageSummary.Id"/>
    /// of the last message from the previous page as <paramref name="olderThan"/>.
    /// Unlike offset pagination, pages stay consistent when new messages arrive or old ones are deleted.
    /// </remarks>
    /// <param name="folder">Full folder path as reported by the server, e.g. <c>INBOX</c>.</param>
    /// <param name="olderThan">
    /// Cursor: only messages older than this one are returned.
    /// <see langword="null"/> requests the first (newest) page.
    /// Must belong to <paramref name="folder"/>.
    /// </param>
    /// <param name="count">Maximum number of messages to return. Must be greater than zero.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// Up to <paramref name="count"/> message summaries. Fewer than <paramref name="count"/>
    /// (including an empty list) means there are no older messages left.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is zero or negative.</exception>
    /// <exception cref="ArgumentException"><paramref name="olderThan"/> belongs to a different folder.</exception>
    /// <exception cref="UidValidityChangedException">
    /// The folder's UIDVALIDITY changed since <paramref name="olderThan"/> was obtained;
    /// the cursor is stale and the folder must be reloaded from the first page.
    /// </exception>
    /// <exception cref="MailboxException">Any other mail server or connection error.</exception>
    Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(string folder, MessageId? olderThan, int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full contents of a message: recipients, body and attachment metadata.
    /// </summary>
    /// <remarks>
    /// Attachment content is not downloaded; only <see cref="AttachmentInfo"/> metadata is returned.
    /// </remarks>
    /// <param name="messageId">Identifier of the message to load.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The message details, or <see langword="null"/> if the message no longer exists
    /// (for example, it was deleted or moved from another client).
    /// </returns>
    /// <exception cref="UidValidityChangedException">
    /// The folder's UIDVALIDITY changed since <paramref name="messageId"/> was obtained.
    /// </exception>
    /// <exception cref="MailboxException">Any other mail server or connection error.</exception>
    Task<MessageDetails?> GetMessageAsync(MessageId messageId, CancellationToken cancellationToken = default);
}