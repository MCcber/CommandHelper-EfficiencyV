using CBHK.Domain;
using CBHK.Model.Data;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.CustomControl.Container
{
    public partial class TransactionItem : Control
    {
        #region Field
        public Image Buy = null;
        public Image BuyB = null;
        public Image Sell = null;
        private Dictionary<string, string> ItemIDAndNameMap;

        /// <summary>
        /// 空图像路径
        /// </summary>
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";

        private CBHKDataContext context = null;
        private DataService dataService = null;
        #endregion

        #region Property

        #region 物品数据
        /// <summary>
        /// 1 或 0 (true/false) - true代表交易会提供经验球。Java版中所有自然生成的村民的交易都会给予经验球。
        /// </summary>
        public bool RewardExp
        {
            get { return (bool)GetValue(RewardExpProperty); }
            set { SetValue(RewardExpProperty, value); }
        }

        public static readonly DependencyProperty RewardExpProperty =
            DependencyProperty.Register(nameof(RewardExp), typeof(bool), typeof(TransactionItem), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 代表在交易选项失效前能进行的最大交易次数。当交易被刷新时，以2到12的随机数增加。
        /// </summary>
        public int MaxUses
        {
            get { return (int)GetValue(MaxUsesProperty); }
            set { SetValue(MaxUsesProperty, value); }
        }

        public static readonly DependencyProperty MaxUsesProperty =
            DependencyProperty.Register(nameof(MaxUses), typeof(int), typeof(TransactionItem), new PropertyMetadata(0));

        /// <summary>
        /// 已经交易的次数，如果大于或等于maxUses，该交易将失效。
        /// </summary>
        public int Uses
        {
            get { return (int)GetValue(UsesProperty); }
            set { SetValue(UsesProperty, value); }
        }

        public static readonly DependencyProperty UsesProperty =
            DependencyProperty.Register(nameof(Uses), typeof(int), typeof(TransactionItem), new PropertyMetadata(0));

        /// <summary>
        /// 村民从此交易选项中能获得的经验值。
        /// </summary>
        public int Xp
        {
            get { return (int)GetValue(XpProperty); }
            set { SetValue(XpProperty, value); }
        }

        public static readonly DependencyProperty XpProperty =
            DependencyProperty.Register(nameof(Xp), typeof(int), typeof(TransactionItem), new PropertyMetadata(0));

        /// <summary>
        /// 根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。
        /// </summary>
        public int Demand
        {
            get { return (int)GetValue(DemandProperty); }
            set { SetValue(DemandProperty, value); }
        }

        public static readonly DependencyProperty DemandProperty =
            DependencyProperty.Register(nameof(Demand), typeof(int), typeof(TransactionItem), new PropertyMetadata(0));

        /// <summary>
        /// 添加到第一个收购物品的价格调节。
        /// </summary>
        public int SpecialPrice
        {
            get { return (int)GetValue(SpecialPriceProperty); }
            set { SetValue(SpecialPriceProperty, value); }
        }

        public static readonly DependencyProperty SpecialPriceProperty =
            DependencyProperty.Register(nameof(SpecialPrice), typeof(int), typeof(TransactionItem), new PropertyMetadata(0));

        /// <summary>
        /// 当前应用到此交易选项价格的乘数。
        /// </summary>
        public float PriceMultiplier
        {
            get { return (float)GetValue(PriceMultiplierProperty); }
            set { SetValue(PriceMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PriceMultiplierProperty =
            DependencyProperty.Register(nameof(PriceMultiplier), typeof(float), typeof(TransactionItem), new PropertyMetadata(0));
        #endregion

        /// <summary>
        /// 当前交易项数据
        /// </summary>
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

                string result = "";
                string rewardExp = "rewardExp:" + (RewardExp ? 1 : 0) + "b,";
                string maxUses = "maxUses:" + MaxUses + ",";
                string uses = "uses:" + Uses + ",";

                #region 购入物品AB与卖出物品数据
                //补齐双引号对
                string buyData = "{oldID:\"minecraft:stick\"}";
                string buyBData = "{}";
                string sellData = "{oldID:\"minecraft:stick\"}";

                if (Buy.Tag is ItemStructure buyItemData)
                {
                    if (buyItemData.NBT is not null)
                    {
                        buyData = Regex.Replace(buyItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                    }
                    else
                    {
                        buyData = "{oldID:\"minecraft:" + buyItemData.IDAndName.Split(':')[0] + "\"}";
                    }
                }

                if (BuyB.Tag is ItemStructure buybItemData)
                {
                    if (buybItemData.NBT is not null)
                    {
                        buyBData = Regex.Replace(buybItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                    }
                    else
                    {
                        buyBData = "{oldID:\"minecraft:" + buybItemData.IDAndName.Split(':')[0] + "\"}";
                    }
                }

                if (Sell.Tag is ItemStructure sellItemData)
                {
                    if (sellItemData.NBT is not null)
                    {
                        sellData = Regex.Replace(sellItemData.NBT, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                    }
                    else
                    {
                        sellData = "{oldID:\"minecraft:" + sellItemData.IDAndName.Split(':')[0] + "\"}";
                    }
                }

                //清除数值型数据的单位
                buyData = Regex.Replace(buyData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                buyBData = Regex.Replace(buyBData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                sellData = Regex.Replace(sellData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

                JObject buyObj = JObject.Parse(buyData);
                JObject buybObj = JObject.Parse(buyBData);
                JObject sellObj = JObject.Parse(sellData);

                buyObj["count"] = int.Parse(buyItemCount);
                buybObj["count"] = int.Parse(buyBItemCount);
                sellObj["count"] = int.Parse(sellItemCount);
                //去除双引号对
                string buy = buyData != "{}" ? "buy:" + buyObj.ToString().Replace("\r", "").Replace("\n", "") + "," : "";
                buy = Regex.Replace(buy, @"\s+", "");
                string buyB = buyBData != "{}" ? "buyB:" + buybObj.ToString().Replace("\r", "").Replace("\n", "") + "," : "";
                buyB = Regex.Replace(buyB, @"\s+", "");
                string sell = "sell:" + sellObj.ToString().Replace("\r", "").Replace("\n", "") + ",";
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

        #region 交易物、交易物B、售卖物的显示数量文本等
        public string BuyCountDisplayText
        {
            get { return (string)GetValue(BuyCountDisplayTextProperty); }
            set { SetValue(BuyCountDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty BuyCountDisplayTextProperty =
            DependencyProperty.Register(nameof(BuyCountDisplayText), typeof(string), typeof(TransactionItem), new PropertyMetadata("x1"));

        public string BuyBCountDisplayText
        {
            get { return (string)GetValue(BuyBCountDisplayTextProperty); }
            set { SetValue(BuyBCountDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty BuyBCountDisplayTextProperty =
            DependencyProperty.Register(nameof(BuyBCountDisplayText), typeof(string), typeof(TransactionItem), new PropertyMetadata("x1"));

        public string BuyDisCountDisplayText
        {
            get { return (string)GetValue(BuyDisCountDisplayTextProperty); }
            set { SetValue(BuyDisCountDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty BuyDisCountDisplayTextProperty =
            DependencyProperty.Register(nameof(BuyDisCountDisplayText), typeof(string), typeof(TransactionItem), new PropertyMetadata(default(string)));

        public string SellCountDisplayText
        {
            get { return (string)GetValue(SellCountDisplayTextProperty); }
            set { SetValue(SellCountDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty SellCountDisplayTextProperty =
            DependencyProperty.Register(nameof(SellCountDisplayText), typeof(string), typeof(TransactionItem), new PropertyMetadata(default(string)));

        public TextDecorationCollection BuyDecorations
        {
            get { return (TextDecorationCollection)GetValue(BuyDecorationsProperty); }
            set { SetValue(BuyDecorationsProperty, value); }
        }

        public static readonly DependencyProperty BuyDecorationsProperty =
            DependencyProperty.Register(nameof(BuyDecorations), typeof(TextDecorationCollection), typeof(TransactionItem), new PropertyMetadata(default(TextDecorationCollection)));


        public Visibility BuyDisCountDisplayVisible
        {
            get { return (Visibility)GetValue(BuyDisCountDisplayVisibleProperty); }
            set { SetValue(BuyDisCountDisplayVisibleProperty, value); }
        }

        public static readonly DependencyProperty BuyDisCountDisplayVisibleProperty =
            DependencyProperty.Register(nameof(BuyDisCountDisplayVisible), typeof(Visibility), typeof(TransactionItem), new PropertyMetadata(default(Visibility)));
        #endregion

        #endregion

        #region Method
        public TransactionItem(CBHKDataContext context, DataService dataService)
        {
            this.context = context;
            this.dataService = dataService;
            ItemIDAndNameMap = dataService.GetItemIDAndNameGroupByVersionMap().SelectMany(item => item.Value).ToDictionary();
            Loaded += TransactionItemViewLoaded;
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

        /// <summary>
        /// 处理打折后的数据
        /// </summary>
        /// <param name="demand">根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。</param>
        /// <param name="priceMultiplier">当前应用到此交易选项价格的乘数。</param>
        /// <param name="minornegative">言论类型</param>
        /// <param name="trading">言论类型</param>
        /// <param name="specialPrice">添加到第一个收购物品的价格调节。</param>
        public void UpdateDiscountData(int minornegative = 0, int minorpositive = 0, int majornegative = 0, int majorpositive = 0, int trading = 0)
        {
            //获取原价
            int originalprice = int.Parse(BuyCountDisplayText.Replace("x", ""));
            int price = originalprice - (int)Math.Floor((5 * majorpositive + minorpositive + trading + minornegative - 5 * majornegative) * PriceMultiplier);
            //如果最终价格不同于原价则开启打折效果
            if (price != originalprice)
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
                //BuyDecorations = null;
                //BuyDisCountDisplayVisible = Visibility.Hidden;
            }
        }

        #endregion

        #region Event
        public void BuyLoaded(object sender, RoutedEventArgs e) => Buy = sender as Image;

        public void BuyBLoaded(object sender, RoutedEventArgs e) => BuyB = sender as Image;

        public void SellLoaded(object sender, RoutedEventArgs e) => Sell = sender as Image;

        /// <summary>
        /// 载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TransactionItemViewLoaded(object sender, RoutedEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48494A"));
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

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

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void Delete(Button button)
        {
            //TransactionItemView templateparent = button.FindParent<TransactionItemView>();
            VillagerViewModel context = (Window.GetWindow(button) as VillagerView).DataContext as VillagerViewModel;
            //context.TransactionItemList.Remove(templateparent);
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
            //context.CurrentItem = button.FindParent<TransactionItemView>();
            context.TransactionDataGridVisibility = Visibility.Visible;
            context.RewardExp = RewardExp;
            context.VillagerGetXp = Xp;

            context.BuyItemIcon = Buy.Source;
            context.BuyBItemIcon = BuyB.Source;
            context.SellItemIcon = Sell.Source;

            context.BuyItemData = Buy.Tag;
            context.BuyBItemData = BuyB.Tag;
            context.SellItemData = Sell.Tag;

            //context.BuyCount = int.Parse(BuyCountDisplayText.Replace("x", ""));
            //context.BuyBCount = int.Parse(BuyBCountDisplayText.Replace("x", ""));
            //context.SellCount = int.Parse(SellCountDisplayText.Replace("x", ""));

            context.MaxUses = MaxUses;
            context.Uses = Uses;
            context.Demand = Demand;
            context.SpecialPrice = SpecialPrice;
            context.PriceMultiplier = PriceMultiplier;
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
        #endregion
    }
}
