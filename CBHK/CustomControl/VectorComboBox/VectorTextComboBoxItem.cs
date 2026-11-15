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
        /// <summary>
        /// 身份口径：有 ItemID 用 ItemID，没有就用 Text。
        /// 以前 ItemID 为空时会拿"对方的 ItemID"去比"自己的 Text"，结果连自己都不等于自己，
        /// Selector 的 Items.IndexOf 恒为 -1 —— 选中项设得进去、界面却显示空（下拉永远显示不出当前值）。
        /// </summary>
        public override bool Equals(object obj)
            => obj is VectorTextComboBoxItem other && string.Equals(Key, other.Key, System.StringComparison.Ordinal);

        public override int GetHashCode() => Key?.GetHashCode() ?? 0;

        private string Key => string.IsNullOrEmpty(ItemID) ? Text : ItemID;
        #endregion
    }
}