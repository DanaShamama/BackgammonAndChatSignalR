using Client1.ViewModels;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client1.Views
{
    /// <summary>
    /// Interaction logic for ChatRoom.xaml
    /// </summary>
    public partial class ChatRoom : Window
    {
        public ChatRoom(string UserName, string otherUser, IHubProxy chatHubProxy)
        {
            DataContext = new ChatRoomViewModel(UserName, otherUser, chatHubProxy);
            InitializeComponent();
        }
    }
}
