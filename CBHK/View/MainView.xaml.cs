using CBHK.CustomControl.Container;
using CBHK.Interface.TreeView;
using CBHK.Model.TreeView;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CBHK.View
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Dispatcher.Invoke(() =>
                {
                    ParentItem Test1 = new()
                    {
                        Key = "Test1"
                    };
                    BoolItem Test2 = new()
                    {
                        Key = "Test2",
                        Value = true
                    };
                    StringItem Test3 = new()
                    {
                        Key = "Test3",
                        Value = ""
                    };
                    StringItem Test4 = new()
                    {
                        Key = "Test4",
                        Value = ""
                    };
                    ObservableCollection<ITreeViewItem> items = 
                    [
                        new BoolItem()
                        {
                            Key = "BoolItem",
                            Value = true
                        },
                        new StringItem()
                        {
                            Key = "StringItem",
                            Value = ""
                        },
                        new DoubleItem()
                        {
                            Key = "DoubleItem",
                            Value = 0.0
                        },
                        new AddRemoveItem()
                        {
                            Key = "添加子级"
                        },
                        new ToolTipItem()
                        {
                            Key = "提示信息",
                            Icon = Application.Current.Resources["ExclamationGeometry"] as Geometry,
                            IconBrush = Brushes.Yellow
                        }
                    ];
                    CompositeItem Test5 = new()
                    {
                        ItemList = items
                    };
                    BaseTreeViewDataItem parent = new()
                    {
                        Content = Test1
                    };
                    BaseTreeViewDataItem boolItem = new()
                    {
                        Content = Test2
                    };
                    BaseTreeViewDataItem stringItem1 = new()
                    {
                        Content = Test3
                    };
                    BaseTreeViewDataItem stringItem2 = new()
                    {
                        Content = Test4
                    };
                    BaseTreeViewDataItem item = new()
                    {
                        Content = Test5
                    };
                    parent.Children.Add(boolItem);
                    boolItem.Children.Add(stringItem1);
                    boolItem.Children.Add(stringItem2);
                    boolItem.Children.Add(item);
                    VectorTreeView treeView = new()
                    {
                        Background = Brushes.Transparent,
                        Style = Application.Current.Resources["VectorTreeViewStyle"] as Style
                    };
                    grid.Children.Add(treeView);
                    treeView.ItemsSource = new ObservableCollection<BaseTreeViewDataItem>() { parent };
                });
            });
        }
    }
}