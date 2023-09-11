using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace cbhk_environment.GeneralTools
{
    public static class UidSpawner
    {
        [DllImport("rpcrt4.dll", SetLastError = true)]
        private static extern int UuidCreateSequential(out Guid guid);

        /// <summary>
        /// 生成连续的UUID，底层调用了Windows API UuidCreateSequential。经测试发现，
        /// UuidCreateSequential，在多CPU并发状态下，有可能会产生重复数据，因此这个方法进行的并发控制，并延迟1毫秒。
        /// 另外，UuidCreateSequential的生成和网络连接有关（网卡），如果电脑上插了Windows Mobile的手机，会产生新的网络连接，
        /// 导致UuidCreateSequential出错，此时，这个方法将使用传统的Guid来替代Uuid。
        /// </summary>
        /// <returns>在本机生成连续的Guid</returns>
        public static Guid NewUuid()
        {
            Guid result;

            lock (typeof(UidSpawner))
            {
                int hr = UuidCreateSequential(out result);

                if (hr == 0)
                    result = Guid.NewGuid();

                Thread.Sleep(1);
            }

            return result;
        }

        /// <summary>
        /// 生成连续的UUID，底层调用了Windows API UuidCreateSequential
        /// </summary>
        /// <returns>在本机生成连续的Guid</returns>
        public static string NewUuidString()
        {
            Guid result = NewUuid();

            byte[] guidBytes = result.ToByteArray();

            for (int i = 0; i < 8; i++)
            {
                byte t = guidBytes[15 - i];
                guidBytes[15 - i] = guidBytes[i];
                guidBytes[i] = t;
            }

            return new Guid(guidBytes).ToString();
        }
    }
}
