using Client1.Commands;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace Client1.ViewModels
{
    class ChatRoomViewModel : ViewModelBase
    {
        private string _otherUserName;
        public string OtherUserName
        {
            get
            {
                return _otherUserName;
            }
            set
            {
                _otherUserName = value;
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
            }
        }


        private static IHubProxy _chatHubProxy;
        public IHubProxy ChatHubProxy
        {
            get
            {
                return _chatHubProxy;
            }
            set
            {
                _chatHubProxy = value;
            }
        }

        private bool _canExecute; // boolean for Icommand _clickCommandSend
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

        private ICommand _clickCommandSend;
        public ICommand ClickCommandSend
        {
            get
            {
                return _clickCommandSend ?? (_clickCommandSend = new CommandHandler(() => SendMessage(YourText, UserName, OtherUserName), _canExecute));
            }
        }

        private string _chatText;
        public string ChatText
        {
            get
            {
                return _chatText;
            }
            set
            {
                _chatText = value;
                OnPropertyChanged("ChatText");
            }
        }

        private string _yourText;
        public string YourText
        {
            get
            {
                    return _yourText;
            }
            set
            {               
                _yourText = value;
                OnPropertyChanged("YourText");
            }
        }

        private StringBuilder _sb;
        public StringBuilder Sb
        {
            get
            {                
                return _sb;
            }
            set
            {
                _sb = value;                   
            }
        }

        // ctor
        public ChatRoomViewModel(string userName, string otherUserName, IHubProxy chatHubProxy)
        {
            OtherUserName = otherUserName;
            UserName = userName;
            _canExecute = true;
            if (_chatHubProxy == null)
            {
                this.ChatHubProxy = chatHubProxy;
            }
            Sb = new StringBuilder("");

            chatHubProxy.On("GetMessage", (string MessageToGet) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Sb.Append(OtherUserName + ": " + MessageToGet + '\n');
                    ChatText = Sb.ToString();
                }));
            });

        }

        private void SendMessage(string messageToSend, string sender, string receiver)
        {
            Sb.Append(UserName + ": " + messageToSend + '\n');
            ChatText = Sb.ToString();
            ChatHubProxy.Invoke("SendMessage", messageToSend, sender, receiver);
            YourText = "";
        }
    }
}
