using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatService
{
    private Socket clientSocket;
    private string username;

    private List<PeerDetail> peers = new List<PeerDetail>();

    public ChatService(Socket clientSocket, string user)
    {
        this.clientSocket = clientSocket;
        this.username = user;
    }

    public void StartBroadcastingPeer()
    {
        if (this.clientSocket.ProtocolType != ProtocolType.Udp)
            throw new Exception("The protocol should be UDP to be able to broadcast");
        
        var ips = IPHelper.GetInterfaceIPAddress();

        foreach (IPAddressDetail ip in ips)
        {
            var targetIP = ip.broadcast;
            EndPoint target = new IPEndPoint(IPAddress.Parse(targetIP), 11111) as EndPoint;
            // Send to anyone, hope someone pickup and reply
            clientSocket.SendTo(MsgToByte("HELLO PEER|" + targetIP + "|"+username), target);
        }
    }

    public byte[] MsgToByte(string msg)
    {
        return Encoding.ASCII.GetBytes(msg);
    }

    public void StartListening()
    {
        while (true) {
            Console.WriteLine("Waiting connection ... ");
            
            // Create simple receiver
            byte[] data =  new byte[1024];
            
            // Prepare the sender variable
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            // Directly Receive as UDP doesn't do SEND ACK 
            int lengthBytes = clientSocket.ReceiveFrom(data, ref sender);
            string hasil = Encoding.UTF8.GetString(data,0, lengthBytes);
            Console.WriteLine(hasil);

            Console.WriteLine(((IPEndPoint) sender).Address);
            string sourceIP = ((IPEndPoint)sender).Address.ToString();
            IPEndPoint responseTarget = ((IPEndPoint)sender);
            responseTarget.Port = 11111;

            // Send using alternative port, the 11111, for broadcast
            clientSocket.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), responseTarget);
            // If it's directly to us, the ncat can print teh response
            clientSocket.SendTo(Encoding.ASCII.GetBytes("\nHello "+sourceIP+"\nPong : " + hasil + "\n"), sender);
        }
    }

    public void HandleListenAction(string listenedChat)
    {
        string[] data = listenedChat.Split("|");
        switch (data[0])
        {
            case "HELLO PEER":
                this.peers.Add(new PeerDetail(ip: data[1], username: data[2]));
                break;
            case "BYE PEER":
                this.peers = this.peers.Where(d => d.ipAddress != data[1]).ToList();
                break;
            case "CHAT PEER":
                // Not Implemented
            default:
                // not implemented
                break;
        }
    }
}

public struct PeerDetail
{
    public PeerDetail(string ip, string username)
    {
        this.ipAddress = ip;
        this.username = username;
    }

    public readonly string ipAddress;
    public readonly string username;

    public IPAddress GetIPAddress()
    {
        return IPAddress.Parse(this.ipAddress);
    }
}