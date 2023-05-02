using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Net.NetworkInformation;

public class ChatService
{
    private Socket clientSocket;
    private string username;

    private List<PeerDetail> peers = new List<PeerDetail>();

    private Dictionary<string, List<ChatRow>> chatData = new Dictionary<string, List<ChatRow>>();

    private Dictionary<string, string> chatPairKey = new Dictionary<string, string>();

    public ChatService(Socket clientSocket, string user)
    {
        this.clientSocket = clientSocket;
        this.username = user;

        // Create a timer with a two second interval.
        var broadcastTimer = new System.Timers.Timer(2000);
        // Hook up the Elapsed event for the timer. 
        broadcastTimer.Elapsed += this.ForkStartBroadcastingPeer;
        broadcastTimer.AutoReset = true;
        broadcastTimer.Enabled = true;
        
        this.ForkStartListening();
    }

    public void StartBroadcastingPeer()
    {
        if (this.clientSocket.ProtocolType != ProtocolType.Udp)
            throw new Exception("The protocol should be UDP to be able to broadcast");
        
        var ips = IPHelper.GetInterfaceIPAddress();

        foreach (IPAddressDetail ip in ips)
        {
            var targetIP = ip.broadcast;
            IPEndPoint target = new IPEndPoint(IPAddress.Parse(targetIP), 11111);
            // Console.WriteLine(target.Address.ToString() + ":"+target.Port.ToString());

            // Send to anyone, hope someone pickup and reply
            clientSocket.EnableBroadcast = true; // If not, will throw error permission denied to broadcast
            clientSocket.SendTo(MsgToByte("HELLO PEER|" + ip.address + "|"+username), target);
        }
    }

    public void ForkStartBroadcastingPeer(Object? source = null, ElapsedEventArgs? e = null)
    {
        // Fork process once
        Thread tr = new Thread(new ThreadStart(this.StartBroadcastingPeer));
        tr.Start();
    }

    public void ForkStartListening()
    {
        // Fork process once
        Thread tr = new Thread(new ThreadStart(this.StartListening));
        tr.Start();
    }

    public byte[] MsgToByte(string msg)
    {
        return Encoding.ASCII.GetBytes(msg);
    }

    public void StartListening()
    {
        var localIPs = IPHelper.GetInterfaceIPAddress();
        // localIPs.ForEach(d => Console.WriteLine(d.address));
        while (true) {
            // Console.WriteLine("Waiting connection ... ");
            
            // Create simple receiver
            byte[] data =  new byte[1024];
            
            // Prepare the sender variable
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            // Directly Receive as UDP doesn't do SEND ACK 
            int lengthBytes = clientSocket.ReceiveFrom(data, ref sender);
            string hasil = Encoding.UTF8.GetString(data,0, lengthBytes);
            // Console.WriteLine(hasil);

            // Handle the protocol
            this.HandleListenAction(hasil);

            // Console.WriteLine(((IPEndPoint) sender).Address);
            // string sourceIP = ((IPEndPoint)sender).Address.ToString();
            // IPEndPoint responseTarget = ((IPEndPoint)sender);
            // responseTarget.Port = 11111;

            // Only responds if it'f from others, so there are no looping back
            // if (localIPs.Where(d => d.address != sourceIP).Count() <= 0)
            // {
            //     // Send using alternative port, the 11111, for broadcast
            //     clientSocket.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), responseTarget);
            //     // If it's directly to us, the ncat can print teh response
            //     clientSocket.SendTo(Encoding.ASCII.GetBytes("\nHello " + sourceIP + "\nPong : " + hasil + "\n"), sender);
            // }
        }
    }

    public void HandleListenAction(string listenedChat)
    {
        string[] data = listenedChat.Split("|");
        switch (data[0])
        {
            case "HELLO PEER":
                if (this.peers.Where(d => d.ipAddress == data[1]).Count() <= 0)
                {
                    // Eg. "HELLO PEER|192.168.1.1|ben"
                    this.peers.Add(new PeerDetail(ip: data[1], username: data[2]));
                }
                break;
            case "BYE PEER":
                // Eg. "BYE PEER|192.168.1.1|ben"
                this.peers = this.peers.Where(d => d.ipAddress != data[1]).ToList();
                break;
            case "CHAT PEER":
                //                   from        to
                // Eg. "CHAT PEER|192.168.1.1|192.168.1.2|Chat message is here"
                this.RecordChat(data[1], data[2], data[3].Replace("-%=-", "|"));
                break;
            default:
                // not implemented
                break;
        }
    }

    public List<PeerDetail> GetListOfPeers()
    {
        var peersCopy = new List<PeerDetail>();
        this.peers.ForEach(peersCopy.Add);

        return peersCopy;
    }

    public void RecordChat(string fromIP, string toIP, string msg)
    {
        PeerDetail from = this.peers.Where(d => d.ipAddress == fromIP).First();
        PeerDetail to = this.peers.Where(d => d.ipAddress == toIP).First();
        string key = "";

        if (this.chatPairKey.Where(d => d.Key == fromIP + "-" + toIP).Count() <= 0)
        {
            // Work around to have primary key, because from and to, to and from between same host
            // is same chat window
            string newKey = System.Guid.NewGuid().ToString();
            this.chatPairKey.Add(fromIP + "-" + toIP, newKey);
            this.chatPairKey.Add(toIP + "-" + fromIP, newKey);
            // init new chat rows
            this.chatData.Add(newKey, new List<ChatRow>());
        }

        key = this.chatPairKey.Where(d => d.Key == fromIP + "-" + toIP).First().Value;
        List<ChatRow> chatRows = this.chatData[key];

        chatRows.Add(new ChatRow()
        {
            from = from,
            to = to,
            message = msg
        });

        this.chatData[key] = chatRows; // replace the old chat
    }

    public void SendChat(string toIP, string msg)
    {
        string fromIP = "";
        
        var fromIPAdd = IPHelper.GetInterfaceIPAddress().Where(d => IPHelper.IsOneBroadcast(d.unicast, IPAddress.Parse(toIP))).First();
        fromIP = fromIPAdd.address;

        this.RecordChat(fromIP, toIP, msg);
        // Compose mesage
        msg.Replace("|", "-%=-");
        var msgByte = MsgToByte("CHAT PEER|"+fromIP+"|"+toIP+"|"+msg);

        // send
        this.clientSocket.SendTo(msgByte, (new IPEndPoint(IPAddress.Parse(toIP), 11111)) as EndPoint);
    }

    public Dictionary<string, List<ChatRow>> GetChatHistory()
    {
        var chatCopy = new Dictionary<string, List<ChatRow>>();
        
        foreach (var c in this.chatData)
        {
            chatCopy.Add(c.Key, c.Value);
        }

        return chatCopy;
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

public struct ChatRow 
{
    public PeerDetail from;
    public PeerDetail to;
    public string message;
}