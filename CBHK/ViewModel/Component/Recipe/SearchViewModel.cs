using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Event;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 搜索页：编辑一个槽位的多物品、Key 与 Tag
    /// </summary>
    public partial class SearchViewModel : ObservableObject, IRecipePanel
    {
        private readonly IEventAggregator eventAggregator;

        public SearchViewModel(RecipeEditorViewModelBase owner, RecipeSlotViewModel slot, IEventAggregator eventAggregator)
        {
            Owner = owner;
            Slot = slot;
            this.eventAggregator = eventAggregator;

            Slot.EnsureTagSlots();
            _currentKey = owner.ResolveSlotKey(slot);
            _currentTag = Slot.Tags.Count > 0 ? Slot.Tags[0] : "";
            _selectedIndex = 0;

            ItemsView = new ListCollectionView(Slot.Items)
            {
                Filter = FilterItem
            };
            UpdateAndCloseCommand = new RelayCommand(UpdateAndClose);
            DeleteItemCommand = new RelayCommand<ItemStructure>(DeleteItem);
        }

        #region Property
        public RecipeEditorViewModelBase Owner { get; }
        public RecipeSlotViewModel Slot { get; }

        [ObservableProperty]
        private string _currentKey = "";

        [ObservableProperty]
        private string _currentTag = "";

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private int _selectedIndex;

        /// <summary>
        /// 过滤后的槽位物品视图
        /// </summary>
        public ListCollectionView ItemsView { get; }

        public Visibility KeyVisibility => Owner.SupportsShaped ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 列表选中项：切换时把上一个物品的 Tag 写回并载入新物品的 Tag
        /// </summary>
        [ObservableProperty]
        private ItemStructure _selectedItem;

        public IRelayCommand UpdateAndCloseCommand { get; }
        public IRelayCommand<ItemStructure> DeleteItemCommand { get; }
        #endregion

        #region Method
        private bool FilterItem(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            ItemStructure itemStructure = item as ItemStructure;
            if (itemStructure is null)
                return false;

            string[] searchList = SearchText.Split(' ');
            string itemID = itemStructure.IDAndName.Split(':')[0];
            return (searchList.Length > 1 && (itemStructure.IDAndName.StartsWith(searchList[0]) || itemStructure.IDAndName.EndsWith(searchList[^1])))
                || (searchList.Length == 1 && itemID.Contains(SearchText));
        }

        /// <summary>
        /// 把当前 Tag 写回选中的物品
        /// </summary>
        private void FlushTag()
        {
            if (Slot.Tags.Count == 0)
                Slot.Tags.Add("");
            int index = SelectedIndex >= 0 && SelectedIndex < Slot.Tags.Count ? SelectedIndex : 0;
            Slot.Tags[index] = CurrentTag ?? "";
        }

        partial void OnSelectedItemChanged(ItemStructure value)
        {
            int index = value is null ? -1 : Slot.Items.IndexOf(value);
            if (index < 0)
                return;

            FlushTag();
            while (Slot.Tags.Count <= index)
                Slot.Tags.Add("");
            CurrentTag = Slot.Tags[index];
            SelectedIndex = index;
        }

        private void DeleteItem(ItemStructure item)
        {
            int index = Slot.Items.IndexOf(item);
            if (index < 0)
                return;

            Slot.Items.RemoveAt(index);
            if (index < Slot.Tags.Count)
                Slot.Tags.RemoveAt(index);
            Slot.EnsureTagSlots();
            if (Slot.Tags.Count == 0)
                Slot.Tags.Add("");

            SelectedIndex = 0;
            CurrentTag = Slot.Tags[0];
            Slot.Refresh();
        }

        /// <summary>
        /// 落盘 Key/Tag，供关闭或被其它面板顶替时调用
        /// </summary>
        public void Apply()
        {
            FlushTag();
            Slot.Key = (CurrentKey ?? "").Replace(" ", "").Replace("\r", "").Replace("\n", "");
            Slot.EnsureTagSlots();
            Slot.Refresh();
        }

        private void UpdateAndClose()
        {
            Apply();
            eventAggregator.GetEvent<CloseRecipePanelEvent>().Publish(this);
        }

        partial void OnSearchTextChanged(string value)
        {
            ItemsView?.Refresh();
        }
        #endregion
    }
}