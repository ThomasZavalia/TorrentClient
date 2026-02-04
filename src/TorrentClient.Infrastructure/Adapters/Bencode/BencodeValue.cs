
namespace TorrentClient.Infrastructure.Adapters.Bencode;

public abstract class BencodeValue { }

public class BencodeInteger : BencodeValue
{
    public long Value { get; }
    public BencodeInteger(long value) => Value = value;
    public override string ToString() => Value.ToString();
}

public class BencodeString : BencodeValue
{
    public string Value { get; } 
    public byte[] RawBytes { get; } 

    public BencodeString(string value, byte[] rawBytes)
    {
        Value = value;
        RawBytes = rawBytes;
    }
    public override string ToString() => Value;
}

public class BencodeList : BencodeValue
{
    public List<BencodeValue> Values { get; }
    public BencodeList(List<BencodeValue> values) => Values = values;
    public override string ToString() => $"List[{Values.Count}]";
}

public class BencodeDictionary : BencodeValue
{
    public Dictionary<string, BencodeValue> Values { get; }
    public BencodeDictionary(Dictionary<string, BencodeValue> values) => Values = values;
    public override string ToString() => $"Dict[{Values.Count}]";
}
