using System.Net;

namespace Relanrel;

internal static class ClientServerTest
{
    public static void Test()
    {
        Console.WriteLine("Welcome to the client server test");

        uint magic = 0x67676767;
        ushort listenPort = 6767;
        bool isServer = false;
        while (true)
        {
            Console.Write("Is this a server? [y/n]: ");
            string? input = Console.ReadLine();
            if(input is null)
            {
                continue;
            }
            input = input.ToLower();
            if(input == "n")
            {
                isServer = false;
                break;
            }
            if(input == "y")
            {
                isServer = true;
                break;
            }
        }

        if (isServer)
        {
            // Server
            ushort port = 0;
            while (true)
            {
                Console.Write("Enter in the target port: ");
                string? input = Console.ReadLine();
                if(input is null)
                {
                    continue;
                }
                if(ushort.TryParse(input, out port))
                {
                    break;
                }
            }
            string info = "";
            while (true)
            {
                Console.Write("Enter in the info: ");
                string? input = Console.ReadLine();
                if(input is null)
                {
                    continue;
                }
                info = input;
                break;
            }
            Server? server = Server.CreateServer(listenPort, port, magic, info);
            if(server is null)
            {
                Console.WriteLine("Could not create server.");
                return;
            }
            while (true)
            {
                server.Tick();
            }
        }
        else
        {
            // Client
            IPAddress? ip = Client.GetBroadcastAddress();
            if(ip is null)
            {
                Console.WriteLine("Cannot get broadcast address.");
                return;
            }
            Client? client = Client.CreateClient(listenPort, magic, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), ip);
            if(client is null)
            {
                Console.WriteLine("Could not create client.");
                return;
            }
            Dictionary<IPEndPoint, ServerInfo> Servers = new();
            while (true)
            {
                (var deadS, var newS) = client.Tick();
                if(deadS.Length > 0)
                {
                    foreach(IPEndPoint server in deadS)
                    {
                        ServerInfo info = Servers[server];
                        Console.WriteLine($"Timed out: {info.EndPoint} ({info.Info})");
                        Servers.Remove(server);
                    }
                    Console.WriteLine("The following servers still exist:");
                    if(Servers.Count == 0)
                    {
                        Console.WriteLine("[None]");
                    }
                    else
                    {
                        foreach(IPEndPoint server in Servers.Keys)
                        {
                            ServerInfo info = Servers[server];
                            Console.WriteLine($"{info.EndPoint} ({info.Info})");
                        }
                    }
                }
                if(newS.Length > 0)
                {
                    foreach(IPEndPoint server in newS)
                    {
                        ServerInfo info = client.Servers[server];
                        Servers[server] = info;
                        Console.WriteLine($"New server: {info.EndPoint} ({info.Info})");
                    }
                    Console.WriteLine("The following servers exist:");
                    if(Servers.Count == 0)
                    {
                        Console.WriteLine("[None]");
                    }
                    else
                    {
                        foreach(IPEndPoint server in Servers.Keys)
                        {
                            ServerInfo info = Servers[server];
                            Console.WriteLine($"{info.EndPoint} ({info.Info})");
                        }
                    }
                }
            }
        }
    }
}