using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.CustomControls.JsonTreeViewComponents.ValueComponents;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Windows.System.RemoteSystems;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 将Json规则转化为TreeView
    /// </summary>
    public class TreeViewRuleReader
    {
        public static List<string> BlockTagList = [];
        public static List<string> ItemTagList = [];

        private static DataStructure RecursivelyTraverseEachMember(DataStructure result, JArray array, int lineNumber, int lineStartPosition, int layerCount, JsonTreeViewItem ParentItem = null,JsonTreeViewItem Last = null)
        {
            foreach (var token in array)
            {
                if (result.ResultString.Length == 0)
                    result.ResultString.Append("{\r\n");

                JsonTreeViewItem item = new(lineNumber, lineStartPosition);
                JsonTreeViewContext currentContext = new();

                if (token["tags"] is JToken tagsToken)
                {
                    foreach (var valueType in tagsToken.Select(item => item.ToString()))
                    {
                        item.ValueTypeList.Add(new CustomControls.TextComboBoxItem()
                        {
                            Text = valueType
                        });
                    }

                    if (item.ValueTypeList.Count > 0)
                    {
                        #region 计算值默认值
                        if (JsonTreeViewItem.NumberTypes.Contains(item.ValueTypeList[0].Text) && token["defaultValue"] is null)
                            item.Value = item.MinValue;
                        #endregion

                        #region 处理值类型
                        switch (item.ValueTypeList[0].Text)
                        {
                            case "TAG_Bool":
                            case "TAG_Byte":
                            case "TAG_Short":
                            case "TAG_Int":
                            case "TAG_Float":
                            case "TAG_Double":
                            case "TAG_Long":
                            case "TAG_Decimal":
                            case "TAG_String":
                            case "TAG_Enum":
                            case "TAG_BlockTag":
                            case "TAG_ItemTag":
                                {
                                    if (token["key"] is JToken keyToken)
                                    {
                                        string key = keyToken.ToString();
                                        item.Key = key;
                                        for (int i = 0; i < layerCount * 4; i++)
                                        {
                                            result.ResultString.Append(' ');
                                        }

                                        if (string.IsNullOrEmpty(item.DisplayText))
                                            item.DisplayText = key;

                                        if (item.ValueTypeList[0].Text == "TAG_Enum" || item.ValueTypeList[0].Text == "TAG_BlockTag" || item.ValueTypeList[0].Text == "TAG_ItemTag")
                                        {
                                            item.IsEnumType = true;
                                            if (item.ValueTypeList[0].Text == "TAG_Enum" && token["valueList"] is JArray valueList)
                                            {
                                                foreach (var enumValue in valueList)
                                                {
                                                    item.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = enumValue.ToString() });
                                                }
                                            }
                                            else
                                            {
                                                if(item.ValueTypeList[0].Text == "TAG_BlockTag")
                                                {
                                                    foreach (var blockTag in BlockTagList)
                                                    {
                                                        item.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = blockTag });
                                                    }
                                                }
                                                else
                                                {
                                                    foreach (var itemTag in ItemTagList)
                                                    {
                                                        item.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = itemTag });
                                                    }
                                                }
                                            }

                                            currentContext.KeyStartOffset = result.ResultString.Length;
                                            result.ResultString.Append("\"" + key + "\"");
                                            currentContext.KeyEndOffset = result.ResultString.Length;
                                            currentContext.ValueStartOffset = currentContext.KeyEndOffset + 2;
                                            result.ResultString.Append(": \"" + item.EnumItemsSource[0].Text + "\"");
                                            currentContext.ValueEndOffset = result.ResultString.Length;
                                            item.SelectedEnumItem = item.EnumItemsSource[0];
                                            break;
                                        }

                                        if (token["defaultValue"] is JToken defaultValueToken)
                                        {
                                            currentContext.KeyStartOffset = result.ResultString.Length;
                                            result.ResultString.Append("\"" + key.ToLower() + "\"");
                                            currentContext.KeyEndOffset = result.ResultString.Length;
                                            string defaultValue = defaultValueToken.ToString();
                                            currentContext.ValueStartOffset = currentContext.KeyEndOffset + 2;

                                            if (bool.TryParse(defaultValue, out bool boolValue))
                                            {
                                                item.IsBoolType = true;
                                                item.Value = boolValue;
                                                result.ResultString.Append(": " + ((bool)item.Value).ToString().ToLower());
                                            }
                                            else
                                                if (int.TryParse(defaultValue, out int intValue))
                                            {
                                                item.IsNumberType = true;
                                                item.Value = intValue;
                                                result.ResultString.Append(": " + item.Value);
                                            }
                                            else
                                            {
                                                item.IsStringType = true;
                                                item.Value = "\"" + defaultValue + "\"";
                                                result.ResultString.Append(": " + item.Value);
                                            }
                                            currentContext.ValueEndOffset = result.ResultString.Length;
                                        }
                                        else
                                        {
                                            if (JsonTreeViewItem.NumberTypes.Contains(item.ValueTypeList[0].Text))
                                            {
                                                item.IsNumberType = true;
                                            }
                                            else
                                            if (item.ValueTypeList[0].Text == "TAG_Bool")
                                            {
                                                item.IsBoolType = true;
                                            }
                                            else
                                                if (item.ValueTypeList[0].Text == "TAG_String")
                                            {
                                                item.IsStringType = true;
                                            }
                                            string currentLastChar = result.ResultString.ToString()[(result.ResultString.Length - 1)..];
                                            while (currentLastChar != "\n" && currentLastChar != "\r")
                                            {
                                                result.ResultString = result.ResultString.Remove(result.ResultString.Length - 1, 1);
                                                currentLastChar = result.ResultString.ToString()[(result.ResultString.Length - 1)..];
                                            }
                                            result.ResultString = result.ResultString.Remove(result.ResultString.Length - 2, 2);
                                            currentContext.KeyStartOffset = result.Context[Last.Path].ValueEndOffset;
                                            currentContext.KeyEndOffset = result.Context[Last.Path].ValueEndOffset;
                                            currentContext.ValueStartOffset = result.Context[Last.Path].ValueEndOffset;
                                            currentContext.ValueEndOffset = result.Context[Last.Path].ValueEndOffset;
                                        }
                                    }
                                }
                                break;
                            case "TAG_Compound":
                                {
                                    currentContext.KeyStartOffset = result.ResultString.Length;
                                    if (token["key"] is JToken keyToken)
                                        result.ResultString.Append("\"" + keyToken.ToString() + "\"");
                                    currentContext.KeyEndOffset = result.ResultString.Length;
                                    currentContext.ValueStartOffset = result.ResultString.Length + 2;
                                    if (token["Children"] is null)
                                        result.ResultString.Append(": {\r\n");
                                    item.IsCompoundType = true;
                                    item.ValueTypeList.Clear();

                                    foreach (var valueType in token["valueList"].Select(item => item.ToString()))
                                    {
                                        item.ValueTypeList.Add(new CustomControls.TextComboBoxItem()
                                        {
                                            Text = valueType
                                        });
                                    }
                                    item.CurrentValueType = item.ValueTypeList[0];
                                }
                                break;
                            case "TAG_Array":
                                {
                                    item.IsArray = true;
                                    currentContext.KeyStartOffset = result.ResultString.Length;
                                    if (token["key"] is JToken keyToken)
                                        result.ResultString.Append("\"" + keyToken.ToString() + "\"");
                                    currentContext.KeyEndOffset = result.ResultString.Length;
                                    result.ResultString.Append(": [\r\n");
                                    currentContext.ValueStartOffset = currentContext.KeyEndOffset + 2;
                                }
                                break;
                            case "TAG_IntProvider":
                                {
                                    if (token["key"] is JToken keyToken)
                                    {
                                        string key = keyToken.ToString();
                                        item = new IntProvider(currentContext, lineNumber, lineStartPosition)
                                        {
                                            IsEnumType = true,
                                            LayerCount = layerCount
                                        };
                                        foreach (var valueType in tagsToken.Select(item => item.ToString()))
                                        {
                                            item.ValueTypeList.Add(new CustomControls.TextComboBoxItem()
                                            {
                                                Text = valueType
                                            });
                                        }

                                        if (string.IsNullOrEmpty(item.DisplayText))
                                            item.DisplayText = key;

                                        foreach (var member in Enum.GetValues(typeof(IntProviderStructures)))
                                        {
                                            item.EnumItemsSource.Add(new CustomControls.TextComboBoxItem()
                                            {
                                                Text = member.ToString()
                                            });
                                        }

                                        for (int i = 0; i < layerCount * 4; i++)
                                        {
                                            result.ResultString.Append(' ');
                                        }

                                        currentContext.KeyStartOffset = result.ResultString.Length;
                                        result.ResultString.Append("\"" + key + "\"");
                                        currentContext.KeyEndOffset = result.ResultString.Length;


                                        currentContext.ValueStartOffset = result.ResultString.Length;
                                        result.ResultString.Append(": 0");
                                        currentContext.ValueEndOffset = result.ResultString.Length;

                                        item.Value = 0;
                                        item.SelectedEnumItem = item.EnumItemsSource[0];
                                        item.SwitchChildren.Add(item);
                                        ObservableCollection<JsonTreeViewItem> defaultSubStructure = IntProvider.StructureChildren[(item as IntProvider).CurrentStructure];
                                        if (defaultSubStructure.Count > 1)
                                            foreach (var subStructure in defaultSubStructure)
                                            {
                                                item.Children.Add(subStructure);
                                            }
                                        else
                                            item.IsNumberType = true;
                                    }
                                }
                                break;
                        }
                        #endregion
                    }
                }

                #region 处理值、范围、显示文本等数据
                if (token["displayText"] is JToken displayTextToken)
                {
                    item.DisplayText = displayTextToken.ToString();
                }

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

                #region 计算复合型节点的值的末尾偏移，处理子节点结构
                if (token["children"] is JArray children && children.Count > 0)
                {
                    DataStructure currentResult = RecursivelyTraverseEachMember(result, children, lineNumber, lineStartPosition + 4, layerCount++, item, Last);

                    if (item.IsCompoundType && item.ValueTypeList.Count > 0 && (Last is not null || ParentItem is not null))
                    {
                        currentContext.ValueEndOffset = result.Context[(Last is not null ? Last : ParentItem).Path].ValueEndOffset + 2 + (layerCount * 4) + 1;
                        foreach (var resultItem in currentResult.Result)
                        {
                            item.SwitchChildren.Add(resultItem);
                        }
                        item.Children.Add(result.Result[0]);
                    }
                    else
                    {
                        foreach (var subItem in result.Result)
                        {
                            item.Children.Add(subItem);
                        }
                    }

                    if (item.IsArray)
                        result.ResultString.Append(']');
                    else
                        result.ResultString.Append('}');
                }
                #endregion

                #region 处理命名空间和生成器上下文
                if (ParentItem is not null && item.Parent is null)
                {
                    item.Parent = ParentItem;
                    currentContext.Path = ParentItem.Path + "." + item.Key;
                }
                else
                {
                    currentContext.Path = item.Path = item.Key;
                }
                result.Context.TryAdd(item.Path, currentContext);
                #endregion

                #region 处理后导逗号、复合结构收尾、换行等操作
                if (!token.Equals(array.Last) && (item.Value is not null || item.SelectedEnumItem is not null))
                    result.ResultString.Append(',');

                result.ResultString.Append("\r\n");
                result.Result.Add(item);
                item.LineNumber = lineNumber++;

                if (item.IsCompoundType)
                    result.ResultString.Append("}\r\n");
                //存储上一个成员
                Last = item;
                #endregion
            }

            //整个json收尾
            result.ResultString.Append('}');
            return result;
        }

        /// <summary>
        /// 执行分析
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <returns>返回从文件解析出的成员列表</returns>
        public static DataStructure Read(string filePath)
        {
            DataStructure result = new();
            if (File.Exists(filePath))
            {
                string originData = File.ReadAllText(filePath);
                JArray jsonArray = JArray.Parse(originData);
                DataStructure currentResult = RecursivelyTraverseEachMember(new(), jsonArray, 2, 2, 1);
                result.ResultString = currentResult.ResultString;
                foreach (var item in currentResult.Result)
                {
                    result.Result.Add(item);
                }
                result.Context = currentResult.Context;
            }
            return result;
        }
    }

    public class DataStructure
    {
        public ObservableCollection<JsonTreeViewItem> Result { get; set; } = [];
        public StringBuilder ResultString { get; set; } = new();
        public Dictionary<string,JsonTreeViewContext> Context { get; set; } = [];
    }
}
