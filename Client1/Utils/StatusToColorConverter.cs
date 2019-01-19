using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Client1.Utils
{
    /// <summary>
    /// a converter for the chatViewModel, converts from StatusOfConnection to Colors(a color), and than to a string with the name of that color
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = Colors.Black;
            if (value == null)
            {
                color = Colors.White;
                return color.ToString();
            }
            else if((StatusOfConnection)value == StatusOfConnection.Offline)
            {
                color = Colors.Red;
                return color.ToString();
            }
            else if ((StatusOfConnection)value == StatusOfConnection.BusyOnGame)
            {
                color = Colors.Purple;
                return color.ToString();
            }
            else if ((StatusOfConnection)value == StatusOfConnection.BusyOnChat)
            {
                color = Colors.Pink;
                return color.ToString();
            }
            else if ((StatusOfConnection)value == StatusOfConnection.Available)
            {
                color = Colors.Green;
                return color.ToString();
            }
            else if ((StatusOfConnection)value == StatusOfConnection.BusyOnChatAndGame)
            {
                color = Colors.Blue;
                return color.ToString();
            }
            return color.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
