using System.Windows.Media;

namespace CBHK.CustomControl.VectorComboBox
{
    public class VectorTextComboBoxItem
    {
        #region Property
        public string ItemID { get; set; }
        public string Text { get; set; }
        public bool IsSelected { get; set; }
        public FontFamily FontFamily { get; set; }
        #endregion

        #region Method
        public override bool Equals(object obj)
        {
            if(obj is VectorTextComboBoxItem other)
            {
                if(!string.IsNullOrEmpty(ItemID))
                {
                    return other.ItemID == ItemID;
                }
                else
                {
                    return other.ItemID == Text;
                }
            }
            return false;
        }
        public override int GetHashCode() => ItemID?.GetHashCode() ?? 0;
        #endregion
    }
}