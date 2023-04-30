using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.NetworkInformation;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
Console.WriteLine("Botak");
Console.WriteLine("What Server do you want to start (1=TCP,2=UDP, 3=EXIT - not protocol): ");
string input;

do
{
    input = Console.ReadLine() ?? "";
    switch (input)
    {
        case "1":
            Console.WriteLine("Start TCP Server");
            StartServerTCP();
            break;
        default:
            Console.WriteLine("Start UDP Server");
            StartServerUDP();
            break;
    }
} while (input != "3");


/// 
/// @see https://www.geeksforgeeks.org/socket-programming-in-c-sharp/
/// On Windows/Nix we can test using telnet
///
static void StartServerTCP() 
{
    // Establish the local endpoint
    // for the socket. Dns.GetHostName
    // returns the name of the host
    // running the application.
    IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
    IPAddress ipAddr = IPAddress.Any;
    IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);
    var ips = GetIPAddress();
    Console.WriteLine(ipHost.HostName + " " + ipAddr.ToString());
    // Creation TCP/IP Socket using
    // Socket Class Constructor
    Socket listener = new Socket(ipAddr.AddressFamily,
                 SocketType.Stream, ProtocolType.Tcp);
 
    try {
         
        // Using Bind() method we associate a
        // network address to the Server Socket
        // All client that will connect to this
        // Server Socket must know this network
        // Address....
        listener.Bind(localEndPoint);
 
        // Using Listen() method we create
        // the Client list that will want
        // to connect to Server
        listener.Listen(10);
 
        while (true) {
            Console.WriteLine("Waiting connection ... ");
            // Suspend while waiting for
            // incoming connection Using
            // Accept() method the server
            // will accept connection of client
            Socket clientSocket = listener.Accept();

            SocketListener obj = new SocketListener(clientSocket);
            Thread tr = new Thread(new ThreadStart(obj.newClient));
            tr.Start();
        }
    }
    catch (Exception e) {
        Console.WriteLine(e.ToString());
    }
}

/// Some inspiration comes from this, but.. this still not threaded..
/// @see http://www.java2s.com/Code/CSharp/Network/UdpServerSample.htm
/// 
static void StartServerUDP()
{
     // Establish the local endpoint
    // for the socket. Dns.GetHostName
    // returns the name of the host
    // running the application.
    IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
    IPAddress ipAddr = IPAddress.Any;
    IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);
    var ips = GetIPAddress();
    
    Console.WriteLine(ipHost.HostName + " " + ipAddr.ToString());
    // Creation TCP/IP Socket using
    // Socket Class Constructor
    Socket listener = new Socket(ipAddr.AddressFamily,
                 SocketType.Dgram, ProtocolType.Udp);
 
    try {
         
        // Using Bind() method we associate a
        // network address to the Server Socket
        // All client that will connect to this
        // Server Socket must know this network
        // Address
        listener.Bind(localEndPoint);

        while (true) {
            Console.WriteLine("Waiting connection ... ");
            
            // Create simple receiver
            byte[] data =  new byte[1024];
            
            // Prepare the sender variable
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            // Directly Receive as UDP doesn't do SEND ACK 
            int lengthBytes = listener.ReceiveFrom(data, ref sender);
            string hasil = Encoding.UTF8.GetString(data,0, lengthBytes);
            Console.WriteLine(hasil);

            Console.WriteLine(((IPEndPoint) sender).Address);
            string sourceIP = ((IPEndPoint)sender).Address.ToString();
            IPEndPoint responseTarget = ((IPEndPoint)sender);
            responseTarget.Port = 11111;

            // Send using alternative port, the 11111, for broadcast
            listener.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), responseTarget);
            // If it's directly to us, the ncat can print teh response
            listener.SendTo(Encoding.ASCII.GetBytes("\nHello "+sourceIP+"\nPong : " + hasil + "\n"), sender);
        }
    }
    catch (Exception e) {
        Console.WriteLine(e.ToString());
    }

}

/// This code derived from https://stackoverflow.com/a/4553625/4906348 
/// and https://stackoverflow.com/a/39338188/4906348 
/// You this need cleanup TODO: Change to class
static List<IPAddressDetail> GetIPAddress()
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
                IPAddresses.Add(new IPAddressDetail(addr.Address.ToString(), addr.IPv4Mask.ToString(), broadcast.ToString()));
            }
        }
    }

    return IPAddresses;
}

static IPAddress? CalculateNetwork(UnicastIPAddressInformation addr)
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
static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
{
    var address = unicastAddress.Address;
    var mask = unicastAddress.IPv4Mask;

    uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
    uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
    uint broadCastIpAddress = ipAddress | ~ipMaskV4;

    return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
}

struct IPAddressDetail
{
    public IPAddressDetail(string addr, string broadcast, string mask)
    {
        address = addr;
        this.broadcast = broadcast;
        this.mask = mask;
    }

    string address;
    string broadcast;
    string mask;
}

public class SocketListener
{
    private Socket clientSocket;

    public SocketListener(Socket clientSocket)
    {
        this.clientSocket = clientSocket;
    }

    public void newClient()
    {

        // Data buffer
        byte[] bytes = new Byte[1024];
        string? data = null;

        while (true)
        {
            EndPoint? sender = clientSocket.RemoteEndPoint;

            int numByte = clientSocket.Receive(bytes);
            
            data += Encoding.ASCII.GetString(bytes, 0, numByte);

            if (data.IndexOf("\n") > -1)
            {
                Console.WriteLine("From " + (((IPEndPoint) sender).Address.ToString() ?? "-") + " : " + data);
                clientSocket.Send(Encoding.ASCII.GetBytes("\nPong : " + data + "\n"));
                data = "";
            }

            if (data.IndexOf("<EOF>") > -1)
            {
                Console.WriteLine("From " + (((IPEndPoint) sender).Address.ToString() ?? "-")
                    + " : " + data);
                clientSocket.Send(Encoding.ASCII.GetBytes("\nPong : " + data + "\n"));
                break;
            }
        }

        Console.WriteLine("Text received -> {0} ", data);
        byte[] message = Encoding.ASCII.GetBytes("\nTest Server");

        // Send a message to Client
        // using Send() method
        clientSocket.Send(message);

        // Close client Socket using the
        // Close() method. After closing,
        // we can use the closed Socket
        // for a new Client Connection
        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
    }
}