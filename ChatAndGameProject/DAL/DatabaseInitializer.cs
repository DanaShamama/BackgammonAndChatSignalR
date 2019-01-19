using Common;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ChatAndGameProject.DAL
{
    public class DatabaseInitializer: DropCreateDatabaseAlways<UsersContext>
    {

        protected override void Seed(UsersContext context)
        {
            User user1 = new User
            {
                UserName = "Dana",
                Password = "1"
            };

            User user2 = new User
            {
                UserName = "Shay",
                Password = "1"
            };

            context.Users.Add(user1);
            context.Users.Add(user2);
            context.SaveChanges();
        }
    }
}