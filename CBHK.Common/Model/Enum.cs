using System.ComponentModel;

namespace CBHK.Common.Model
{
    public enum WindowVisualType
    {
        [Description("普通")]
        Default,
        [Description("亚克力")]
        Acrylic,
        [Description("云母")]
        Mica,
        [Description("云母Alt")]
        MicaAlt
    }

    public enum WindowThemeType
    {
        [Description("浅色")]
        Light,
        [Description("深色")]
        Dark,
        [Description("跟随系统")]
        FllowSystem,
        [Description("自定义")]
        Custom
    }

    public enum WindowCornerPreference
    {
        Default = 0,    // 系统默认
        DoNotRound = 1, // 强制方形
        Round = 2,      // 强制圆角（常用）
        RoundSmall = 3  // 强制小圆角
    }

    public enum LanuchState
    {
        Visible,
        Hidden
    }
}
