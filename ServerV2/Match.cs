using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Describes what I think a match should look like
public class Match
{
   public List<Move> Moves = new();
   public List<Player> Players = new(2);
    public Dictionary<Player, int> Score = new Dictionary<Player, int>();   
   public int Round = 0;
    public DateTime StartedAt;
}

