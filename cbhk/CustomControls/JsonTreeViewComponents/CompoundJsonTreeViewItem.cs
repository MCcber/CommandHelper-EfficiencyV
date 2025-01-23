using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.Interface.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static cbhk.Model.Common.Enums;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public partial class CompoundJsonTreeViewItem : JsonTreeViewItem
    {
        #region Property
        public bool IsCloning { get; set; } = false;
        public int EndLineNumber { get; set; }
        public DocumentLine EndLine { get; set; }

        /// <summary>
        /// 枚举键
        /// </summary>
        public string EnumKey = "";

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
                        InputBoxVisibility = Visibility.Collapsed;
                        break;
                    case DataTypes.NullableCompound:
                        RemoveElementButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.OptionalCompound:
                        AddElementButtonVisibility = Visibility.Visible;
                        break;
                    case DataTypes.CustomCompound:
                        AddElementButtonVisibility = InputBoxVisibility = Visibility.Visible;
                        break;
                    case DataTypes.List:
                        AddElementButtonVisibility = Visibility.Visible;
                        InputBoxVisibility = Visibility.Collapsed;
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

        #region Events
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
            #region 初始化变量
            int CurrentStartLineNumber = 0;
            if (!StartLine.IsDeleted)
                CurrentStartLineNumber = StartLine.LineNumber;
            #endregion

            #region 处理分支或直接赋值
            //当有分支子级时
            if (ChildrenStringList.Count > 0)
            {
                int index = -1;

                //#region 确认当前选中的枚举类型是否为当前复合节点的默认类型并处理后续
                //index = JsonItemTool.SetDefaultStructureJsonItem(this, OptionalChildrenString);
                //if (index > -1)
                //{
                //    #region 为SwitchKey与CompoundHead赋值
                //    CompoundJsonTreeViewItem CurrentBranch = SwitchChildren[index] as CompoundJsonTreeViewItem;
                //    CompoundHead = SwitchKey = "";
                //    Plan.UpdateCompoundHeadAndSwitchKey(CurrentBranch, this);
                //    if (SwitchChildren[index].Value is not null)
                //        Value = CurrentBranch.Value;
                //    else
                //        Value = CurrentBranch.DefaultValue;
                //    #endregion

                //    #region 克隆模板中的成员为实例成员，然后整理结构数据，清空平铺列表
                //    FlattenDescendantNodeList.Clear();
                //    CurrentBranch.JsonItemTool = JsonItemTool;
                //    CurrentBranch.Plan = Plan;
                //    Children = ((SwitchChildren[index] as CompoundJsonTreeViewItem).Clone() as CompoundJsonTreeViewItem).Children;
                //    Children = TreeViewRuleReader.RecursivelyUpdateMemberLayer(this, Plan.KeyValueContextDictionary, Children, StartLine.LineNumber + 1, LayerCount + 1);
                //    Children = JsonItemTool.SetSwitchChildrenOfSubItem(Children, this, this);
                //    #endregion

                //    #region 遍历子级节点，寻找值模板类型，填充SwitchChildren
                //    JsonItemTool.SetEnumData(Children, null, this);
                //    #endregion

                //    #region 更新右侧Json视图、计算新的行尾对象、更新当前值
                //    string space = new(' ', LayerCount * 2);
                //    string currentValue = "{" + JsonItemTool.RecursiveIntegrationOfSubstructureValuesWhenSwitch(this, Children, (DataType is DataTypes.ValueProvider || DataType is DataTypes.EnumCompound) ? this : Parent).ToString() + space + "}";
                //    int newCount = Regex.Matches(currentValue, @"\r\n").Count;
                //    if (CurrentStartLineNumber == 0)
                //        CurrentStartLineNumber = 1;

                //    #region 执行替换前再次复检末尾行是否被删除
                //    if (EndLine.IsDeleted)
                //    {
                //        EndLine = StartLine;
                //    }
                //    #endregion

                //    Plan.UpdateValueBySpecifyingInterval(this, ReplaceType.Compound, currentValue);
                //    JsonItemTool.SetDocumentLineByNumber(Children);
                //    JsonItemTool.UpdateFlattenDescendantNodeList(this, Children);
                //    EndLine = Plan.GetLineByNumber(CurrentStartLineNumber + newCount);
                //    Value = currentValue;
                //    InputBoxVisibility = Visibility.Collapsed;
                //    #endregion

                //}
                //else
                //{
                //    #region 清除子级，更新显示内容，更新Json视图
                //    Children.Clear();
                //    InputBoxVisibility = InfoIconVisibility = EnumBoxVisibility = Visibility.Visible;
                //    Plan.UpdateValueBySpecifyingInterval(this, ReplaceType.Compound, DefaultValue is null ? "0" : (string)DefaultValue);
                //    #endregion
                //}
                //#endregion
            }
            else//没有时直接当作枚举类型执行
                Plan.UpdateValueBySpecifyingInterval(this, ReplaceType.Input, "\"" + SelectedEnumItem.Text + "\"");
            #endregion

            oldSelectedEnumItem = SelectedEnumItem;
        }

        /// <summary>
        /// 添加数组元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddSubStructure_Click(object sender, RoutedEventArgs e) => JsonItemTool.AddSubStructure(this, sender as UIElement);

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RemoveSubStructure_Click(object sender, RoutedEventArgs e)
        {
            switch (DataType)
            {
                case DataTypes.OptionalCompound:
                case DataTypes.NullableCompound:
                case DataTypes.OptionalAndNullableCompound:
                    {
                        RemoveElementButtonVisibility = Visibility.Collapsed;
                        AddElementButtonVisibility = Visibility.Visible;
                        Children.Clear();
                        int startLineNumber = StartLine.LineNumber;
                        if (startLineNumber - 1 > 0)
                        {
                            DocumentLine lastLine = Plan.GetLineByNumber(startLineNumber - 1);
                            if (DataType is DataTypes.OptionalCompound)
                            {
                                Plan.DeleteAllLinesInTheSpecifiedRange(this);
                            }
                            if (DataType is DataTypes.NullableCompound)
                            {
                                DocumentLine startLine = Plan.GetLineByNumber(startLineNumber);
                                string startLineText = Plan.GetRangeText(startLine.Offset, startLine.EndOffset - startLine.Offset);
                                string endLineText = Plan.GetRangeText(EndLine.Offset, EndLine.EndOffset - EndLine.Offset);
                                int startIndex = startLineText.IndexOf('{') + 1;
                                int endIndex = endLineText.LastIndexOf('}');
                                Plan.DeleteAllLinesInTheSpecifiedRange(startLine.Offset + startIndex, EndLine.Offset + endIndex);
                            }
                            StartLineNumber = EndLineNumber = 0;
                            if (DataType is DataTypes.OptionalCompound)
                                StartLine = EndLine = lastLine;
                            else
                                EndLine = StartLine;
                        }
                        break;
                    }
                case DataTypes.ArrayElement:
                    {
                        JsonItemTool.RemoveSubStructure(this, Plan);
                        break;
                    }
                case DataTypes.InnerArray:
                    {
                        break;
                    }
            }
        }

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
        public CompoundJsonTreeViewItem(ICustomWorldUnifiedPlan plan, IJsonItemTool jsonItemTool)
        {
            Plan = plan;
            JsonItemTool = jsonItemTool;
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
            foreach (var item in FlattenDescendantNodeList)
            {
                item.StartLineNumber = item.StartLine.LineNumber;
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    compoundJsonTreeViewItem.EndLineNumber = compoundJsonTreeViewItem.EndLine.LineNumber;
                }
            }
            foreach (var item in nearbyItem.FlattenDescendantNodeList)
            {
                item.StartLineNumber = item.StartLine.LineNumber;
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    compoundJsonTreeViewItem.EndLineNumber = compoundJsonTreeViewItem.EndLine.LineNumber;
                }
            }
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
                item.StartLine = item.Plan.GetLineByNumber(item.StartLineNumber + (IsDown ? span : -span));
                item.StartLineNumber = 0;
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    compoundJsonTreeViewItem.EndLine = item.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber + (IsDown ? span : -span));
                    compoundJsonTreeViewItem.EndLineNumber = 0;
                }
            }
            foreach (var item in nearbyItem.FlattenDescendantNodeList)
            {
                item.StartLine = item.Plan.GetLineByNumber(item.StartLineNumber - (IsDown ? nearbySpan : -nearbySpan));
                item.StartLineNumber = 0;
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