using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 配方页里的一个插槽：承载物品组、Key/Tag 与拖放行为
    /// </summary>
    public partial class RecipeSlotViewModel : ObservableObject
    {
        private static BitmapImage emptyImage;

        public RecipeSlotViewModel(RecipeEditorViewModelBase owner, string uid, RecipeSlotRole role, string title = "")
        {
            Owner = owner;
            Uid = uid;
            Role = role;
            Title = title;
            Image = GetEmptyImage();
            DropCommand = new RelayCommand<ItemStructure>(Drop);
            OpenCommand = new RelayCommand(Open);
            ClearCommand = new RelayCommand(Clear);
        }

        #region Property
        public RecipeEditorViewModelBase Owner { get; }
        public string Uid { get; }
        public RecipeSlotRole Role { get; }
        public string Title { get; }
        public ObservableCollection<ItemStructure> Items { get; } = [];
        /// <summary>
        /// 与 Items 一一对应的 Tag
        /// </summary>
        public ObservableCollection<string> Tags { get; } = [""];

        [ObservableProperty]
        private string _key = "";

        [ObservableProperty]
        private ImageSource _image;

        [ObservableProperty]
        private string _toolTipText;

        [ObservableProperty]
        private Visibility _multiItemVisibility = Visibility.Collapsed;

        public IRelayCommand<ItemStructure> DropCommand { get; }
        public IRelayCommand OpenCommand { get; }
        public IRelayCommand ClearCommand { get; }
        #endregion

        #region Method
        private static BitmapImage GetEmptyImage()
        {
            if (emptyImage is not null)
                return emptyImage;

            string path = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\Empty.png";
            // 图集缺失时不要抛，槽位按无图显示
            if (!System.IO.File.Exists(path))
                return null;

            return emptyImage = new BitmapImage(new Uri(path));
        }

        public static string GetItemId(ItemStructure item)
        {
            if (item is null || string.IsNullOrEmpty(item.IDAndName))
                return "";
            int index = item.IDAndName.IndexOf(':');
            return index < 0 ? item.IDAndName : item.IDAndName[..index];
        }

        private void Drop(ItemStructure item)
        {
            if (item is not null)
                Owner.ApplyDrop(this, item);
        }

        private void Open()
        {
            Owner.OpenSearch(this);
        }

        private void Clear()
        {
            Items.Clear();
            Tags.Clear();
            Tags.Add("");
            Refresh();
        }

        /// <summary>
        /// 补齐 Tag 槽位，保证与物品数量对齐
        /// </summary>
        public void EnsureTagSlots()
        {
            while (Tags.Count < Items.Count)
                Tags.Add("");
        }

        /// <summary>
        /// 按当前物品组刷新槽位显示
        /// </summary>
        public void Refresh()
        {
            MultiItemVisibility = Owner.MultiSelect && Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (Items.Count == 0)
            {
                Image = GetEmptyImage();
                ToolTipText = null;
                return;
            }
            Image = Items[0].ImagePath ?? GetEmptyImage();
            ToolTipText = Owner.MultiSelect && Items.Count > 1 ? "物品组，左击编辑" : Items[0].IDAndName;
        }
        #endregion
    }
}
