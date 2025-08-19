using System.Collections.Generic;

namespace CBHK.Model.Common
{
    public class WikiTreeViewItem
    {
        /// <summary>
        /// 节点类型列表
        /// </summary>
        public List<string> ItemTypeList { get; set; } = [];
        /// <summary>
        /// 节点Key
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// 注入字符串(代指另一个Wiki文档)
        /// </summary>
        public string InheritString { get; set; } = string.Empty;
        /// <summary>
        /// 数据Key
        /// </summary>
        public string DataKeyWord { get; set; } = string.Empty;
    }
}
