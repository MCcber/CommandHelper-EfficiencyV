using System;

namespace CBHK.Utility.Data
{
    public static class UUIDHelper
    {
        public static (string Most, string Least) SplitGuid(Guid guid)
        {
            string hex = guid.ToString("N"); // 32位十六进制，无连字符
            string most = Convert.ToInt64(hex[..16], 16).ToString();  // 前16位十六进制 = 高64位
            string least = Convert.ToInt64(hex.Substring(16, 16), 16).ToString(); // 后16位十六进制 = 低64位

            return (most, least);
        }

        public static int[] GuidToMinecraftIntArray(Guid guid)
        {
            // 获取无连字符的32位十六进制字符串
            string hex = guid.ToString("N");

            int[] result = new int[4];
            for (int i = 0; i < 4; i++)
            {
                // 每8个十六进制字符对应一个32位整数
                string chunk = hex.Substring(i * 8, 8);
                // 将无符号32位整数转为有符号int（Minecraft的int是有符号的）
                result[i] = unchecked((int)Convert.ToUInt32(chunk, 16));
            }
            return result;
        }
    }
}
