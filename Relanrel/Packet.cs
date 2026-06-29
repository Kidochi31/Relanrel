using System.Buffers.Binary;
using System.Text;

namespace Relanrel;


internal abstract class Packet
{
    public static byte Version => 0x00;
    public const byte RequestType = 0x00;
    public const byte ResponseType = 0x01;
    public abstract byte PacketType {get;}
    public uint Magic;
    public uint RequestId;
    public static int HeaderSize => 1 + 1 + 4 + 4;
    public abstract int PacketSize {get;}

    protected void CreateHeader(Span<byte> bytes)
    {
        bytes[0] = Version;
        bytes = bytes[1..];
        bytes[0] = PacketType;
        bytes = bytes[1..];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, Magic);
        bytes = bytes[4..];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, RequestId);
        bytes = bytes[4..];
        return;
    }

    public abstract byte[] CreatePacket();

    public static Packet? InterpretPacket(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length < HeaderSize)
        {
            return null;
        }
        byte version = bytes[0];
        bytes = bytes[1..];
        byte packetType = bytes[0];
        bytes = bytes[1..];
        uint magic = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        bytes = bytes[4..];
        uint requestId = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        bytes = bytes[4..];

        if(version != Version)
        {
            return null;
        }
        switch (packetType)
        {
            case RequestType:
                return RequestPacket.CreateRequestPacket(magic, requestId);
            case ResponseType:
                return ResponsePacket.CreateResponsePacket(magic, requestId, bytes);
            default:
                return null;
        }
    }
}

internal class RequestPacket : Packet
{
    public override byte PacketType => RequestType;
    public override int PacketSize => HeaderSize;
    public override byte[] CreatePacket()
    {
        byte[] packet = new byte[PacketSize];
        CreateHeader(packet);
        return packet;
    }

    public static RequestPacket CreateRequestPacket(uint magic, uint requestId)
    {
        RequestPacket packet = new(magic, requestId);
        return packet;
    }

    public RequestPacket(uint magic, uint requestId)
    {
        Magic = magic;
        RequestId = requestId;
    }

    public override string ToString()
    {
        return $"Request Packet (Type {PacketType:X}): Ver {Version} with magic {Magic:X} and id {RequestId:X}";
    }
}

internal class ResponsePacket : Packet
{
    public override byte PacketType => ResponseType;
    public ushort Port;
    public string Info;

    public override int PacketSize => HeaderSize + 2 + Encoding.UTF8.GetByteCount(Info);

    public override byte[] CreatePacket()
    {
        byte[] packet = new byte[PacketSize];
        CreateHeader(packet);
        Span<byte> bytes = packet.AsSpan(HeaderSize);
        BinaryPrimitives.WriteUInt16BigEndian(bytes, Port);
        bytes = bytes[2..];
        Encoding.UTF8.GetBytes(Info, bytes);
        return packet;
    }

    public static ResponsePacket? CreateResponsePacket(uint magic, uint requestId, ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length < 2)
        {
            return null;
        }
        ushort port = BinaryPrimitives.ReadUInt16BigEndian(bytes);
        bytes = bytes[2..];
        string info = Encoding.UTF8.GetString(bytes);

        ResponsePacket packet = new(magic, requestId, port, info);
        return packet;
    }

    public ResponsePacket(uint magic, uint requestId, ushort port, string info)
    {
        Magic = magic;
        RequestId = requestId;
        Port = port;
        Info = info;
    }

    public override string ToString()
    {
        return $"Response Packet (Type {PacketType:X}): Ver {Version} with magic {Magic:X} and id {RequestId:X}. Port={Port} Info={Info}";
    }
}