using System.Net;

namespace Relanrel;

public class ServerInfo
{
    public readonly IPEndPoint EndPoint;
    public readonly string Info;

    internal ServerInfo(IPEndPoint endPoint, string info)
    {
        EndPoint = endPoint;
        Info = info;
    }
}