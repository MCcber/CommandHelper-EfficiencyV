using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using static cbhk.Model.Common.Enums;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public partial class CompoundJsonTreeViewItem : JsonTreeViewItem
    {
        #region Property
        public int EndLineNumber { get; set; }
        public DocumentLine EndLine { get; set; }

        /// <summary>
        /// 枚举键
        /// </summary>
        public string EnumKey { get; set; } = "";

        private bool isCanSort = false;

        public bool IsCanSort
        {
            get => isCanSort;
            set
            {
                SetProperty(ref isCanSort, value);
                if (isCanSort)
                {
                    SortButtonVisibility = Visibility.Visible;
                }
                else
                {
                    SortButtonVisibility = Visibility.Collapsed;
                }
            }
        }

        private DataTypes dataType = DataTypes.Compound;
        public new DataTypes DataType
        {
            get => dataType;
            set
            {
                SetProperty(ref dataType, value);
                SortButtonVisibility = RemoveElementButtonVisibility = BoolButtonVisibility = EnumBoxVisibility = ErrorIconVisibility = InfoIconVisibility = InputBoxVisibility = EnumBoxVisibility = Visibility.Collapsed;
                switch (dataType)
                {
                    case DataTypes.Input:
                    case DataTypes.String:
                    case DataTypes.Byte:
                    case DataTypes.Short:
                    case DataTypes.Int:
                    case DataTypes.Float:
                    case DataTypes.Double:
                    case DataTypes.Long:
                        InputBoxVisibility = Visibility.Visible;
                        break;
                    case DataTypes.BlockTag:
                    case DataTypes.ItemTag:
                    case DataTypes.EntityID:
                    case DataTypes.BlockID:
                    case DataTypes.ItemID:
                    case DataTypes.Enum:
                        EnumBoxVisibility = Visibility.Visible;
                        break;
                    case DataTypes.Bool:
                        BoolButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.NullableCompound:
                        RemoveElementButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.OptionalCompound:
                        AddElementButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.CustomCompound:
                        AddElementButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.List:
                        AddElementButtonVisibility = Visibility.Visible;
                        break;
                }
            }
        }

        [ObservableProperty]
        public Visibility _addElementButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _switchBoxVisibility = Visibility.Collapsed;

        /// <summary>
        /// 可切换的子节点集合
        /// </summary>
        [ObservableProperty]
        public List<string> _childrenStringList = [];

        /// <summary>
        /// 子节点集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _children = [];

        /// <summary>
        /// 平铺后代节点集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _flattenDescendantNodeList = [];

        public ObservableCollection<TextComboBoxItem> ValueTypeList { get; set; } = [];
        #endregion

        #region Field
        private IContainerProvider _container;
        #endregion

        #region Event
        /// <summary>
        /// 节点成员载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TreeItem_Loaded(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (sender as FrameworkElement).FindParent<TreeViewItem>();
            treeViewItem.IsExpanded = true;
        }

        /// <summary>
        /// 处理枚举值和数据类型的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnumType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region 替换当前枚举节点的值
            if (StartLine is null)
            {
                #region 定位上一个已有值的节点
                JsonTreeViewItem previous = Previous;
                while (previous is not null && previous.StartLine is null)
                {
                    if (previous.Previous is null)
                    {
                        break;
                    }
                    previous = previous.Previous;
                }
                #endregion

                if(previous is not null)
                {
                    Plan.UpdateNullValueBySpecifyingInterval(previous.StartLine.EndOffset, "\r\n" + new string(' ', LayerCount * 2) + "\"" + Key + "\":\"" + SelectedEnumItem.Text + "\"");
                }
                else
                if(Parent is not null)
                {
                    DocumentLine parentStartLine = Plan.GetLineByNumber(Parent.StartLine.LineNumber);
                    if(Parent.StartLine == Parent.EndLine || Parent.EndLine is null)
                    {
                        string parentStartLineText = Plan.GetRangeText(parentStartLine.Offset, parentStartLine.EndOffset - parentStartLine.Offset);
                        int index = parentStartLineText.IndexOf('{') + 1;
                        Plan.SetRangeText(index, 0, "\r\n" + new string(' ', LayerCount * 2) + "\"" + Key + "\":\"" + SelectedEnumItem.Text + "\"");
                    }
                }
                else
                {
                    Plan.SetRangeText(1, 0, "\r\n" + new string(' ', LayerCount * 2) + "\"" + Key + "\":\"" + SelectedEnumItem.Text + "\"");
                }
            }
            else
            {
                string currentLineText = Plan.GetRangeText(StartLine.Offset, StartLine.EndOffset - StartLine.Offset);
                int colonIndex = currentLineText.IndexOf(':') + 2;
                int length = 0;
                if(currentLineText.TrimEnd().EndsWith(','))
                {
                    length = currentLineText.TrimEnd().LastIndexOf(',') - colonIndex;
                }
                else
                {
                    length = currentLineText.Length - colonIndex;
                }

                bool isNotStringType = false;
                string quotationMarks = "\"";

                isNotStringType = bool.TryParse(SelectedEnumItem.Text, out bool boolValue) || decimal.TryParse(SelectedEnumItem.Text, out decimal decimalValue);
                if(isNotStringType)
                {
                    quotationMarks = "";
                }

                if (colonIndex > 0)
                {
                    Plan.SetRangeText(StartLine.Offset + colonIndex, length, quotationMarks + SelectedEnumItem.Text + quotationMarks);
                }
            }
            #endregion

            #region 判断是否需要应用枚举结构
            if (EnumKey.Length > 0)
            {
                if (Plan.EnumCompoundDataDictionary.TryGetValue(EnumKey, out Dictionary<string, List<string>> targetDependencyDictionary) && targetDependencyDictionary.TryGetValue(SelectedEnumItem.Text, out List<string> targetRawList))
                {
                    HtmlHelper htmlHelper = _container.Resolve<HtmlHelper>();
                    htmlHelper.plan = Plan;
                    htmlHelper.jsonTool = JsonItemTool;
                    JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), targetRawList, LayerCount);
                    Parent.Children.AddRange(result.Result);
                    Plan.SetRangeText(StartLine.EndOffset, 0, ",\r\n"+ result.ResultString.ToString());
                    JsonItemTool.SetLineNumbersForEachItem(result.Result, Parent, true);
                }
            }
            #endregion

            oldSelectedEnumItem = SelectedEnumItem;
        }

        /// <summary>
        /// 添加数组元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddSubStructure_Click(object sender, RoutedEventArgs e) => JsonItemTool.AddSubStructure(this);

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RemoveSubStructure_Click(object sender, RoutedEventArgs e) => JsonItemTool.RemoveSubStructure(this);

        /// <summary>
        /// 向上移动数组元素节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MoveItemUp_Click(object sender, RoutedEventArgs e)
        {
            MoveItem(false);
        }

        /// <summary>
        /// 向下移动数组元素节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MoveItemDown_Click(object sender, RoutedEventArgs e)
        {
            MoveItem(true);
        }
        #endregion

        #region Methods
        public CompoundJsonTreeViewItem(ICustomWorldUnifiedPlan plan, IJsonItemTool jsonItemTool,IContainerProvider containerProvider)
        {
            Plan = plan;
            JsonItemTool = jsonItemTool;
            _container = containerProvider;
        }

        /// <summary>
        /// 移动数组元素节点
        /// </summary>
        /// <param name="IsDown"></param>
        private void MoveItem(bool IsDown)
        {
            #region 初始化并更新前后关系
            if ((IsDown && Next is null) || (!IsDown && Previous is null))
            {
                return;
            }
            int nearbyOffset = 0, nearbyLength = 0, offset = 0, length = 0, span = 0, nearbySpan = 0, startlineNumber = 0, endlineNumber = 0, nearbyStartLineNumber = 0, nearbyEndLineNumber = 0;
            string currentLineText = "", nearbyLineText = "";
            CompoundJsonTreeViewItem addToButtomItem = Parent.Children[^1] as CompoundJsonTreeViewItem;
            Parent.Children.Remove(addToButtomItem);
            CompoundJsonTreeViewItem nearbyItem = (IsDown ? Next : Previous) as CompoundJsonTreeViewItem;
            int index = Parent.Children.IndexOf(this);
            int nearbyIndex = Parent.Children.IndexOf(nearbyItem);

            JsonTreeViewItem nextItem = Next;
            JsonTreeViewItem nextNextItem = null;
            if (Next is not null)
                nextNextItem = Next.Next;
            JsonTreeViewItem lastItem = Previous;
            JsonTreeViewItem lastLastItem = null;
            if (Previous is not null)
                lastLastItem = Previous.Previous;
            #endregion

            #region 提前保存子结构节点集的旧行号
            //foreach (var item in FlattenDescendantNodeList)
            //{
            //    item.StartLineNumber = item.StartLine.LineNumber;
            //    if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
            //    {
            //        compoundJsonTreeViewItem.EndLineNumber = compoundJsonTreeViewItem.EndLine.LineNumber;
            //    }
            //}
            //foreach (var item in nearbyItem.FlattenDescendantNodeList)
            //{
            //    item.StartLineNumber = item.StartLine.LineNumber;
            //    if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
            //    {
            //        compoundJsonTreeViewItem.EndLineNumber = compoundJsonTreeViewItem.EndLine.LineNumber;
            //    }
            //}
            #endregion

            #region 计算偏移与标记
            bool CurrentNeedComma = false, NearbyNeedComma = false;

            NearbyNeedComma = IsDown;
            CurrentNeedComma = IsDown && Next is not null && Next.Next is not null;

            if (!NearbyNeedComma)
                NearbyNeedComma = !IsDown && Next is not null;
            if (!CurrentNeedComma)
                CurrentNeedComma = !IsDown;

            startlineNumber = StartLine.LineNumber;
            endlineNumber = EndLine.LineNumber;

            nearbyStartLineNumber = nearbyItem.StartLine.LineNumber;
            nearbyEndLineNumber = nearbyItem.EndLine.LineNumber;

            nearbyOffset = StartLine.Offset;
            nearbyLength = EndLine.EndOffset - nearbyOffset;

            offset = nearbyItem.StartLine.Offset;
            length = nearbyItem.EndLine.EndOffset - offset;

            //当前节点向上移动时需要让邻近节点的所属文本加上替换后的偏移量
            if (!IsDown)
            {
                nearbyOffset += nearbyLength - length + (Next is null ? 1 : 0);
            }

            span = nearbyItem.EndLine.LineNumber - nearbyItem.StartLine.LineNumber + 1;
            nearbySpan = EndLine.LineNumber - StartLine.LineNumber + 1;
            #endregion

            #region 计算当前节点和附近节点范围内的所有文本
            currentLineText = Plan.GetRangeText(StartLine.Offset, EndLine.EndOffset - StartLine.Offset);
            nearbyLineText = Plan.GetRangeText(nearbyItem.StartLine.Offset, nearbyItem.EndLine.EndOffset - nearbyItem.StartLine.Offset);
            #endregion

            #region 互换位置
            Parent.Children.Remove(this);
            Parent.Children.Remove(nearbyItem);
            int lastIndex = Parent.Children.Count - 1;
            //处理边界替换情况
            if (Parent.Children.Count > 1 || (index > lastIndex && !IsDown))
            {
                if (index < nearbyIndex)
                {
                    Parent.Children.Add(nearbyItem);
                    Parent.Children.Add(this);
                }
                else
                {
                    Parent.Children.Add(this);
                    Parent.Children.Add(nearbyItem);
                }
            }
            else//处理中间替换情况
            {
                Parent.Children.Insert(index, nearbyItem);
                Parent.Children.Insert(nearbyIndex, this);
            }

            int lastCommaIndex = 0;
            if (CurrentNeedComma && !currentLineText.TrimEnd().EndsWith(','))
            {
                currentLineText += ',';
            }
            else
            if (!CurrentNeedComma)
            {
                lastCommaIndex = currentLineText.LastIndexOf(',');
                if (lastCommaIndex > -1 && currentLineText.TrimEnd().EndsWith(','))
                    currentLineText = currentLineText.Remove(lastCommaIndex);
            }
            if (NearbyNeedComma && !nearbyLineText.TrimEnd().EndsWith(','))
            {
                nearbyLineText += ',';
            }
            else
            if (!NearbyNeedComma)
            {
                lastCommaIndex = nearbyLineText.LastIndexOf(',');
                if (lastCommaIndex > -1 && nearbyLineText.TrimEnd().EndsWith(','))
                    nearbyLineText = nearbyLineText.Remove(lastCommaIndex);
            }

            Plan.SetRangeText(offset, length, currentLineText);
            Plan.SetRangeText(nearbyOffset, nearbyLength, nearbyLineText);
            #endregion

            #region 更新子结构中所有节点的行引用并更新当前元素与邻近元素的行引用
            foreach (var item in FlattenDescendantNodeList)
            {
                item.StartLine = item.Plan.GetLineByNumber(item.StartLine.LineNumber + (IsDown ? span : -span));
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    compoundJsonTreeViewItem.EndLine = item.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber + (IsDown ? span : -span));
                    compoundJsonTreeViewItem.EndLineNumber = 0;
                }
            }
            foreach (var item in nearbyItem.FlattenDescendantNodeList)
            {
                item.StartLine = item.Plan.GetLineByNumber(item.StartLine.LineNumber - (IsDown ? nearbySpan : -nearbySpan));
                if (item is CompoundJsonTreeViewItem nearbyCompoundJsonTreeViewItem)
                {
                    nearbyCompoundJsonTreeViewItem.EndLine = item.Plan.GetLineByNumber(nearbyCompoundJsonTreeViewItem.EndLineNumber - (IsDown ? nearbySpan : -nearbySpan));
                    nearbyCompoundJsonTreeViewItem.EndLineNumber = 0;
                }
            }
            StartLine = Plan.GetLineByNumber(startlineNumber + (IsDown ? span : -span));
            EndLine = Plan.GetLineByNumber(endlineNumber + (IsDown ? span : -span));
            nearbyItem.StartLine = nearbyItem.Plan.GetLineByNumber(nearbyStartLineNumber - (IsDown ? nearbySpan : -nearbySpan));
            nearbyItem.EndLine = nearbyItem.Plan.GetLineByNumber(nearbyEndLineNumber - (IsDown ? nearbySpan : -nearbySpan));

            if (IsDown)
            {
                nearbyItem.Previous = lastItem;
                nearbyItem.Next = this;

                Previous = nextItem;
                Next = nextNextItem;
            }
            else
            {
                nearbyItem.Previous = this;
                nearbyItem.Next = nextItem;

                Previous = lastLastItem;
                Next = nearbyItem;
            }
            Parent.Children.Add(addToButtomItem);
            #endregion
        }
        #endregion
    }
}