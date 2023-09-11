namespace cbhk_environment.Generators.EntityGenerator
{
    /// <summary>
    /// Entity.xaml 的交互逻辑
    /// </summary>
    public partial class Entity
    {
        /// <summary>
        /// 主窗体
        /// </summary>
        public static MainWindow cbhk = null;
        public Entity(MainWindow win = null)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
