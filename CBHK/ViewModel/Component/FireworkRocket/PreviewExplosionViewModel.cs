using CBHK.View.Component.FireworkRocket;
using CBHK.View.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace CBHK.ViewModel.Component.FireworkRocket
{
    public partial class PreviewExplosionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "预览烟花爆炸效果";

        public void FireworkModel_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 预览效果
        /// </summary>
        private void PreviewEffect(FrameworkElement frameworkElement)
        {
            //FireworkRocketView fireworkRocket = Window.GetWindow(frameworkElement) as FireworkRocketView;
            //FireworkRocketPageView fireworkRocketPages = SelectedFireworkRocketPage.Content as FireworkRocketPageView;
            //FireworkRocketPageViewModel fireworkRocketPagesDataContext = fireworkRocketPages.DataContext as FireworkRocketPageViewModel;
            //if (fireworkRocketPagesDataContext.MainColorList.Count + fireworkRocketPagesDataContext.FadeColorList.Count == 0) return;

            ////烟花火箭模型引用
            //TranslateTransform3D fireworkModel = fireworkRocket.FireworkModel;
            ////获取用户摄像机
            //TranslateTransform3D userCamera = fireworkRocket.UserCamera;
            ////清除上一个烟花粒子簇
            //fireworkRocketPagesDataContext.ParticleContainer.Children.Clear();

            //#region 初始化动画
            //DoubleAnimation cameraAnimation = new()
            //{
            //    From = 10,
            //    By = fireworkRocketPagesDataContext.FireworkFlyHeight * (fireworkRocketPagesDataContext.Duration <= 0 ? 1 : fireworkRocketPagesDataContext.Duration),
            //    Duration = new Duration(TimeSpan.FromSeconds(0.5)),
            //    AutoReverse = false,
            //    FillBehavior = FillBehavior.HoldEnd
            //};
            //DoubleAnimation fireworkRocketAnimation = new()
            //{
            //    From = 1,
            //    By = fireworkRocketPagesDataContext.FireworkFlyHeight * (fireworkRocketPagesDataContext.Duration <= 0 ? 1 : fireworkRocketPagesDataContext.Duration),
            //    Duration = new Duration(TimeSpan.FromSeconds(0.5)),
            //    AutoReverse = false,
            //    FillBehavior = FillBehavior.Stop
            //};
            //#endregion

            //#region 执行动画
            //fireworkRocketAnimation.Completed += fireworkRocketPagesDataContext.FireworkExploded;
            //userCamera.BeginAnimation(TranslateTransform3D.OffsetYProperty, cameraAnimation);
            //fireworkModel.BeginAnimation(TranslateTransform3D.OffsetYProperty, fireworkRocketAnimation);
            //#endregion
        }
    }
}
