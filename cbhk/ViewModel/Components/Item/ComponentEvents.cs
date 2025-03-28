using CBHK.ControlsDataContexts;
using CBHK.CustomControls;
using CBHK.CustomControls.Interfaces;
using CBHK.GeneralTools;
using CBHK.Generators.ItemGenerator;
using CBHK.Generators.ItemGenerator.Components;
using CBHK.Generators.ItemGenerator.Components.SpecialNBT;
using CBHK.ViewModel.Generators;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CBHK.ViewModel.Components.Item
{
    public class ComponentEvents
    {
        private static DataTable BlockTable = null;

        /// <summary>
        /// 首次获得焦点时执行绑定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ValueChangedHandler(object sender, RoutedEventArgs e)
        {
            #region 共通变量
            if ((sender as FrameworkElement).Parent is not Grid parentGrid) return;
            ScrollViewer scrollViewer = parentGrid.Parent as ScrollViewer;
            TextTabItems parent = scrollViewer.Parent as TextTabItems;
            int currentIndex = 0;
            ItemPageViewModel context = parent.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            PropertyPath propertyPath = null;
            Binding valueBinder = new()
            {
                Mode = BindingMode.OneWayToSource,
                Converter = new TagToString()
            };
            if (!context.SpecialTagsResult.ContainsKey(context.SelectedItemId.ComboBoxItemId))
                context.SpecialTagsResult.Add(context.SelectedItemId.ComboBoxItemId, []);
            #endregion

            #region 是否为谜之炖菜状态效果
            if (sender is Accordion && (sender as FrameworkElement).Uid == "StewEffectList")
            {
                Accordion accordion = sender as Accordion;

                //加入版本更新队列
                if (!context.VersionNBTList.ContainsKey(accordion))
                    context.VersionNBTList.Add(accordion, StewEffectList_LostFocus);
                accordion.Modify = new RelayCommand<FrameworkElement>(AddStewEffectCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearStewEffectCommand);
                accordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += StewEffectList_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为地图装饰器
            if (sender is Accordion && (sender as FrameworkElement).Uid == "MapDecorations")
            {
                Accordion accordion = sender as Accordion;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddMapDecorationsCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearMapDecorationsCommand);
                accordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += MapDecorations_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为地图属性
            if (sender is Accordion && (sender as FrameworkElement).Uid == "MapDisplay")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += MapDisplay_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为盾牌上的旗帜
            if (sender is Accordion && (sender as FrameworkElement).Uid == "BannerBlockEntityTag")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += ShieldBlockEntityTag_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为自定义药水效果列表
            if (sender is Accordion && (sender as FrameworkElement).Uid == "CustomPotionEffectList")
            {
                Accordion accordion = sender as Accordion;
                //加入版本更新队列
                if (!context.VersionNBTList.ContainsKey(accordion))
                    context.VersionNBTList.Add(accordion, CustomPotionEffects_LostFocus);
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddCustomPotionEffectCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearCustomPotionEffectsCommand);

                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += CustomPotionEffects_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为引用的命名空间
            if (sender is Accordion && (sender as FrameworkElement).Uid == "NameSpaceReference")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddNameSpaceReference);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearNameSpaceReference);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += NameSpaceReference_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为附魔列表
            if (sender is Accordion && (sender as FrameworkElement).Uid == "StoredEnchantments")
            {
                Accordion accordion = sender as Accordion;
                //加入版本更新队列
                if (!context.VersionNBTList.ContainsKey(accordion))
                    context.VersionNBTList.Add(accordion, StoredEnchantments_LostFocus);
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddEnchantment);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearEnchantments);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += StoredEnchantments_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为调试属性
            if (sender is Accordion && (sender as Accordion).Uid == "DebugProperty")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddDebugProperty);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearDebugProperties);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += DebugProperties_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为背包
            if (sender is Accordion && (sender as FrameworkElement).Uid == "Items")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddItemCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearItemCommand);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += Item_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为指向的坐标
            if (sender is Accordion && (sender as FrameworkElement).Uid == "LodestonePos")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.ModifyVisibility = Visibility.Collapsed;
                accordion.FreshVisibility = Visibility.Collapsed;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += LodestonePos_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 当前为数字框
            if (sender is Slider)
            {
                Slider slider = sender as Slider;
                slider.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = slider.Tag as NBTDataStructure;
                slider.ValueChanged += NumberBoxValueChanged;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = slider.Tag;
                BindingOperations.SetBinding(slider, FrameworkElement.TagProperty, valueBinder);
                slider.Tag = currentTag;
            }
            #endregion

            #region 当前为Long型、文本框
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                textBox.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
                if (dataStructure.DataType == "TAG_String_List")
                    textBox.LostFocus += StringListBox_LostFocus;
                else
                if (dataStructure.DataType == "TAG_Long")
                    textBox.LostFocus += LongNumberBox_LostFocus;
                else
                    textBox.LostFocus += StringBox_LostFocus;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(textBox.Tag as NBTDataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currengTag = dataStructure;
                BindingOperations.SetBinding(textBox, FrameworkElement.TagProperty, valueBinder);
                textBox.Tag = currengTag;
            }
            #endregion

            #region 当前为是否框
            if (sender is TextCheckBoxs)
            {
                TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
                textCheckBoxs.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = textCheckBoxs.Tag as NBTDataStructure;
                textCheckBoxs.Checked += CheckBox_Checked;
                textCheckBoxs.Unchecked += CheckBox_Unchecked;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = textCheckBoxs.Tag;
                BindingOperations.SetBinding(textCheckBoxs, FrameworkElement.TagProperty, valueBinder);
                textCheckBoxs.Tag = currentTag;
            }
            #endregion

            #region 当前为枚举值
            if (sender is ComboBox)
            {
                ComboBox comboBox = sender as ComboBox;
                comboBox.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = comboBox.Tag as NBTDataStructure;
                comboBox.SelectionChanged += EnumBox_SelectionChanged;

                context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Add(dataStructure);
                currentIndex = context.SpecialTagsResult[context.SelectedItemId.ComboBoxItemId].Count - 1;
                propertyPath = new PropertyPath("SpecialTagsResult[" + context.SelectedItemId.ComboBoxItemId + "][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = comboBox.Tag;
                BindingOperations.SetBinding(comboBox, FrameworkElement.TagProperty, valueBinder);
                comboBox.Tag = currentTag;
            }
            #endregion
        }

        /// <summary>
        /// 添加谜之炖菜效果
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddStewEffectCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            StackPanel stackPanel = scrollViewer.Content as StackPanel;
            SuspiciousStewEffects suspiciousStewEffects = new();
            stackPanel.Children.Add(suspiciousStewEffects);
            ItemPageViewModel itemPageDataContext = suspiciousStewEffects.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(suspiciousStewEffects);
        }

        /// <summary>
        /// 清空谜之炖菜效果
        /// </summary>
        /// <param name="obj"></param>
        private void ClearStewEffectCommand(FrameworkElement obj)
        {
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            Accordion accordion = obj as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            StackPanel stackPanel = scrollViewer.Content as StackPanel;
            for (int i = 0; i < stackPanel.Children.Count; i++)
            {
                itemPageDataContext.VersionComponents.Remove(stackPanel.Children[i] as SuspiciousStewEffects);
                stackPanel.Children.RemoveAt(i);
            }
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 合并谜之炖菜状态效果数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        public async void StewEffectList_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
                if (stackPanel.Children.Count > 0)
                {
                    StringBuilder result = new();
                    string effectString = "";
                    foreach (IVersionUpgrader suspiciousStewEffects in stackPanel.Children.Cast<IVersionUpgrader>())
                    {
                        effectString = await suspiciousStewEffects.Result();
                        result.Append(effectString + ",");
                    }
                    dataStructure.Result = "Effects:[" + result.ToString().Trim(',') + "]";
                }
                else
                    dataStructure.Result = "";
            });
        }

        /// <summary>
        /// 添加地图装饰器
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddMapDecorationsCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            StackPanel stackPanel = scrollViewer.Content as StackPanel;
            stackPanel.Children.Add(new MapDecorations());
        }

        /// <summary>
        /// 清空地图装饰器
        /// </summary>
        /// <param name="obj"></param>
        private void ClearMapDecorationsCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            StackPanel stackPanel = scrollViewer.Content as StackPanel;
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 合并地图装饰器数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void MapDecorations_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if (stackPanel.Children.Count > 0)
            {
                MapDecorations mapDecorations = stackPanel.Children[0] as MapDecorations;
                dataStructure.Result = accordion.Name + ":[" + mapDecorations.Result + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 合并地图显示属性数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MapDisplay_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if (stackPanel.Children.Count > 0)
            {
                MapDisplay mapDisplay = stackPanel.Children[0] as MapDisplay;
                dataStructure.Result = accordion.Name + ":{" + mapDisplay.Result + "}";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 合并盾牌实体方块数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShieldBlockEntityTag_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            ShieldBlockEntityTag shieldBlockEntityTag = stackPanel.Children[0] as ShieldBlockEntityTag;
            dataStructure.Result = shieldBlockEntityTag.Result;
        }

        /// <summary>
        /// 合并药水效果数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CustomPotionEffects_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
                string result = "";
                if (stackPanel.Children.Count > 0)
                {
                    int currentVersion = (accordion.FindParent<ItemPagesView>().DataContext as ItemPageViewModel).CurrentMinVersion;
                    string effectString = "";
                    foreach (IVersionUpgrader potion in stackPanel.Children.Cast<IVersionUpgrader>())
                    {
                        effectString = await potion.Result();
                        result += effectString + ",";
                    }
                    dataStructure.Result = (currentVersion < 1202 ? "CustomPotionEffects" : "custom_potion_effects") + ":[" + result.Trim(',') + "]";
                }
                else
                    dataStructure.Result = "";
            });
        }

        /// <summary>
        /// 清空药水效果列表
        /// </summary>
        /// <param name="obj"></param>
        public void ClearCustomPotionEffectsCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            dataStructure.Result = "";
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Clear();
        }

        /// <summary>
        /// 添加药水效果
        /// </summary>
        /// <param name="obj"></param>
        public void AddCustomPotionEffectCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            StackPanel stackPanel = scrollViewer.Content as StackPanel;
            CustomPotionEffects customPotionEffects = new();
            stackPanel.Children.Add(customPotionEffects);
            ItemPageViewModel itemPageDataContext = customPotionEffects.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(customPotionEffects);
        }

        /// <summary>
        /// 添加命名空间
        /// </summary>
        /// <param name="obj"></param>
        public void AddNameSpaceReference(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Add(new NameSpaceReference());
        }

        /// <summary>
        /// 清空命名空间
        /// </summary>
        /// <param name="obj"></param>
        public void ClearNameSpaceReference(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 合并命名空间数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NameSpaceReference_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            string result = "";
            if (stackPanel.Children.Count > 0)
            {
                foreach (NameSpaceReference nameSpaceReference in stackPanel.Children)
                {
                    result += "\"" + nameSpaceReference.ReferenceBox.Text + "\",";
                }
                dataStructure.Result = accordion.Name + ":[" + result.Trim(',') + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 添加附魔
        /// </summary>
        /// <param name="obj"></param>
        public void AddEnchantment(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            EnchantmentItems enchantmentItems = new();
            stackPanel.Children.Add(enchantmentItems);
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(enchantmentItems);
        }

        /// <summary>
        /// 清空附魔
        /// </summary>
        /// <param name="obj"></param>
        public void ClearEnchantments(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 合并附魔数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void StoredEnchantments_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
                string result = "";
                if (stackPanel.Children.Count > 0)
                {
                    string enchantString = "";
                    foreach (IVersionUpgrader enchantmentItems in stackPanel.Children.Cast<IVersionUpgrader>())
                    {
                        enchantString = await enchantmentItems.Result();
                        result += enchantString + ",";
                    }
                    dataStructure.Result = "StoredEnchantments:[" + result.Trim(',') + "]";
                }
                else
                    dataStructure.Result = "";
            });
        }

        /// <summary>
        /// 合并调试属性数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DebugProperties_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if (BlockTable is null)
            {
                ItemViewModel context = Window.GetWindow(accordion).DataContext as ItemViewModel;
                BlockTable = context.BlockTable;
            }
            string currentID;
            string result = "";
            if (stackPanel.Children.Count > 0)
            {
                foreach (DebugProperties debugProperties in stackPanel.Children)
                {
                    IconComboBoxItem selectedItem = debugProperties.BlockId.SelectedItem as IconComboBoxItem;
                    currentID = BlockTable.Select("name='" + selectedItem.ComboBoxItemText + "'").First()["id"].ToString();
                    if (debugProperties.BlockProperty.Items.Count > 0)
                        result += "\"" + currentID + "\":\"" + debugProperties.BlockProperty.SelectedItem.ToString() + "\",";
                }
                dataStructure.Result = "DebugProperty:{" + result.Trim(',') + "}";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 添加调试属性
        /// </summary>
        /// <param name="obj"></param>
        public void AddDebugProperty(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Add(new DebugProperties());
        }

        /// <summary>
        /// 清空调试属性
        /// </summary>
        /// <param name="obj"></param>
        public void ClearDebugProperties(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 合并指向坐标数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LodestonePos_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            UUIDOrPosGroup uUIDOrPosGroup = (accordion.Content as ScrollViewer).Content as UUIDOrPosGroup;
            if (uUIDOrPosGroup.EnableButton.IsChecked.Value)
                dataStructure.Result = accordion.Uid + ":{X:" + uUIDOrPosGroup.number0.Value + ",Y:" + uUIDOrPosGroup.number1.Value + ",Z:" + uUIDOrPosGroup.number2.Value + "}";
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 清空背包中的物品
        /// </summary>
        /// <param name="obj"></param>
        public void ClearItemCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel itemPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            itemPanel.Children.Clear();
            dataStructure.Result = "";
        }

        /// <summary>
        /// 为背包添加物品
        /// </summary>
        /// <param name="obj"></param>
        public void AddItemCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel itemPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            itemPanel.Children.Add(new InlineItems());
        }

        /// <summary>
        /// 处理背包或物品数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Item_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            StackPanel itemPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            List<string> itemList = [];
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (itemPanel.Children.Count > 0)
            {
                if (accordion.Uid == "TAG_List")
                {
                    for (int i = 0; i < itemPanel.Children.Count; i++)
                    {
                        FrameworkElement frameworkElement = itemPanel.Children[i] as FrameworkElement;
                        itemList.Add(frameworkElement.Tag.ToString());
                    }
                    dataStructure.Result = accordion.Name + ":[" + string.Join(",", itemList) + "]";
                }
                else
                {
                    object obj = (itemPanel.Children[0] as FrameworkElement).Tag;
                    if (obj != null)
                        dataStructure.Result = accordion.Name + ":" + obj.ToString();
                    else
                        dataStructure.Result = "";
                }
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 数值型控件值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NumberBoxValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            NBTDataStructure dataStructure = slider.Tag as NBTDataStructure;
            if (slider.Value == 0)
            {
                dataStructure.Result = "";
            }
            else
                dataStructure.Result = slider.Name + ":" + slider.Value + dataStructure.DataType.Replace("TAG_", "").ToLower()[0..1].Replace("i", "");
        }

        /// <summary>
        /// 文本框文本值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StringBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
            if (textBox.Text.Length == 0)
                dataStructure.Result = "";
            else
                dataStructure.Result = textBox.Name + ":\"" + textBox.Text + "\"";
        }

        /// <summary>
        /// 字符串数组文本更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StringListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
            if (textBox.Text.Length == 0)
                dataStructure.Result = "";
            else
            {
                List<string> valueList = [.. textBox.Text.Split(',')];
                StringBuilder result = new();
                _ = valueList.All(item =>
                {
                    result.Append("\"" + item + "\",");
                    return true;
                });
                dataStructure.Result = textBox.Name + ":[" + result.ToString().TrimEnd(',') + "]";
            }
        }

        /// <summary>
        /// long型数值文本更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LongNumberBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
            if (textBox.Text.Trim().Length > 0)
                dataStructure.Result = textBox.Name + ":" + textBox.Text + "l";
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 切换按钮取消选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
            NBTDataStructure dataStructure = textCheckBoxs.Tag as NBTDataStructure;
            dataStructure.Result = "";
        }

        /// <summary>
        /// 切换按钮选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
            NBTDataStructure dataStructure = textCheckBoxs.Tag as NBTDataStructure;
            dataStructure.Result = textCheckBoxs.Name + ":1b";
        }

        /// <summary>
        /// 枚举变量切换时更新数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnumBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            NBTDataStructure dataStructure = comboBox.Tag as NBTDataStructure;
            if (dataStructure.resultType == "String")
                dataStructure.Result = comboBox.Name + ":\"" + comboBox.SelectedItem.ToString() + "\"";
            else
            if (dataStructure.resultType == "Number")
                dataStructure.Result = comboBox.Name + ":" + double.Parse(comboBox.SelectedItem.ToString());
            else
            if (dataStructure.resultType == "Boolean")
                dataStructure.Result = comboBox.Name + ":" + bool.Parse(comboBox.SelectedItem.ToString());
            else
            if (dataStructure.resultType == "Index")
                dataStructure.Result = comboBox.Name + ":" + comboBox.SelectedIndex;
        }
    }

    /// <summary>
    /// Tag转字符串
    /// </summary>
    public class TagToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (NBTDataStructure)value;
        }
    }

    public class NBTDataStructure : FrameworkElement
    {
        /// <summary>
        /// 结果
        /// </summary>
        public string Result { get; set; }

        public string DataType { get; set; }
        public string resultType { get; set; }

        /// <summary>
        /// 标记当前实例属于哪个共通标签
        /// </summary>
        public string NBTGroup { get; set; }
    }
}