using CBHK.Interface.Component.Recipe;
using CBHK.Interface.Visual;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Visual.MessageTip;
using CBHK.ViewModel.Component.Recipe.Event;
using CBHK.ViewModel.Component.Recipe.Strategy;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 所有配方编辑器的公共基类：设置项、槽位、生成与拖放
    /// </summary>
    public abstract partial class RecipeEditorViewModelBase : ObservableObject, IPageViewModel
    {
        private readonly SlotDropStrategyRegistry dropStrategies;

        protected RecipeEditorViewModelBase(IEventAggregator eventAggregator, SlotDropStrategyRegistry dropStrategies)
        {
            EventAggregator = eventAggregator;
            this.dropStrategies = dropStrategies;
        }

        protected IEventAggregator EventAggregator { get; }

        #region Property
        public MessagePopup MessagePopup { get; set; }

        public abstract RecipeType Kind { get; }

        /// <summary>
        /// 材料槽位
        /// </summary>
        public ObservableCollection<RecipeSlotViewModel> Slots { get; } = [];

        /// <summary>
        /// 结果槽位
        /// </summary>
        public RecipeSlotViewModel ResultSlot { get; protected set; }

        [ObservableProperty]
        private bool _multiSelect;

        [ObservableProperty]
        private bool _shaped;

        [ObservableProperty]
        private string _groupName = "";

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private decimal _count = 1;

        [ObservableProperty]
        private decimal _experience;

        [ObservableProperty]
        private decimal _cookingtime = 10;

        [ObservableProperty]
        private string _trimPattern = "";

        /// <summary>
        /// 当前选中的配方格式，决定 JSON 的字段写法
        /// </summary>
        public RecipeFormat Format { get; set; } = RecipeFormat.Legacy;

        public virtual bool SupportsTrimPattern => false;

        public virtual bool SupportsShaped => false;
        public virtual bool SupportsMultiSelect => true;
        public virtual bool SupportsGroupName => true;
        public virtual bool SupportsCount => false;
        public virtual bool SupportsExperience => false;
        public virtual bool SupportsCookingTime => false;

        protected virtual string SuccessMessage => Kind + "配方生成成功！";

        /// <summary>
        /// 配方窗体所有提示框沿用的图标（与窗口 Icon 同一份资产）
        /// </summary>
        public static ImageSource RecipeToastIcon { get; } = CreateRecipeToastIcon();

        private static ImageSource CreateRecipeToastIcon()
        {
            try
            {
                return new BitmapImage(new Uri("pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconRecipe.png", UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 生成成功提示的主文本颜色（淡金）
        /// </summary>
        public static SolidColorBrush SuccessMessageBrush { get; } = new((Color)ColorConverter.ConvertFromString("#E8D18F"));

        /// <summary>
        /// 缺材料提示的主文本颜色（琥珀）
        /// </summary>
        public static SolidColorBrush WarningMessageBrush { get; } = new((Color)ColorConverter.ConvertFromString("#F0B429"));

        private readonly List<string> buildWarnings = [];

        /// <summary>
        /// 上一次生成过程中产生的告警（例如缺材料用了占位）
        /// </summary>
        public IReadOnlyList<string> BuildWarnings => buildWarnings;

        protected void ClearBuildWarnings()
        {
            buildWarnings.Clear();
        }

        protected void AddBuildWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                buildWarnings.Add(message);
        }

        /// <summary>
        /// 把本次生成的告警推到右上角提示框
        /// </summary>
        protected void PushBuildWarnings(string prefix = "")
        {
            if (buildWarnings.Count == 0 || MessagePopup is null)
                return;

            MessagePopup.PushMessage(new GeneratorMessage()
            {
                Message = prefix + string.Join("；", buildWarnings),
                MessageBrush = WarningMessageBrush,
                SubMessage = "配方生成器",
                SubMessageBrush = Brushes.DarkGray,
                Icon = RecipeToastIcon
            });
        }

        /// <summary>
        /// 生成结果
        /// </summary>
        public string Result { get; protected set; } = "";

        /// <summary>
        /// 需要弹出保存对话框
        /// </summary>
        public bool NeedSave { get; set; } = true;
        #endregion

        #region Method
        public abstract string BuildResult();

        /// <summary>
        /// 载入外部导入的配方数据
        /// </summary>
        public virtual void LoadExternalData(JObject json)
        {
        }

        /// <summary>
        /// 切版本时清掉所有槽位里该版本不存在的物品（含结果槽）
        /// </summary>
        public void PruneItemsToVersion(HashSet<string> validItemIds)
        {
            if (validItemIds is null || validItemIds.Count == 0)
                return;

            foreach (RecipeSlotViewModel slot in Slots)
                PruneSlot(slot, validItemIds);

            if (ResultSlot is not null)
                PruneSlot(ResultSlot, validItemIds);
        }

        private static void PruneSlot(RecipeSlotViewModel slot, HashSet<string> validItemIds)
        {
            for (int i = slot.Items.Count - 1; i >= 0; i--)
            {
                if (validItemIds.Contains(RecipeSlotViewModel.GetItemId(slot.Items[i])))
                    continue;

                slot.Items.RemoveAt(i);
                if (i < slot.Tags.Count)
                    slot.Tags.RemoveAt(i);
            }

            slot.EnsureTagSlots();
            slot.Refresh();
        }

        public void ApplyDrop(RecipeSlotViewModel slot, ItemStructure item)
        {
            dropStrategies.Get(slot.Role).Apply(slot, item);
        }

        /// <summary>
        /// 打开搜索页时补全该槽的 Key（有序配方才有意义）
        /// </summary>
        public virtual string ResolveSlotKey(RecipeSlotViewModel slot)
        {
            return slot.Key;
        }

        public void OpenSearch(RecipeSlotViewModel slot)
        {
            EventAggregator.GetEvent<RequestRecipePanelEvent>().Publish(new SearchViewModel(this, slot, EventAggregator));
        }

        [RelayCommand]
        public async Task Run()
        {
            // 先算内容（缺材料在这里补上占位），提示一律等用户选完路径、按下确定之后再弹
            Result = BuildResult();
            if (!NeedSave)
            {
                await PushGeneratedToastsAsync();
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                AddExtension = true,
                Filter = "Json文件|*.json;",
                DefaultExt = ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                Title = "选择配方文件存储路径"
            };
            if (!saveFileDialog.ShowDialog().Value)
                return;

            try
            {
                await File.WriteAllTextAsync(saveFileDialog.FileName, Result);
            }
            catch
            {
                MessagePopup?.PushMessage(new GeneratorMessage()
                {
                    Message = "配方生成失败：文件写入不成功",
                    MessageBrush = Brushes.Red,
                    SubMessage = "配方生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = RecipeToastIcon
                });
                return;
            }

            await PushGeneratedToastsAsync();
        }

        /// <summary>
        /// 生成结束后的提示：先弹缺材料，隔一会儿再弹成功
        /// </summary>
        private async Task PushGeneratedToastsAsync()
        {
            if (BuildWarnings.Count > 0)
            {
                PushBuildWarnings();
                // 给缺料提示留出阅读时间，避免被随后的成功提示挤在一起看不清
                await Task.Delay(1500);
            }

            MessagePopup?.PushMessage(new GeneratorMessage()
            {
                Message = SuccessMessage,
                MessageBrush = SuccessMessageBrush,
                SubMessage = "配方生成器",
                SubMessageBrush = Brushes.DarkGray,
                Icon = RecipeToastIcon
            });
        }

        partial void OnMultiSelectChanged(bool value)
        {
            foreach (RecipeSlotViewModel slot in Slots)
                slot.Refresh();
            ResultSlot?.Refresh();
        }
        #endregion
    }
}
