using CBHK.Model.Data;
using CBHK.ViewModel.Component.Villager.Event;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.UI.WebUI;

namespace CBHK.ViewModel.Component.Villager.Control
{
    public partial class TransactionItemViewModel(IEventAggregator EventAggregator):ObservableObject
    {
        #region Field
        private readonly IEventAggregator eventAggregator = EventAggregator;

        /// <summary>
        /// 空图像路径
        /// </summary>
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";
        #endregion

        #region Property
        [ObservableProperty]
        public Brush background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48494A"));

        #region 物品数据
        /// <summary>
        /// 1 或 0 (true/false) - true代表交易会提供经验球。Java版中所有自然生成的村民的交易都会给予经验球。
        /// </summary>
        [ObservableProperty]
        public bool rewardExp = false;

        /// <summary>
        /// 代表在交易选项失效前能进行的最大交易次数。当交易被刷新时，以2到12的随机数增加。
        /// </summary>
        [ObservableProperty]
        public int maxUses = 1;

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
        public float priceMultiplier = 0;
        #endregion

        #region 当前交易项数据
        [ObservableProperty]
        public string buyCountDisplayText = "x1";

        [ObservableProperty]
        public string buyBCountDisplayText = "x1";

        [ObservableProperty]
        public string sellCountDisplayText = "x1";

        [ObservableProperty]
        public string buyDisCountDisplayText = string.Empty;

        [ObservableProperty]
        public bool isDiscounted = false;

        [ObservableProperty]
        private ImageSource buyImageSource = new BitmapImage(new Uri("/CBHK;component/Resource/CBHK/Image/empty.png", UriKind.RelativeOrAbsolute));
        [ObservableProperty]
        private ImageSource buyBImageSource = new BitmapImage(new Uri("/CBHK;component/Resource/CBHK/Image/empty.png", UriKind.RelativeOrAbsolute));
        [ObservableProperty]
        private ImageSource sellImageSource = new BitmapImage(new Uri("/CBHK;component/Resource/CBHK/Image/empty.png", UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        private ItemStructure buyTag = null;
        [ObservableProperty]
        private ItemStructure buyBTag = null;
        [ObservableProperty]
        private ItemStructure sellTag = null;

        [ObservableProperty]
        public Visibility buyDisCountDisplayVisible = Visibility.Collapsed; 
        #endregion

        #endregion

        #region Method

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

            oldImage.ToolTip = toolTip;
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
                IsDiscounted = true;
                BuyDisCountDisplayText = "x" + price.ToString();
                BuyDisCountDisplayVisible = Visibility.Visible;
            }
            else
            {
                IsDiscounted = false;
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
                IsDiscounted = false;
                BuyDisCountDisplayVisible = Visibility.Hidden;
            }
        }

        public string GetTransactionItemData()
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

            if (BuyTag is ItemStructure buyItemData)
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

            if (BuyBTag is ItemStructure buybItemData)
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

            if (SellTag is ItemStructure sellItemData)
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

        #endregion

        #region Event
        /// <summary>
        /// 更新第一个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateBuyItem(object sender, DragEventArgs e)
        {
            if (sender is not Image currentImage || e.Data.GetData(typeof(Image)) is not Image image)
                return;
            BuyImageSource = image.Source;
            BuyTag = image.Tag as ItemStructure;
            UpdateItem(currentImage, image);
        }

        /// <summary>
        /// 更新第二个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateBuybItem(object sender, DragEventArgs e)
        {
            if (sender is not Image currentImage || e.Data.GetData(typeof(Image)) is not Image image)
                return;
            BuyBImageSource = image.Source;
            BuyBTag = image.Tag as ItemStructure;
            UpdateItem(currentImage, image);
        }

        /// <summary>
        /// 更新出售物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateSellItem(object sender, DragEventArgs e)
        {
            if (sender is not Image currentImage || e.Data.GetData(typeof(Image)) is not Image image)
                return;
            SellImageSource = image.Source;
            SellTag = image.Tag as ItemStructure;
            UpdateItem(currentImage, image);
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        private void Delete() => eventAggregator.GetEvent<RemoveTransactionItemEvent>().Publish(this);

        /// <summary>
        /// 编辑器当前交易项的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        private void Edit() => eventAggregator.GetEvent<OpenTransactionEditPanelEvent>().Publish(this);

        /// <summary>
        /// 清空主项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteBuyItem()
        {
            BuyImageSource = new BitmapImage(new Uri(emptyIcon));
            BuyTag = null;
        }

        /// <summary>
        /// 清空副项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteBuyBItem()
        {
            BuyBImageSource = new BitmapImage(new Uri(emptyIcon));
            BuyBTag = null;
        }

        /// <summary>
        /// 清空售卖物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void DeleteSellItem()
        {
            SellImageSource = new BitmapImage(new Uri(emptyIcon));
            SellTag = null;
        }
        #endregion
    }
}
