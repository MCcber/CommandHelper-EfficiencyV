using cbhk.CustomControls;
using System.Collections.Generic;
using System.Windows.Controls;

namespace cbhk.GeneralTools.DataService
{
    public class ItemService
    {
        #region Field
        private List<string> ItemNameList = [];
        private List<Image> ItemImageList = [];
        private Dictionary<int, List<IconComboBoxItem>> ItemItemMap = [];
        #endregion

        public List<string> GetAllItemName()
        {

            return ItemNameList;
        }

        public List<Image> GetAllItemImage()
        {

            return ItemImageList;
        }

        public Dictionary<int, List<IconComboBoxItem>> GetItemItemByVersion(int version)
        {

            return ItemItemMap;
        }
    }
}
