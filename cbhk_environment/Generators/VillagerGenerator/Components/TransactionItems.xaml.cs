﻿using cbhk_environment.GeneralTools.ScrollViewerHelper;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.VillagerGenerator.Components
{
    /// <summary>
    /// TransactionItems.xaml 的交互逻辑
    /// </summary>
    public partial class TransactionItems : UserControl
    {
        //箭头图像背景文件路径
        string arrow_path = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\images\\arrow.png";
        //按钮图像背景文件路径
        string background_button_path = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\images\\background_button.png";
        //黑边框
        SolidColorBrush BlackBrush = new SolidColorBrush(Color.FromRgb(0,0,0));
        //白边框
        SolidColorBrush WhiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

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
        private string maxUses = "0";
        public string MaxUses
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
        private string uses = "0";
        public string Uses
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
        private string xp = "0";
        public string Xp
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
        private string demand = "0";
        public string Demand
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
        private string specialPrice = "0";
        public string SpecialPrice
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
        private string priceMultiplier = "0";
        public string PriceMultiplier
        {
            get { return priceMultiplier; }
            set
            {
                priceMultiplier = value;
            }
        }
        #endregion

        #region 当前交易项数据
        public string TransactionItemData
        {
            get
            {
                string result;
                string rewardExp = "rewardExp:" + (RewardExp?1:0) + "b,";
                string maxUses = MaxUses.Trim() != "" ? "maxUses:" + MaxUses + ",":"";
                string uses = Uses.Trim() != "" ? "uses:" + Uses + ",":"";
                string buy = Buy.Tag != null ? "buy:" + (Buy.Tag.ToString().Contains("{")?Buy.Tag.ToString():"{id:\"minecraft:"+Buy.Tag.ToString().Split(' ')[0] + "\",Count:1b}") +",":"";
                string buyB = BuyB.Tag != null ?"buyB" + (BuyB.Tag.ToString().Contains("{") ? BuyB.Tag.ToString() : "{id:\"minecraft:" + BuyB.Tag.ToString().Split(' ')[0] + "\",Count:1b}") + ",":"";
                string sell = Sell.Tag != null ?"sell:"+ (Sell.Tag.ToString().Contains("{") ? Sell.Tag.ToString() : "{id:\"minecraft:" + Sell.Tag.ToString().Split(' ')[0] + "\",Count:1b}") + ",":"";
                string xp = Xp.Trim() != "" ?"xp:"+Xp+",":"";
                string demand = Demand.Trim() != "" ? "demand:"+Demand+"," :"";
                string specialPrice = SpecialPrice.Trim() != "" ? "specialPrice:"+ SpecialPrice+"," :"";
                string priceMultiplier = PriceMultiplier.Trim() != "" ? "priceMultiplier:"+PriceMultiplier+"," :"";
                result = rewardExp + maxUses + uses + buy + buyB + sell + xp + demand + specialPrice + priceMultiplier;
                result = "{"+result.TrimEnd(',')+"}";
                return result;
            }
        }
        #endregion

        public TransactionItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入箭头背景图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArrowLoaded(object sender, RoutedEventArgs e)
        {
            Image current_image = sender as Image;
            current_image.Source = new BitmapImage(new Uri(arrow_path,UriKind.Absolute));
        }

        /// <summary>
        /// 载入按钮背景图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundButtonLoaded(object sender, RoutedEventArgs e)
        {
            Grid current_grid = sender as Grid;
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(background_button_path);
            bitmap = GeneralTools.ChangeBitmapSize.Magnifier(bitmap,10);
            BitmapImage bitmapImage = GeneralTools.BitmapImageConverter.ToBitmapImage(bitmap);
            current_grid.Background = new ImageBrush(bitmapImage);
        }

        /// <summary>
        /// 鼠标进入后边框变白
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WhiteBorder(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.BorderBrush = WhiteBrush;
        }

        /// <summary>
        /// 鼠标离开后边框变黑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlackBorder(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.BorderBrush = BlackBrush;
        }

        /// <summary>
        /// 更新物品显示图像以及文本提示
        /// </summary>
        /// <param name="old_image"></param>
        /// <param name="new_image"></param>
        private void UpdateItem(Image old_image,Image new_image)
        {
            string file_name = AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + new_image.Tag.ToString().Split(' ')[0] + ".png";
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(file_name);
            bitmap = GeneralTools.ChangeBitmapSize.Magnifier(bitmap,20);
            BitmapImage bitmapImage = GeneralTools.BitmapImageConverter.ToBitmapImage(bitmap);
            old_image.Source = bitmapImage;
            old_image.Tag = new_image.Tag;
            old_image.ToolTip = old_image.Tag.ToString();
            ToolTipService.SetShowDuration(old_image, 1000);
            ToolTipService.SetInitialShowDelay(old_image, 0);
        }

        /// <summary>
        /// 更新第一个收购物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateBuyItem(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image current_image = sender as Image;
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
            UpdateItem(current_image, image);
        }

        /// <summary>
        /// 更新价格数据
        /// </summary>
        /// <param name="demand">根据需求确定的第一个收购物品的价格调节。当村民重新供应后更新此字段。</param>
        /// <param name="priceMultiplier">当前应用到此交易选项价格的乘数。</param>
        /// <param name="minor_negative">言论类型</param>
        /// <param name="trading">言论类型</param>
        /// <param name="specialPrice">添加到第一个收购物品的价格调节。</param>
        public void UpdateDiscountData(int minor_negative = 0,int trading = 0)
        {
            //获取原价
            if (BuyItemCount.Text == "" || int.Parse(BuyItemCount.Text) < 2) return;
            int original_price = int.Parse(BuyItemCount.Text);
            int price = (int.Parse(Demand) * int.Parse(PriceMultiplier) * original_price) + (int.Parse(PriceMultiplier) * minor_negative) - (int.Parse(PriceMultiplier) * trading * 10) + int.Parse(SpecialPrice) + original_price;

            //如果最终价格不同于原价则开启打折效果
            if (price != original_price)
            {
                BuyItemCount.TextDecorations = DiscountStyle.TextDecorations;
                DisCount.Text = price.ToString();
                DisCount.Visibility = Visibility.Visible;
            }
            else
            {
                BuyItemCount.TextDecorations = null;
                DisCount.Text = "";
            }
        }

        /// <summary>
        /// 恢复价格数据
        /// </summary>
        public void HideDiscountData(bool Hide = true)
        {
            if(Hide)
            {
                BuyItemCount.TextDecorations = null;
                DisCount.Visibility = Visibility.Collapsed;
            }
            else
            {
                BuyItemCount.TextDecorations = DiscountStyle.TextDecorations;
                DisCount.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 设置物品信息窗体显示位置
        /// </summary>
        public void SetInfomationWindowPosition()
        {
            Window window = Window.GetWindow(this);
            Point point = TransformToAncestor(window).Transform(new Point(0, 0));
            point = PointToScreen(point);
            villager_datacontext.transactionItemDataForm.Left = point.X;
            villager_datacontext.transactionItemDataForm.Top = point.Y;
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            CustomControls.IconTextButtons iconTextButtons = sender as CustomControls.IconTextButtons;
            TransactionItems template_parent = iconTextButtons.FindParent<TransactionItems>();
            villager_datacontext.transactionItems.Remove(template_parent);
        }
    }
}
