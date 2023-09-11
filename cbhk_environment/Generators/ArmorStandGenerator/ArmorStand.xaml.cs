
namespace cbhk_environment.Generators.ArmorStandGenerator
{
    /// <summary>
    /// ArmorStand.xaml 的交互逻辑
    /// </summary>
    public partial class ArmorStand
    {
        //主页引用
        public static MainWindow cbhk = null;
        public ArmorStand(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
