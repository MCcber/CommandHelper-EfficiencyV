using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace cbhk_environment.More
{
    /// <summary>
    /// DonateUs.xaml 的交互逻辑
    /// </summary>
    public partial class DonateUs
    {
        public DonateUs()
        {
            InitializeComponent();
            List<DonateDataItem> donateDataItems = new()
            {
                new DonateDataItem()
                {
                    Icon = new(new System.Uri("pack://application:,,,/cbhk_environment;component/resources/cbhk_form/images/more/thanks/9Hover.png",System.UriKind.Absolute)),
                    Description = "觉得本项目美术不错？/很喜欢本项目的美术？可以考虑赞助此开发者！",
                    NoRequired = Visibility.Visible
                },
                new DonateDataItem()
                {
                    Icon = new(new System.Uri("pack://application:,,,/cbhk_environment;component/resources/cbhk_form/images/more/thanks/7Hover.png",System.UriKind.Absolute)),
                    Description = "觉得本项目数据源齐全？/很喜欢本项目的数据源？可以考虑赞助此开发者！",
                    NoRequired = Visibility.Visible
                },
                new DonateDataItem()
                {
                    Icon = new(new System.Uri("pack://application:,,,/cbhk_environment;component/resources/cbhk_form/images/more/thanks/1Hover.png",System.UriKind.Absolute)),
                    Description = "觉得本项目程序很牛？/很喜欢本项目的程序？可以考虑赞助此开发者！",
                    NoRequired = Visibility.Visible
                }
            };
            grid.ItemsSource = donateDataItems;
        }
    }

    public class DonateDataItem
    {
        public BitmapImage Icon { get; set; }
        public string Description { get; set; }
        public BitmapImage Donate { get; set; } = null;
        public Visibility NoRequired { get; set; }
    }
}
