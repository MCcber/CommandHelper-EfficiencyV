using System;

namespace cbhk.GeneralTools.Displayer
{
    /// <summary>
    /// 物品数据结构(用于快速查找中英文以提升搜索效率)
    /// </summary>
    public class ItemStructure(Uri ImagePath, string IDAndName, string NBT = "")
    {
        public Uri ImagePath { get; set; } = ImagePath;
        public string IDAndName { get; set; } = IDAndName;

        public string NBT { get; set; } = NBT;
    }
}
