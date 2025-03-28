namespace CBHK.Model
{
    public class MainWindowProperties
    {
        /// <summary>
        /// 关闭后缩小到托盘
        /// </summary>
        public static bool CloseToTray { get; set; } = false;

        public static bool ShowNotice { get; set; } = true;

        /// <summary>
        /// 主页可见性
        /// </summary>
        public enum Visibility
        {
            KeepState,
            MinState,
            Close
        }
    }
}
