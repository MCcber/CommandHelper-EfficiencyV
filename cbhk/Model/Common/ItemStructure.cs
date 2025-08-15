using System.Windows.Media;

namespace CBHK.Model.Common
{
    /// <summary>
    /// 物品数据结构(用于快速查找中英文以提升搜索效率)
    /// </summary>
    public class ItemStructure
    {
        public ImageSource ImagePath { get; set; }
        public string IDAndName { get; set; }

        public string NBT { get; set; }
    }
}