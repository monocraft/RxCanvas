using RxCanvas.Core;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RxCanvas
{
    public partial class SelectedItemControl : UserControl
    {
        public SelectedItemControl()
        {
            InitializeComponent();
        }
    }

    public class XColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (IColor)value;
            return string.Concat('#', color.A.ToString("X2"), color.R.ToString("X2"), color.G.ToString("X2"), color.B.ToString("X2"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            return new XColor(byte.Parse(str.Substring(1, 2), NumberStyles.HexNumber),
                byte.Parse(str.Substring(3, 2), NumberStyles.HexNumber),
                byte.Parse(str.Substring(5, 2), NumberStyles.HexNumber),
                byte.Parse(str.Substring(7, 2), NumberStyles.HexNumber));
        }
    }

    public class XPointValueConverter : IValueConverter
    {
        private static char[] Separators = new char[] { ';' };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var point = (IPoint)value;
            return string.Concat(point.X.ToString(), Separators[0], point.Y.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            string[] values = str.Split(Separators);
            return new XPoint(double.Parse(values[0]), double.Parse(values[1]));
        }
    }
}
