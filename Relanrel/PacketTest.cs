using System.Text;

namespace Relanrel;

public static class PacketTest
{
    public static void TestPackets()
    {
        bool passed = true;
        for(int i = 0; i < 100000; i++)
        {
            if (!TestRequestPacket())
            {
                passed = false;
            }
        }
        if (!passed)
        {
            Console.WriteLine("did not pass request tests");
        }
        else
        {
            Console.WriteLine("passed request tests");
        }

        passed = true;
        for(int i = 0; i < 100000; i++)
        {
            if (!TestResponsePacket())
            {
                passed = false;
            }
        }
        if (!passed)
        {
            Console.WriteLine("did not pass response tests");
        }
        else
        {
            Console.WriteLine("passed response tests");
        }

        passed = true;
        for(int i = 0; i < 100000; i++)
        {
            if (!TestInvalidVersionPacket())
            {
                passed = false;
            }
        }
        if (!passed)
        {
            Console.WriteLine("did not pass version tests");
        }
        else
        {
            Console.WriteLine("passed version tests");
        }

        passed = true;
        for(int i = 0; i < 100000; i++)
        {
            if (!TestInvalidTypePacket())
            {
                passed = false;
            }
        }
        if (!passed)
        {
            Console.WriteLine("did not pass type tests");
        }
        else
        {
            Console.WriteLine("passed type tests");
        }

        passed = true;
        for(int i = 0; i < 100000; i++)
        {
            if (!TestInvalidPacket())
            {
                passed = false;
            }
        }
        if (!passed)
        {
            Console.WriteLine("did not pass invalid tests");
        }
        else
        {
            Console.WriteLine("passed invalid tests");
        }
    }

    public static bool TestRequestPacket()
    {
        uint Magic = (uint)((long)new Random().Next() + int.MaxValue);
        uint Id = (uint)((long)new Random().Next() + int.MaxValue);
        RequestPacket packet = new RequestPacket(Magic, Id);
        byte[] bytes = packet.CreatePacket();
        Packet? newPacket = Packet.InterpretPacket(bytes);
        if(newPacket is null)
        {
            Console.WriteLine("null packet");
            return false;
        }
        if(newPacket.ToString() != packet.ToString())
        {
            Console.WriteLine($"strings do not match: '{newPacket}' but expected '{packet}'");
            return false;
        }
        return true;
    }

    public static bool TestInvalidVersionPacket()
    {
        byte version = Packet.Version;
        while(version == Packet.Version)
        {
            version = (byte)new Random().Next();
        }
        int length = new Random().Next(0, 500);
        byte[] bytes = new byte[length];
        new Random().NextBytes(bytes);
        byte[] packet = [version, .. bytes];
        Packet? newPacket = Packet.InterpretPacket(packet);
        if(newPacket is not null)
        {
            Console.WriteLine($"packet version not checked: {newPacket}");
            return false;
        }
        return true;
    }

    public static bool TestInvalidTypePacket()
    {
        byte version = Packet.Version;
        byte type = (byte)new Random().Next(2, 256);
        int length = new Random().Next(0, 500);
        byte[] bytes = new byte[length];
        new Random().NextBytes(bytes);
        byte[] packet = [version, type, .. bytes];
        Packet? newPacket = Packet.InterpretPacket(packet);
        if(newPacket is not null)
        {
            Console.WriteLine($"packet version not checked: {newPacket}");
            return false;
        }
        return true;
    }

    public static bool TestInvalidPacket()
    {
        int length = new Random().Next(0, 100);
        byte[] bytes = new byte[length];
        new Random().NextBytes(bytes);
        byte[] packet = [..bytes];
        Packet? newPacket = Packet.InterpretPacket(packet);
        return true;
    }


    public static bool TestResponsePacket()
    {
        uint Magic = (uint)((long)new Random().Next() + int.MaxValue);
        uint Id = (uint)((long)new Random().Next() + int.MaxValue);
        ushort Port = (ushort)new Random().Next();
        int length = new Random().Next(0, 500);
        string Info = GenerateRandomUnicodeString(length);
        ResponsePacket packet = new ResponsePacket(Magic, Id, Port, Info);
        byte[] bytes = packet.CreatePacket();
        Packet? newPacket = Packet.InterpretPacket(bytes);
        if(newPacket is null)
        {
            Console.WriteLine("null packet");
            return false;
        }
        if(newPacket.ToString() != packet.ToString())
        {
            Console.WriteLine($"strings do not match: '{newPacket}' but expected '{packet}'");
            return false;
        }
        return true;
    }

    public static string GenerateRandomUnicodeString(int length)
    {
        StringBuilder builder = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            // U+0020 to U+D7FF avoids control characters and surrogate blocks
            int codePoint = new Random().Next(0x0020, 0xD800); 
            builder.Append((char)codePoint);
        }
        
        return builder.ToString();
    }
}