namespace cbhk_environment.Generators.FireworkRocketGenerator
{
    /// <summary>
    /// FireworkRocket.xaml 的交互逻辑
    /// </summary>
    public partial class FireworkRocket
    {
        //主窗体
        public static MainWindow cbhk = null;
        public FireworkRocket(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
