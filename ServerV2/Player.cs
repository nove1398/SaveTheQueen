using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

//Holder for client connections. Stateful representation
public class Player
{
    private const int DataBufferSize = 4096;
    private bool IsAuthorized = false;
    public TcpClient? Socket;
    private int ConnectionId;
    private NetworkStream? Stream;
    private string Name = string.Empty;
    private bool IsMyTurn = false;
    private byte[] ReceiveBuffer = new byte[DataBufferSize];

    public Player(int id)
    {
        ConnectionId = id;

    }

    public void SetAuthorized(bool authorized)
    {
        IsAuthorized = authorized;
    }

    public void Connect(TcpClient socket)
    {
        Socket = socket;
        Socket.SendBufferSize = DataBufferSize;
        Socket.ReceiveBufferSize = DataBufferSize;

        Stream = socket.GetStream();
        Stream.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

        Console.WriteLine("Connected");
        SendData("Connected");
    }

    private void SendData(string message)
    {
        try
        {

            //Processing
            var stream = Socket.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Possibly a dead socket, fix it", ex.Message);
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
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
            var msg = Encoding.UTF8.GetString(data).Split(":");
            var cmd = msg[0];
            var payload = msg[1];
            Command(cmd, payload);
            //End Processing
            Stream.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket receive error {ex.Message}");
            var exitingSock = Server.ClientStore[ConnectionId];
            if (exitingSock != null)
            {
                Name = string.Empty;
                IsAuthorized = false;
                IsMyTurn = false;
                exitingSock.SetAuthorized(false);
                exitingSock?.Socket?.Close();
                exitingSock?.Socket?.Dispose();
                //Clean up match data
                var playerMath = FindMatch();
                if (playerMath != null)
                {
                    CleanupMatches();
                    playerMath.Players.ForEach(p => { if (p.Socket != null) p.SendData("error:Match closed"); });
                    playerMath.Players.Clear();
                    playerMath.Round = 0;
                }
            }

        }
    }

    private void Command(string cmd, string input)
    {
        switch (cmd)
        {
            case "log":
            string user = input.Split("|")[0];
            string pass = input.Split("|")[1];
            if (GameDB.USER_LIST.Any(x => x.Username == user) &&
                GameDB.USER_LIST.Any(x => x.Password == pass))
            {
                SendData("log:true");
                Name = user;
                JoinMatch();
            }
            else
            {
                SendData("Failed to login");
            }
            break;
            case "move":
            var match = FindMatch();
            if (match == null)
            {
                SendData("error:Match not found");
                return;
            }

            //Handle if round 5
            if (match.Round >= 6)
            {
                Console.WriteLine(match.Round);
                MatchAnnouncement(match, $"end_game:Thanks for playing!!");
                var players = FindMatchPlayers(match);
                var score1 = match.Score[players[0]];
                var score2 = match.Score[players[1]];
                if (score1 > score2)
                {
                    players[0].SendData("victory");
                    players[1].SendData("defeat");
                }
                else
                {
                    players[1].SendData("victory");
                    players[0].SendData("defeat");
                }
                return;
            }

            if (!IsMyTurn)
            {
                SendData("error: Not your turn!");
                return;
            }
            var playerGuess = Int32.Parse(input);
            match.Moves.Add(new Move { Guess = playerGuess, PlayerId = ConnectionId });
            match.Round += 1;

            UpdateScore(match);
            ToggleTurn(match);
            MatchAnnouncement(match, $"player_move:{Name} selected {playerGuess}");
            break;
            default:
            break;
        }
    }

    private void ToggleTurn(Match currentMatch)
    {
        currentMatch.Players[0].IsMyTurn = !currentMatch.Players[0].IsMyTurn;
        currentMatch.Players[1].IsMyTurn = !currentMatch.Players[1].IsMyTurn;
    }

    #region Matches
    private void UpdateScore(Match currentMatch)
    {
        if (currentMatch.Moves.Count % 2 == 0)
        {
            if (currentMatch.Score.ContainsKey(this))
            {
                currentMatch.Score[this] = currentMatch.Score[this] += 1;
            }
            else
            {
                currentMatch.Score.Add(this, 1);

            }
        }
    }

    private void MatchAnnouncement(Match currentMatch, string msg)
    {
        currentMatch.Players.ForEach(p => p.SendData(msg));
    }

    private void CleanupMatches()
    {
        Server.GameStore.ForEach((m) =>
        {
            if (m.Round > 0 && m.Players.Any())
            {
                SendData("error: Opponent left unexpectedly");
                m.Players.Clear();
            }
        });
    }

    private List<Player> FindMatchPlayers(Match currentMatch)
    {
        return currentMatch.Players;
    }

    private Match? FindMatch()
    {
        var match = Server.GameStore
            .Where(match => match.Players.Any(x => x.ConnectionId == ConnectionId))
            .FirstOrDefault();

        return match;
    }

    private void JoinMatch()
    {
        if (!IsAuthorized)
        {
            SendData("error:Not authorized to join a match");
        }

        if (!Server.GameStore.Any(m => m.Players.Count < 2))
        {
            var match = new Match();
            match.Round = 0;
            match.Players.Add(this);
            Server.GameStore.Add(match);
        }
        else
        {
            var exisitngMatch = Server.GameStore.FirstOrDefault(x => x.Players.Count == 1);
            if (exisitngMatch != null)
            {
                exisitngMatch.Players.Add(this);
                exisitngMatch.StartedAt = DateTime.Now;
                exisitngMatch.Players.First().IsMyTurn = true;
                exisitngMatch.Players.ForEach(p => p.SendData("begin:Game has started!"));
            }
            else
            {
                SendData("error:all matches are full");
            }
        }
    }
    #endregion

}
