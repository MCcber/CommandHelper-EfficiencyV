using ICSharpCode.AvalonEdit.Document;

namespace CBHK.Model.Data
{
    public class KeyValueAnchors
    {
        public string Key { get; set; }
        public bool IsContainer { get; set; }
        public bool IsArray { get; set; }
        public ITextAnchor KeyStart;    // 键的起始锚点（首个引号之后或第一个字符）
        public ITextAnchor KeyEnd;      // 键的结束锚点（键字符串结束，不含冒号前的空格）
        public ITextAnchor ValueStart;  // 值的起始锚点
        public ITextAnchor ValueEnd;    // 值的结束锚点（指向值最后一个字符之后）
    }
}
