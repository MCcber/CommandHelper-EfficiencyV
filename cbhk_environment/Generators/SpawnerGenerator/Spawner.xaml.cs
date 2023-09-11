using System.Windows;

namespace cbhk_environment.Generators.SpawnerGenerator
{
    /// <summary>
    /// Spawner.xaml 的交互逻辑
    /// </summary>
    public partial class Spawner
    {
        public static Window cbhk = null;
        public Spawner(Window win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
