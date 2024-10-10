using cbhk.CustomControls;
using System.Collections.Generic;
using System.Windows.Controls;

namespace cbhk.GeneralTools.DataService
{
    public class BlockService
    {
        #region Field
        private List<string> BlockNameList = [];
        private List<string> BlockNameTagList = [];
        private List<Image> BlockImageList = [];
        private Dictionary<int, List<IconComboBoxItem>> BlockItemMap = [];
        #endregion

        public List<string> GetAllBlockName()
        {

            return BlockNameList;
        }

        public List<string> GetAllBlockNameTag()
        {

            return BlockNameTagList;
        }

        public List<Image> GetAllBlockImage()
        {

            return BlockImageList;
        }

        public Dictionary<int, List<IconComboBoxItem>> GetBlockItemByVersion(int version)
        {

            return BlockItemMap;
        }
    }
}