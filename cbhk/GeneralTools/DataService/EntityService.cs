using cbhk.CustomControls;
using System.Collections.Generic;
using System.Windows.Controls;

namespace cbhk.GeneralTools.DataService
{
    public class EntityService
    {
        #region Field
        private List<string> EntityNameList = [];
        private List<Image> EntityImageList = [];
        private Dictionary<int, List<IconComboBoxItem>> EntityItemMap = [];
        #endregion

        public List<string> GetAllEntityName()
        {

            return EntityNameList;
        }

        public List<Image> GetAllEntityImage()
        {

            return EntityImageList;
        }

        public Dictionary<int, List<IconComboBoxItem>> GetEntityItemByVersion(int version)
        {

            return EntityItemMap;
        }
    }
}
