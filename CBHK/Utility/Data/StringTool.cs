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

        public static string ConvertCamelCaseToSnakeCase(this string target)
        {
            string result = "";
            for (int i = 0; i < target.Length; i++)
            {
                char c = target[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        result += "_";
                    }
                    result += char.ToLower(c);
                }
                else
                {
                    result += c;
                }
            }
            return result;
        }
    }
}
