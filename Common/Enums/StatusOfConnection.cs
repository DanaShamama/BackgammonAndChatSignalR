using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum StatusOfConnection
    {
        Offline = 0,
        Available,
        BusyOnChat,
        BusyOnGame,
        BusyOnChatAndGame
    }
}
