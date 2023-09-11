using System.Windows.Controls;


namespace cbhk_environment.CustomControls.TimeLines
{
    /// <summary>
    /// TimelineMark.xaml 的交互逻辑
    /// </summary>
    public partial class TimelineMark : UserControl
    {
        public TimelineMark(int seconds)
        {
            InitializeComponent();
            string m = (seconds / 60).ToString();
            if (m.Length < 2)
                m = "0" + m;
            string s = (seconds % 60).ToString();
            if (s.Length < 2)
                s = "0" + s;
            text.Text = m + ":" + s;
        }
    }
}
