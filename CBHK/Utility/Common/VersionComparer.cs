namespace CBHK.Utility.Common
{
    public static class VersionComparer
    {
        public static bool IsInRange(string sourceVersion, string targetVersion)
        {
            bool result = false;
            sourceVersion = sourceVersion.Replace(".","");
            targetVersion = targetVersion.Replace(".", "");
            string[] sourceVersionArray = sourceVersion.Split('-',System.StringSplitOptions.RemoveEmptyEntries);
            if(sourceVersionArray.Length > 1)
            {
                result = int.Parse(sourceVersionArray[1]) <= int.Parse(targetVersion);
            }
            else
            {
                result = int.Parse(sourceVersion) <= int.Parse(targetVersion);
            }
            return result;
        }
    }
}
