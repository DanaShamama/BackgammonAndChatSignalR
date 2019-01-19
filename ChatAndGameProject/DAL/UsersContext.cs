using Common;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ChatAndGameProject.DAL
{
    public class UsersContext: DbContext
    {
        public DbSet<User> Users { get; set; }
    }
}