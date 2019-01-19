using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatAndGameProject.DAL
{
    public static class UsersRepository
    {
        public static void Add(User user)
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    context.Users.Add(user);
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed connect to data base" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
            }
        }

        public static IEnumerable<User> GetUsers()
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    return context.Users.ToList();
                }

            }
            catch (InvalidOperationException e)
            {
                Logger.LOG.WriteToLog("Failed Get users" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return null;
            }     
        }

        public static IEnumerable<string> GetUsersNames()
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    List<string> userNames = new List<string>();
                    foreach (User user in context.Users)
                    {
                        userNames.Add(user.UserName);
                    }
                    return userNames;
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed get users names" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return null;
            }
        }

        public static User GetUserById(int userId)
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    return context.Users.Find(userId);
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed get a user by Id" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return null;
            }
            
        }

        public static User GetUserByName(string userName)
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    return context.Users.Find(userName);
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed get a user by Name" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return null;
            }
           
        }

        /// <summary>
        /// Checks if the user is valid.
        /// </summary>
        /// <param name="userName">The unique user name.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>True if the user exist, false otherwise.</returns>
        public static bool IsValidUser(string userName, string password)
        {
            try
            {
                using (UsersContext context = new UsersContext())
                {
                    return context.Users.FirstOrDefault(u => u.UserName == userName && u.Password == password) != null;
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed check if the user is valid" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return false;
            }            
        }
    }
}