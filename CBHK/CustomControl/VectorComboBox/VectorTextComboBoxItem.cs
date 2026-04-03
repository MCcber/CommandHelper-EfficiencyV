using System.Windows.Media;

namespace CBHK.CustomControl.VectorComboBox
{
    public class VectorTextComboBoxItem
    {
        #region Property
        public string Text { get; set; }
        public bool IsSelected { get; set; }
        public FontFamily FontFamily { get; set; }
        public Brush MemberBrush { get; set; } = Brushes.White;
        public Brush DisplayPanelBrush { get; set; } = Brushes.Black;
        #endregion
    }
}
