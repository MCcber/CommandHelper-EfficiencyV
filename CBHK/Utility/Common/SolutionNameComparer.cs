using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Common
{
    public class SolutionNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string xNumber = Regex.Match(Path.GetFileNameWithoutExtension(x), @"\d+$").ToString();
            string yNumber = Regex.Match(Path.GetFileNameWithoutExtension(y), @"\d+$").ToString();
            int xInt = int.Parse(xNumber);
            int yInt = int.Parse(yNumber);
            return yInt.CompareTo(xInt);
        }
    }
}