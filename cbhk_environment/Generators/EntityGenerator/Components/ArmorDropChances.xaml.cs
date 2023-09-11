using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// ArmorDropChances.xaml 的交互逻辑
    /// </summary>
    public partial class ArmorDropChances : UserControl
    {
        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                if(EnableButton.IsChecked.Value)
                {
                    string data = (boots.Value / 100) + "f," + (legs.Value / 100) + "f," + (chest.Value / 100) + "f," + (helmet.Value / 100) + "f";
                    result = "ArmorDropChances:[" + data.Trim(',') + "]";
                }
                return result;
            }
        }
        #endregion

        public ArmorDropChances()
        {
            InitializeComponent();
        }
    }
}
