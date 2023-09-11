using cbhk_environment.CustomControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace cbhk_environment.GeneralTools.TreeViewComponentsHelper
{
    public class HeaderToJson : IValueConverter
    {
        /// <summary>
        /// 将容器内控件按照一定的逻辑转为JSON字符串
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "";
            RichTreeViewItems currentItem = parameter as RichTreeViewItems;
            DockPanel dockPanel = value as DockPanel;
            //标记当前节点下应该追加中括号还是花括号
            string bracketContent = "";

            #region 处理起始括号类型
            if (currentItem.Items.Count > 0)
            {
                JArray tagList = JArray.Parse(JObject.Parse(currentItem.Tag.ToString())["tag"].ToString());
                bracketContent = "{";
                for (int i = 0; i < tagList.Count; i++)
                {
                    if (tagList[i].ToString().EndsWith("List") || tagList[i].ToString().EndsWith("Array"))
                        bracketContent = "[";
                }
            }
            #endregion

            foreach (FrameworkElement item in dockPanel.Children)
            {
                if (item is TextBlock && item.Uid == "key")
                    result += "\"" + (item as TextBlock).Text + "\":" + bracketContent;
                else
                if (item is Slider)
                {
                    Slider numberBox = item as Slider;
                    string number = "";
                    if (numberBox.Value.ToString().Contains('.'))
                        number = numberBox.Value.ToString()[..numberBox.Value.ToString().IndexOf('.')];
                    result += number + ",";
                }
                else
                    if (item is TextBox && item.Uid.Length == 0)
                    result += "\"" + (item as TextBox).Text + "\",";
                else
                    if (item is TextToggleButtons && (item as TextToggleButtons).IsChecked.Value)
                    result += (item as TextToggleButtons).Content + ",";
            }

            #region 处理末尾括号类型
            #endregion

            //格式化转换后的内容
            result = JsonConvert.SerializeObject(result, Formatting.Indented);
            return result;
        }

        /// <summary>
        /// 按照JSON字符串数据结构转为对应的树视图节点容器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return null;
        }
    }
}
