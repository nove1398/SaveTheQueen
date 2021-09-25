// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;


Console.Title = "Client";

const int DataBufferSize = 4096;
byte[] ReceiveBuffer = new byte[DataBufferSize];
TcpClient Socket = new TcpClient();
int ConnectionAttempts = 0;
string username = string.Empty;
string password = string.Empty;
bool IsLogged = false;
bool MatchStarted = false;
bool RoundOver = false;

NetworkStream Stream;

AttemptConnect();
//Init UI
DisplayMenuUI();


void DisplayMenuUI()
{
    while (Socket.Connected)
    {
        if (!MatchStarted)
        {

            Console.WriteLine("Select option: a -> Login | q -> Quit | ccc -> Clear screen");
            var choice = Console.ReadLine();
            switch (choice)
            {
                case "a":
                LoginUi();
                break;
                case "ccc":
                Console.Clear();
                break;
                case "q":
                Shutdown();
                break;
            }
        }
        else
        {
            while (true)
            {

            var guess = Console.ReadLine();
            Console.WriteLine("Where is the queen 1, 2 or 3?");
            SendData($"move:{guess}");
                if(guess == "q")
                {
                    break;
                }
            }
        }
    } 
}


void AttemptConnect()
{
    while (!Socket.Connected)
    {
        try
        {
            Socket.Connect(IPAddress.Loopback, 7621);
            Stream = Socket.GetStream();
            Stream.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReadCallback, null);
        }
        catch (Exception e)
        {
            Console.Clear();
            ConnectionAttempts++;
            Console.WriteLine($"Failed to connect, retry {ConnectionAttempts}");
        }
    }
}

void LoginUi()
{
    if (IsLogged)
    {
        Console.WriteLine("Already logged in");
        return;
    }

    Console.WriteLine("Enter username");
    username = Console.ReadLine() ?? "";
    Console.WriteLine("Enter password");
    password = Console.ReadLine() ?? "";

    SendData("log:" + username + "|" + password);
}

void SendData(string message)
{
    //Processing
    var stream = Socket.GetStream();
    byte[] bytes = Encoding.UTF8.GetBytes(message);
    stream.Write(bytes, 0, bytes.Length);
}


//Client action controller
void ReadCallback(IAsyncResult ar)
{
    try
    {
        var byteLength = Stream.EndRead(ar);
        if (byteLength <= 0)
        {
            return;
        }

        byte[] data = new byte[byteLength];
        Array.Copy(ReceiveBuffer, data, byteLength);

        //Processing
        var msg = Encoding.UTF8.GetString(data).Trim();
        if (msg.Contains(":"))
        {
            var cmd = msg.Split(":")[0];
            var payload = msg.Split(":")[1];
            ClientCommands(cmd, payload);
        }
        //End processing

        Stream.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReadCallback, null);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Socket receive error {ex.Message}");
    }

}

//Controller for commands, could be optimized some more into a delegate and more structured packets
void ClientCommands(string command, string payload)
{
    switch (command)
    {
        case "log":
        if (payload == "true")
        {
            IsLogged = true;
            Console.WriteLine("Successfully logged in");
        }
        break;
        case "begin":
        Console.Clear();
        Console.WriteLine("Find The Queen has started");
        MatchStarted = true;
        
        break;
        case "player_move":
        Console.WriteLine(payload);
        break;
        case "victory":
        ShowVictory();
        Shutdown();
        break;
        case "defeat":
        ShowDefeat();
        Shutdown();
        break;
        case "end_game":
        RoundOver = true;
        break;
        case "error":
        RoundOver = true;
        MatchStarted = false;
        Console.Clear();
        Console.WriteLine(payload);
        break;
        default:
        break;
    }
}

void Shutdown()
{
    //Socket.Close();
    //Socket.Dispose();
    Console.WriteLine("Bye");
    Environment.Exit(0);
}

void ShowVictory() { Console.Clear(); Console.WriteLine("Victory"); }

void ShowDefeat() { Console.Clear(); Console.WriteLine("Defeat"); }