using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace CBHK.ViewModel.Component.Spawner.Control
{
    /// <summary>
    /// 潜在实体（SpawnPotentialView）的全部业务：界面上那几个输入项 + NBT 拼装 + 删除自己。
    /// 视图侧只剩 InitializeComponent，拼装逻辑放在 <see cref="BuildResult"/> 这个方法里，不做成属性 getter。
    /// </summary>
    public partial class SpawnPotentialViewModel : ObservableObject
    {
        private readonly Action<SpawnPotentialViewModel> requestDelete;

        [ObservableProperty]
        private string weight = "1";

        /// <summary>方块光照用默认值（对应"使用默认值"开关）。</summary>
        [ObservableProperty]
        private bool useDefaultBlockLight = true;

        /// <summary>方块光照填整数（true）还是填上下限（false）。</summary>
        [ObservableProperty]
        private bool blockLightUseSingleValue = true;

        [ObservableProperty]
        private string blockLightValue = "0";

        [ObservableProperty]
        private string blockLightMin = "0";

        [ObservableProperty]
        private string blockLightMax = "15";

        /// <summary>天空光照用默认值。</summary>
        [ObservableProperty]
        private bool useDefaultSkyLight = true;

        [ObservableProperty]
        private bool skyLightUseSingleValue = true;

        [ObservableProperty]
        private string skyLightValue = "0";

        [ObservableProperty]
        private string skyLightMin = "0";

        [ObservableProperty]
        private string skyLightMax = "15";

        /// <summary>这一项要生成的实体（引用实体槽的 ViewModel，文本与图标都在它自己那里）。</summary>
        public ReferenceEntityViewModel EntitySlot { get; } = new();

        public SpawnPotentialViewModel() : this(null) { }

        public SpawnPotentialViewModel(Action<SpawnPotentialViewModel> requestDelete)
        {
            this.requestDelete = requestDelete;
        }

        /// <summary>整数模式的输入框是否可见；区间模式的整块反过来。</summary>
        public Visibility SingleValueVisibility => BlockLightUseSingleValue ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RangeVisibility => BlockLightUseSingleValue ? Visibility.Collapsed : Visibility.Visible;

        partial void OnBlockLightUseSingleValueChanged(bool value)
        {
            OnPropertyChanged(nameof(SingleValueVisibility));
            OnPropertyChanged(nameof(RangeVisibility));
        }

        [RelayCommand]
        private void Delete() => requestDelete?.Invoke(this);

        /// <summary>
        /// 拼出这一项的 NBT：{weight:n,data:{custom_spawn_rules:{...},entity:{...}}}。
        /// 数字解析不出来就**跳过那条规则**（旧实现直接 int.Parse，空输入会抛异常把整页生成打断）。
        /// </summary>
        public string BuildResult()
        {
            var rules = BuildLightRule("block_light_limit", UseDefaultBlockLight, BlockLightUseSingleValue,
                    BlockLightValue, BlockLightMin, BlockLightMax)
                + BuildLightRule("sky_light_limit", UseDefaultSkyLight, SkyLightUseSingleValue,
                    SkyLightValue, SkyLightMin, SkyLightMax);

            var body = rules.Length > 0 ? "custom_spawn_rules:{" + rules.TrimEnd(',') + "}," : "";
            if (!string.IsNullOrWhiteSpace(EntitySlot.Tag))
            {
                body += "entity:" + EntitySlot.Tag + ",";
            }

            var weight = TryParseInt(Weight, out var parsedWeight) ? parsedWeight : 1;
            var data = body.TrimEnd(',');
            return "{weight:" + weight + (data.Length > 0 ? ",data:{" + data + "}" : "") + "}";
        }

        /// <summary>一条光照规则；用默认值或数字不合法时返回空串。</summary>
        private static string BuildLightRule(string key, bool useDefault, bool singleValue,
            string value, string min, string max)
        {
            if (useDefault) return "";
            if (singleValue)
            {
                return TryParseInt(value, out var single) ? key + ":" + single + "," : "";
            }
            if (TryParseInt(min, out var low) && TryParseInt(max, out var high))
            {
                return key + ":[" + low + "," + high + "],";
            }
            return "";
        }

        /// <summary>解析整数：允许空/空白/带符号，失败不抛。</summary>
        private static bool TryParseInt(string text, out int value)
            => int.TryParse((text ?? "").Trim(), out value);
    }
}
