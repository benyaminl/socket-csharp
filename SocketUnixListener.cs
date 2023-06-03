using System.Net;
using System.Net.Sockets;
using System.Text;

public class SocketUnixListener : SocketIPListener
{
    public SocketUnixListener(Socket clientSocket): base(clientSocket) {}

    public new void newClient()
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
                // Console.WriteLine("From " + (((IPEndPoint) sender).Address.ToString() ?? "-") + " : " + data);
                clientSocket.Send(Encoding.ASCII.GetBytes("\nPong : " + data + "\n"));
                data = "";
            }

            if (data.IndexOf("<EOF>") > -1)
            {
                // Console.WriteLine("From " + (((IPEndPoint) sender).Address.ToString() ?? "-")
                //     + " : " + data);
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