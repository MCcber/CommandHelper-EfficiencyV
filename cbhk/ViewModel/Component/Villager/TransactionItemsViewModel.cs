using CBHK.GeneralTool;
using CBHK.Model.Common;
using CBHK.View.Component.Villager;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Villager
{
    public partial class TransactionItemsViewModel:ObservableObject
    {
        #region Field
        public Image Buy = null;
        public Image BuyB = null;
        public Image Sell = null;

        /// <summary>
        /// 空图像路径
        /// </summary>
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";

        DataTable ItemTable = null;
        #endregion

        #region Property

        #region 物品数据
        /// <summary>
        /// 1 或 0 (true/false) - true代表交易会提供经验球。Java版中所有自然生成的村民的交易都会给予经验球。
        /// </summary>
        [ObservableProperty]
        public bool _rewardExp = true;

        /// <summary>
        /// 代表在交易选项失效前能进行的最大交易次数。当交易被刷新时，以2到12的随机数增加。
        /// </summary>
        [ObservableProperty]
        public int _maxUses = 0;

        /// <summary>
        /// 已经交易的次数，如果大于或等于maxUses，该交易将失效。
        /// </summary>
        [ObservableProperty]
        public int uses = 0;

        /// <summary>
        /// 村民从此交易选项中能获得的经验值。
        /// </summary>
        [ObservableProperty]
        public int xp = 0;

        /// <summary>
        /// 根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。
        /// </summary>
        [ObservableProperty]
        public int demand = 0;

        /// <summary>
        /// 添加到第一个收购物品的价格调节。
        /// </summary>
        [ObservableProperty]
        public int specialPrice = 0;

        /// <summary>
        /// 当前应用到此交易选项价格的乘数。
        /// </summary>
        [ObservableProperty]
        public float priceMultiplier = 0f;
        #endregion
        #region 当前交易项数据
        public string TransactionItemData
        {
            get
            {
                string buyItemCount = BuyCountDisplayText.Replace("x", "");
                string buyBItemCount = BuyBCountDisplayText.Replace("x", "");
                string sellItemCount = SellCountDisplayText.Replace("x", "");
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
        #region 交易物、交易物B、售卖物的显示数量文本等
        [ObservableProperty]
        public string _buyCountDisplayText = "x1";
        [ObservableProperty]
        public string _buyBCountDisplayText = "x1";
        [ObservableProperty]
        public string _buyDisCountDisplayText = null;
        [ObservableProperty]
        public string _sellCountDisplayText = "x1";
        [ObservableProperty]
        public TextDecorationCollection _buyDecorations = null;
        [ObservableProperty]
        public Visibility _buyDisCountDisplayVisible = Visibility.Collapsed;
        #endregion

        #endregion

        public void Buy_Loaded(object sender, RoutedEventArgs e) => Buy = sender as Image;

        public void BuyB_Loaded(object sender, RoutedEventArgs e) => BuyB = sender as Image;

        public void Sell_Loaded(object sender, RoutedEventArgs e) => Sell = sender as Image;

        /// <summary>
        /// 载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TransactionItems_Loaded(object sender, RoutedEventArgs e)
        {
            VillagerViewModel context = Window.GetWindow(sender as TransactionItemView).DataContext as VillagerViewModel;
            ItemTable = context.ItemTable;
        }

        /// <summary>
        /// 更新物品显示图像以及文本提示
        /// </summary>
        /// <param name="oldImage"></param>
        /// <param name="newImage"></param>
        private void UpdateItem(Image oldImage, Image newImage)
        {
            string toolTip = string.Empty;
            if (newImage.Tag is ItemStructure newItemStructure)
            {
                toolTip = newItemStructure.IDAndName;
            }

            oldImage.Source = newImage.Source;
            ToolTip tooltipObj = new()
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#484848")),
                Content = toolTip
            };
            oldImage.ToolTip = tooltipObj;
            ToolTipService.SetBetweenShowDelay(oldImage, 0);
            ToolTipService.SetInitialShowDelay(oldImage, 0);
        }

        #region 处理拖拽后的数据更新
        /// <summary>
        /// 更新第一个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateBuyItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image)) as Image;
            Image currentImage = sender as Image;
            currentImage.Tag = image.Tag;
            UpdateItem(currentImage, image);
        }

        /// <summary>
        /// 更新第二个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateBuybItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image)) as Image;
            Image currentImage = sender as Image;
            currentImage.Tag = image.Tag;
            UpdateItem(currentImage, image);
        }

        /// <summary>
        /// 更新出售物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateSellItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image)) as Image;
            Image currentImage = sender as Image;
            currentImage.Tag = image.Tag;
            UpdateItem(currentImage, image);
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
        public void UpdateDiscountData(int minor_negative = 0, int minor_positive = 0, int major_negative = 0, int major_positive = 0, int trading = 0)
        {
            //获取原价
            int original_price = int.Parse(BuyCountDisplayText.Replace("x", ""));
            int price = original_price - (int)Math.Floor((5 * major_positive + minor_positive + trading + minor_negative - 5 * major_negative) * PriceMultiplier);
            //如果最终价格不同于原价则开启打折效果
            if (price != original_price)
            {
                TextDecorationCollection textDecorationCollection = [];
                TextDecoration textDecoration = new(TextDecorationLocation.Baseline, new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BA370F")), 2), -5, TextDecorationUnit.Pixel, TextDecorationUnit.Pixel);
                textDecorationCollection.Add(textDecoration);
                BuyDecorations = textDecorationCollection;
                BuyDisCountDisplayText = "x" + price.ToString();
                BuyDisCountDisplayVisible = Visibility.Visible;
            }
            else
            {
                BuyDecorations = null;
                BuyDisCountDisplayVisible = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 恢复价格数据
        /// </summary>
        public void HideDiscountData(bool Hide = true)
        {
            if (!Hide)
            {
                BuyDecorations = null;
                BuyDisCountDisplayVisible = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void Delete(Button button)
        {
            TransactionItemView template_parent = button.FindParent<TransactionItemView>();
            VillagerViewModel context = (Window.GetWindow(button) as VillagerView).DataContext as VillagerViewModel;
            context.transactionItems.Remove(template_parent);
        }

        /// <summary>
        /// 编辑器当前交易项的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void Edit(Button button)
        {
            VillagerViewModel context = Window.GetWindow(button).DataContext as VillagerViewModel;
            context.TransactionDataGridVisibility = Visibility.Visible;
            context.BuyItemIcon = Buy.Source;
            context.BuyBItemIcon = BuyB.Source;
            context.SellItemIcon = Sell.Source;
            context.BuyItemData = Buy.Tag;
            context.BuyBItemData = BuyB.Tag;
            context.SellItemData = Sell.Tag;
            context.BuyCount = int.Parse(BuyCountDisplayText.Replace("x", ""));
            context.BuyBCount = int.Parse(BuyBCountDisplayText.Replace("x", ""));
            context.SellCount = int.Parse(SellCountDisplayText.Replace("x", ""));

            context.CurrentItem = button.FindParent<TransactionItemView>();
        }

        /// <summary>
        /// 清空主项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteBuyItem()
        {
            Buy.Source = new BitmapImage(new Uri(emptyIcon));
            Buy.Tag = null;
        }

        /// <summary>
        /// 清空副项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteBuyBItem()
        {
            BuyB.Source = new BitmapImage(new Uri(emptyIcon));
            BuyB.Tag = null;
        }

        /// <summary>
        /// 清空售卖物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteSellItem()
        {
            Sell.Source = new BitmapImage(new Uri(emptyIcon));
            Sell.Tag = null;
        }
    }
}