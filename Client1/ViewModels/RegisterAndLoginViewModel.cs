using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Client1.Commands;
using Client1.Views;
using Common;
using Microsoft.AspNet.SignalR.Client;
using System.Windows.Navigation;

namespace Client1.ViewModels
{
    class RegisterAndLoginViewModel : ViewModelBase
    {
        private ObservableCollection<UserWithStatus> _usersNamesWithStatus = new ObservableCollection<UserWithStatus>(); // UserWithStatus holds a string user name with an enum StatusOfConnection (offline, OnChat, OnGame...)
        public ObservableCollection<UserWithStatus> UsersNamesWithStatus
        {
            get
            {
                return _usersNamesWithStatus;
            }
            set
            {
                _usersNamesWithStatus = value;
                OnPropertyChanged("UsersNamesWithStatus");
            }
        }

        private StatusOfConnection _status; // an enum StatusOfConnection (offline, OnChat, OnGame...)
        public StatusOfConnection Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }


        private ObservableCollection<string> _userNames = new ObservableCollection<string>();
        public ObservableCollection<string> UserNames
        {
            get
            {
                return _userNames;
            }
            set
            {
                _userNames = value;
                OnPropertyChanged("UserNames");
            }
        }

        private ICommand _clickCommand;
        public ICommand ClickCommand
        {
            get
            {
                return _clickCommand ?? (_clickCommand = new CommandHandler(() => Register(), _canExecute));
            }
        }

        private bool _canExecute; // boolean for Icommand
        public bool CanExecute
        {
            get
            {
                return _canExecute;
            }
            set
            {
                _canExecute = value;
            }
        }

        private ICommand _clickCommandLogin; // ICommand of clicking the login button
        public ICommand ClickCommandLogin
        {
            get
            {
                return _clickCommandLogin ?? (_clickCommandLogin = new CommandHandler(() => Login(), _canExecute));
            }
        }

        private ICommand _clickCommandStartChat; // ICommand of clicking the chat button
        public ICommand ClickStartChat
        {
            get
            {
                return _clickCommandStartChat ?? (_clickCommandStartChat = new CommandHandler(() => ConnectToUserChat(), _canExecute));
            }
        }

        private ICommand _clickCommandStartGame; // ICommand of clicking the game button
        public ICommand ClickStartGame
        {
            get
            {
                return _clickCommandStartGame ?? (_clickCommandStartGame = new CommandHandler(() => ConnectToUserGame(), _canExecute));

            }
        }

        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        private string password;
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
                OnPropertyChanged("Password");
            }
        }

        private bool _enableButtonRegister; // a bolean to disable the register button after a registration has made 
        public bool EnableButtonRegister
        {
            get
            {
                return _enableButtonRegister;
            }
            set
            {
                _enableButtonRegister = value;
                OnPropertyChanged("EnableButtonRegister");
            }
        }

        private bool _enableButtonLogin; // a bolean to disable the login button after a login has made 
        public bool EnableButtonLogin
        {
            get
            {
                return _enableButtonLogin;
            }
            set
            {
                _enableButtonLogin = value;
                OnPropertyChanged("EnableButtonLogin");
            }
        }

        private static HubConnection _hubConnection = null;
        public static HubConnection HubConnection
        {
            get
            {
                if (_hubConnection == null)
                {
                    _hubConnection = new HubConnection("http://localhost:45946/");
                }
                return _hubConnection;
            }
            set
            {
                _hubConnection = value;
            }
        }

        private static IHubProxy _chatHubProxy = null;
        public static IHubProxy ChatHubProxy
        {
            get
            {
                if (_chatHubProxy == null)
                {
                    _chatHubProxy = HubConnection.CreateHubProxy("GameAndChatManager");
                }
                return _chatHubProxy;
            }
            set
            {
                _chatHubProxy = value;
            }
        }

        private UserWithStatus _selectedName; // selected name from the list of names in the view
        public UserWithStatus SelectedName
        {
            get
            {
                return _selectedName;
            }
            set
            {
                _selectedName = value;
            }
        }
        // ctor
        public RegisterAndLoginViewModel()
        {
            CanExecute = true;
            EnableButtonRegister = true;
            EnableButtonLogin = true;
            ConnectToServer();
        }

        /// <summary>
        ///     a new user is being registrated, if the user name is not already exist.
        ///     the user will not login automatically, only if he clicks the login button
        /// </summary>
        public void Register()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("You must choose a User Name and a Password, do not leave the fields empty");
                    return;
                }

                User newUser = new User { UserName = UserName, Password = Password };

                bool userAdded = ChatHubProxy.Invoke<bool>("GetNewUser", newUser).Result; // userAdded is true if the new user was registrated
                if (!userAdded)
                {
                    MessageBox.Show("User name already exist! please choose another user name");
                }
                else
                {
                    MessageBox.Show("You are now registered to the system, you can login now");
                    EnableButtonRegister = false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("an error occurred while trying to register. we're sorry for the inconvenient");
                Logger.LOG.WriteToLog("Failed register a new user" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
                return;
            }
        }

        /// <summary>
        /// when a client connect to the server, he is registering to events from the server by "ChatHubProxy.On..."
        /// </summary>
        public void ConnectToServer()
        {
            /// <summary>
            /// getting a request from other user to chat with him
            /// </summary>
            /// <param name="senderName"></param>
            ChatHubProxy.On("ChatRequest", (string senderName) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool approve = ApproveChatInvitation(senderName);
                    ChatHubProxy.Invoke("ApproveToChat", approve, UserName, senderName); // send a bolean to the server and than to the other user, if he wants to chat with him or not
                }));
            });

            /// <summary>
            /// getting a request from other user to play backgammon with him
            /// </summary>
            /// <param name="senderName"></param>
            ChatHubProxy.On("GameRequest", (string senderName) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool approve = ApproveGameInvitation(senderName);
                    ChatHubProxy.Invoke("ApproveToGame", approve, UserName, senderName); // // send a bolean to the server and than to the other user, if he wants to play bacgammon with him or not
                }));
            });

            /// <summary>
            /// get an approve or disapprove to his own chat request from the server (the server get it from the other user)
            /// </summary>
            /// <param name="approve">boolean, that approve the chat request from the other user</param>
            /// <param name="otherUser">string, the name of the other user on the chat</param>
            ChatHubProxy.On("responseOfChatRequestToSender", (bool approve, string otherUser) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (approve)
                    {
                        MessageBox.Show("Chat will start!");
                        ChatRoom chatRoom = new ChatRoom(UserName, otherUser, ChatHubProxy);
                        chatRoom.Show();
                    }
                    else
                    {
                        MessageBox.Show("Some other time...");
                    }
                }));
            });

            /// <summary>
            /// get an approve or disapprove to his own game request from the server (the server get it from the other user)
            /// </summary>
            /// <param name="approve">boolean, that approve the game request from the other user</param>
            /// <param name="otherUser">string, the name of the other user on the game</param>
            /// <param name="startGameUser">string, the user that sent the request, he will be the one that starts the game -> the "Red"</param>
            ChatHubProxy.On("responseOfGameRequestToSender", (bool approve, string otherUser, string startGameUser) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (approve)
                    {
                        string msg = "Game will start!\nPlease click the disc and then the cube in order to move a disc,\nTo get an eaten cube back to the board click only on the cube,\n";
                        if (UserName.Equals(startGameUser))
                        {
                            msg += "You are the Red,\nYou start!";
                        }
                        else if (otherUser.Equals(startGameUser))
                        {
                            msg += "You are the Black,\nThe Red starts";
                        }
                        MessageBox.Show(msg);
                        Application.Current.MainWindow.Content = new Game(UserName, otherUser, ChatHubProxy, startGameUser);
                    }
                    else
                    {
                        MessageBox.Show("Some other time...");
                    }
                }));
            });

            HubConnection.Start().Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Login()
        {
            /// <summary>
            /// when the user is logging-in he get all the names of the registreted users, and their status of connection (available, on-game, on-chat...). any update of the status of other user, or of new user that registreted will appear
            /// </summary>
            /// <param name="usersWithStatus"></param>
            ChatHubProxy.On("UpdateAllUsersNamesAndConnectionStatus", (List<UserWithStatus> usersWithStatus) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    usersWithStatus = usersWithStatus.Where(x => x.UserName != UserName).ToList();
                    UsersNamesWithStatus = ConvertListToObservableCollection(usersWithStatus);                    
                }));
            });
            try
            {
                if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("You must enter a User Name and a Password, do not leave the fields empty");
                    return;
                }

                User user = new User { UserName = UserName, Password = Password };

                Response response = ChatHubProxy.Invoke<Response>("ValidateLogin", user).Result;

                if (response.IsOk1)
                {
                    EnableButtonLogin = false; // after login, can't do another login
                    EnableButtonRegister = false; // after login, can't do registreting
                    MessageBox.Show(response.Message1);
                }
                else if (!response.IsOk1)
                {
                    MessageBox.Show(response.Message1);
                }
            }
            catch (Exception e)
            {
                Logger.LOG.WriteToLog("Failed login in ChatViewModel" + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + "Exception details: " + e.ToString() + "," + e.GetType().FullName + Environment.NewLine);
            }
        }

        /// <summary>
        /// display a message if can't connect to user
        /// </summary>
        public void ConnectToUserChat()
        {
            bool canConnectToUser = CanConnectToUserChat();

            if (!canConnectToUser)
            {
                MessageBox.Show("The user you are trying to connect to is not available");
            }
        }

        /// <summary>
        /// display a message if can't connect to user
        /// </summary>
        public void ConnectToUserGame()
        {
            bool canConnectToUser = CanConnectToUserGame();

            if (!canConnectToUser)
            {
                MessageBox.Show("The user you are trying to connect to is not available");
            }
        }

        /// <summary>
        /// check if the user can connect to the selected user in order to chat
        /// </summary>
        /// <returns>boolean, to tell if the user is available or not</returns>
        public bool CanConnectToUserChat()
        {
            if (SelectedName == null)
            {
                return false;
            }

            string selectedUserName = SelectedName.UserName;
            bool responseValidateAvailabilityOfOtherUser = ChatHubProxy.Invoke<bool>("ValidateAvailabilityOfOtherUser", selectedUserName, GameOrChatRequestEnum.Chat).Result;

            if (responseValidateAvailabilityOfOtherUser)
            {
                ChatHubProxy.Invoke("AskUserToStartChat", selectedUserName, UserName);
            }
            return responseValidateAvailabilityOfOtherUser;
        }

        /// <summary>
        /// check if the user can connect to the selected user in order to play a game
        /// </summary>
        /// <returns>boolean, to tell if the user is available or not</returns>
        public bool CanConnectToUserGame()
        {
            if (SelectedName == null)
            {
                return false;
            }

            string selectedUserName = SelectedName.UserName;

            bool responseValidateAvailabilityOfOtherUser = ChatHubProxy.Invoke<bool>("ValidateAvailabilityOfOtherUser", selectedUserName, GameOrChatRequestEnum.Game).Result;

            if (responseValidateAvailabilityOfOtherUser)
            {
                ChatHubProxy.Invoke("AskUserToStartGame", selectedUserName, UserName);
            }
            return responseValidateAvailabilityOfOtherUser;
        }

        /// <summary>
        /// converts a list of UserWithStatus to an ObservableCollection of UserWithStatus
        /// </summary>
        /// <param name="usersWithStatus"></param>
        /// <returns></returns>
        private ObservableCollection<UserWithStatus> ConvertListToObservableCollection(List<UserWithStatus> usersWithStatus)
        {
            ObservableCollection<UserWithStatus> newList = new ObservableCollection<UserWithStatus>();
            foreach (var user in usersWithStatus)
            {
                newList.Add(user);
            }            
            return newList;
        }

        /// <summary>
        /// this message will show if another user wants to chat with this user
        /// </summary>
        /// <param name="userAsking"></param>
        /// <returns></returns>
        public bool ApproveChatInvitation(string userAsking)
        {
            string question = "Would You Like To Chat With " + userAsking + "?";
            string caption = "";
            if (MessageBox.Show(question, caption, MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// this message will show if another user wants to play the game with this user
        /// </summary>
        /// <param name="userAsking"></param>
        /// <returns></returns>
        public bool ApproveGameInvitation(string userAsking)
        {
            string question = "Would You Like To play backgammon With " + userAsking + "?";
            string caption = "";
            if (MessageBox.Show(question, caption, MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
