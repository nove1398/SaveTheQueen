// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;



//Rename window
Console.Title = "Server";

//KICK OFF!
Server.Start();
Console.ReadKey();





//Mastermind to control game logic
public class Server
{
    private static TcpListener? listener;
    private const int Port = 7621;
    public const int MaxConnections = 4;
    public static Dictionary<int, Player> ClientStore = new();
    public static List<Match> GameStore = new();
    


    public static void Start()
    {

        InitializeServerData();
        listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Start();
        listener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectionCallback), null);

        Console.WriteLine("Server listening...");

    }

    private static void TcpConnectionCallback(IAsyncResult ar)
    {
        TcpClient newClient = listener.EndAcceptTcpClient(ar);
        listener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectionCallback), null);
        Console.WriteLine($"Incoming connection from {newClient.Client.RemoteEndPoint}...");

        for (int i = 1; i <= MaxConnections; i++)
        {
            if (ClientStore[i].Socket == null)
            {
                var newSocket = ClientStore[i];
                newSocket.Connect(newClient);
                newSocket.SetAuthorized(true);
                
                return;
            }
        }
        var stream = newClient.GetStream();
        byte[] bytes = Encoding.UTF8.GetBytes("error:Server full");
        stream.Write(bytes, 0, bytes.Length);
        Console.WriteLine($"{newClient.Client.RemoteEndPoint} cannot connect Server full!");
    }

    private static void InitializeServerData()
    {
        for (int i = 1; i <= MaxConnections; i++)
        {
            ClientStore.Add(i, new Player(i));
        }
        Console.WriteLine("Initialized player slots");
    }
}


//Super great Database
public static class GameDB
{
    public record User(string Username, string Password);
    public static List<User> USER_LIST = new List<User>()
    {
        new User("dannyboi","dre@margh_shelled"),
        new User("matty7","win&win99"),
        new User("a","b"),
        new User("c","d"),
    };
}


