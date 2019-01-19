using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class UserWithStatus
    {
        public string UserName { get; set; }
        public StatusOfConnection Status { set; get; }

        public override string ToString()
        {
            return UserName + ": " + Status;
        }
    }
}
