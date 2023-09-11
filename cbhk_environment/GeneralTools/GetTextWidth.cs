using cbhk_environment.CustomControls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace cbhk_environment.GeneralTools
{
    public class GetTextWidth
    {
        [Obsolete]
        public static double Get(Run run)
        {
            var formattedText = new FormattedText(run.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(run.FontFamily, 
                run.FontStyle, 
                run.FontWeight, 
                run.FontStretch),
                run.FontSize,
                Brushes.Black);
            Size size = new Size(formattedText.Width, formattedText.Height);
            return size.Width;
        }

        [Obsolete]
        public static double Get(RichRun run)
        {
            var formattedText = new FormattedText(run.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(run.FontFamily,
                run.FontStyle,
                run.FontWeight,
                run.FontStretch),
                run.FontSize,
                Brushes.Black);
            Size size = new Size(formattedText.Width, formattedText.Height);
            return size.Width;
        }
    }
}
