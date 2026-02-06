namespace CBHK.Model.Common
{
    public class FormattingState
    {
        public bool? Bold { get; set; } // null = 混合状态
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }
        public bool? Obfuscated { get; set; }

        public static readonly FormattingState None = new()
        {
            Bold = false,
            Italic = false,
            Underline = false
        };
    }
}
