using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace cbhk.Generators.EntityGenerator.Components
{
    public partial class ComponentEvents
    {
        /// <summary>
        /// 首次获得焦点时执行绑定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ValueChangedHandler(object sender, RoutedEventArgs e)
        {
            #region 共通变量
            Grid parentGrid = (sender as FrameworkElement).Parent as Grid;
            if (parentGrid == null) return;
            ScrollViewer scrollViewer = parentGrid.Parent as ScrollViewer;
            TextTabItems parent = scrollViewer.Parent as TextTabItems;
            int currentIndex = 0;
            EntityPages entityPages = parent.FindParent<EntityPages>();
            if (entityPages == null) return;
            EntityPagesDataContext specialContext = entityPages.DataContext as EntityPagesDataContext;
            PropertyPath propertyPath = null;
            Binding valueBinder = new()
            {
                Mode = BindingMode.OneWayToSource,
                Converter = new TagToString()
            };
            if (!specialContext.SpecialTagsResult.ContainsKey(specialContext.SelectedEntityIdString))
                specialContext.SpecialTagsResult.Add(specialContext.SelectedEntityIdString, new System.Collections.ObjectModel.ObservableCollection<NBTDataStructure>());
            #endregion

            #region 是否为穿戴装备的掉率
            if(sender is Accordion && (sender as FrameworkElement).Name == "ArmorDropChances")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddPassengerClick);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearPassengerClick);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += ArmorDropChances_LostFocus;

                specialContext.CommonResult.Add(dataStructure);
                currentIndex = specialContext.CommonResult.Count - 1;
                propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为穿戴装备
            if (sender is Accordion && (sender as FrameworkElement).Name == "ArmorItems")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddPassengerClick);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearPassengerClick);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += ArmorItems_LostFocus;

                specialContext.CommonResult.Add(dataStructure);
                currentIndex = specialContext.CommonResult.Count - 1;
                propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为双手装备的掉率
            if (sender is Accordion && (sender as FrameworkElement).Name == "HandDropChances")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddPassengerClick);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearPassengerClick);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += HandDropChances_LostFocus;

                specialContext.CommonResult.Add(dataStructure);
                currentIndex = specialContext.CommonResult.Count - 1;
                propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为双手装备
            if (sender is Accordion && (sender as FrameworkElement).Name == "HandItems")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddPassengerClick);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearPassengerClick);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += HandItems_LostFocus;

                specialContext.CommonResult.Add(dataStructure);
                currentIndex = specialContext.CommonResult.Count - 1;
                propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为乘客
            if (sender is Accordion && (sender as FrameworkElement).Name == "Passengers")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddPassengerClick);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearPassengerClick);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += PassengerItems_LostFocus;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult["+ specialContext.SelectedEntityIdString + "][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为实体属性
            if (sender is Accordion && (sender as FrameworkElement).Name == "Attributes")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddAttributeCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearAttributeCommand);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += Attributes_LostFocus;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    string selectedEntityIdString = specialContext.SelectedEntityIdString;
                    propertyPath = new PropertyPath("SpecialTagsResult["+ selectedEntityIdString + "][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为药水云施加状态效果列表
            if (sender is Accordion && (sender as FrameworkElement).Name == "Effects")
            {
                Accordion areaEffectCloudEffectsAccordion = sender as Accordion;
                areaEffectCloudEffectsAccordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = areaEffectCloudEffectsAccordion.Tag as NBTDataStructure;
                areaEffectCloudEffectsAccordion.LostFocus += AreaEffectCloudEffects_LostFocus;
                areaEffectCloudEffectsAccordion.Modify = new RelayCommand<FrameworkElement>(AddAreaEffectCloudCommand);
                areaEffectCloudEffectsAccordion.Fresh = new RelayCommand<FrameworkElement>(ClearAreaEffectCloudCommand);
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult["+ specialContext.SelectedEntityIdString + "][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = areaEffectCloudEffectsAccordion.Tag;
                BindingOperations.SetBinding(areaEffectCloudEffectsAccordion, FrameworkElement.TagProperty, valueBinder);
                areaEffectCloudEffectsAccordion.Tag = currentTag;
            }
            #endregion

            #region 实体状态效果列表
            if (sender is Accordion && (sender as FrameworkElement).Name == "ActiveEffects")
            {
                Accordion areaEffectCloudEffectsAccordion = sender as Accordion;
                areaEffectCloudEffectsAccordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = areaEffectCloudEffectsAccordion.Tag as NBTDataStructure;
                areaEffectCloudEffectsAccordion.LostFocus += ActiveEffects_LostFocus;
                areaEffectCloudEffectsAccordion.Modify = new RelayCommand<FrameworkElement>(AddAreaEffectCloudCommand);
                areaEffectCloudEffectsAccordion.Fresh = new RelayCommand<FrameworkElement>(ClearAreaEffectCloudCommand);
                specialContext.CommonResult.Add(dataStructure);
                currentIndex = specialContext.CommonResult.Count - 1;
                propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                valueBinder.Path = propertyPath;
                var currentTag = areaEffectCloudEffectsAccordion.Tag;
                BindingOperations.SetBinding(areaEffectCloudEffectsAccordion, FrameworkElement.TagProperty, valueBinder);
                areaEffectCloudEffectsAccordion.Tag = currentTag;
            }
            #endregion

            #region 是否为拴绳相关数据
            if (sender is Accordion && (sender as Accordion).Name == "Leash")
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += Leash_LostFocus;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    accordion.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    accordion.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("SpecialTagsResult[" + specialContext.SelectedEntityIdString + "][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为可疑实体列表
            if (sender is SuspectsEntities)
            {
                SuspectsEntities suspectsEntities = sender as SuspectsEntities;
                NBTDataStructure dataStructure = suspectsEntities.Tag as NBTDataStructure;
                suspectsEntities.GotFocus -= ValueChangedHandler;
                suspectsEntities.LostFocus += SuspectsEntities_LostFocus;
                specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                suspectsEntities.Uid = currentIndex.ToString();
                propertyPath = new("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = suspectsEntities.Tag;
                BindingOperations.SetBinding(suspectsEntities, FrameworkElement.TagProperty, valueBinder);
                suspectsEntities.Tag = currentTag;
            }
            #endregion

            #region 是否为振动监听器
            if (sender is VibrationMonitors)
            {
                VibrationMonitors vibrationMonitors = sender as VibrationMonitors;
                vibrationMonitors.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = vibrationMonitors.Tag as NBTDataStructure;
                vibrationMonitors.LostFocus += VibrationMonitors_LostFocus;
                specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                vibrationMonitors.Uid = currentIndex.ToString();
                propertyPath = new("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");

                valueBinder.Path = propertyPath;
                var currentTag = vibrationMonitors.Tag;
                BindingOperations.SetBinding(vibrationMonitors, FrameworkElement.TagProperty, valueBinder);
                vibrationMonitors.Tag = currentTag;
            }
            #endregion

            #region 是否为背包
            if (sender is Accordion && ((sender as FrameworkElement).Name == "Item" || (sender as FrameworkElement).Name == "Items" || (sender as FrameworkElement).Name == "Inventory"))
            {
                Accordion accordion = sender as Accordion;
                accordion.GotFocus -= ValueChangedHandler;
                accordion.Modify = new RelayCommand<FrameworkElement>(AddItemCommand);
                accordion.Fresh = new RelayCommand<FrameworkElement>(ClearItemCommand);
                NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
                accordion.LostFocus += Item_LostFocus;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    accordion.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    accordion.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = accordion.Tag;
                BindingOperations.SetBinding(accordion, FrameworkElement.TagProperty, valueBinder);
                accordion.Tag = currentTag;
            }
            #endregion

            #region 是否为坐标或UID
            if (sender is UUIDOrPosGroup)
            {
                UUIDOrPosGroup uUIDOrPosGroup = sender as UUIDOrPosGroup;
                uUIDOrPosGroup.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = uUIDOrPosGroup.Tag as NBTDataStructure;
                uUIDOrPosGroup.LostFocus += UUIDOrPosGroup_LostFocus;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    uUIDOrPosGroup.Uid = currentIndex.ToString();
                    propertyPath = new("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    uUIDOrPosGroup.Uid = currentIndex.ToString();
                    propertyPath = new("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = uUIDOrPosGroup.Tag;
                BindingOperations.SetBinding(uUIDOrPosGroup, FrameworkElement.TagProperty, valueBinder);
                uUIDOrPosGroup.Tag = currentTag;
            }
            #endregion

            #region 是否为方块状态
            if (sender is BlockState)
            {
                BlockState blockState = sender as BlockState;
                blockState.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = blockState.Tag as NBTDataStructure;
                blockState.MouseLeave += BlockState_MouseLeave;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    blockState.Uid = currentIndex.ToString();
                    propertyPath = new("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    blockState.Uid = currentIndex.ToString();
                    propertyPath = new("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = blockState.Tag;
                BindingOperations.SetBinding(blockState, FrameworkElement.TagProperty, valueBinder);
                blockState.Tag = currentTag;
            }
            #endregion

            #region 是否为网格
            if (sender is Grid)
            {
                Grid grid = sender as Grid;
                grid.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = grid.Tag as NBTDataStructure;
                if (dataStructure.DataType == "TAG_Float_Array")
                {
                    string unit = "";
                    unit = grid.Uid[(grid.Uid.IndexOf('_') + 1)..grid.Uid.LastIndexOf('_')].ToLower()[0..1].Replace("i", "");
                    Slider subSlider0 = grid.FindChild<Slider>("0");
                    Slider subSlider1 = grid.FindChild<Slider>("1");
                    dataStructure.Result = "\"" + grid.Name + "\":[" + subSlider0.Value + unit + "," + subSlider1 + unit + "]";
                    grid.LostFocus += Grid_LostFocus;
                }
                else//路径引用
                if(dataStructure.DataType == "TAG_StringReference")
                {
                    (grid.Children[1] as IconTextButtons).Click += StringReference_Clicked;
                }

                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = grid.Tag;
                BindingOperations.SetBinding(grid, FrameworkElement.TagProperty, valueBinder);
                grid.Tag = currentTag;
            }
            #endregion

            #region 当前为数字框
            if (sender is Slider)
            {
                Slider slider = sender as Slider;
                slider.GotFocus -= ValueChangedHandler;
                NBTDataStructure dataStructure = slider.Tag as NBTDataStructure;
                slider.ValueChanged += NumberBoxValueChanged;
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
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

                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(textBox.Tag as NBTDataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(textBox.Tag as NBTDataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }

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

                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }

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
                if (parent.Uid != "SpecialTags")
                {
                    specialContext.CommonResult.Add(dataStructure);
                    currentIndex = specialContext.CommonResult.Count - 1;
                    comboBox.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("CommonResult[" + currentIndex + "]");
                }
                else
                {
                    specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Add(dataStructure);
                    currentIndex = specialContext.SpecialTagsResult[specialContext.SelectedEntityIdString].Count - 1;
                    comboBox.Uid = currentIndex.ToString();
                    propertyPath = new PropertyPath("SpecialTagsResult[specialContext.SelectedEntityIdString][" + currentIndex + "]");
                }
                valueBinder.Path = propertyPath;
                var currentTag = comboBox.Tag;
                BindingOperations.SetBinding(comboBox, FrameworkElement.TagProperty, valueBinder);
                comboBox.Tag = currentTag;
            }
            #endregion
        }

        /// <summary>
        /// 双手装备掉率合并数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandDropChances_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            HandDropChances handDropChances = (accordion.Content as ScrollViewer).Content as HandDropChances;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (handDropChances.EnableButton.IsChecked.Value)
                dataStructure.Result = handDropChances.Result;
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 双手装备合并数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandItems_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            HandItems handItems = (accordion.Content as ScrollViewer).Content as HandItems;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (handItems.EnableButton.IsChecked.Value)
                dataStructure.Result = handItems.Result;
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 穿戴盔甲掉率合并数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArmorDropChances_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            ArmorDropChances armorDropChances = (accordion.Content as ScrollViewer).Content as ArmorDropChances;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (armorDropChances.EnableButton.IsChecked.Value)
                dataStructure.Result = armorDropChances.Result;
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 穿戴盔甲合并数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArmorItems_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            ArmorItems armorItems = (accordion.Content as ScrollViewer).Content as ArmorItems;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (armorItems.EnableButton.IsChecked.Value)
                dataStructure.Result = armorItems.Result;
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 添加乘客
        /// </summary>
        /// <param name="sender"></param>
        private void AddPassengerClick(FrameworkElement sender)
        {
            Accordion accordion = sender as Accordion;
            EntityPagesDataContext context = accordion.FindParent<EntityPages>().DataContext as EntityPagesDataContext;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            context.CommonResult.Add(new NBTDataStructure());
            int currentIndex = context.CommonResult.Count - 1;
            Binding valueBinder = new()
            {
                Path = new PropertyPath("CommonResult[" + currentIndex + "]"),
                Mode = BindingMode.OneWayToSource
            };
            PassengerItems passengerItems = new()
            {
                Margin = new Thickness(0, 2, 0, 0),
                Tag = new NBTDataStructure() { Result = "", DataType = "TAG_List", NBTGroup = "EntityCommonTags" }
            };
            passengerItems.ReferenceIndex.Minimum = 0;
            var currentTag = passengerItems.Tag;
            BindingOperations.SetBinding(passengerItems, FrameworkElement.TagProperty, valueBinder);
            passengerItems.Tag = currentTag;
            stackPanel.Children.Add(passengerItems);
        }

        /// <summary>
        /// 清空乘客
        /// </summary>
        /// <param name="sender"></param>
        private void ClearPassengerClick(FrameworkElement sender)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
            dataStructure.Result = "";
        }

        /// <summary>
        /// 处理乘客数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void PassengerItems_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            StackPanel stackPanel =(accordion.Content as ScrollViewer).Content as StackPanel;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            dataStructure.Result = "";
            if (stackPanel.Children.Count > 0)
            {
                StringBuilder result = new();
                foreach (PassengerItems passenger in stackPanel.Children)
                {
                    if (passenger.DisplayEntity.Tag != null)
                        result.Append(passenger.DisplayEntity.Tag.ToString() + ',');
                }
                if (result.Length > 0)
                {
                    result = result.Remove(result.Length - 1, 1);
                    dataStructure.Result = "Passengers:[" + result + "]";
                }
            }
        }

        /// <summary>
        /// 处理状态效果数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ActiveEffects_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StringBuilder result = new();
            foreach (AreaEffectCloudEffects effect in stackPanel.Children)
            {
                result.Append(effect.Result + ',');
            }
            if (result.Length > 0)
            {
                result = result.Remove(result.Length - 1, 1);
                dataStructure.Result = accordion.Name + ":[" + result.ToString().TrimEnd(',') + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="obj"></param>
        public void AddAttributeCommand(FrameworkElement obj)
        {
            Attributes attributes = new();
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Add(attributes);
        }

        /// <summary>
        /// 处理实体属性数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Attributes_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            StringBuilder result = new();
            if (stackPanel.Children.Count > 0)
            {
                foreach (Attributes attribute in stackPanel.Children)
                {
                    result.Append(attribute.Result + ",");
                }
                result.Remove(result.Length - 1, 1);
                dataStructure.Result = "Attributes:[" + result.ToString() + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 清空属性
        /// </summary>
        /// <param name="obj"></param>
        private void ClearAttributeCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Clear();
            dataStructure.Result = "";
        }

        /// <summary>
        /// 处理拴绳相关数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Leash_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            LeashData leashData = accordion.Content as LeashData;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (leashData.Tied.IsChecked.Value)
            {
                if(leashData.TiedByEntity.IsChecked.Value)
                    dataStructure.Result = accordion.Name + ":{UUID:[" + leashData.tractor.number0.Value + "," + leashData.tractor.number1.Value + "," + leashData.tractor.number2.Value + "," + leashData.tractor.number3.Value + "]}";
                else
                    dataStructure.Result = accordion.Name + ":{X:" + leashData.fence.number0.Value + ",Y:" + leashData.fence.number1.Value + ",Z:" + leashData.fence.number2.Value + "}";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 整合引用路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StringReference_Clicked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                AddExtension = true,
                CheckFileExists = true,
                DefaultExt = ".json",
                Multiselect = false,
                Filter = "(*.json)|*.json;",
                RestoreDirectory = true,
                Title = "请选择一个JSON文件"
            };
            if(openFileDialog.ShowDialog().Value)
            {
                if(File.Exists(openFileDialog.FileName))
                {
                    Grid grid = (sender as IconTextButtons).Parent as Grid;
                    NBTDataStructure dataStructure = grid.Tag as NBTDataStructure;
                    TextBox textBox = grid.Children[0] as TextBox;
                    textBox.Text = openFileDialog.FileName;
                    dataStructure.Result = grid.Name + ":\"" + textBox.Text + "\"";
                }
            }
        }

        /// <summary>
        /// 清空药水云施加效果列表
        /// </summary>
        /// <param name="obj"></param>
        private void ClearAreaEffectCloudCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            dataStructure.Result = "";
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Clear();
        }

        /// <summary>
        /// 添加药水云施加效果
        /// </summary>
        /// <param name="obj"></param>
        public void AddAreaEffectCloudCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Add(new AreaEffectCloudEffects());
        }

        /// <summary>
        /// 处理药水云施加状态效果列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AreaEffectCloudEffects_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if (stackPanel.Children.Count > 0)
            {
                StringBuilder result = new();
                foreach (AreaEffectCloudEffects item in stackPanel.Children)
                {
                    result.Append(item.Result + ",");
                }
                dataStructure.Result = accordion.Name + ":[" + result.ToString().TrimEnd(',') + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 处理背包或物品数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_LostFocus(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            List<string> itemList = new();
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            if (stackPanel.Children.Count > 0)
            {
                if (accordion.Uid == "TAG_List")
                {
                    for (int i = 0; i < stackPanel.Children.Count; i++)
                    {
                        EntityBag entityBag = stackPanel.Children[i] as EntityBag;
                        itemList.Add(entityBag.ItemIcon.Tag.ToString());
                    }
                    dataStructure.Result = accordion.Name + ":[" + string.Join(",", itemList) + "]";
                }
                else
                {
                    object obj = (stackPanel.Children[0] as EntityBag).ItemIcon.Tag;
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
        /// 枚举变量切换时更新数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnumBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            NBTDataStructure dataStructure = comboBox.Tag as NBTDataStructure;
            dataStructure.Result = comboBox.Name + ":\"" + comboBox.SelectedItem.ToString() + "\"";
        }

        /// <summary>
        /// 清空背包中的物品
        /// </summary>
        /// <param name="obj"></param>
        private void ClearItemCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            NBTDataStructure dataStructure = accordion.Tag as NBTDataStructure;
            dataStructure.Result = "";
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }

        /// <summary>
        /// 为背包添加物品
        /// </summary>
        /// <param name="obj"></param>
        private void AddItemCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            EntityBag entityBag = new();
            if(accordion.Name == "Items" || accordion.Name == "Inventory" || (stackPanel.Children.Count == 0 && accordion.Name == "Item"))
            stackPanel.Children.Add(entityBag);
        }

        /// <summary>
        /// 可疑实体列表数据更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SuspectsEntities_LostFocus(object sender, RoutedEventArgs e)
        {
            SuspectsEntities suspectsEntities = sender as SuspectsEntities;
            NBTDataStructure dataStructure = suspectsEntities.Tag as NBTDataStructure;
            Accordion accordion = suspectsEntities.FindChild<Accordion>();
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if (stackPanel.Children.Count > 0)
            {
                string result = "";
                foreach (StackPanel item in stackPanel.Children)
                {
                    if(item.Tag != null)
                    result += item.Tag.ToString() + ",";
                }
                result = result.TrimEnd(',');
                dataStructure.Result = "anger:{suspects:[" + result + "]}";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 振动传感器数据更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VibrationMonitors_LostFocus(object sender, RoutedEventArgs e)
        {
            VibrationMonitors vibrationMonitors = sender as VibrationMonitors;
            NBTDataStructure dataStructure = vibrationMonitors.Tag as NBTDataStructure;
            if (vibrationMonitors.VibrationMonitorsEnableButton.IsChecked.Value)
            {
                TabControl tabControl = vibrationMonitors.FindChild<TabControl>();
                StringBuilder result = new();
                for (int i = 0; i < tabControl.Items.Count; i++)
                {
                    FrameworkElement control = tabControl.Items[i] as FrameworkElement;
                    if (control.Tag != null)
                        result.Append(control.Tag.ToString() + ",");
                }
                string eventResult = "event_delay:" + vibrationMonitors.EventDelay.Value + ",";
                string rangeResult = "range:" + vibrationMonitors.Range.Value + ",";
                string content = result.ToString().TrimEnd(',').Length > 0 ? result.ToString().TrimEnd(',') : "";
                content = eventResult + rangeResult + content;
                content = content.TrimEnd(',');
                dataStructure.Result = vibrationMonitors.Name + ":{" + content + "}";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// UID或坐标组数据更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UUIDOrPosGroup_LostFocus(object sender, RoutedEventArgs e)
        {
            UUIDOrPosGroup uUIDOrPosGroup = sender as UUIDOrPosGroup;
            NBTDataStructure dataStructure = uUIDOrPosGroup.Tag as NBTDataStructure;
            if (uUIDOrPosGroup.EnableButton.IsChecked.Value)
            {
                string unit = dataStructure.DataType.Replace("TAG_", "").ToLower()[0..1].Replace("i", "").Replace("u","");
                dataStructure.Result = uUIDOrPosGroup.Name + ":[" + uUIDOrPosGroup.number0.Value + unit + "," + uUIDOrPosGroup.number1.Value + unit + "," + uUIDOrPosGroup.number2.Value + unit + (dataStructure.DataType == "TAG_UUID" ? "," + uUIDOrPosGroup.number3.Value + unit : "") + "]";
            }
            else
                dataStructure.Result = "";
        }

        /// <summary>
        /// 方块状态数据更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockState_MouseLeave(object sender, MouseEventArgs e)
        {
            BlockState blockState = sender as BlockState;
            NBTDataStructure dataStructure = blockState.Tag as NBTDataStructure;
            StringBuilder AttributeContent = new();
            int value = 0;
            bool IsInt;
            string blockID = (blockState.BlockIdBox.SelectedItem as IconComboBoxItem).ComboBoxItemId;
            int index = 0;
            string currentValue = "";
            _ = blockState.SelectedAttributeKeys.All(item =>
            {
                if(item != null && item.Length > 0 && blockState.SelectedAttributeValues.Count > 0 && index < blockState.SelectedAttributeValues.Count)
                {
                    currentValue = blockState.SelectedAttributeValues[index];
                    _ = bool.TryParse(currentValue, out bool IsBool);
                    IsInt = int.TryParse(currentValue, out value);
                    if (!IsBool && !IsInt)
                        currentValue = "\"" + currentValue + "\"";
                    AttributeContent.Append(item + ":" + currentValue + ",");
                    index++;
                    return true;
                }
                return false;
            });
            dataStructure.Result = blockState.Name + ":{Name:\"" + blockID + "\",Properties:{" + AttributeContent.ToString().TrimEnd(',') + "}";
        }

        /// <summary>
        /// 数值型控件值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberBoxValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            NBTDataStructure dataStructure = slider.Tag as NBTDataStructure;
            if (slider.Value == 0)
            {
                dataStructure.Result = "";
            }
            else
                dataStructure.Result = slider.Name + ":" + slider.Value + dataStructure.DataType.Replace("TAG_","").ToLower()[0..1].Replace("i", "");
        }

        /// <summary>
        /// 更新网格内所携带所有对象的值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_LostFocus(object sender,RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            NBTDataStructure dataStructure = grid.Tag as NBTDataStructure;

            #region 两个浮点数成员
            Slider subSlider0 = grid.FindChild<Slider>("0");
            Slider subSlider1 = grid.FindChild<Slider>("1");
            if (subSlider0 != null && subSlider1 != null)
            {
                if (subSlider0.Value != 0 || subSlider1.Value != 0)
                    dataStructure.Result = grid.Name + ":[" + subSlider0.Value + "f," + subSlider1.Value+ "f]";
                else
                    dataStructure.Result = "";
            }
            #endregion
        }

        /// <summary>
        /// 文本框文本值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StringBox_LostFocus(object sender, RoutedEventArgs e)
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
        private void StringListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
            if(textBox.Text.Length == 0)
                dataStructure.Result = "";
            else
            {
                List<string> valueList = textBox.Text.Split(',').ToList();
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
        private void LongNumberBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            NBTDataStructure dataStructure = textBox.Tag as NBTDataStructure;
            if (textBox.Text.Trim().Length > 0)
                dataStructure.Result = textBox.Name + ":" + textBox.Text + "l";
            else
                dataStructure.Result = "";
            EntityPagesDataContext context = textBox.FindParent<EntityPages>().DataContext as EntityPagesDataContext;
        }

        /// <summary>
        /// 切换按钮取消选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
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
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
            NBTDataStructure dataStructure = textCheckBoxs.Tag as NBTDataStructure;
            dataStructure.Result = textCheckBoxs.Name + ":1b";
        }
    }

    /// <summary>
    /// Tag转字符串
    /// </summary>
    public class TagToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (NBTDataStructure)value;
    }

    public class NBTDataStructure:FrameworkElement
    {
        public string Result { get; set; }

        public string DataType { get; set; }

        /// <summary>
        /// 标记当前实例属于哪个共通标签
        /// </summary>
        public string NBTGroup { get; set; }
    }
}