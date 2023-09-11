using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace cbhk_environment
{
    public class FilePathComparator
    {
        #region 文件路径比较器
        public class FileNameString
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public int FileIndex { get; set; }
        }

        public class FileNameComparer : IComparer<FileNameString>
        {
            public int Compare(FileNameString x, FileNameString y)
            {
                int x_number = int.Parse(Regex.Match(x.FileName, @"\d+").ToString());
                int y_number = int.Parse(Regex.Match(y.FileName, @"\d+").ToString());
                if (x_number < y_number)
                    return -1;
                return 1;
            }
        }
        #endregion
    }
}
