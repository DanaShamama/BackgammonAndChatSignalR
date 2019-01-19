using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Cubes
    {
        public int[] CubesNumbers { get; set; }
        public int Moves { get; set; }
        public bool IsDouble { get; set; }

        public Cubes()
        {
            CubesNumbers = new int[] {1, 1, 0, 0 };
            Moves = 2;
            IsDouble = false;
        }
    }
}
