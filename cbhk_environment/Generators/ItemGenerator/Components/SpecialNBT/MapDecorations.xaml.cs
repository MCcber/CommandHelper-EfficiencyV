using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// MapDecorations.xaml 的交互逻辑
    /// </summary>
    public partial class MapDecorations : UserControl
    {
        string mapTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\MapIcons.ini";

        #region 合并结果
        public string Result
        {
            get
            {
                string result;
                string typeResult = "type:" + type.SelectedIndex + ",";
                string posResult = "x:"+pos.number0.Value + ",z:"+pos.number2.Value + ",";
                string rotResult = rot.Value > 0 ? "rot:" + rot.Value + "," : "";
                result = "{" + ("id:\"" + (Uid.Length > 0 ? Uid : Guid.NewGuid().ToString())  + "\"," + typeResult + posResult + rotResult).Trim(',') + "}";
                result = result == "{}" ?"":result;
                return result;
            }
        }
        #endregion

        public MapDecorations()
        {
            InitializeComponent();
            rot.Minimum = 0;
            rot.Maximum = 360;

            List<string> typeList = File.ReadAllLines(mapTypeFilePath).ToList();
            type.ItemsSource = typeList;
            type.SelectedIndex = 0;
        }
    }
}
