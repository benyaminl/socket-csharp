using System.Net;
using System.Text;
using System.Net.Sockets;

internal class Program
{
    static Socket? unixListener;
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("Botak");
        Console.CancelKeyPress += HandleCancel;
        AppDomain.CurrentDomain.ProcessExit += HandleCancel;
        
        string input; ChatService? svc = null;

        do
        {
            Console.WriteLine("What Server do you want to start (-2=History,-1=,0=Chat,1=TCP,2=UDP,3=SocketFile 4=EXIT - not protocol): ");
            input = Console.ReadLine() ?? "";
            switch (input)
            {
                case "1":
                    Console.WriteLine("Start TCP Server");
                    StartServerTCP();
                    break;
                case "2":
                    Console.WriteLine("Start UDP Server");
                    if (svc == null)
                        StartServerUDPChat(ref svc);
                    // StartServerUDP();
                    break;
                case "3":
                    Console.WriteLine("Start Unix Socket File Server");
                    StartUnixSocketFile(ref unixListener);
                    break;
                case "0":
                    Console.WriteLine("List Of Peers");
                    svc?.GetListOfPeers().ForEach(d =>
                    {
                        Console.WriteLine(d.ipAddress + " - " + d.username);
                    });

                    Console.WriteLine("Who : ");
                    string who = Console.ReadLine() ?? "";
                    Console.WriteLine("Msg : ");
                    string msg = Console.ReadLine() ?? "";
                    svc?.SendChat(who, msg);
                    break;
                case "-1":
                    Console.WriteLine("List Interfaces : ");
                    IPHelper.GetInterfaceIPAddress(debug: true);
                    break;
                case "-2":
                    Console.WriteLine("List Chats : ");
                    var history = svc?.GetChatHistory();

                    foreach (var h in history)
                    {
                        Console.WriteLine("Chat : " + h.Key);
                        h.Value.ForEach(d =>
                        {
                            Console.WriteLine(d.from.ipAddress + "-" + d.to.ipAddress + ": " + d.message);
                        });
                    }

                    break;
            }
        } while (input != "4");


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
            var ips = IPHelper.GetInterfaceIPAddress();
            Console.WriteLine(ipHost.HostName + " " + ipAddr.ToString());
            // Creation TCP/IP Socket using
            // Socket Class Constructor
            Socket listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);

            try
            {

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

                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");
                    // Suspend while waiting for
                    // incoming connection Using
                    // Accept() method the server
                    // will accept connection of client
                    Socket clientSocket = listener.Accept();

                    SocketIPListener obj = new SocketIPListener(clientSocket);
                    Thread tr = new Thread(new ThreadStart(obj.newClient));
                    tr.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void StartServerUDPChat(ref ChatService? service)
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

            Console.WriteLine(ipHost.HostName + " " + ipAddr.ToString());
            // Creation TCP/IP Socket using
            // Socket Class Constructor
            Socket listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Dgram, ProtocolType.Udp);

            listener.Bind(localEndPoint);
            Console.Write("Type your username : ");
            string username = Console.ReadLine() ?? "undefined";
            service = new ChatService(listener, username);
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
            var ips = IPHelper.GetInterfaceIPAddress();

            Console.WriteLine(ipHost.HostName + " " + ipAddr.ToString());
            // Creation TCP/IP Socket using
            // Socket Class Constructor
            Socket listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Dgram, ProtocolType.Udp);

            try
            {

                // Using Bind() method we associate a
                // network address to the Server Socket
                // All client that will connect to this
                // Server Socket must know this network
                // Address
                listener.Bind(localEndPoint);

                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");

                    // Create simple receiver
                    byte[] data = new byte[1024];

                    // Prepare the sender variable
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                    // Directly Receive as UDP doesn't do SEND ACK 
                    int lengthBytes = listener.ReceiveFrom(data, ref sender);
                    string hasil = Encoding.UTF8.GetString(data, 0, lengthBytes);
                    Console.WriteLine(hasil);

                    Console.WriteLine(((IPEndPoint)sender).Address);
                    string sourceIP = ((IPEndPoint)sender).Address.ToString();
                    IPEndPoint responseTarget = (IPEndPoint)sender;
                    responseTarget.Port = 11111;

                    // Send using alternative port, the 11111, for broadcast
                    listener.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), responseTarget);
                    // If it's directly to us, the ncat can print teh response
                    listener.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), sender);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        /// This is my Own (ben) implementation from StackOverflow
        /// @see https://stackoverflow.com/questions/40195290/how-to-connect-to-a-unix-domain-socket-in-net-core-in-c-sharp
        /// @see https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.unixdomainsocketendpoint?view=netcore-2.1#applies-to
        static void StartUnixSocketFile(ref Socket? unixListener)
        {
            Console.Write("Fill the location (default /tmp/unix-dotnet.sock): ");
            string input = Console.ReadLine() ?? "";
            string location = input.Length <= 0 ? "/tmp/unix-dotnet.sock" : input;
            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            EndPoint fileEndPoint = new UnixDomainSocketEndPoint(location);
            unixListener = socket;
            try
            {
                socket.Bind(fileEndPoint);

                // Using Listen() method we create
                // the Client list that will want
                // to connect to Server
                socket.Listen(20);

                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");
                    // Suspend while waiting for
                    // incoming connection Using
                    // Accept() method the server
                    // will accept connection of client
                    Socket clientSocket = socket.Accept();

                    SocketUnixListener obj = new SocketUnixListener(clientSocket);
                    Thread tr = new Thread(new ThreadStart(obj.newClient));
                    tr.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// Cancel handle on 
        /// @see https://learn.microsoft.com/en-us/dotnet/api/system.console.cancelkeypress?redirectedfrom=MSDN&view=net-7.0#examples
        ///
        static void HandleCancel(object sender, EventArgs args)
        {
            Console.WriteLine("Shutting Down...");

            // Specific to Exit? Probably 
            // @see https://stackoverflow.com/a/1119869/4906348
            if (args.GetType() == typeof(ConsoleCancelEventArgs))
            {
                ConsoleCancelEventArgs con = (ConsoleCancelEventArgs)args;
                con.Cancel = false;
            }
            
            // Disconnect doesn't remove the file socket sadly
            unixListener?.Disconnect(false);
            // We need to delete manually, get the path
            string? path = unixListener?.LocalEndPoint?.ToString();

            if (path != null)
                System.IO.File.Delete(path); // proceed to delete the socket file
        }
    }
}

public class SocketIPListener
{
    protected Socket clientSocket;

    public SocketIPListener(Socket clientSocket)
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