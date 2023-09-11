namespace cbhk_environment.Generators.OnlyOneCommandGenerator
{
    /// <summary>
    /// OnlyOneCommand.xaml 的交互逻辑
    /// </summary>
    public partial class OnlyOneCommand
    {
        //主页引用
        public static MainWindow cbhk = null;
        public OnlyOneCommand(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
