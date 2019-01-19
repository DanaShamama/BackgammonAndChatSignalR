using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Move
    {
        public int From { get; set; }
        public int ChosenCube { get; set; }
        public BoardState CurrentBoardState { get; set; }
    }
}
