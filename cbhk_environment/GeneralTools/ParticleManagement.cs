using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace cbhk_environment.GeneralTools
{
    public class ParticleManagement
    {
        /// <summary>
        /// 粒子路径
        /// </summary>
        private Geometry particleGeometry;

        /// <summary>
        /// 粒子个数
        /// </summary>
        private int particleCount = 100;

        /// <summary>
        /// 粒子最小尺寸
        /// </summary>
        private static int sizeMin = 10;

        /// <summary>
        /// 粒子最大尺寸
        /// </summary>
        private int sizeMax = 20;

        /// <summary>
        /// 随机数
        /// </summary>
        private Random random;

        /// <summary>
        /// 粒子列表
        /// </summary>
        private List<Particle> particles;

        /// <summary>
        /// 粒子容器
        /// </summary>
        private Canvas containerParticles;


        public ParticleManagement(Geometry _path, int _maxRadius, int _particleCount, Canvas _containerParticles)
        {
            particleGeometry = _path;
            particleCount = _particleCount;
            sizeMax = _maxRadius;
            containerParticles = _containerParticles;
            random = new Random();
            particles = new List<Particle>();
            GenerateParticle();
        }

        /// <summary>
        /// 初始化粒子
        /// </summary>
        private void GenerateParticle()
        {
            //清空粒子队列
            particles.Clear();
            containerParticles.Children.Clear();

            //生成粒子
            for (int i = 0; i < particleCount; i++)
            {
                double size = random.Next(sizeMin, sizeMax + 1);
                while (true)
                {
                    Point po = new Point(random.Next((int)particleGeometry.Bounds.Left, (int)particleGeometry.Bounds.Right), random.Next((int)particleGeometry.Bounds.Top, (int)particleGeometry.Bounds.Bottom));
                    if (particleGeometry.FillContains(po, 2, ToleranceType.Absolute))
                    {
                        Particle p = new Particle
                        {
                            Shape = new Ellipse
                            {
                                Width = size,
                                Height = size,
                                Stretch = System.Windows.Media.Stretch.Fill,
                                Fill = GetRandomColorBursh(),
                            },
                            Position = po,
                        };
                        SetParticleSizeAnimation(p.Shape);
                        particles.Add(p);
                        Canvas.SetLeft(p.Shape, p.Position.X);
                        Canvas.SetTop(p.Shape, p.Position.Y);
                        containerParticles.Children.Add(p.Shape);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 设置粒子大小动画
        /// </summary>
        private void SetParticleSizeAnimation(Ellipse p)
        {
            Storyboard sb = new Storyboard();
            //动画完成事件 再次设置此动画
            sb.Completed += (S, E) =>
            {
                SetParticleSizeAnimation(p);
            };
            int size = random.Next(sizeMin, sizeMax + 1);
            int time = random.Next(100, 1000);
            DoubleAnimation daX = new DoubleAnimation(size, new Duration(TimeSpan.FromMilliseconds(time)));
            DoubleAnimation daY = new DoubleAnimation(size, new Duration(TimeSpan.FromMilliseconds(time)));
            Storyboard.SetTarget(daX, p);
            Storyboard.SetTarget(daY, p);
            Storyboard.SetTargetProperty(daX, new PropertyPath("Width"));
            Storyboard.SetTargetProperty(daY, new PropertyPath("Height"));
            sb.Children.Add(daX);
            sb.Children.Add(daY);
            sb.Begin();
        }

        /// <summary>
        /// 获取随机颜色画刷
        /// </summary>
        private SolidColorBrush GetRandomColorBursh()
        {
            byte r = (byte)random.Next(128, 256);
            byte g = (byte)random.Next(128, 256);
            byte b = (byte)random.Next(128, 256);
            return new SolidColorBrush(Color.FromArgb(125, r, g, b));
        }

        //private ParticleManagement ps;
        //private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    Geometry g = CreateTextPath("M", new Point(cvs_particleContainer.Margin.Left, cvs_particleContainer.Margin.Top), new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 150);
        //    ps = new ParticleManagement(g, 10, 200, cvs_particleContainer);
        //}
        ///// <summary>
        ///// 创建文本路径
        ///// </summary>
        ///// <param name="word">文本字符串</param>
        ///// <param name="point">显示位置</param>
        ///// <param name="typeface">字体信息</param>
        ///// <param name="fontSize">字体大小</param>
        ///// <returns></returns>
        //private Geometry CreateTextPath(string word, Point point, Typeface typeface, int fontSize)
        //{
        //    FormattedText text = new FormattedText(word, new System.Globalization.CultureInfo("en-US"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
        //    Geometry g = text.BuildGeometry(point);
        //    PathGeometry path = g.GetFlattenedPathGeometry();
        //    return path;
        //}
    }

    public class Particle
    {
        /// <summary>
        /// 形状
        /// </summary>
        public Ellipse Shape;
        /// <summary>
        /// 坐标
        /// </summary>
        public Point Position;
    }
}
