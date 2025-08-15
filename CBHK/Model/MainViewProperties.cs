namespace CBHK.Model
{
    public class MainViewProperties
    {
        /// <summary>
        /// 关闭后缩小到托盘
        /// </summary>
        public string CloseToTray { get; set; } = "true";

        public string ShowNotice { get; set; } = "true";

        public Visibility MainViewVisibility { get; set; }

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
