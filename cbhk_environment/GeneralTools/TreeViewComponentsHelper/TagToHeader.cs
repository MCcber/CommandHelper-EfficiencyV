using cbhk_environment.CustomControls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace cbhk_environment.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 处理当前节点的Tag属性向控件的转换
    /// </summary>
    public class TagToHeader : IValueConverter
    {
        /// <summary>
        /// 白色刷子
        /// </summary>
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        /// <summary>
        /// 黑色刷子
        /// </summary>
        SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //将tag转换为JObject
            JObject currentValue = JObject.Parse(value.ToString());
            //所附着的节点
            RichTreeViewItems parentItem = parameter as RichTreeViewItems;
            //ComponentsRequests
            List<string> requests = new();

            #region 实例化节点容器
            DockPanel dockPanel = new()
            {
                LastChildFill = false
            };
            #endregion

            #region 分析JObject中所有可能的键,生成一组控件需求链表
            //key
            string key = currentValue["key"].ToString();
            requests.Add("key-" + key);
            //description
            JToken description = currentValue["description"];
            if(description != null && description.ToString().Length > 0)
            requests.Add("description-" + description);
            //tag
            JArray tagList = JArray.Parse(currentValue["tag"].ToString());
            //enumList
            ObservableCollection<string> enumList = null;

            string firstTag = tagList[0].ToString().Replace("TAG_", "");
            switch (firstTag)
            {
                case "Boolean":
                    requests.Add("bool");
                    break;
                case "Int":
                    requests.Add("number-Int");
                    break;
                case "Short":
                    requests.Add("number-Short");
                    break;
                case "Float":
                    requests.Add("number-Float");
                    break;
                case "Double":
                    requests.Add("number-Double");
                    break;
                case "Byte":
                    requests.Add("number-Byte");
                    break;
                case "String":
                    requests.Add("String");
                    break;
                case "List":
                case "Compound":
                    JToken dependency = currentValue["dependency"];
                    JArray children = JArray.Parse(currentValue["children"].ToString());
                    if(children.Count == 0 || firstTag == "List")
                    requests.Add(firstTag + (dependency != null ? "-"+ dependency.ToString() : ""));
                    break;
            }

            if (tagList.Count > 1)
            {
                enumList = new();
                foreach (var item in tagList)
                {
                    //构造枚举内容
                    enumList.Add(item.ToString().Replace("TAG_", ""));
                }
            }
            #endregion

            #region 调用组件构造器
            ReturnTargetComponents.SetHeader(ref dockPanel,requests,enumList);
            #endregion

            return dockPanel;
        }

        /// <summary>
        /// 判断是否需要切换子结构
        /// </summary>
        /// <returns></returns>
        public static bool IsNeedSwitchStrcuture(string currentStructure)
        {
            bool result = false;
            JObject currentValue = JObject.Parse(currentStructure);
            JToken HaveEnum = currentValue["enum"];
            if (HaveEnum != null)
                result = bool.Parse(HaveEnum.ToString());
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
