using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 「数据包变种资源」页：一个下拉收编 13 个变种生成器（猫/鸡/牛/青蛙/画/猪/狼/僵尸鹦鹉螺，各自带音效变体）。
    /// 它们的入口都是 minecraft:resource，键是 Id 的驼峰转下划线；各入口的 #[since] 不同，
    /// 所以每次切入口都要按新入口重算版本下拉。
    /// </summary>
    public partial class VariantResourceViewModel(Resource resource):McdocGeneratorViewModelBase(resource, "cat_variant", null)
    {
        #region Field
        private Resource resource = resource;

        /// <summary>下拉成员图标的内嵌资源目录（与主面板生成器图标同目录，文件名 = 成员 Id）。</summary>
        private const string GeneratorIconBasePath =
            "pack://application:,,,/CBHK;component/Resource/CBHK/Image/Generator/";
        #endregion

        #region Property
        [ObservableProperty]
        private string title = "数据包变种资源";

        /// <summary>
        /// 生成器候选。**必须在字段初始化时就把清单建出来**：
        /// ItemsSource 走的是 EnumMemberToTextComboBoxItemConverter，它返回的是一份快照集合，
        /// 绑定求值之后再往集合里 Add 不会让转换器重跑 —— 在 Loaded 里填的表现就是"下拉永远没有成员"。
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VectorIconTextComboBoxItem> generatorList = LoadGeneratorList();

        [ObservableProperty]
        private VectorTextComboBoxItem selectedGenerator = new();
        #endregion

        #region Event
        public void VariantResource_Loaded(object sender, RoutedEventArgs e)
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
        /// 按当前选中的变种开一棵树；版本/选择还没就绪时静默跳过。
        /// 建完树再按新入口重算版本下拉：zombie_nautilus_variant 要到 1.21.11，5 个音效变体要到 26.1。
        /// </summary>
        private void OpenGeneratorTree()
        {
            var version = SelectedGameVersionOption?.Value?.LiteralValue?.ToString();
            var entryId = SelectedGenerator?.ItemID;
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(entryId))
            {
                return;
            }
            OpenMcdocTree(version, entryId.ConvertCamelCaseToSnakeCase(), resource.McdocTypes);
            RefreshGameVersionOptions();
        }

        /// <summary>GeneratorCatalog.VariantEntries → 下拉项（Text 显示中文名，ItemID 带原 Id）。</summary>
        private static ObservableCollection<VectorIconTextComboBoxItem> LoadGeneratorList()
        {
            ObservableCollection<VectorIconTextComboBoxItem> result = [];
            foreach (var entry in GeneratorCatalog.VariantEntries)
            {
                result.Add(new VectorIconTextComboBoxItem
                {
                    Text = entry.Name,
                    ItemID = entry.Id,
                    Image = LoadGeneratorIcon(entry.Id)
                });
            }
            return result;
        }

        /// <summary>按成员 Id 加载内嵌图标；缺图或加载失败时静默回退为空图标。</summary>
        private static ImageSource LoadGeneratorIcon(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            try
            {
                return new BitmapImage(new Uri(GeneratorIconBasePath + id + ".png", UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
