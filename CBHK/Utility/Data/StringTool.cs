using System;

namespace CBHK.Utility.Data
{
    public static class StringTool
    {
        public static bool IsMatchSearchText(string targetText,string SearchText)
        {
            if (string.IsNullOrEmpty(SearchText) || targetText.StartsWith(SearchText))
            {
                return true;
            }
            ReadOnlySpan<char> searchTextSpan = SearchText.AsSpan();
            int index = 0;
            for (int i = 0; i < targetText.Length; i++)
            {
                if (char.ToLowerInvariant(targetText[i]) == char.ToLowerInvariant(searchTextSpan[index]))
                {
                    index++;
                    if (index == searchTextSpan.Length)
                    {
                        return true;
                    }
                }
            }
            return index == searchTextSpan.Length;
        }
    }
}
