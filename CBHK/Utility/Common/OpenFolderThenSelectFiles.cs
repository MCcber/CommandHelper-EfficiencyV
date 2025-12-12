using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CBHK.Utility.Common
{
    public class OpenFolderThenSelectFiles
    {
        /// <summary>
        /// 打开路径并定位文件
        /// </summary>
        /// <param name="filePath">文件绝对路径</param>
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(nint pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern nint ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(nint pidlList, uint cild, nint children, uint dwFlags);

        public static void ExplorerFile(string filePath)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return;

            if (Directory.Exists(filePath))
                Process.Start(@"explorer.exe", "/select,\"" + filePath + "\"");
            else
            {
                nint pidlList = ILCreateFromPathW(filePath);
                if (pidlList != nint.Zero)
                {
                    try
                    {
                        Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, 0, nint.Zero, 0));
                    }
                    finally
                    {
                        ILFree(pidlList);
                    }
                }
            }
        }
    }
}
