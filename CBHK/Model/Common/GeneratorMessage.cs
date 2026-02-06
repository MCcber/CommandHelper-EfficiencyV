using System.Windows;
using System.Windows.Media;

namespace CBHK.Model.Common
{
    public class GeneratorMessage
    {
        public ImageSource Icon { get; set; }
        public string Message { get; set; }
        public string SubMessage { get; set; }
        public Brush FirstBackground { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121"));
        public Brush SecondBackground { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
        public Brush MessageBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A00E"));
        public Brush SubMessageBrush { get; set; } = Brushes.White;
        public string CollapseCount { get; set; } = "1";

        public Visibility CollapseBlockVisibility { get; set; } = Visibility.Collapsed;
    }
}
