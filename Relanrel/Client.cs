using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Relanrel;

public class Client
{
    private readonly Socket Socket;
    public readonly uint Magic;
    public readonly uint RequestId;
    public Dictionary<IPEndPoint, ServerInfo> Servers = new();
    Dictionary<IPEndPoint, DateTime> ServerTimeouts = new();
    List<IPEndPoint> DeadServers = new();
    List<IPEndPoint> NewServers = new();
    DateTime LastRequestTime;
    readonly IPEndPoint ListenEndPoint;
    readonly TimeSpan RequestPeriod;
    readonly TimeSpan ServerTimeout;

    public static Client? CreateClient(ushort listenPort, uint magic, TimeSpan requestPeriod, TimeSpan serverTimeout, IPAddress broadcastAddress)
    {
        try{
            Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket.EnableBroadcast = true;
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            return new Client(Socket, new IPEndPoint(broadcastAddress, listenPort), magic, requestPeriod, serverTimeout);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public (IPEndPoint[] deadServers, IPEndPoint[] newServers) Tick()
    {
        TickSend(DateTime.UtcNow);
        IPEndPoint[] newServers = TickReceive(DateTime.UtcNow);
        IPEndPoint[] deadServers = TickTimeouts(DateTime.UtcNow);
        return (deadServers, newServers);
    }

    private void TickSend(DateTime time)
    {
        if(LastRequestTime + RequestPeriod < time)
        {
            // need to send another request
            RequestPacket request = new RequestPacket(Magic, RequestId);
            try{
            Socket.SendTo(request.CreatePacket(), ListenEndPoint);
            } catch(Exception){}

            // reset last request time
            LastRequestTime = time;
        }
    }

    private static (IPAddress Address, IPAddress Mask)?  GetLocalIPv4()
    {
        foreach(NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if(nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach(UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses)
            {
                if(address.Address.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(address.Address))
                {
                    return (address.Address, address.IPv4Mask);
                }
            }
        }

        return null;
    }

    public static IPAddress? GetBroadcastAddress()
    {
        (IPAddress, IPAddress)? localIP = GetLocalIPv4();
        if(localIP is null)
        {
            return null;
        }
        (IPAddress address, IPAddress mask) = localIP.Value;
        return GetBroadcastAddress(address, mask);
    }

    private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
    {
        uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
    }

    private IPEndPoint[] TickReceive(DateTime time)
    {
        NewServers.Clear();
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
            if(packet is null || packet.Magic != Magic || packet.PacketType != Packet.ResponseType || packet.RequestId != RequestId)
            {
                continue;
            }
            // it must be a response
            ResponsePacket response = (ResponsePacket)packet;
            ushort port = response.Port;
            string info = response.Info;

            IPEndPoint serverEndPoint = new IPEndPoint(ipRemote.Address, port);
            if (!Servers.ContainsKey(serverEndPoint))
            {
                // new server
                NewServers.Add(serverEndPoint);
                ServerInfo serverInfo = new ServerInfo(serverEndPoint, info);
                Servers[serverEndPoint] = serverInfo;
            }
            ServerTimeouts[serverEndPoint] = time + ServerTimeout;
            } catch (Exception){}
        }
        } catch (Exception){}
        return [..NewServers];
    }

    private IPEndPoint[] TickTimeouts(DateTime time)
    {
        DeadServers.Clear();
        // remove all servers which have reached their timeout
        foreach((IPEndPoint server, DateTime timeout) in ServerTimeouts)
        {
            if(timeout < time)
            {
                // timeout
                DeadServers.Add(server);
            }
        }
        foreach(IPEndPoint server in DeadServers)
        {
            Servers.Remove(server);
            ServerTimeouts.Remove(server);
        }
        return [..DeadServers];
    }

    private Client(Socket socket, IPEndPoint listenEndPoint, uint magic, TimeSpan requestPeriod, TimeSpan serverTimeout)
    {
        Socket = socket;
        ListenEndPoint = listenEndPoint;
        Magic = magic;
        unchecked {RequestId = (uint)new Random().Next();}
        RequestPeriod = requestPeriod;
        ServerTimeout = serverTimeout;
        LastRequestTime = DateTime.UnixEpoch;
    }
}