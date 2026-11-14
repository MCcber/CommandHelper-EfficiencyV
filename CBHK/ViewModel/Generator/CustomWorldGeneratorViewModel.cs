using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dimension（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class CustomWorldGeneratorViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "dimension")
    {
        #region Field
        private static readonly string GeneratorFolderPath =
            AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\CustomWorldGenerator";

        private Resource resource = resource;
        private string generatorPrefixString = "worldgen/";
        #endregion

        #region Property
        /// <summary>
        /// 生成器候选。**必须在字段初始化时就把目录扫出来**：
        /// ItemsSource 走的是 EnumMemberToTextComboBoxItemConverter，它返回的是一份快照集合，
        /// 绑定求值之后再往集合里 Add 不会让转换器重跑 —— 在 Loaded 里填的表现就是"下拉永远没有成员"。
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> generatorList = LoadGeneratorList();

        [ObservableProperty]
        private VectorTextComboBoxItem selectedGenerator = new();
        #endregion

        #region Event
        public void CustomWorldGenerator_Loaded(object sender, RoutedEventArgs e)
        {
            // 进来先选中第一项，下拉与树的入口用的是同一个对象
            if (string.IsNullOrEmpty(SelectedGenerator?.Text))
            {
                SelectedGenerator = GeneratorList.FirstOrDefault();
            }
            OpenGeneratorTree();
        }

        public void GeneratorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            => OpenGeneratorTree();
        #endregion

        #region Method
        /// <summary>
        /// 按当前选中的生成器开一棵树；版本/选择还没就绪时静默跳过（旧实现直接下标取值，目录缺失就崩）。
        /// </summary>
        private void OpenGeneratorTree()
        {
            var version = SelectedGameVersionOption?.Value?.LiteralValue?.ToString();
            var generatorName = DispatchKeyOf(SelectedGenerator?.Text);
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(generatorName))
            {
                return;
            }
            OpenMcdocTreeFromDispatcher(version, "minecraft:resource",
                McdocIndex.Static(generatorName), resource.McdocTypes);
        }

        /// <summary>
        /// 目录名 → minecraft:resource 的 dispatcher 成员键。
        /// 默认是"前缀 + 驼峰转下划线"，但有两条不一致，转换规则猜不出来，只能显式覆盖：
        /// PlaceFeature 是 worldgen/placed_feature（不是 place_feature），
        /// FlatWorldPreset 是 worldgen/flat_level_generator_preset（不是 flat_world_preset）。
        /// 对不上的键会静默解出兜底树 —— 表现就是"切到这两个生成器时树和 Json 都不变"。
        /// </summary>
        private static readonly Dictionary<string, string> DispatchKeyOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PlaceFeature"] = "worldgen/placed_feature",
            ["FlatWorldPreset"] = "worldgen/flat_level_generator_preset",
        };

        /// <summary>目录名 → dispatcher 成员键（先查覆盖表，再按前缀 + 驼峰转下划线）。</summary>
        private string DispatchKeyOf(string generatorName)
        {
            if (string.IsNullOrEmpty(generatorName))
            {
                return null;
            }
            return DispatchKeyOverrides.TryGetValue(generatorName, out var overridden)
                ? overridden
                : generatorPrefixString + generatorName.ConvertCamelCaseToSnakeCase();
        }

        /// <summary>
        /// 扫配置目录得到候选（跳过 Data / Image 两个资源目录）。
        /// </summary>
        private static ObservableCollection<VectorTextComboBoxItem> LoadGeneratorList()
        {
            ObservableCollection<VectorTextComboBoxItem> result = [];
            if (!Directory.Exists(GeneratorFolderPath))
            {
                return result;
            }

            string[] directoryArray = [.. Directory.GetDirectories(GeneratorFolderPath).OrderBy(Path.GetFileName, StringComparer.Ordinal)];
            foreach (var directory in directoryArray)
            {
                var name = Path.GetFileName(directory);
                if (name is "Data" or "Image")
                {
                    continue;
                }
                result.Add(new VectorTextComboBoxItem
                {
                    Text = name,
                    ItemID = name
                });
            }
            return result;
        } 
        #endregion
    }
}
