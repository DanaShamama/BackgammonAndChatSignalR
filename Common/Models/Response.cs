using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Response
    {
        public bool IsOk1 { get; set; }
        public bool IsOk2 { get; set; }
        public bool IsOk3 { get; set; }
        public bool IsWin { get; set; }
        public string Message1 { get; set; }
        public string Message2 { get; set; }
        public string Message3 { get; set; }
        public BoardState CurrentBoardState { get; set; }
        public object additionalValue { get; set; }

        public Response()
        {
            IsOk1 = false;
            IsOk2 = false;
            IsOk3 = false;
            IsWin = false;
            Message1 = "";
            Message2 = "";
            Message3 = "";
            CurrentBoardState = new BoardState();
        }
    }
}
