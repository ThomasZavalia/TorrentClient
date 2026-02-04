using System.Text;
using Xunit;
using TorrentClient.Infrastructure.Adapters.Bencode;

namespace TorrentClient.Infrastructure.Tests.Adapters.Bencode;

public class BencodeDecoderTests
{
    [Fact]
    public void Decode_Integer_ShouldReturnCorrectValue()
    {
        var decoder = new BencodeDecoder();
        var encoded = Encoding.ASCII.GetBytes("i42e"); 

        var result = decoder.Decode(encoded);

        Assert.IsType<BencodeInteger>(result);
        Assert.Equal(42L, ((BencodeInteger)result).Value);
    }

    [Fact]
    public void Decode_String_ShouldReturnCorrectValue()
    {
        var decoder = new BencodeDecoder();
        var encoded = Encoding.ASCII.GetBytes("4:spam");

        var result = decoder.Decode(encoded);

        Assert.IsType<BencodeString>(result);
        Assert.Equal("spam", ((BencodeString)result).Value);
    }


}