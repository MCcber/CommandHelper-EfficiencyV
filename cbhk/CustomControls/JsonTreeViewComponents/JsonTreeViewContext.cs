namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public class JsonTreeViewContext
    {
        /// <summary>
        /// 键起始偏移
        /// </summary>
        public int KeyStartOffset { get; set; }
        /// <summary>
        /// 键末尾偏移
        /// </summary>
        public int KeyEndOffset { get; set; }
        /// <summary>
        /// 值起始偏移
        /// </summary>
        public int ValueStartOffset { get; set; }
        /// <summary>
        /// 值末尾偏移
        /// </summary>
        public int ValueEndOffset { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 当前成员
        /// </summary>
        public JsonTreeViewItem Item { get; set; }
    }
}
