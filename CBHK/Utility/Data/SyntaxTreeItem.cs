using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 语法树节点类
    /// </summary>
    public class SyntaxTreeItem
    {
        public enum SyntaxTreeItemType
        {
            Literal,
            Reference,
            Radical,
            DataType,
            Redirect
        }

        /// <summary>
        /// 对应的键
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// 对应的文本
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 文本的数据类型
        /// </summary>
        public SyntaxTreeItemType Type { get; set; }

        public string Description { get; set; } = "";

        /// <summary>
        /// 当前节点的子级
        /// </summary>
        public List<SyntaxTreeItem> Children { get; set; } = [];
    }
}
