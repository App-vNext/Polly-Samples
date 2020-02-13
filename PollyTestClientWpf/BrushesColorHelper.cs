using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Color = PollyDemos.OutputHelpers.Color;

namespace PollyTestClientWpf
{
    public static class BrushesColorHelper
    {
        public static SolidColorBrush ToBrushColor(this Color color)
        {
            switch (color)
            {
                case Color.White:
                    return Brushes.White;
                case Color.Green:
                    return Brushes.Green;
                case Color.Magenta:
                    return Brushes.Magenta;
                case Color.Red:
                    return Brushes.Red;
                case Color.Yellow:
                    return Brushes.Coral;
                case Color.Default:
                    return Brushes.Black;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}