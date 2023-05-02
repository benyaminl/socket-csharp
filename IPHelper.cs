using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public class IPHelper
{
    /// This code derived from https://stackoverflow.com/a/4553625/4906348 
    /// and https://stackoverflow.com/a/39338188/4906348 
    public static List<IPAddressDetail> GetInterfaceIPAddress()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces();
        // Using struct for making data 
        var IPAddresses = new List<IPAddressDetail>();

        foreach (var nic in nics) {
            var ipProps = nic.GetIPProperties();

            // We're only interested in IPv4 addresses for this example.
            var ipv4Addrs = ipProps.UnicastAddresses
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork);

            foreach (var addr in ipv4Addrs) {
                var network = CalculateNetwork(addr);
                var broadcast = GetBroadcastAddress(addr);

                if (network != null)
                {
                    Console.WriteLine("Addr: {0}   Mask: {1}  Network: {2} Broadcast : {3}", addr.Address, addr.IPv4Mask, network, broadcast);
                    IPAddresses.Add(new IPAddressDetail(addr.Address.ToString(), broadcast.ToString(), addr.IPv4Mask.ToString()));
                }
            }
        }

        return IPAddresses;
    }

    public static IPAddress? CalculateNetwork(UnicastIPAddressInformation addr)
    {
        // The mask will be null in some scenarios, like a dhcp address 169.254.x.x
        if (addr.IPv4Mask == null)
            return null;

        var ip = addr.Address.GetAddressBytes();
        var mask = addr.IPv4Mask.GetAddressBytes();
        var result = new Byte[4];
        for (int i = 0; i < 4; ++i) {
            result[i] = (Byte)(ip[i] & mask[i]);
        }

        return new IPAddress(result);
    }

    /// Only Unicast Address can have mask
    /// @see https://stackoverflow.com/a/39338188/4906348
    ///
    public static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
    {
        var address = unicastAddress.Address;
        var mask = unicastAddress.IPv4Mask;

        uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
    }

    public static bool IsOneBroadcast(UnicastIPAddressInformation ownIP, IPAddress target)
    {
        bool status = false;
        var ownBroadcast = GetBroadcastAddress(ownIP);
        
        var mask = ownIP.IPv4Mask;

        uint ipAddress = BitConverter.ToUInt32(target.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;
        var targetBroadcast = new IPAddress(BitConverter.GetBytes(broadCastIpAddress));

        if (targetBroadcast.Equals(ownBroadcast)) status = true;

        return status;
    }
}

public struct IPAddressDetail
{
    public IPAddressDetail(string addr, string broadcast, string mask)
    {
        address = addr;
        this.broadcast = broadcast;
        this.mask = mask;
    }

    public string address;
    public string broadcast;
    public string mask;
}