using System.Windows;

namespace cbhk_environment.Generators.ItemGenerator
{
    /// <summary>
    /// Item.xaml 的交互逻辑
    /// </summary>
    public partial class Item
    { 
        //主页引用
        public static MainWindow cbhk = null;
        public Item(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
