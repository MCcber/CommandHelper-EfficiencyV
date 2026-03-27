using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace CBHK.Utility.Data
{
    public class LineFoldingStrategy
    {
        /// <summary>
        /// 添加大纲
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="document"></param>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        public static void AddFolding(FoldingManager manager, TextDocument document, DocumentLine startLine, DocumentLine endLine)
        {
            #region 检查重复项
            bool Had = false;
            foreach (FoldingSection item in manager.AllFoldings)
            {
                if (item.StartOffset == startLine.Offset)
                {
                    Had = true;
                    break;
                }
            }
            if (Had)
                return;
            #endregion
            #region 处理大纲预览文本
            string previewText = document.GetText(startLine);
            previewText = previewText.Length < 9 ? "#region" : previewText[9..];
            if (previewText.Length > 50)
                previewText = previewText[0..50];
            #endregion
            #region 处理添加
            FoldingSection foldingSection = manager.CreateFolding(startLine.Offset, endLine.EndOffset);
            foldingSection.Title = previewText;
            #endregion
        }
    }
}
