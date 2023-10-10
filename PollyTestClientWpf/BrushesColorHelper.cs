using System.Windows.Media;
using Color = PollyDemos.OutputHelpers.Color;

namespace PollyTestClientWpf
{
    public static class BrushesColorHelper
    {
        public static SolidColorBrush ToBrushColor(this Color color) => color switch
        {
            Color.White => Brushes.White,
            Color.Green => Brushes.Green,
            Color.Magenta => Brushes.Magenta,
            Color.Red => Brushes.Red,
            Color.Yellow => Brushes.Coral,
            Color.Default => Brushes.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(color)),
        };
    }
}
