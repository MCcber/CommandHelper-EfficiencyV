using cbhk_environment.CustomControls;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace cbhk_environment.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 负责JSON数据转换为TreeView成员
    /// </summary>
    public class TreeViewConveter
    {
        /// <summary>
        /// 普通文本块的灰刷
        /// </summary>
        private static SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#838383"));

        /// <summary>
        /// 处理JSON数据，返回一个可提醒UI更新的树视图节点集合
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ObservableCollection<RichTreeViewItems> Handler(string data)
        {
            //返回结果
            ObservableCollection<RichTreeViewItems> result = new();
            //解析第一层数据
            JToken rootToken = JToken.Parse(data);
            JArray rootArray = null;
            //将原始数据解析为JArray对象
            if (rootToken.GetType() == typeof(JArray))
                rootArray = JArray.Parse(data);
            else
            {
                JObject rootObj = JObject.Parse(data);
                rootArray = JArray.Parse(rootObj["children"].ToString());
            }
            //遍历JArray对象
            foreach (JObject firstLayerObj in rootArray)
            {
                RichTreeViewItems rootItem = new()
                {
                    ConnectingLineFill = grayBrush,
                    Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                    Tag = firstLayerObj.ToString(),
                    TextState = new TreeViewRun()
                };
                //视图成员指向自己
                rootItem.TextState.ViewItem = rootItem;

                #region 绑定器
                Binding componentsBinder = new()
                {
                    Path = new PropertyPath("Tag"),
                    Mode = BindingMode.OneTime,
                    RelativeSource = RelativeSource.Self,
                    Converter = new TagToHeader(),
                    ConverterParameter = rootItem
                };
                Binding textBinder = new()
                {
                    Path = new PropertyPath("Header"),
                    Mode = BindingMode.TwoWay,
                    RelativeSource = RelativeSource.Self,
                    Converter = new HeaderToJson(),
                    ConverterParameter = rootItem
                };
                #endregion

                //绑定组件转换器
                BindingOperations.SetBinding(rootItem, HeaderedItemsControl.HeaderProperty, componentsBinder);
                //绑定JSON转换器
                BindingOperations.SetBinding(rootItem, Run.TextProperty, textBinder);

                result.Add(rootItem);
                JArray tagList = JArray.Parse(firstLayerObj["tag"].ToString());
                string firstTag = tagList[0].ToString();
                JArray children = JArray.Parse(firstLayerObj["children"].ToString());
                if (tagList.Count > 1 && (firstTag.Contains("Compound") || firstTag.Contains("Array")))
                {
                    rootItem.Items.Add(new TreeViewItem() { Uid = "null" });
                    rootItem.Expanded += CompoundItemExpanded;
                }
            }

            return result;
        }

        /// <summary>
        /// 展开复合节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CompoundItemExpanded(object sender, RoutedEventArgs e)
        {
            #region 清空用于展开的空白子级
            RichTreeViewItems currentItem = sender as RichTreeViewItems;
            TreeViewItem testItem = currentItem.Items[0] as TreeViewItem;
            if (testItem.Uid == "null")
                currentItem.Items.Clear();
            #endregion

            #region 判断是否拥有子级,有则处理子级数据
            string data = currentItem.Tag.ToString();
            JObject Obj = JObject.Parse(data);
            //获取子级数据
            JArray childrenArray = JArray.Parse(Obj["children"].ToString());
            string firstTag = JArray.Parse(Obj["tag"].ToString())[0].ToString();

            #region 设置为空,避免反复处理子结构
            Obj["children"] = "[]";
            currentItem.Tag = Obj.ToString();
            #endregion

            if (childrenArray.Count > 0)
            {
                #region 处理子节点的子级结构响应和标签数据
                foreach (JObject subObj in childrenArray.Cast<JObject>())
                {
                    #region 子节点
                    RichTreeViewItems subItem = new()
                    {
                        ConnectingLineFill = grayBrush,
                        Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                        Tag = subObj.ToString(),
                        TextState = new TreeViewRun()
                    };
                    JArray subChildren = JArray.Parse(subObj["children"].ToString());
                    JArray subTagList = JArray.Parse(subObj["tag"].ToString());
                    string subFirstTag = subTagList[0].ToString();
                    if (subChildren.Count > 0 && (subFirstTag.Contains("Array") || subFirstTag.Contains("Compound")))
                    {
                        subItem.Items.Add(new TreeViewItem() { Uid = "null" });
                        subItem.Expanded += CompoundItemExpanded;
                    }
                    #endregion

                    #region 绑定器
                    Binding componentsBinder = new()
                    {
                        Path = new PropertyPath("Tag"),
                        Mode = BindingMode.OneTime,
                        RelativeSource = RelativeSource.Self,
                        Converter = new TagToHeader(),
                        ConverterParameter = subItem
                    };
                    Binding textBinder = new()
                    {
                        Path = new PropertyPath("Header"),
                        Mode = BindingMode.TwoWay,
                        RelativeSource = RelativeSource.Self,
                        Converter = new HeaderToJson(),
                        ConverterParameter = subItem
                    };
                    #endregion

                    //绑定组件转换器
                    BindingOperations.SetBinding(subItem, HeaderedItemsControl.HeaderProperty, componentsBinder);
                    //绑定JSON转换器
                    BindingOperations.SetBinding(subItem, Run.TextProperty, textBinder);
                    currentItem.Items.Add(subItem);
                }
                #endregion
            }
            #endregion
        }
    }
}
