using cbhk.GeneralTools;
using cbhk.GeneralTools.Displayer;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.VillagerGenerator.Components
{
    /// <summary>
    /// TransactionItems.xaml 的交互逻辑
    /// </summary>
    public partial class TransactionItems : UserControl
    {
        #region 物品数据
        /// <summary>
        /// 1 或 0 (true/false) - true代表交易会提供经验球。Java版中所有自然生成的村民的交易都会给予经验球。
        /// </summary>
        private bool rewardExp = true;
        public bool RewardExp
        {
            get { return rewardExp; }
            set
            {
                rewardExp = value;
            }
        }

        /// <summary>
        /// 代表在交易选项失效前能进行的最大交易次数。当交易被刷新时，以2到12的随机数增加。
        /// </summary>
        private int maxUses = 0;
        public int MaxUses
        {
            get { return maxUses; }
            set
            {
                maxUses = value;
            }
        }

        /// <summary>
        /// 已经交易的次数，如果大于或等于maxUses，该交易将失效。
        /// </summary>
        private int uses = 0;
        public int Uses
        {
            get { return uses; }
            set
            {
                uses = value;
            }
        }

        /// <summary>
        /// 村民从此交易选项中能获得的经验值。
        /// </summary>
        private int xp = 0;
        public int Xp
        {
            get { return xp; }
            set
            {
                xp = value;
            }
        }

        /// <summary>
        /// 根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。
        /// </summary>
        private int demand = 0;
        public int Demand
        {
            get { return demand; }
            set
            {
                demand = value;
            }
        }

        /// <summary>
        /// 添加到第一个收购物品的价格调节。
        /// </summary>
        private int specialPrice = 0;
        public int SpecialPrice
        {
            get { return specialPrice; }
            set
            {
                specialPrice = value;
            }
        }

        /// <summary>
        /// 当前应用到此交易选项价格的乘数。
        /// </summary>
        private float priceMultiplier = 0f;
        public float PriceMultiplier
        {
            get { return priceMultiplier; }
            set
            {
                priceMultiplier = value;
            }
        }

        /// <summary>
        /// 主项数量
        /// </summary>
        public int BuyCount { get; set; } = 1;

        /// <summary>
        /// 副项数量
        /// </summary>
        public int BuyBCount { get; set; } = 1;

        /// <summary>
        /// 售卖数量
        /// </summary>
        public int SellCount { get; set; } = 1;

        #endregion

        #region 当前交易项数据
        public string TransactionItemData
        {
            get
            {
                string buyItemCount = BuyCount.ToString();
                string buyBItemCount = BuyBCount.ToString();
                string sellItemCount = SellCount.ToString();
                if (buyItemCount.Contains('.'))
                    buyItemCount = buyItemCount[..buyItemCount.IndexOf('.')];
                if (buyBItemCount.Contains('.'))
                    buyBItemCount = buyBItemCount[..buyBItemCount.IndexOf('.')];
                if (sellItemCount.Contains('.'))
                    sellItemCount = sellItemCount[..sellItemCount.IndexOf('.')];

                string result;
                string rewardExp = "rewardExp:" + (RewardExp ? 1 : 0) + "b,";
                string maxUses = "maxUses:" + MaxUses + ",";
                string uses = "uses:" + Uses + ",";

                #region 购入物品AB与卖出物品数据
                //补齐双引号对
                string buyData = Buy.Tag is ItemStructure buyItemData ? Regex.Replace(buyItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":") : "{}";
                string buyBData = BuyB.Tag is ItemStructure buyBItemData ? Regex.Replace(buyBItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":") : "{}";
                string sellData = Sell.Tag is ItemStructure sellItemData ? Regex.Replace(sellItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":") : "{id:\"minecraft:air\",Count:1}";
                //清除数值型数据的单位
                buyData = Regex.Replace(buyData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                buyBData = Regex.Replace(buyBData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                sellData = Regex.Replace(sellData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

                JObject buyObj = JObject.Parse(buyData);
                JObject buybObj = JObject.Parse(buyBData);
                JObject sellObj = JObject.Parse(sellData);

                buyObj["Count"] = int.Parse(buyItemCount);
                buybObj["Count"] = int.Parse(buyBItemCount);
                sellObj["Count"] = int.Parse(sellItemCount);
                //去除双引号对
                string buy = buyData != "{}" ? "buy:" + Regex.Replace(buyObj.ToString(), @"([\{\[,])([\s+]?\w+[\s+]?):", "$1$2:").Replace("\r", "").Replace("\n", "") + "," : "";
                buy = Regex.Replace(buy, @"\s+", "");
                string buyB = buyBData != "{}" ? "buyB:" + Regex.Replace(buybObj.ToString(), @"([\{\[,])([\s+]?\w+[\s+]?):", "$1$2:").Replace("\r", "").Replace("\n", "") + "," : "";
                buyB = Regex.Replace(buyB, @"\s+", "");
                string sell = "sell:" + Regex.Replace(sellObj.ToString(), @"([\{\[,])([\s+]?\w+[\s+]?):", "$1$2:").Replace("\r", "").Replace("\n", "") + ",";
                sell = Regex.Replace(sell, @"\s+", "");
                #endregion

                string xp = "xp:" + Xp + ",";
                string demand = "demand:" + Demand + ",";
                string specialPrice = "specialPrice:" + SpecialPrice + ",";
                string priceMultiplier = "priceMultiplier:" + PriceMultiplier + ",";
                result = rewardExp + maxUses + uses + buy + buyB + sell + xp + demand + specialPrice + priceMultiplier;
                result = "{" + result.TrimEnd(',') + "}";
                return result;
            }
        }
        #endregion

        /// <summary>
        /// 空图像路径
        /// </summary>
        string emptyIcon = "pack://application:,,,/cbhk;component/resources/cbhk/images/empty.png";

        DataTable ItemTable = null;

        public TransactionItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransactionItems_Loaded(object sender, RoutedEventArgs e)
        {
            VillagerDataContext context = Window.GetWindow(this).DataContext as VillagerDataContext;
            ItemTable = context.ItemTable;
        }

        /// <summary>
        /// 更新物品显示图像以及文本提示
        /// </summary>
        /// <param name="old_image"></param>
        /// <param name="new_image"></param>
        private void UpdateItem(Image old_image,Image new_image)
        {
            int startIndex = new_image.Source.ToString().LastIndexOf('/') + 1;
            int endIndex = new_image.Source.ToString().LastIndexOf('.');
            string itemID = new_image.Source.ToString()[startIndex..endIndex];
            string toolTip = itemID + ":" + ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
            old_image.Source = new_image.Source;
            ToolTip tooltipObj = new()
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#484848")),
                Content = toolTip
            };
            old_image.ToolTip = tooltipObj;
            ToolTipService.SetBetweenShowDelay(old_image, 0);
            ToolTipService.SetInitialShowDelay(old_image, 0);
        }

        #region 处理拖拽后的数据更新
        /// <summary>
        /// 更新第一个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateBuyItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image current_image = sender as Image;
            current_image.Tag = image.Tag;
            UpdateItem(current_image,image);
        }

        /// <summary>
        /// 更新第二个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateBuybItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image current_image = sender as Image;
            current_image.Tag = image.Tag;
            UpdateItem(current_image, image);
        }

        /// <summary>
        /// 更新出售物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateSellItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image current_image = sender as Image;
            current_image.Tag = image.Tag;
            UpdateItem(current_image, image);
        }
        #endregion

        /// <summary>
        /// 处理打折后的数据
        /// </summary>
        /// <param name="demand">根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。</param>
        /// <param name="priceMultiplier">当前应用到此交易选项价格的乘数。</param>
        /// <param name="minor_negative">言论类型</param>
        /// <param name="trading">言论类型</param>
        /// <param name="specialPrice">添加到第一个收购物品的价格调节。</param>
        public void UpdateDiscountData(int minor_negative = 0,int minor_positive =0,int major_negative = 0,int major_positive = 0, int trading = 0)
        {
            //获取原价
            int original_price = BuyCount;
            int price = original_price - (int)Math.Floor((5 * major_positive + minor_positive + trading + minor_negative - 5 * major_negative) * PriceMultiplier);
            //如果最终价格不同于原价则开启打折效果
            if (price != original_price)
            {
                TextDecorationCollection textDecorationCollection = [];
                TextDecoration textDecoration = new(TextDecorationLocation.Baseline,new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BA370F")),2),-5,TextDecorationUnit.Pixel,TextDecorationUnit.Pixel);
                textDecorationCollection.Add(textDecoration);
                BuyCountDisplay.TextDecorations = textDecorationCollection;
                BuyDisCountDisplay.Text = "x" + price.ToString();
                BuyDisCountDisplay.Visibility = Visibility.Visible;
            }
            else
            {
                BuyCountDisplay.TextDecorations = null;
                BuyDisCountDisplay.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 恢复价格数据
        /// </summary>
        public void HideDiscountData(bool Hide = true)
        {
            if(!Hide)
            {
                BuyCountDisplay.TextDecorations = null;
                BuyDisCountDisplay.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            Button iconTextButtons = sender as Button;
            TransactionItems template_parent = iconTextButtons.FindParent<TransactionItems>();
            VillagerDataContext context = (Window.GetWindow(iconTextButtons) as Villager).DataContext as VillagerDataContext;
            context.transactionItems.Remove(template_parent);
        }

        /// <summary>
        /// 编辑器当前交易项的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            VillagerDataContext context = Window.GetWindow(sender as Button).DataContext as VillagerDataContext;
            context.TransactionDataGridVisibility = Visibility.Visible;
            context.BuyItemIcon = Buy.Source;
            context.BuyBItemIcon = BuyB.Source;
            context.SellItemIcon = Sell.Source;
            context.BuyItemData = Buy.Tag;
            context.BuyBItemData = BuyB.Tag;
            context.SellItemData = Sell.Tag;

            context.CurrentItem = this;
        }

        /// <summary>
        /// 清空主项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteBuyItem_Click(object sender, RoutedEventArgs e)
        {
            Buy.Source = new BitmapImage(new Uri(emptyIcon));
            Buy.Tag = null;
        }

        /// <summary>
        /// 清空副项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteBuyBItem_Click(object sender, RoutedEventArgs e)
        {
            BuyB.Source = new BitmapImage(new Uri(emptyIcon));
            BuyB.Tag = null;
        }
        
        /// <summary>
        /// 清空售卖物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSellItem_Click(object sender, RoutedEventArgs e)
        {
            Sell.Source = new BitmapImage(new Uri(emptyIcon));
            Sell.Tag = null;
        }
    }
}
