using System.Net;
using System.Text;
using System.Net.Sockets;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
Console.WriteLine("Botak");
StartServer();

/// 
/// @see https://www.geeksforgeeks.org/socket-programming-in-c-sharp/
/// On Windows/Nix we can test using telnet
///
static void StartServer() 
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
            if (data.IndexOf("<EOF>") > -1)
                break;
        }

        Console.WriteLine("Text received -> {0} ", data);
        byte[] message = Encoding.ASCII.GetBytes("Test Server");

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