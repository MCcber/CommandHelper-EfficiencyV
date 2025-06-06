﻿using System.Windows.Controls;

namespace CBHK.View.Component.Item.SpecialNBT
{
    /// <summary>
    /// MapDisplay.xaml 的交互逻辑
    /// </summary>
    public partial class MapDisplay : UserControl
    {
        #region 合并结果
        public string Result
        {
            get
            {
                return EnableButton.IsChecked.Value? "display:{MapColor:"+color.Value+"}" :"";
            }
        }
        #endregion
        public MapDisplay()
        {
            InitializeComponent();
        }
    }
}
