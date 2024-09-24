using CBHKTester.Interface;
using CBHKTester.Model;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CBHKTester.Tool
{
    public class JsonToJsonTreeViewItemConverter : IValueConverter
    {
        #region Field
        public static List<string> BlockTagList = [];
        public static List<string> ItemTagList = [];
        public static JsonTreeViewDataStructure CurrentData = new();
        public static ICustomWorldUnifiedPlan plan = null;
        public static IJsonItemTool jsonItemTool = null;
        public static TextEditor textEditor = null;
        #endregion

        public static JsonTreeViewDataStructure RecursivelyTraverseEachMember(JsonTreeViewDataStructure result, JArray array, int lineNumber, int layerCount, CompoundJsonTreeViewItem ParentItem = null, JsonTreeViewItem Last = null, bool isProcessingTemplate = false)
        {
            foreach (var token in array)
            {
                #region 处理起始字符串
                if (result.ResultString.Length == 0)
                    result.ResultString.Append("{\r\n");
                #endregion

                #region 声明当前节点、连接前后节点
                JsonTreeViewItem item = new()
                {
                    StartLineNumber = lineNumber
                };
                JsonTreeViewItem lastItem = null, nextItem = null;
                #endregion

                #region 处理值类型
                if (token["dataTypeList"] is JArray tagsToken)
                {
                    foreach (var dataType in tagsToken.Select(item => item.ToString()))
                    {
                        #region 确定当前的节点数据类型
                        bool IsSimpleItem = false;
                        switch (dataType)
                        {
                            case "TAG_Bool":
                                {
                                    IsSimpleItem = true;
                                    item.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Bool;
                                    break;
                                }
                            case "TAG_Byte":
                            case "TAG_Short":
                            case "TAG_Int":
                            case "TAG_Float":
                            case "TAG_Double":
                            case "TAG_Long":
                            case "TAG_Decimal":
                            case "TAG_String":
                                {
                                    IsSimpleItem = true;
                                    item.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Input;
                                    if (dataType == "TAG_String")
                                        item.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.String;
                                    break;
                                }
                        }
                        if (!IsSimpleItem)
                        {
                            if (!isProcessingTemplate)
                            {
                                lastItem = item.Last;
                                nextItem = item.Next;
                            }
                            item = new CompoundJsonTreeViewItem(plan, jsonItemTool);
                            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                            {
                                compoundJsonTreeViewItem.ValueTypeList.Add(new cbhk.CustomControls.TextComboBoxItem()
                                {
                                    Text = dataType
                                });
                            }
                        }
                        #endregion

                        #region 处理值类型
                        if (item is JsonTreeViewItem and not CompoundJsonTreeViewItem)
                        {
                            switch (item.DataType)
                            {
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Bool:
                                    {
                                        if (token["key"] is JToken keyToken)
                                        {
                                            string key = keyToken.ToString();
                                            item.Key = key;
                                            item.BoolButtonVisibility = Visibility.Visible;

                                            if (token["defaultValue"] is JToken defaultValueToken)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                string defaultValue = defaultValueToken.ToString();

                                                if (bool.TryParse(defaultValue, out bool boolValue))
                                                {
                                                    item.Value = item.DefaultValue = boolValue;
                                                    if (boolValue)
                                                        item.IsTrue = true;
                                                    else
                                                        item.IsFalse = true;
                                                    result.ResultString.Append(": " + boolValue.ToString().ToLower());
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Input:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.String:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Byte:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Short:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Int:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Float:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Double:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Decimal:
                                case cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Long:
                                    {
                                        if (token["key"] is JToken keyToken)
                                        {
                                            string key = keyToken.ToString();
                                            item.Key = key;
                                            item.InputBoxVisibility = Visibility.Visible;

                                            if (token["defaultValue"] is JToken defaultValueToken)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                item.InputBoxVisibility = Visibility.Visible;
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                string defaultValue = defaultValueToken.ToString();
                                                item.Value = item.DefaultValue = defaultValue;
                                                if (item.DataType is not cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.String)
                                                    result.ResultString.Append(": " + defaultValue.ToString().ToLower());
                                                else
                                                    result.ResultString.Append(": \"" + defaultValue.ToString().ToLower() + "\"");
                                            }
                                            else
                                                item.Value = null;
                                        }
                                        break;
                                    }
                            }
                            item.StartLineNumber = lineNumber;
                            if (Last is not null && !isProcessingTemplate)
                            {
                                Last.Next = item;
                                item.Last = Last;
                            }
                        }
                        #endregion

                        #region 处理复合类型
                        if (item is CompoundJsonTreeViewItem SetTypeCompoundItem)
                        {
                            switch (dataType)
                            {
                                case "TAG_Enum":
                                case "TAG_BlockTag":
                                case "TAG_ItemTag":
                                    {
                                        if (token["key"] is JToken keyToken)
                                        {
                                            #region 控制显示隐藏、Key和前导空格
                                            SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                            string key = keyToken.ToString();
                                            SetTypeCompoundItem.Key = key;
                                            result.ResultString.Append(new string(' ', layerCount * 2));
                                            #endregion

                                            #region 处理数据类型、填充数据集
                                            SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Enum;
                                            if (token["enumList"] is JArray valueList)
                                            {
                                                foreach (var enumValue in valueList)
                                                {
                                                    SetTypeCompoundItem.EnumItemsSource.Add(new cbhk.CustomControls.TextComboBoxItem() { Text = enumValue.ToString() });
                                                }
                                            }
                                            else
                                            {
                                                if (dataType == "TAG_BlockTag")
                                                {
                                                    foreach (var blockTag in BlockTagList)
                                                    {
                                                        SetTypeCompoundItem.EnumItemsSource.Add(new cbhk.CustomControls.TextComboBoxItem() { Text = blockTag });
                                                    }
                                                }
                                                else
                                                {
                                                    foreach (var compoundTag in ItemTagList)
                                                    {
                                                        SetTypeCompoundItem.EnumItemsSource.Add(new cbhk.CustomControls.TextComboBoxItem() { Text = compoundTag });
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region 处理默认值
                                            result.ResultString.Append("\"" + key + "\"");
                                            result.ResultString.Append(": \"" + SetTypeCompoundItem.EnumItemsSource[0].Text + "\"");
                                            SetTypeCompoundItem.SelectedEnumItem = SetTypeCompoundItem.EnumItemsSource[0];
                                            if (token["defaultValue"] is JToken defaultValueToken)
                                            {
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                string defaultValue = defaultValueToken.ToString();

                                                if (bool.TryParse(defaultValue, out bool boolValue))
                                                {
                                                    SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Bool;
                                                    SetTypeCompoundItem.Value = boolValue;
                                                    if (boolValue)
                                                        SetTypeCompoundItem.IsTrue = true;
                                                    else
                                                        SetTypeCompoundItem.IsFalse = true;
                                                    result.ResultString.Append(": " + boolValue.ToString().ToLower());
                                                }
                                                else
                                                    if (int.TryParse(defaultValue, out int intValue))
                                                {
                                                    SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Int;
                                                    SetTypeCompoundItem.Value = intValue;
                                                    result.ResultString.Append(": " + intValue);
                                                }
                                                else
                                                {
                                                    SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.String;
                                                    SetTypeCompoundItem.Value = "\"" + defaultValue + "\"";
                                                    result.ResultString.Append(": \"" + defaultValue + "\"");
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    break;
                                case "TAG_OptionalCompound":
                                case "TAG_NullableCompound":
                                case "TAG_Compound":
                                case "TAG_CustomCompound":
                                    {
                                        #region 设置"展开"按钮与数据类型
                                        if (dataType == "TAG_NullableCompound" || dataType == "TAG_OptionalCompound")
                                        {
                                            SetTypeCompoundItem.DataType = dataType == "TAG_NullableCompound" ? cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.NullableCompound : cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound;
                                        }
                                        else
                                        if (dataType == "TAG_CustomCompound")
                                        {
                                            SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.CustomCompound;
                                        }
                                        else
                                        {
                                            SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Compound;
                                        }
                                        #endregion

                                        #region 计算当前数据类型、追加Key、前置追加空格
                                        string key = "";
                                        SetTypeCompoundItem.DefaultValue = "";
                                        if (token["key"] is JToken keyToken)
                                            SetTypeCompoundItem.Key = key = keyToken.ToString();
                                        if (SetTypeCompoundItem.DataType is not cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound)
                                        {
                                            result.ResultString.Append(new string(' ', layerCount * 2));
                                            SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                            if (key.Length > 0)
                                            {
                                                result.ResultString.Append("\"" + key + "\": {\r\n");
                                                SetTypeCompoundItem.LayerCount = layerCount;
                                            }
                                        }
                                        SetTypeCompoundItem.CurrentValueType ??= SetTypeCompoundItem.ValueTypeList[0];
                                        #endregion
                                    }
                                    break;
                                case "TAG_InnerArray":
                                case "TAG_Array":
                                    {
                                        if (token["key"] is JToken keyToken)
                                        {
                                            SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                            SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Array;
                                            if (dataType == "TAG_InnerArray")
                                                SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.InnerArray;
                                            SetTypeCompoundItem.Key = keyToken.ToString();
                                            SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                            if (Last is CompoundJsonTreeViewItem lastCompletedCompoundItem && (lastCompletedCompoundItem.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound || lastCompletedCompoundItem.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.NullableCompound || lastCompletedCompoundItem.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Compound || lastCompletedCompoundItem.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.CustomCompound))
                                            {
                                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lastCompletedCompoundItem.EndLineNumber + 1;
                                            }

                                            result.ResultString.Append(new string(' ', layerCount * 2) + (dataType == "TAG_Array" ? "\"" + keyToken.ToString() + "\"" : ""));
                                        }
                                        result.ResultString.Append((dataType == "TAG_Array" ? ": " : "") + "[]");
                                    }
                                    break;
                                case "TAG_IntProvider":
                                case "TAG_FloatProvider":
                                case "TAG_HeightProvider":
                                case "TAG_VerticalAnchor":
                                case "TAG_BlockPredicate":
                                case "TAG_BlockStateProvider":
                                    {
                                        if (token["key"] is JToken keyToken && token["dataTypeList"] is JArray dataTypeList)
                                        {
                                            string key = keyToken.ToString();
                                            if (plan.ValueProviderContextDictionary.TryGetValue(dataTypeList[0].ToString().Replace("TAG_", ""), out CompoundJsonTreeViewItem currentCompoundItem))
                                            {
                                                SetTypeCompoundItem.SwitchChildren = currentCompoundItem.SwitchChildren;
                                                SetTypeCompoundItem.SwitchKey = currentCompoundItem.SwitchKey;
                                                SetTypeCompoundItem.CompoundHead = currentCompoundItem.CompoundHead;
                                                SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.ValueProvider;
                                                SetTypeCompoundItem.Value = SetTypeCompoundItem.DefaultValue = "0";
                                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                                SetTypeCompoundItem.EnumBoxVisibility = SetTypeCompoundItem.InputBoxVisibility = Visibility.Visible;
                                                SetTypeCompoundItem.EnumItemsSource = currentCompoundItem.EnumItemsSource;
                                                SetTypeCompoundItem.SelectedEnumItem = currentCompoundItem.EnumItemsSource.FirstOrDefault();
                                                SetTypeCompoundItem.Key = key;
                                                SetTypeCompoundItem.CurrentValueType = SetTypeCompoundItem.ValueTypeList[0];
                                                result.ResultString.Append(new string(' ', SetTypeCompoundItem.LayerCount * 2) + "\"" + SetTypeCompoundItem.Key + "\": " + SetTypeCompoundItem.Value);
                                            }
                                            else//表示当前正在初始化值提供器
                                            if (isProcessingTemplate)
                                            {
                                                SetTypeCompoundItem.Key = key;
                                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                                SetTypeCompoundItem.ValueProviderType = (cbhk.CustomControls.JsonTreeViewComponents.Enums.ValueProviderTypes)Enum.Parse(typeof(cbhk.CustomControls.JsonTreeViewComponents.Enums.ValueProviderTypes), dataTypeList[0].ToString().Replace("TAG_", ""));
                                                SetTypeCompoundItem.DataType = cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.ValueProvider;
                                            }
                                        }
                                    }
                                    break;
                            }

                            #region 设置前后关系和行号
                            if (Last is not null && !isProcessingTemplate)
                            {
                                Last.Next = SetTypeCompoundItem;
                                SetTypeCompoundItem.Last = Last;
                            }
                            #endregion
                        }
                        #endregion

                        #region 计算可缺省参数的默认值
                        if (((item.DefaultValue is null && item.SelectedEnumItem is null) || (item is CompoundJsonTreeViewItem compoundJsonItem && compoundJsonItem.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound)) && ((Last is not null && Last.StartLineNumber == lineNumber) || (Last is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLineNumber == lineNumber) || (Last is null && lineNumber == 2)))
                        {
                            lineNumber--;
                            item.StartLineNumber = lineNumber;
                            if (item is CompoundJsonTreeViewItem optionalItem)
                            {
                                optionalItem.EndLineNumber = lineNumber;
                            }
                        }
                        if (token["defaultValue"] is JToken DefaultValue)
                            item.DefaultValue = DefaultValue.ToString();
                        #endregion

                        item.Plan = plan;
                    }
                }
                #endregion

                #region 处理值范围、显示文本等数据
                if (token["displayText"] is JToken displayTextToken)
                {
                    item.DisplayText = displayTextToken.ToString();
                }
                else
                if (item.DisplayText is null || item.DisplayText.Length == 0)
                    item.DisplayText = item.Key;

                if (token["range"] is JToken rangeToken)
                {
                    string range = rangeToken.ToString();
                    if (range.Contains('-'))
                    {
                        int index = range.LastIndexOf('-');
                        item.MinValue = range[..index];
                        item.MaxValue = range[(index + 1)..];
                    }
                }

                if (token["infoToolTip"] is JToken infoToolTipToken)
                {
                    item.InfoTiptext = infoToolTipToken.ToString();
                    item.InfoIconVisibility = Visibility.Visible;
                }

                if (token["rangeType"] is JToken rangeTypeToken)
                {
                    string rangeType = rangeTypeToken.ToString();
                    if (!rangeType.Contains('{'))
                    {
                        item.MultiplierMode = true;
                        item.Magnification = int.Parse(rangeType.ToString()[..^1]);
                    }
                }
                #endregion

                #region 为CustomCompound节点填充子级与枚举
                if (token["dataSource"] is JToken dataSourceToken && item is CompoundJsonTreeViewItem CustomCompound && CustomCompound.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.CustomCompound)
                {

                }
                #endregion

                #region 计算复合型节点的值的末尾偏移，处理子节点结构
                if (token["children"] is JArray children && children.Count > 0 && item is CompoundJsonTreeViewItem currentCompound)
                {
                    JsonTreeViewDataStructure currentResult = RecursivelyTraverseEachMember(new JsonTreeViewDataStructure(), children, lineNumber + 1, layerCount + 1, currentCompound, Last, isProcessingTemplate);

                    if (currentCompound.DataType is not cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound && (currentCompound.DefaultValue is not null || currentCompound.SelectedEnumItem is not null))
                    {
                        if (currentResult.Result[^1] is CompoundJsonTreeViewItem subCompoundItem)
                            currentCompound.EndLineNumber = subCompoundItem.EndLineNumber + 1;
                        else
                            currentCompound.EndLineNumber = currentResult.Result[^1].StartLineNumber + 1;
                        currentCompound.Value = "";
                        //过滤掉第一个多余的开花括号与换行符
                        result.ResultString.Append(currentResult.ResultString.ToString()[3..]);
                    }

                    #region 多数据类型、枚举或数据型节点则加入切换子结构中作备用
                    bool addToSwitch = currentCompound.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Array || currentCompound.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.InnerArray || currentCompound.DataType is cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.OptionalCompound;

                    if (addToSwitch)
                    {
                        foreach (var resultItem in currentResult.Result)
                        {
                            currentCompound.SwitchChildren.Add(resultItem);
                        }
                    }
                    else
                    {
                        foreach (var resultItem in currentResult.Result)
                        {
                            if (!currentCompound.FlattenDescendantNodeList.Contains(resultItem))
                                currentCompound.FlattenDescendantNodeList.Add(resultItem);
                            currentCompound.Children.Add(resultItem);
                            resultItem.Parent = currentCompound;
                        }
                    }
                    result.Result.Add(currentCompound);
                    #endregion
                }
                #endregion

                #region 处理路径和生成器上下文
                string compoundHead = "";
                if (ParentItem is not null && item.Parent is null)
                {
                    item.Parent = ParentItem;
                    if (ParentItem.DataType is not cbhk.CustomControls.JsonTreeViewComponents.Enums.DataTypes.Array)
                        ParentItem.FlattenDescendantNodeList.Add(item);
                    compoundHead = ParentItem is CompoundJsonTreeViewItem ParentJsonTreeViewItem && ParentJsonTreeViewItem.CompoundHead is not null && ParentJsonTreeViewItem.CompoundHead.Length > 0 ? ParentJsonTreeViewItem.CompoundHead + "." : "";
                }
                if (!isProcessingTemplate)
                    item.Path = (ParentItem is not null ? ParentItem.Path + "." : "") + compoundHead + item.Key;

                if (token["children"] is null)
                    result.Result.Add(item);
                #endregion

                #region 处理后导逗号、复合结构收尾、换行等操作
                if (!token.Equals(array.Last) && (item.Value is not null || item.SelectedEnumItem is not null) && !result.ResultString.ToString().TrimEnd().EndsWith(','))
                    result.ResultString.Append(",\r\n");
                lineNumber++;
                //存储上一个成员
                Last = item;
                #endregion
            }

            #region Json收尾后返回
            result.ResultString.Append("\r\n" + new string(' ', (layerCount - 1) * 2) + '}');
            return result;
            #endregion
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string originData = (string)value;
            if (originData is null || originData.Length == 0)
                return null;
            JArray jsonArray = JArray.Parse(originData);
            CurrentData = RecursivelyTraverseEachMember(new(), jsonArray, 2, 1);

            return CurrentData.Result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
