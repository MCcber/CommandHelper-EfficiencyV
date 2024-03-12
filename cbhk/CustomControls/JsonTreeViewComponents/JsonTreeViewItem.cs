using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public class JsonTreeViewItem(Dictionary<string, string> presetJsonData, int lineNumber, int linePosition) : TreeViewItem
    {
        //public enum DataType
        //{
        //    String,
        //    BlockID,
        //    ItemID,
        //    EntityID,
        //    Byte,
        //    Short,
        //    Int,
        //    Float,
        //    Double,
        //    Long
        //}

        /// <summary>
        /// 预设的Json数据
        /// </summary>
        private Dictionary<string, string> PresetJsonData { get; set; } = presetJsonData;

        private string CurrentJsonData { get; set; } = "";

        private int LineNumber { get; set; } = lineNumber;

        private int LinePosition { get; set; } = linePosition;

        /// <summary>
        /// 是否为Json数组
        /// </summary>
        public bool IsArray { get; set; } = false;

        //public DataType CurrentDataType
        //{
        //    get { return (DataType)GetValue(CurrentDataTypeProperty); }
        //    set { SetValue(CurrentDataTypeProperty, value); }
        //}

        //public static readonly DependencyProperty CurrentDataTypeProperty =
        //    DependencyProperty.Register("CurrentDataType", typeof(DataType), typeof(JsonTreeViewItem), new PropertyMetadata(DataType.String,DataTypeChanged));

        //private static void DataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    JsonTreeViewItem item = d as JsonTreeViewItem;
        //    DataType dataType = (DataType)e.NewValue;
        //    item.CurrentJsonData = item.PresetJsonData[dataType];
        //}

        /// <summary>
        /// 通过偏移量获取对应的节点
        /// </summary>
        /// <param name="Offset">目标偏移量</param>
        /// <returns>找到的节点</returns>
        public static JsonTreeViewItem GetItemByOffset(Dictionary<string, JsonTreeViewItem> FlatJsonTreeViewItem, int lineNumber, int Offset)
        {
            JsonTreeViewItem result = null;
            Parallel.ForEach(FlatJsonTreeViewItem.Keys, (key, ParallelLoopState) =>
            {
                JsonTreeViewItem CurrentTreeViewItem = FlatJsonTreeViewItem[key];
                int number = CurrentTreeViewItem.LineNumber;
                int position = CurrentTreeViewItem.LinePosition;
                //偏移量在区间内并且需要节点的Json格式合法
                if (number == lineNumber && position == Offset && CurrentTreeViewItem.IsItTrue())
                {
                    result = CurrentTreeViewItem;
                    ParallelLoopState.Break();
                }
            });
            return result;
        }

        /// <summary>
        /// 通过修改区间更新树的数据
        /// </summary>
        /// <param name="StartOffset">起始偏移量</param>
        /// <param name="EndOffset">末尾偏移量</param>
        public static void UpdateTreeDataByRange(Dictionary<string, JsonTreeViewItem> FlatJsonTreeViewItem, int StartLineIndex, int EndLineIndex, int StartOffset, int EndOffset)
        {
            JsonTreeViewItem startTargetItem = GetItemByOffset(FlatJsonTreeViewItem, StartLineIndex, StartOffset);
            JsonTreeViewItem endTargetItem = GetItemByOffset(FlatJsonTreeViewItem, EndLineIndex, EndOffset);
            if (startTargetItem is not null)
            {
                startTargetItem.CurrentJsonData = startTargetItem.CurrentJsonData[..StartOffset];
            }
            if (endTargetItem is not null)
            {
                endTargetItem.CurrentJsonData = endTargetItem.CurrentJsonData[..EndOffset];
            }
        }

        /// <summary>
        /// 验证目标Json是否合法
        /// </summary>
        /// <returns></returns>
        public bool IsItTrue()
        {
            //string data = PresetJsonData[CurrentDataType];
            //try
            //{
            //    _ = new JsonTextReader(new StringReader(data));
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
            return true;
        }

        /// <summary>
        /// 逐层向上遍历验证合法性
        /// </summary>
        public JsonTreeViewItem VerifyLegalityLayerByLayerUpwards()
        {
            JsonTreeViewItem ParentItem = Parent as JsonTreeViewItem;
            while (ParentItem is not null)
            {
                if (!ParentItem.IsItTrue())
                    ParentItem = ParentItem.Parent as JsonTreeViewItem;
                else
                    break;
            }
            return ParentItem;
        }
    }
}
