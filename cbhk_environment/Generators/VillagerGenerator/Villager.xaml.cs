namespace cbhk_environment.Generators.VillagerGenerator
{
    /// <summary>
    /// Villager.xaml 的交互逻辑
    /// </summary>
    public partial class Villager
    {
        /// <summary>
        /// 主窗体引用
        /// </summary>
        public static MainWindow cbhk = null;

        public Villager(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
