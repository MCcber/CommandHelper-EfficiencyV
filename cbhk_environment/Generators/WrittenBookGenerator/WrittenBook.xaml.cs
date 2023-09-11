using System.Windows;
using System.Windows.Input;

namespace cbhk_environment.Generators.WrittenBookGenerator
{
    /// <summary>
    /// WrittenBook.xaml 的交互逻辑
    /// </summary>
    public partial class WrittenBook
    {
        /// <summary>
        /// 主窗体
        /// </summary>
        public static MainWindow cbhk = null;
        public WrittenBook(MainWindow win, bool AsAnInternalTool = false)
        {
            InitializeComponent();
            written_book_datacontext.AsInternalTool = AsAnInternalTool;
            cbhk = win;
        }
    }
}
