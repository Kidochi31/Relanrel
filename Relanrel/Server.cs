using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Relanrel;

public class Server
{
    public const int MaxInfoLength = 1000;
    private readonly Socket Socket;
    public readonly ushort ListenPort;
    public readonly ushort ServerPort;
    public readonly uint Magic;
    public readonly string Info;

    public static Server? CreateServer(ushort listenPort, ushort serverPort, uint magic, string info)
    {
        if(Encoding.UTF8.GetByteCount(info) > MaxInfoLength)
        {
            return null;
        }
        try{
            Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket.EnableBroadcast = true;
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Socket.Bind(new IPEndPoint(IPAddress.Any, listenPort));

            return new Server(Socket, listenPort, serverPort, magic, info);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Tick()
    {
        try{
        while (Socket.Available > 0)
        {
            try{
            byte[] buffer = new byte[2048];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            int length = Socket.ReceiveFrom(buffer, ref remote);

            Span<byte> data = buffer.AsSpan(0..length);
            IPEndPoint ipRemote = (IPEndPoint)remote;

            Packet? packet = Packet.InterpretPacket(data);
            if(packet is null || packet.Magic != Magic || packet.PacketType != Packet.RequestType)
            {
                continue;
            }
            uint id = packet.RequestId;
            Packet response = new ResponsePacket(Magic, id, ServerPort, Info);
            byte[] bytes = response.CreatePacket();
            Socket.SendTo(bytes, ipRemote);
            } catch (Exception){}
        }
        } catch (Exception){}
    }

    private Server(Socket socket, ushort listenPort, ushort serverPort, uint magic, string info)
    {
        Socket = socket;
        ListenPort = listenPort;
        ServerPort = serverPort;
        Magic = magic;
        Info = info;
    }
}