using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.Model.Common
{
    /// <summary>
    /// 物品数据结构(用于快速查找中英文以提升搜索效率)
    /// </summary>
    public class ItemStructure(ImageSource ImagePath, string IDAndName, string NBT = "")
    {
        public ImageSource ImagePath { get; set; } = ImagePath;
        public string IDAndName { get; set; } = IDAndName;

        public string NBT { get; set; } = NBT;
    }
}