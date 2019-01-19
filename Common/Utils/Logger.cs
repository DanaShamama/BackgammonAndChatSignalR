using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Logger
    {
        private static object Key = new object();

        private static Logger _logerInstance { get; set; }
        public static Logger LOG
        {
            get
            {
                lock (Key)
                {
                    if (_logerInstance == null)
                        _logerInstance = new Logger();
                    return _logerInstance;
                }
            }
        }

        private Logger() { }

        public void WriteToLog(string msg)
        {
            File.AppendAllText("LOG.txt", msg + Environment.NewLine);
        }
    }
}
