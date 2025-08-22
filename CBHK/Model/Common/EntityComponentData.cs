namespace CBHK.Model.Common
{
    /// <summary>
    /// 动态控件数据结构
    /// </summary>
    public class EntityComponentData
    {
        public string Children { get; set; }
        public string DataType { get; set; }
        public string NBTType { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public string ToolTip { get; set; }
        public string Dependency { get; set; }
    }
}
