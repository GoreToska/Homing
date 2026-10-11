using Homing.Infrastructure.Imap;
using MimeKit;
using ImapFlags = MailKit.MessageFlags;
using MessageFlags = Homing.Core.Messages.MessageFlags;

namespace Homing.Infrastructure.Tests.Imap;

public class ImapMapperTests
{
    [Theory]
    [InlineData(ImapFlags.None, MessageFlags.None)]
    [InlineData(ImapFlags.Seen, MessageFlags.Seen)]
    [InlineData(ImapFlags.Answered, MessageFlags.Answered)]
    [InlineData(ImapFlags.Flagged, MessageFlags.Flagged)]
    [InlineData(ImapFlags.Deleted, MessageFlags.Deleted)]
    [InlineData(ImapFlags.Draft, MessageFlags.Draft)]
    public void ToMessageFlagsSingleFlagMapsToSameDomainFlag(ImapFlags input, MessageFlags expected)
    {
        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToMessageFlagsSeveralFlagsMapsAllOfThem()
    {
        var input = ImapFlags.Seen | ImapFlags.Flagged;

        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(MessageFlags.Seen | MessageFlags.Flagged, result);
    }

    [Fact]
    public void ToMessageFlagsUndefinedFlagsMeansNone()
    {
        var input = ImapFlags.UserDefined | ImapFlags.Recent;

        var result = ImapMapper.ToMessageFlags(input);

        Assert.Equal(MessageFlags.None, result);
    }

    [Theory]
    [InlineData("", "some_address@mail.ru")]
    [InlineData("   ", "some_address2@mail.ru")]
    public void ToEmailAddressEmptyNameMeansNull(string name, string address)
    {
        var input = new MailboxAddress(name, address);

        var result = ImapMapper.ToEmailAddress(input);

        Assert.Equal(address, result.Address);
        Assert.Null(result.Name);
    }

    [Fact]
    public void ToEmailAddressSomeNameMeansOk()
    {
        var input = new MailboxAddress("SomeName", "some_address@mail.ru");

        var result = ImapMapper.ToEmailAddress(input);

        Assert.Equal("some_address@mail.ru", result.Address);
        Assert.Equal("SomeName", result.Name);
    }
}