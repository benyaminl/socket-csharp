using System.Net;
using System.Text;
using System.Net.Sockets;

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
            StartServerTCP();
            break;
        default:
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
        // Address
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

            listener.SendTo(Encoding.ASCII.GetBytes("\nPong : " + hasil + "\n"), sender);
        }
    }
    catch (Exception e) {
        Console.WriteLine(e.ToString());
    }

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

            int numByte = clientSocket.Receive(bytes);

            data += Encoding.ASCII.GetString(bytes,
                                        0, numByte);
            Console.WriteLine(data);

            if (data.IndexOf("\n") > -1)
            {
                clientSocket.Send(Encoding.ASCII.GetBytes("\nPong : " + data + "\n"));
                data = "";
            }

            if (data.IndexOf("<EOF>") > -1)
            {
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