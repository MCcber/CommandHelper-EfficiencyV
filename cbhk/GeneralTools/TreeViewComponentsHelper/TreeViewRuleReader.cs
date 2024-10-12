using cbhk.CustomControls;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Model.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 将Json规则转化为TreeView
    /// </summary>
    public class TreeViewRuleReader
    {
        /// <summary>
        /// 递归整理子结构上下文
        /// </summary>
        /// <param name="list">当前节点集合</param>
        /// <param name="Parent">当前父级节点</param>
        /// <param name="dictionary">当前上下文字典</param>
        /// <returns>整理后的节点集合</returns>
        public static ObservableCollection<JsonTreeViewItem> RecursivelyUpdateMemberLayer(CompoundJsonTreeViewItem Parent, Dictionary<string, JsonTreeViewItem> dictionary, ObservableCollection<JsonTreeViewItem> list, int lineNumber, int layer = 2)
        {
            bool haveCompoundHead = false;
            bool haveSwitchKey = false;
            string ParentCompoundHead = "";
            //string ParentSwitchKey = "";
            for (int i = 0; i < list.Count; i++)
            {
                //CompoundHead和SwitchKey都会占用一行，所以遇到需要让行号加1

                #region 处理CompoundHead
                if (list[i].Parent.CompoundHead is not null && list[i].Parent.CompoundHead.Length > 0 && !haveCompoundHead)
                {
                    ParentCompoundHead = list[i].Parent.CompoundHead + ".";
                    haveCompoundHead = true;
                    lineNumber++;
                }
                #endregion

                #region 处理SwitchKey
                if (list[i].Parent.SwitchKey is not null && list[i].Parent.SwitchKey.Length > 0 && !haveSwitchKey)
                {
                    //ParentSwitchKey = list[i].Parent.SwitchKey + ".";
                    haveSwitchKey = true;
                    lineNumber++;
                }
                #endregion

                #region 处理首行与尾行、拼接节点路径
                if (list[i].Parent.SwitchKey.Length > 0)
                    list[i].Parent = Parent;
                list[i].Path = list[i].Parent.Path + "." + ParentCompoundHead + list[i].Key;
                list[i].StartLineNumber = lineNumber;
                //判断是否为复合数据类型
                if (list[i] is CompoundJsonTreeViewItem compoundJsonTreeViewItem && (compoundJsonTreeViewItem.DataType is Enums.DataTypes.Array || compoundJsonTreeViewItem.DataType is Enums.DataTypes.Compound || compoundJsonTreeViewItem.DataType is Enums.DataTypes.EnumCompound || compoundJsonTreeViewItem.DataType is Enums.DataTypes.MultiType || compoundJsonTreeViewItem.DataType is Enums.DataTypes.ValueProvider))
                    compoundJsonTreeViewItem.EndLineNumber = lineNumber;
                #endregion

                #region 绑定前后关系
                if (i > 0)
                {
                    list[i].Last = list[i - 1];
                    list[i - 1].Next = list[i];
                }
                #endregion

                #region 处理节点层级
                int currentLayerCount = layer;
                if (ParentCompoundHead.Length > 0 && list[i].Last is null)
                    currentLayerCount += 1;
                if ((list[i].Parent.DataType is Enums.DataTypes.Array || list[i].Parent.DataType is Enums.DataTypes.ArrayElement) && list[i].Last is null)
                    currentLayerCount += 1;
                list[i].LayerCount = layer = currentLayerCount;
                #endregion

                #region 将处理完的节点添加到当前上下文字典中、递归处理
                if (!dictionary.ContainsKey(list[i].Path))
                    dictionary.Add(list[i].Path, list[i]);
                if (list[i] is CompoundJsonTreeViewItem compound && compound.Children.Count > 0)
                {
                    RecursivelyUpdateMemberLayer(compound, dictionary, list, lineNumber + 1, layer + 1);
                }
                #endregion

                #region 复合标头复位、行号自增1
                ParentCompoundHead /*= ParentSwitchKey */= "";
                lineNumber++;
                #endregion
            }
            return list;
        }

        /// <summary>
        /// 读取值提供器
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<string, CompoundJsonTreeViewItem> LoadValueProviderStructure(string filePath)
        {
            Dictionary<string, CompoundJsonTreeViewItem> result = [];
            if (File.Exists(filePath))
            {
                string data = File.ReadAllText(filePath);
                JArray valueProviderArray = JArray.Parse(data);
                foreach (var obj in valueProviderArray.Cast<JObject>())
                {
                    #region 读取一个提供器类型
                    if (obj["key"] is JToken keyToken)
                    {
                        #region 确认值模板类型
                        CompoundJsonTreeViewItem jsonTreeViewItem = new(null, null)
                        {
                            Key = keyToken.ToString(),
                            DataType = Enums.DataTypes.ValueProvider,
                            Plan = null
                        };

                        if (Enum.TryParse(keyToken.ToString(), true, out Enums.ValueProviderTypes valueProviderType))
                            jsonTreeViewItem.ValueProviderType = valueProviderType;
                        #endregion

                        #region 值
                        if (obj["value"] is JToken valueToken)
                            jsonTreeViewItem.Value = valueToken.ToString();
                        #endregion

                        #region 数据类型
                        if (obj["dataType"] is JToken dataTypeToken)
                            jsonTreeViewItem.ValueTypeList.Add(new TextComboBoxItem() { Text = dataTypeToken.ToString() });
                        #endregion

                        #region 枚举列表
                        if (obj["enumList"] is JArray tagsToken)
                        {
                            foreach (var tag in tagsToken)
                            {
                                string text = tag.ToString();
                                string[] textList = text.Split('_');
                                for (int i = 0; i < textList.Length; i++)
                                {
                                    textList[i] = textList[i][0].ToString().ToUpper() + textList[i][1..];
                                }
                                text = string.Join(' ', textList);
                                jsonTreeViewItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = text });
                            }
                        }
                        #endregion

                        #region 解析当前提供器的结构
                        if (obj["children"] is JArray childrenToken)
                        {
                            ObservableCollection<JsonTreeViewItem> switchList = [];
                            foreach (var structure in childrenToken)
                            {
                                CompoundJsonTreeViewItem subItem = new(null, null);
                                if (structure["switchKey"] is JToken structureKeyToken)
                                {
                                    subItem.SwitchKey = structureKeyToken.ToString();
                                    if (structure["displayText"] is JToken displayTextToken)
                                        subItem.DisplayText = displayTextToken.ToString();
                                    else
                                        subItem.DisplayText = subItem.Key;

                                    if (structure["value"] is JToken subValueToken)
                                    {
                                        subItem.DefaultValue = subValueToken.ToString();
                                    }
                                    if (structure["peerNodeList"] is JArray peerNodeListToken)
                                    {
                                        JsonTreeViewDataStructure dataStructure = /*HtmlHelper.AnalyzeHTMLData("")*/null;
                                        subItem.Children = dataStructure.Result;
                                        switchList.Add(subItem);
                                    }
                                    if (structure["compoundHead"] is JToken compoundHeadToken)
                                    {
                                        subItem.CompoundHead = compoundHeadToken.ToString();
                                    }
                                }
                            }
                            jsonTreeViewItem.SubChildrenString = /*switchList*/"";
                        }
                        #endregion

                        result.Add(keyToken.ToString(), jsonTreeViewItem);
                    }
                    #endregion
                }
            }
            return result;
        }
    }
}