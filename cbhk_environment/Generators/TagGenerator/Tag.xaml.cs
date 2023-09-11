namespace cbhk_environment.Generators.TagGenerator
{
    /// <summary>
    /// Tag.xaml 的交互逻辑
    /// </summary>
    public partial class Tag
    {
        //主页引用
        public static MainWindow cbhk = null;
        public Tag(MainWindow win)
        {
            InitializeComponent();
            cbhk = win;
        }
    }
}
