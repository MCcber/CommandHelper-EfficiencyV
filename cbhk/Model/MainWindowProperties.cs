namespace cbhk.Model
{
    public class MainWindowProperties
    {
        /// <summary>
        /// 关闭后缩小到托盘
        /// </summary>
        public static bool CloseToTray { get; set; } = false;

        /// <summary>
        /// 轮播图播放延迟
        /// </summary>
        public static int LinkAnimationDelay { get; set; } = 3;

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
