namespace CBHK.Utility.Data
{
    public static class StringHelper
    {
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
