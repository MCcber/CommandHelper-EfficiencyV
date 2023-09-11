namespace cbhk_environment.Generators.DataPackGenerator
{
    /// <summary>
    /// Datapack.xaml 的交互逻辑
    /// </summary>
    public partial class Datapack
    {
        /// <summary>
        /// 主窗体
        /// </summary>
        public static MainWindow cbhk = null;
        public Datapack(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
