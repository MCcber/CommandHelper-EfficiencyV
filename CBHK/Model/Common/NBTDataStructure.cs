using System.Windows;

namespace CBHK.Model.Common
{
    public class NBTDataStructure : FrameworkElement
    {
        /// <summary>
        /// 结果
        /// </summary>
        public string Result { get; set; }

        public string DataType { get; set; }
        public string ResultType { get; set; }

        /// <summary>
        /// 标记当前实例属于哪个共通标签
        /// </summary>
        public string NBTGroup { get; set; }
    }
}
