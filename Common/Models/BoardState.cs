using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class BoardState
    {
        public int[] BoardArray { get; set; }
        public string Message { get; set; }
        public bool RedTurn { get; set; }
        public Cubes Cubes { get; set; }
        public int RedMoves { get; set; }
        public int BlackMoves { get; set; }
        public bool IsRedWin { get; set; }
        public int RedEatten { get; set; }
        public int BlackEatten { get; set; }
        public int RedDiscsOnBoard { get; set; }
        public int BlackDiscsOnBoard { get; set; }
        public bool RedAllInHome { get; set; }
        public bool BlackAllInHome { get; set; }
        public int OutRed { get; set; }
        public int OutBlack { get; set; }
        public bool CurrentUserCanMove { get; set; }

        public BoardState()
        {
            BoardArray = new[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 };
            RedTurn = true;
            Cubes = new Cubes();
            RedEatten = 0;
            BlackEatten = 0;
            RedMoves = Cubes.Moves;
            BlackMoves = 0;
            Message = "";
            OutBlack = 0;
            OutRed = 0;
        }   
    }
}
