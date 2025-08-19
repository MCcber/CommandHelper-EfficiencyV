using CBHK.CustomControl;
using CBHK.CustomControl.AnimationComponents;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryIoc;
using HelixToolkit.Wpf;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CBHK.View.Generator;
using CBHK.ViewModel.Component.Item;
using CBHK.View.Component.Item;
using CBHK.Utility.MessageTip;
using CBHK.Utility.Common;

namespace CBHK.ViewModel.Generator
{
    public partial class ArmorStandViewModel : ObservableObject
    {
        #region Field
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconArmorStand.png";
        /// <summary>
        /// 开始三轴合一
        /// </summary>
        private bool UsingThreeAxis = false;
        private ArmorStandCameraMovementType _armorStandCameraMovementType;
        private ArmorStandCameraOverTheShoulderType _armorStandCameraOverTheShoulderType;
        private bool IsLeftShoulder = false;
        //右上角Gizimo中圆锥的底面半径
        double ConeBaseRadius = 0.8;
        //右上角Gizimo中圆锥的高
        double ConeHeight = 2;
        //右上角Gizimo中圆锥离原点的距离
        double ConeDistance = 2.8;
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;

        /// <summary>
        /// 当前视图模型
        /// </summary>
        private Viewport3D ArmorStandViewer = null;
        private ModelVisual3D ModelGroup = null;

        /// <summary>
        /// ArmorStand的所有NBT项
        /// </summary>
        private List<string> ArmorStandNBTList = [];
        /// <summary>
        /// 动画容器
        /// </summary>
        private AnimationContainer animationContainer = null;

        /// <summary>
        /// 超出三轴合一按钮范围
        /// </summary>
        private bool OutOfThreeAxis = false;
        /// <summary>
        /// 当前三轴合一按钮位置
        /// </summary>
        private Point CurrentButtonCenter;
        /// <summary>
        /// 布尔型NBT链表
        /// </summary>
        List<string> BoolTypeNBT = [];
        /// <summary>
        /// 禁止移除或改变总值
        /// </summary>
        private int CannotTakeOrReplceSum;
        /// <summary>
        /// 禁止添加或改变总值
        /// </summary>
        private int CannotPlaceOrReplaceSum;
        /// <summary>
        /// 禁止添加总值
        /// </summary>
        private int CannotPlaceSum;
        /// <summary>
        /// 布尔NBT集合
        /// </summary>
        StackPanel NBTList = null;

        private readonly IContainerProvider _container;

        /// <summary>
        /// 样式化文本框引用
        /// </summary>
        StylizedTextBox stylizedTextBox = null;
        /// <summary>
        /// 标签文本框
        /// </summary>
        TagRichTextBox tagRichTextBox = null;

        private Uri stoneUri = new(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\smooth_stone.png");
        private Uri stoneSideUri = new(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\stoneSide.png");
        public BitmapImage axisRed = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\axisRed.png"));
        public BitmapImage axisGreen = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\axisGreen.png"));
        public BitmapImage axisBlue = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\axisBlue.png"));
        public BitmapImage axisGray = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\axisGray.png"));

        //版本切换锁,防止属性之间无休止更新
        private bool permission_switch_lock = false;

        #region 重置动作的按钮前景颜色对象
        //灰色
        private SolidColorBrush GrayBrush = new((Color)ColorConverter.ConvertFromString("#8F8F8F"));
        //白色
        private SolidColorBrush BlackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        #endregion

        #region 所有3D视图对象
        public PerspectiveCamera MainCamera = new();
        public PerspectiveCamera GizimoCamera = new();
        public Viewport3D SceneGizmo = new();
        public GeometryModel3D HeadModel = null;
        public GeometryModel3D LeftLegModel = null;
        public GeometryModel3D RightLegModel = null;
        public GeometryModel3D TopModel = null;
        public GeometryModel3D BottomModel = null;
        public GeometryModel3D LeftModel = null;
        public GeometryModel3D RightModel = null;

        private double lastMousePosX = 0.0;

        private double lastMousePosY = 0.0;

        private double deltaMoveX;

        private double deltaMoveY;
        #endregion

        /// <summary>
        /// 记录上一次的鼠标位置
        /// </summary>
        private Point LastCursorPosition;

        #region 控制右侧预览视图的双向旋转
        private double horizontalAngleDelta = 0;
        private double verticalAngleDelta = 0;
        #endregion

        #region 三轴合一数据更新载体
        TextBlock XAxis = new();
        TextBlock YAxis = new();
        TextBlock ZAxis = new();

        //用于自增和自减
        float XAxisValue = 0f;
        float YAxisValue = 0f;
        float ZAxisValue = 0f;
        #endregion

        #endregion

        #region Property
        /// <summary>
        /// 生成器标题
        /// </summary>
        [ObservableProperty]
        public string _title = "盔甲架生成器";

        /// <summary>
        /// 预览文本
        /// </summary>
        [ObservableProperty]
        public string _previewModeText = "预览";

        /// <summary>
        /// 预览功能菜单可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _previewMenuVisibility = Visibility.Visible;

        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showGeneratorResult = false;

        /// <summary>
        /// 名称
        /// </summary>
        [ObservableProperty]
        private string _customName = "";

        /// <summary>
        /// 标签列表
        /// </summary>
        [ObservableProperty]
        private string _tagList = "";

        /// <summary>
        /// 整体水平旋转
        /// </summary>
        [ObservableProperty]
        private float _rotationX = 0f;

        #region 是否拥有副手权限
        private Visibility haveOffHandPermission = Visibility.Visible;
        public Visibility HaveOffHandPermission
        {
            get => haveOffHandPermission;
            set
            {
                SetProperty(ref haveOffHandPermission, value);
                if (!permission_switch_lock)
                {
                    permission_switch_lock = !permission_switch_lock;
                    UseMainHandPermission = SelectedVersion.Text == "1.9.0" ? Visibility.Collapsed : Visibility.Visible;
                    permission_switch_lock = false;
                }
            }
        }
        #endregion

        #region 是否与主手共用权限
        private Visibility useMainHandPermission = Visibility.Collapsed;
        public Visibility UseMainHandPermission
        {
            get => useMainHandPermission;
            set
            {
                SetProperty(ref useMainHandPermission, value);
                if (!permission_switch_lock)
                {
                    permission_switch_lock = !permission_switch_lock;
                    HaveOffHandPermission = SelectedVersion.Text == "1.8.0" ? Visibility.Collapsed : Visibility.Visible;
                    permission_switch_lock = false;
                }
            }
        }
        #endregion

        #region 为盔甲架和坐标轴映射纹理
        [ObservableProperty]
        public BitmapImage _armImage = null;
        /// <summary>
        /// 木头纹理
        /// </summary>
        [ObservableProperty]
        public BitmapImage _woodenImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\oak_planks.png"));
        [ObservableProperty]
        public BitmapImage _horizontalImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\HorizontalPlanks.png"));
        [ObservableProperty]
        public BitmapImage _stone = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\smooth_stone.png"));
        [ObservableProperty]
        public BitmapImage _stoneSide = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Image\stoneSide.png"));
        #endregion

        #region 名称可见性
        [ObservableProperty]
        private bool _customNameVisible = false;
        private string CustomNameVisibleString
        {
            get => CustomNameVisible ? "CustomNameVisible:1b," : "";
        }
        #endregion

        #region BoolNBTList
        private string BoolNBTList
        {
            get
            {
                string result = "";
                foreach (string item in BoolTypeNBT)
                {
                    result += item + ":1b,";
                }
                return result;
            }
        }
        #endregion

        #region DisabledValue
        private string DisabledValue
        {
            get
            {
                string result = CannotPlaceSum + CannotTakeOrReplceSum + CannotPlaceOrReplaceSum + "";
                return int.Parse(result) > 0 ? "DisabledSlots:" + result + "," : "";
            }
        }
        #endregion

        #region PoseString
        private string PoseString
        {
            get
            {
                string result = "";
                bool haveValue = HeadXValue || HeadYValue || HeadZValue || BodyXValue || BodyYValue || BodyZValue || LArmXValue || LArmYValue || LArmZValue || RArmXValue || RArmYValue || RArmZValue || LLegXValue || LLegYValue || LLegZValue || RLegXValue || RLegYValue || RLegZValue;

                result = haveValue ? (HeadXValue || HeadYValue || HeadZValue ? "Head:[" + HeadX + "f," + HeadY + "f," + HeadZ + "f]," : "") + (BodyXValue || BodyYValue || BodyZValue ? "Body:[" + BodyX + "f," + BodyY + "f," + BodyZ + "f]," : "")
                      + (LArmXValue || LArmYValue || LArmZValue ? "LeftArm:[" + LArmX + "f," + LArmY + "f," + LArmZ + "f]," : "")
                      + (RArmXValue || RArmYValue || RArmZValue ? "RightArm:[" + RArmX + "f," + RArmY + "f," + RArmZ + "f]," : "")
                      + (LLegXValue || LLegYValue || LLegZValue ? "LeftLeg:[" + LLegX + "f," + LLegY + "f," + LLegZ + "f]," : "")
                      + (RLegXValue || RLegYValue || RLegZValue ? "RightLeg:[" + RLegX + "f," + RLegY + "f," + RLegZ + "f]," : "") : "";

                result = result.ToString() != "" ? "Pose:{" + result.TrimEnd(',') + "}" : "";
                return result.ToString();
            }
        }
        #endregion

        #region 摄像机初始坐标和朝向
        double initHorizontalAngle = 0;
        double initVerticalAngle = 0;
        #endregion

        #region 重置按钮属性

        #region 是否可以重置所有动作
        private bool canResetAllPose;
        public bool CanResetAllPose
        {
            get => canResetAllPose;
            set
            {
                SetProperty(ref canResetAllPose, value);
                ResetAllPoseButtonForeground = CanResetAllPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion

        /// <summary>
        /// 重置所有动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetAllPoseButtonForeground;

        #region 是否可以重置头部动作
        private bool canResetHeadPose;
        public bool CanResetHeadPose
        {
            get => canResetHeadPose;
            set
            {
                SetProperty(ref canResetHeadPose, value);
                ResetHeadPoseButtonForeground = CanResetHeadPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        /// <summary>
        /// 重置头部动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetHeadPoseButtonForeground;

        #region 是否可以重置身体动作
        private bool canResetBodyPose;
        public bool CanResetBodyPose
        {
            get => canResetBodyPose;
            set
            {
                SetProperty(ref canResetBodyPose, value);
                ResetBodyPoseButtonForeground = CanResetBodyPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        #region 重置身体动作的按钮前景
        [ObservableProperty]
        private Brush _resetBodyPoseButtonForeground;
        #endregion

        #region 是否可以重置左臂动作
        private bool canResetLarmPose;
        public bool CanResetLArmPose
        {
            get => canResetLarmPose;
            set
            {
                SetProperty(ref canResetLarmPose, value);
                ResetLArmPoseButtonForeground = CanResetLArmPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        /// <summary>
        /// 重置左臂动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetLArmPoseButtonForeground;

        #region 是否可以重置右臂动作
        private bool canResetRArmPose;
        public bool CanResetRArmPose
        {
            get => canResetRArmPose;
            set
            {
                SetProperty(ref canResetRArmPose, value);
                ResetRArmPoseButtonForeground = CanResetRArmPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        /// <summary>
        /// 重置右臂动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetRArmPoseButtonForeground;

        #region 是否可以重置左腿动作
        private bool canResetLLegPose;
        public bool CanResetLLegPose
        {
            get => canResetLLegPose;
            set
            {
                SetProperty(ref canResetLLegPose, value);
                ResetLLegPoseButtonForeground = CanResetLLegPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        /// <summary>
        /// 重置左臂动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetLLegPoseButtonForeground;

        #region 是否可以重置右腿动作
        private bool canResetRLegPose;
        public bool CanResetRLegPose
        {
            get => canResetRLegPose;
            set
            {
                SetProperty(ref canResetRLegPose, value);
                ResetRLegPoseButtonForeground = CanResetRLegPose ? BlackBrush : GrayBrush;
            }
        }
        #endregion
        /// <summary>
        /// 重置右臂动作的按钮前景
        /// </summary>
        [ObservableProperty]
        private Brush _resetRLegPoseButtonForeground;
        #endregion

        #region 头部XYZ
        private bool HeadXValue;
        private float headX = 0f;
        public float HeadX
        {
            get => headX;
            set
            {
                SetProperty(ref headX, value);
                HeadXValue = HeadX != 0f ? true : false;
            }
        }

        private bool HeadYValue;
        private float headY = 0f;
        public float HeadY
        {
            get => headY;
            set
            {
                SetProperty(ref headY, value);
                HeadYValue = HeadY != 0f ? true : false;
            }
        }

        private bool HeadZValue;
        private float headZ = 0f;
        public float HeadZ
        {
            get => headZ;
            set
            {
                SetProperty(ref headZ, value);
                HeadZValue = HeadZ != 0f ? true : false;
            }
        }
        #endregion

        #region 身体XYZ
        private bool BodyXValue;
        private float bodyX = 0f;
        public float BodyX
        {
            get => bodyX;
            set
            {
                SetProperty(ref bodyX, value);
                BodyXValue = BodyX != 0f ? true : false;
            }
        }

        private bool BodyYValue;
        private float bodyY = 0f;
        public float BodyY
        {
            get => bodyY;
            set
            {
                SetProperty(ref bodyY, value);
                BodyYValue = BodyY != 0f ? true : false;
            }
        }

        private bool BodyZValue;
        private float bodyZ = 0f;
        public float BodyZ
        {
            get => bodyZ;
            set
            {
                SetProperty(ref bodyZ, value);
                BodyZValue = BodyZ != 0f ? true : false;
            }
        }
        #endregion

        #region 左臂XYZ
        private bool LArmXValue;
        private float larmX = 0f;
        public float LArmX
        {
            get => larmX;
            set
            {
                SetProperty(ref larmX, value);
                LArmXValue = LArmX != 0f;
            }
        }

        private bool LArmYValue;
        private float larmY = 0f;
        public float LArmY
        {
            get => larmY;
            set
            {
                SetProperty(ref larmY, value);
                LArmYValue = LArmY != 0f ? true : false;
            }
        }

        private bool LArmZValue;
        private float larmZ = 0f;
        public float LArmZ
        {
            get => larmZ;
            set
            {
                SetProperty(ref larmZ, value);
                LArmZValue = LArmZ != 0f ? true : false;
            }
        }
        #endregion

        #region 右臂XYZ
        private bool RArmXValue;
        private float rarmX = 0f;
        public float RArmX
        {
            get => rarmX;
            set
            {
                SetProperty(ref rarmX, value);
                RArmXValue = RArmX != 0f ? true : false;
            }
        }

        private bool RArmYValue;
        private float rarmY = 0f;
        public float RArmY
        {
            get => rarmY;
            set
            {
                SetProperty(ref rarmY, value);
                RArmYValue = RArmY != 0f ? true : false;
            }
        }

        private bool RArmZValue;
        private float rarmZ = 0f;
        public float RArmZ
        {
            get => rarmZ;
            set
            {
                SetProperty(ref rarmZ, value);
                RArmZValue = RArmZ != 0f ? true : false;
            }
        }
        #endregion

        #region 左腿XYZ
        private bool LLegXValue;
        private float llegX = 0f;
        public float LLegX
        {
            get => llegX;
            set
            {
                SetProperty(ref llegX, value);
                LLegXValue = LLegX != 0f ? true : false;
            }
        }

        private bool LLegYValue;
        private float llegY = 0f;
        public float LLegY
        {
            get => llegY;
            set
            {
                SetProperty(ref llegY, value);
                LLegYValue = LLegY != 0f ? true : false;
            }
        }

        private bool LLegZValue;
        private float llegZ = 0f;
        public float LLegZ
        {
            get => llegZ;
            set
            {
                SetProperty(ref llegZ, value);
                LLegZValue = LLegZ != 0f ? true : false;
            }
        }
        #endregion

        #region 右腿XYZ
        private bool RLegXValue;
        private float rlegX = 0f;
        public float RLegX
        {
            get => rlegX;
            set
            {
                SetProperty(ref rlegX, value);
                RLegXValue = RLegX != 0f ? true : false;
            }
        }

        private bool RLegYValue;
        private float rlegY = 0f;
        public float RLegY
        {
            get => rlegY;
            set
            {
                SetProperty(ref rlegY, value);
                RLegYValue = RLegY != 0f ? true : false;
            }
        }

        private bool RLegZValue;
        private float rlegZ = 0f;
        public float RLegZ
        {
            get => rlegZ;
            set
            {
                SetProperty(ref rlegZ, value);
                RLegZValue = RLegZ != 0f ? true : false;
            }
        }
        #endregion

        #region 装备

        #region 合并装备数据
        private string EquipmentList
        {
            get
            {
                string result;
                string ArmorItems = (HeadItem.Length + BodyItem.Length + LegItem.Length + FeetItem.Length) > 0 ? "ArmorItems:[" + (HeadItem + "," + BodyItem + "," + LegItem + "," + FeetItem).Trim(',') + "]," : "";
                string HandItems = (LeftHandItem.Length + RightHandItem.Length) > 0 ? "HandItems:[" + (LeftHandItem + "," + RightHandItem).Trim(',') + "]," : "";
                result = ArmorItems + HandItems;
                return result;
            }
        }
        #endregion

        /// <summary>
        /// Head
        /// </summary>
        [ObservableProperty]
        private string _headItem = "";

        /// <summary>
        /// Body
        /// </summary>
        [ObservableProperty]
        private string _bodyItem = "";

        /// <summary>
        /// LeftHand
        /// </summary>
        [ObservableProperty]
        private string _leftHandItem = "";

        /// <summary>
        /// RightHand
        /// </summary>
        [ObservableProperty]
        private string _rightHandItem = "";

        /// <summary>
        /// Legs
        /// </summary>
        [ObservableProperty]
        private string _legItem = "";

        /// <summary>
        /// Boots
        /// </summary>
        [ObservableProperty]
        private string _feetItem = "";

        #endregion

        #region 控制注视旋转的旋转角度
        [ObservableProperty]
        private double _horizontalAngle = 0;
        [ObservableProperty]
        private double _verticalAngle = 0;
        #endregion

        #region 各部位装备图像源
        [ObservableProperty]
        private ImageSource _leftHandItemImage;

        [ObservableProperty]
        private ImageSource _rightHandItemImage;

        [ObservableProperty]
        private ImageSource _headItemImage = new BitmapImage(new Uri("/CBHK;component/Resource/Common/Image/Helmet.png", UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        private ImageSource _chestItemImage = new BitmapImage(new Uri("/CBHK;component/Resource/Common/Image/Chestplate.png", UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        private ImageSource _legItemImage = new BitmapImage(new Uri("/CBHK;component/Resource/Common/Image/Leggings.png", UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        private ImageSource _feetItemImage = new BitmapImage(new Uri("/CBHK;component/Resource/Common/Image/Boots.png", UriKind.RelativeOrAbsolute));
        #endregion

        #region 禁止移除或改变头部、身体、手部、腿部、脚部装备
        private bool cannotTakeOrReplaceHead;
        public bool CannotTakeOrReplaceHead
        {
            get => cannotTakeOrReplaceHead;
            set
            {
                SetProperty(ref cannotTakeOrReplaceHead, value);
                CannotTakeOrReplceSum += cannotTakeOrReplaceHead ? 4096 : -4096;
            }
        }

        private bool cannotTakeOrReplaceBody;
        public bool CannotTakeOrReplaceBody
        {
            get => cannotTakeOrReplaceBody;
            set
            {
                SetProperty(ref cannotTakeOrReplaceBody, value);
                CannotTakeOrReplceSum += cannotTakeOrReplaceBody ? 2048 : -2048;
            }
        }

        private bool cannotTakeOrReplaceMainhand;
        public bool CannotTakeOrReplaceMainHand
        {
            get => cannotTakeOrReplaceMainhand;
            set
            {
                SetProperty(ref cannotTakeOrReplaceMainhand, value);
                CannotTakeOrReplceSum += cannotTakeOrReplaceMainhand ? 256 : -256;
            }
        }

        private bool cannotTakeOrReplaceOffHand;
        public bool CannotTakeOrReplaceOffHand
        {
            get => cannotTakeOrReplaceOffHand;
            set
            {
                SetProperty(ref cannotTakeOrReplaceOffHand, value);
                CannotTakeOrReplceSum += HaveOffHandPermission == Visibility.Visible && cannotTakeOrReplaceOffHand ? 8192 : -8192;
            }
        }

        private bool cannotTakeOrReplaceLegs;
        public bool CannotTakeOrReplaceLegs
        {
            get => cannotTakeOrReplaceLegs;
            set
            {
                SetProperty(ref cannotTakeOrReplaceLegs, value);
                CannotTakeOrReplceSum += cannotTakeOrReplaceLegs ? 1024 : -1024;
            }
        }

        private bool cannotTakeOrReplaceBoots;
        public bool CannotTakeOrReplaceBoots
        {
            get => cannotTakeOrReplaceBoots;
            set
            {
                SetProperty(ref cannotTakeOrReplaceBoots, value);
                CannotTakeOrReplceSum += cannotTakeOrReplaceBoots ? 512 : -512;
            }
        }
        #endregion

        #region 禁止添加或改变头部、身体、手部、腿部、脚部装备
        private bool cannotPlaceOrReplacehead;
        public bool CannotPlaceOrReplaceHead
        {
            get => cannotPlaceOrReplacehead;
            set
            {
                SetProperty(ref cannotPlaceOrReplacehead, value);
                CannotPlaceOrReplaceSum += cannotPlaceOrReplacehead ? 16 : -16;
            }
        }

        private bool cannotPlaceOrReplacebody;
        public bool CannotPlaceOrReplaceBody
        {
            get => cannotPlaceOrReplacebody;
            set
            {
                SetProperty(ref cannotPlaceOrReplacebody, value);
                CannotPlaceOrReplaceSum += cannotPlaceOrReplacebody ? 8 : -8;
            }
        }

        private bool cannotPlaceOrReplaceMainHand;
        public bool CannotPlaceOrReplaceMainHand
        {
            get => cannotPlaceOrReplaceMainHand;
            set
            {
                SetProperty(ref cannotPlaceOrReplaceMainHand, value);
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceMainHand ? 1 : -1;
            }
        }

        private bool cannotPlaceOrReplaceOffHand;
        public bool CannotPlaceOrReplaceOffHand
        {
            get => cannotPlaceOrReplaceOffHand;
            set
            {
                SetProperty(ref cannotPlaceOrReplaceOffHand, value);
                CannotPlaceOrReplaceSum += HaveOffHandPermission == Visibility.Visible && cannotPlaceOrReplaceOffHand ? 32 : -32;
            }
        }

        private bool cannotPlaceOrReplaceLegs;
        public bool CannotPlaceOrReplaceLegs
        {
            get => cannotPlaceOrReplaceLegs;
            set
            {
                SetProperty(ref cannotPlaceOrReplaceLegs, value);
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceLegs ? 4 : -4;
            }
        }

        private bool cannotPlaceOrReplaceBoots;
        public bool CannotPlaceOrReplaceBoots
        {
            get => cannotPlaceOrReplaceBoots;
            set
            {
                SetProperty(ref cannotPlaceOrReplaceBoots, value);
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceBoots ? 2 : -2;
            }
        }
        #endregion

        #region 禁止添加头部、身体、手部、腿部、脚部装备
        private bool cannotPlaceHead;
        public bool CannotPlaceHead
        {
            get => cannotPlaceHead;
            set
            {
                SetProperty(ref cannotPlaceHead, value);
                CannotPlaceSum += cannotPlaceHead ? 1048576 : -1048576;
            }
        }

        private bool cannotPlaceBody;
        public bool CannotPlaceBody
        {
            get => cannotPlaceBody;
            set
            {
                SetProperty(ref cannotPlaceBody, value);
                CannotPlaceSum += cannotPlaceBody ? 524288 : -524288;
            }
        }

        private bool cannotPlaceMainHand;
        public bool CannotPlaceMainHand
        {
            get => cannotPlaceMainHand;
            set
            {
                SetProperty(ref cannotPlaceMainHand, value);
                CannotPlaceSum += cannotPlaceMainHand ? 65536 : -65536;
            }
        }

        private bool cannotPlaceOffHand;
        public bool CannotPlaceOffHand
        {
            get => cannotPlaceOffHand;
            set
            {
                SetProperty(ref cannotPlaceOffHand, value);
                CannotPlaceSum += HaveOffHandPermission == Visibility.Visible && cannotPlaceOffHand ? 2097152 : -2097152;
            }
        }

        private bool cannotPlaceLegs;
        public bool CannotPlaceLegs
        {
            get => cannotPlaceLegs;
            set
            {
                SetProperty(ref cannotPlaceLegs, value);
                CannotPlaceSum += cannotPlaceLegs ? 262144 : -262144;
            }
        }

        private bool cannotPlaceBoots;
        public bool CannotPlaceBoots
        {
            get => cannotPlaceBoots;
            set
            {
                SetProperty(ref cannotPlaceBoots, value);
                CannotPlaceSum += cannotPlaceBoots ? 131072 : -131072;
            }
        }
        #endregion

        #region 版本数据源
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _versionSource = [
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.13.0" },
            new TextComboBoxItem() { Text = "1.12.0" },
            new TextComboBoxItem() { Text = "1.9.0" },
            new TextComboBoxItem() { Text = "1.8.0" }
            ];
        #endregion

        #region 已选择的版本
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }

        private int currentMinVersion = 1202;
        public int CurrentMinVersion
        {
            get => currentMinVersion;
            set => currentMinVersion = value;
        }
        #endregion

        #endregion

        #region Method
        public ArmorStandViewModel(IContainerProvider container,MainView mainView)
        {
            //连接三个轴的上下文
            XAxis.DataContext = this;
            YAxis.DataContext = this;
            ZAxis.DataContext = this;

            _container = container;
            home = mainView;

            ResetAllPoseButtonForeground = ResetHeadPoseButtonForeground = ResetLArmPoseButtonForeground = ResetRArmPoseButtonForeground = ResetBodyPoseButtonForeground = ResetLLegPoseButtonForeground = ResetRLegPoseButtonForeground = GrayBrush;
        }

        private ImageSource CreateColoredImage(BitmapImage originalImage, Color overlayColor)
        {
            // 创建一个新的 DrawingVisual
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // 绘制原始图像
                drawingContext.DrawImage(originalImage, new Rect(0, 0, originalImage.PixelWidth, originalImage.PixelHeight));

                // 创建带颜色的填充
                SolidColorBrush colorBrush = new SolidColorBrush(overlayColor);
                colorBrush.Opacity = 0.5; // 设定透明度

                // 绘制覆盖颜色
                drawingContext.DrawRectangle(colorBrush, null, new Rect(0, 0, originalImage.PixelWidth, originalImage.PixelHeight));
            }

            // 创建一个 RenderTargetBitmap 用于将 DrawingVisual 转换为 BitmapImage
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(originalImage.PixelWidth, originalImage.PixelHeight, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            return renderBitmap;
        }

        private DependencyObject GetHitObject(Point mousePosition, Viewport3D visual3D)
        {
            // 获取鼠标点击的东东视口坐标
            PointHitTestParameters hitParams = new(mousePosition);
            HitTestResult hitResult = VisualTreeHelper.HitTest(visual3D, mousePosition);

            if (hitResult is not null && hitResult.VisualHit is not null)
            {
                return hitResult.VisualHit;
            }
            return null;
        }

        /// <summary>
        /// 移动摄像机与模型之间的相对距离
        /// </summary>
        /// <param name="d"></param>
        public void Move(double d)
        {
            double scale = 0.05;
            Vector3D lookDirection = MainCamera.LookDirection;
            Point3D position = MainCamera.Position;
            lookDirection.Normalize();
            position += scale * lookDirection * d;
            MainCamera.Position = position;
        }

        /// <summary>
        /// 旋转模型
        /// </summary>
        /// <param name="center">旋转中心点</param>
        /// <param name="model">旋转对象</param>
        /// <param name="seconds">旋转持续时间</param>
        /// <param name="axis">旋转轴</param>
        /// <param name="X">x轴旋转角度</param>
        /// <param name="Y">y轴旋转角度</param>
        /// <param name="Z">z轴旋转角度</param>
        public void TurnModel(Point3D center, GeometryModel3D model, double seconds, bool axis, float X, float Y, float Z)
        {
            Vector3D vector5 = new(0.0, 1.0, 0.0);
            Vector3D vector4 = new(1.0, 0.0, 0.0);
            Vector3D vector3 = new(0.0, 0.0, 1.0);
            AxisAngleRotation3D rotation5 = new(vector5, 0.0);
            AxisAngleRotation3D rotation4 = new(vector4, 0.0);
            AxisAngleRotation3D rotation3 = new(vector3, 0.0);
            RotateTransform3D rotateTransform5 = new(rotation5, center);
            RotateTransform3D rotateTransform4 = new(rotation4, center);
            RotateTransform3D rotateTransform3 = new(rotation3, center);
            Transform3DGroup transformGroup = new();
            transformGroup.Children.Add(rotateTransform5);
            transformGroup.Children.Add(rotateTransform4);
            transformGroup.Children.Add(rotateTransform3);
            model.Transform = transformGroup;
            if (axis)
            {
                DoubleAnimation doubleAnimation5 = new(double.Parse(Y.ToString()), double.Parse(Y.ToString()), DurationTS(seconds))
                {
                    BeginTime = DurationTS(0.0)
                };
                rotation5.BeginAnimation(AxisAngleRotation3D.AngleProperty, doubleAnimation5);
                DoubleAnimation doubleAnimation4 = new(double.Parse(X.ToString()), double.Parse(X.ToString()), DurationTS(seconds))
                {
                    BeginTime = DurationTS(0.0)
                };
                rotation4.BeginAnimation(AxisAngleRotation3D.AngleProperty, doubleAnimation4);
                DoubleAnimation doubleAnimation3 = new(double.Parse(Z.ToString()), double.Parse(Z.ToString()), DurationTS(seconds))
                {
                    BeginTime = DurationTS(0.0)
                };
                rotation3.BeginAnimation(AxisAngleRotation3D.AngleProperty, doubleAnimation3);
            }
        }

        private int DurationM(double seconds)
        {
            return (int)(seconds * 1000.0);
        }

        public TimeSpan DurationTS(double seconds)
        {
            return new TimeSpan(0, 0, 0, 0, DurationM(seconds));
        }

        public void WatchMode()
        {
            initHorizontalAngle = initVerticalAngle = 0;
            ArmorStandViewer.Cursor = Cursors.Arrow;
            ResetCameraState();
        }

        public void FreeMode()
        {
            initHorizontalAngle = initVerticalAngle = 0;
            ArmorStandViewer.Cursor = Cursors.Arrow;
            ResetCameraState();
        }

        public void ThirdPersonMode()
        {
            initHorizontalAngle = initVerticalAngle = 0;
            ArmorStandViewer.Cursor = Cursors.Arrow;
            ResetCameraState();
        }

        public void OverTheShoulder()
        {
            initHorizontalAngle = initVerticalAngle = 0;
            ArmorStandViewer.Cursor = Cursors.Arrow;
            ResetCameraState();
        }

        /// <summary>
        /// 获取摄像机视线朝向
        /// </summary>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <returns></returns>
        private Vector3D GetLookDirection(double pitch, double yaw)
        {
            // 将角度转换为弧度
            double pitchRadians = pitch * (Math.PI / 180.0);
            double yawRadians = yaw * (Math.PI / 180.0);

            // 计算朝向向量
            double x = Math.Cos(pitchRadians) * Math.Cos(yawRadians);
            double y = Math.Sin(pitchRadians);
            double z = Math.Cos(pitchRadians) * Math.Sin(yawRadians);

            return new Vector3D(x, y, z);
        }

        /// <summary>   
        /// 设置鼠标的坐标   
        /// </summary>   
        /// <param name="x">横坐标</param>   
        /// <param name="y">纵坐标</param>   
        [DllImport("User32")]
        public extern static void SetCursorPos(int x, int y);

        #endregion

        #region Event

        /// <summary>
        /// 载入并创建SceneGizmo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Gizimo_Loaded(object sender, RoutedEventArgs e)
        {
            SceneGizmo = sender as Viewport3D;
            GizimoCamera = SceneGizmo.Camera as PerspectiveCamera;

            #region z正半轴
            MeshBuilder ZAxisPositiveBuilder = new MeshBuilder(false, false);
            ZAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D ZAxisPositiveMesh = ZAxisPositiveBuilder.ToMesh();
            Transform3DGroup ZAxisPositiveTransform3DGroup = new();
            TranslateTransform3D ZAxisPositiveTranslateTransform3D = new(0, 0, ConeDistance);
            RotateTransform3D ZAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(-1, 0, 0), 90));
            ZAxisPositiveTransform3DGroup.Children.Add(ZAxisPositiveRotateTransform3D);
            ZAxisPositiveTransform3DGroup.Children.Add(ZAxisPositiveTranslateTransform3D);
            GeometryModel3D ZAxisPositiveModel = new()
            {
                Geometry = ZAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Green),
                Transform = ZAxisPositiveTransform3DGroup,
            };
            ModelVisual3D ZAxisPositiveVisual = new()
            {
                Content = ZAxisPositiveModel
            };
            ZAxisPositiveVisual.SetValue(UIElement.UidProperty, "ZAxisPositiveVisual");
            SceneGizmo.Children.Add(ZAxisPositiveVisual);
            #endregion
            #region z负半轴
            MeshBuilder ZAxisNegativeBuilder = new(false, false);
            ZAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D ZAxisNegativeMesh = ZAxisNegativeBuilder.ToMesh();
            Transform3DGroup ZAxisNegativeTransform3DGroup = new();
            TranslateTransform3D ZAxisNegativeTranslateTransform3D = new(0, 0, -ConeDistance);
            RotateTransform3D ZAxisNegativeRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90));
            ZAxisNegativeTransform3DGroup.Children.Add(ZAxisNegativeRotateTransform3D);
            ZAxisNegativeTransform3DGroup.Children.Add(ZAxisNegativeTranslateTransform3D);
            GeometryModel3D ZAxisNegativeModel = new()
            {
                Geometry = ZAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = ZAxisNegativeTransform3DGroup
            };
            ModelVisual3D ZAxisNegativeVisual = new()
            {
                Content = ZAxisNegativeModel
            };
            ZAxisNegativeVisual.SetValue(UIElement.UidProperty, "ZAxisNegativeVisual");
            SceneGizmo.Children.Add(ZAxisNegativeVisual);
            #endregion

            #region y正半轴
            MeshBuilder YAxisPositiveBuilder = new(false, false);
            YAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D YAxisPositiveMesh = YAxisPositiveBuilder.ToMesh();
            Transform3DGroup YAxisPositiveTransform3DGroup = new();
            TranslateTransform3D YAxisPositiveTranslateTransform3D = new(0, ConeDistance, 0);
            RotateTransform3D YAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));
            YAxisPositiveTransform3DGroup.Children.Add(YAxisPositiveRotateTransform3D);
            YAxisPositiveTransform3DGroup.Children.Add(YAxisPositiveTranslateTransform3D);
            GeometryModel3D YAxisPositiveModel = new()
            {
                Geometry = YAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Blue),
                Transform = YAxisPositiveTransform3DGroup
            };
            ModelVisual3D YAxisPositiveVisual = new()
            {
                Content = YAxisPositiveModel
            };
            YAxisPositiveVisual.SetValue(UIElement.UidProperty, "YAxisPositiveVisual");
            SceneGizmo.Children.Add(YAxisPositiveVisual);
            #endregion
            #region y负半轴
            MeshBuilder YAxisNegativeBuilder = new(false, false);
            YAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D YAxisNegativeMesh = YAxisNegativeBuilder.ToMesh();
            Transform3DGroup YAxisNegativeTransform3DGroup = new();
            TranslateTransform3D YAxisNegativeTranslateTransform3D = new(0, -ConeDistance, 0);
            YAxisNegativeTransform3DGroup.Children.Add(YAxisNegativeTranslateTransform3D);
            GeometryModel3D YAxisNegativeModel = new()
            {
                Geometry = YAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = YAxisNegativeTransform3DGroup
            };
            ModelVisual3D YAxisNegativeVisual = new()
            {
                Content = YAxisNegativeModel
            };
            YAxisNegativeVisual.SetValue(UIElement.UidProperty, "YAxisNegativeVisual");
            SceneGizmo.Children.Add(YAxisNegativeVisual);
            #endregion

            #region x正半轴
            MeshBuilder XAxisPositiveBuilder = new(false, false);
            XAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D XAxisPositiveMesh = XAxisPositiveBuilder.ToMesh();
            Transform3DGroup XAxisPositiveTransform3DGroup = new();
            TranslateTransform3D XAxisPositiveTranslateTransform3D = new(ConeDistance, 0, 0);
            RotateTransform3D XAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90));
            XAxisPositiveTransform3DGroup.Children.Add(XAxisPositiveRotateTransform3D);
            XAxisPositiveTransform3DGroup.Children.Add(XAxisPositiveTranslateTransform3D);
            GeometryModel3D XAxisPositiveModel = new()
            {
                Geometry = XAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Red),
                Transform = XAxisPositiveTransform3DGroup
            };
            ModelVisual3D XAxisPositiveVisual = new()
            {
                Content = XAxisPositiveModel
            };
            XAxisPositiveVisual.SetValue(UIElement.UidProperty, "XAxisPositiveVisual");
            SceneGizmo.Children.Add(XAxisPositiveVisual);
            #endregion
            #region x负半轴
            MeshBuilder XAxisNegativeBuilder = new(false, false);
            XAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), ConeBaseRadius, 0, ConeHeight, true, true, 32);
            MeshGeometry3D XAxisNegativeMesh = XAxisNegativeBuilder.ToMesh();
            Transform3DGroup XAxisNegativeTransform3DGroup = new();
            TranslateTransform3D XAxisNegativeTranslateTransform3D = new(-ConeDistance, 0, 0);
            RotateTransform3D XAxisNegativeRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90));
            XAxisNegativeTransform3DGroup.Children.Add(XAxisNegativeRotateTransform3D);
            XAxisNegativeTransform3DGroup.Children.Add(XAxisNegativeTranslateTransform3D);
            GeometryModel3D XAxisNegativeModel = new()
            {
                Geometry = XAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = XAxisNegativeTransform3DGroup
            };
            ModelVisual3D XAxisNegativeVisual = new()
            {
                Content = XAxisNegativeModel
            };
            XAxisNegativeVisual.SetValue(UIElement.UidProperty, "XAxisNegativeVisual");
            SceneGizmo.Children.Add(XAxisNegativeVisual);
            #endregion
        }

        public void AnimationContainer_Loaded(object sender, RoutedEventArgs e) => animationContainer = sender as AnimationContainer;

        /// <summary>
        /// 切换运镜模式
        /// </summary>
        /// <param name="type"></param>
        [RelayCommand]
        public void UpdateCameraMovementType(ArmorStandCameraMovementType type)
        {
            _armorStandCameraMovementType = type;
            Action action = type switch
            {
                ArmorStandCameraMovementType.Watch => WatchMode,
                ArmorStandCameraMovementType.Free => FreeMode,
                ArmorStandCameraMovementType.ThirdPerson => ThirdPersonMode,
                ArmorStandCameraMovementType.OverTheShoulder => OverTheShoulder,
                _ => null
            };
            PreviewModeText = "预览-" + type.ToString();
            action?.Invoke();
        }

        /// <summary>
        /// 载入名称文本框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomNameBox_Loaded(object sender, RoutedEventArgs e)
        {
            stylizedTextBox = sender as StylizedTextBox;
        }

        /// <summary>
        /// 载入标签文本框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TagRichTextBox_Loaded(object sender, RoutedEventArgs e) => tagRichTextBox = sender as TagRichTextBox;

        /// <summary>
        /// 载入基础NBT列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NBTCheckboxList_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Data\base_nbt.ini"))
            {
                ArmorStandNBTList = [.. File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ArmorStand\Data\base_nbt.ini", Encoding.UTF8)];
            }
            NBTList = sender as StackPanel;

            if (ArmorStandNBTList.Count > 0)
            {
                foreach (string item in ArmorStandNBTList)
                {
                    TextCheckBoxs textCheckBox = new()
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        Margin = new Thickness(0, 0, 0, 10),
                        HeaderText = item,
                        HeaderHeight = 20,
                        FontSize = 15,
                        HeaderWidth = 20,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                        Style = Application.Current.Resources["TextCheckBox"] as Style
                    };
                    NBTList.Children.Add(textCheckBox);
                    textCheckBox.Checked += NBTChecked;
                    textCheckBox.Unchecked += NBTUnchecked;
                    switch (item)
                    {
                        case "ShowArms":
                            {
                                textCheckBox.Checked += ShowArmsInModel;
                                textCheckBox.Unchecked += HideArmsInModel;
                                break;
                            }
                        case "NoBasePlate":
                            {
                                textCheckBox.Checked += NoBasePlateInModel;
                                textCheckBox.Unchecked += HaveBasePlateInModel;
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender, RoutedEventArgs e) => await stylizedTextBox.Upgrade(CurrentMinVersion);

        /// <summary>
        /// 检测名称文本框内容为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StylizedTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Back || e.Key == Key.Delete))
            {
                if (stylizedTextBox.richTextBox.Document.Blocks.FirstBlock is null)
                    stylizedTextBox.richTextBox.Document.Blocks.Add(new RichParagraph());
                else
                {
                    Paragraph paragraph = stylizedTextBox.richTextBox.Document.Blocks.FirstBlock as Paragraph;
                    if (paragraph.Inlines.Count == 1)
                    {
                        Run run = paragraph.Inlines.FirstInline as Run;
                        run = new RichRun() { Text = run.Text };
                    }
                    else
                    if (paragraph.Inlines.Count == 0)
                    {
                        paragraph.Inlines.Add(new RichRun());
                    }
                }
            }
        }

        [RelayCommand]
        private void PreviewMenuVisibilitySwitcher()
        {
            if (PreviewMenuVisibility == Visibility.Visible)
                PreviewMenuVisibility = Visibility.Collapsed;
            else
                PreviewMenuVisibility = Visibility.Visible;
        }

        /// <summary>
        /// 检测点击了哪个Gizimo成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Gizimo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(sender as Viewport3D);
            DependencyObject hitObject = GetHitObject(mousePos, sender as Viewport3D);
            if (hitObject is ModelVisual3D modelVisual3D)
            {
                string uid = modelVisual3D.GetValue(UIElement.UidProperty) as string;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置摄像机状态
        /// </summary>
        private void ResetCameraState()
        {
            HorizontalAngle = initHorizontalAngle;
            VerticalAngle = initVerticalAngle;
            lastMousePosX = lastMousePosY = deltaMoveX = deltaMoveY = horizontalAngleDelta = verticalAngleDelta = 0;
            switch (_armorStandCameraMovementType)
            {
                case ArmorStandCameraMovementType.Watch:
                    {
                        MainCamera.LookDirection = new(-1, -1, -1);
                        MainCamera.Position = new(0, 0, 0);
                        break;
                    }
                //自由视角的重置和第三人称共用
                case ArmorStandCameraMovementType.Free:
                case ArmorStandCameraMovementType.ThirdPerson:
                    {
                        MainCamera.LookDirection = new(0, -0.4, 1);
                        MainCamera.Position = new(-2.4, -1, -6);
                        break;
                    }
                case ArmorStandCameraMovementType.OverTheShoulder:
                    {
                        IsLeftShoulder = !IsLeftShoulder;

                        _armorStandCameraOverTheShoulderType = IsLeftShoulder ? ArmorStandCameraOverTheShoulderType.Left : ArmorStandCameraOverTheShoulderType.Right;

                        MainCamera.LookDirection = new(0, 0, 1);
                        if (_armorStandCameraOverTheShoulderType is ArmorStandCameraOverTheShoulderType.Left)
                        {
                            MainCamera.Position = new(-1.5, -1, -5.5);
                        }
                        else
                        {
                            MainCamera.Position = new(-3, -1, -5.5);
                        }
                        break;
                    }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 设置盔甲架的装备
        /// </summary>
        private void SetItem(FrameworkElement element)
        {
            Button iconTextButtons = element as Button;
            ItemView item = new();
            ItemViewModel itemContext = item.DataContext as ItemViewModel;
            itemContext.IsCloseable = false;

            #region 设置已生成的数据
            string Result = "";
            if (item.ShowDialog().Value)
            {
                ItemPageViewModel pageContext = (itemContext.ItemPageList[0].Content as ItemPageView).DataContext as ItemPageViewModel;

                #region 如果已有数据，则导入
                //switch (iconTextButtons.Uid)
                //{
                //    case "Head":
                //        if (HeadItem != null && HeadItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(HeadItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string HeadData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(HeadData);
                //        }
                //        break;
                //    case "Body":
                //        if (BodyItem != null && BodyItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(BodyItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string BodyData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(BodyData);
                //        }
                //        break;
                //    case "LeftHand":
                //        if (LeftHandItem != null && LeftHandItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(LeftHandItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string LeftHandData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(LeftHandData);
                //        }
                //        break;
                //    case "RightHand":
                //        if (RightHandItem != null && RightHandItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(RightHandItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string RightHandData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(RightHandData);
                //        }
                //        break;
                //    case "Legs":
                //        if (LegsItem != null && LegsItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(LegsItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string LegsData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(LegsData);
                //        }
                //        break;
                //    case "Feet":
                //        if (FeetItem != null && FeetItem.Length > 0)
                //        {
                //            ObservableCollection<RichTabItems> richTabItems = itemContext.ItemPageList;
                //            ExternalDataImportManager.ImportItemDataHandler(FeetItem, ref richTabItems, false);
                //            itemContext.ItemPageList.Remove(itemContext.ItemPageList[0]);
                //            string FeetData = ExternalDataImportManager.GetItemDataHandler(HeadItem, false);
                //            pageContext.ExternallyReadEntityData = JObject.Parse(FeetData);
                //        }
                //        break;
                //}
                #endregion

                Result = pageContext.Result;
                string nbt = ExternalDataImportManager.GetItemDataHandler(Result, false);
                if (nbt.Length == 0) return;
                JObject data = JObject.Parse(nbt);
                string itemID = data.SelectToken("id").ToString().Replace("minecraft:", "");
                string filePath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png";
                switch (iconTextButtons.Uid)
                {
                    case "Head":
                        if (File.Exists(filePath))
                            HeadItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        HeadItem = Result;
                        break;
                    case "Body":
                        if (File.Exists(filePath))
                            ChestItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        BodyItem = Result;
                        break;
                    case "LeftHand":
                        if (File.Exists(filePath))
                            LeftHandItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        LeftHandItem = Result;
                        break;
                    case "RightHand":
                        if (File.Exists(filePath))
                            RightHandItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        RightHandItem = Result;
                        break;
                    case "Legs":
                        if (File.Exists(filePath))
                            LegItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        LegItem = Result;
                        break;
                    case "Feet":
                        if (File.Exists(filePath))
                            FeetItemImage = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                        FeetItem = Result;
                        break;
                }
            }
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 反选所有bool类NBT
        /// </summary>
        public void ReverseAllNBT()
        {
            foreach (TextCheckBoxs item in NBTList.Children)
            {
                item.IsChecked = !item.IsChecked.Value;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 全选所有bool类NBT
        /// </summary>
        /// <param name="obj"></param>
        public void SelectAllNBT(FrameworkElement frameworkElement)
        {
            bool currentValue = (frameworkElement as TextCheckBoxs).IsChecked.Value;
            foreach (TextCheckBoxs item in NBTList.Children)
            {
                item.IsChecked = currentValue;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 生成ArmorStand
        /// </summary>
        private async Task Run()
        {
            string result;
            CustomName = "";
            await stylizedTextBox.Upgrade(CurrentMinVersion);
            await tagRichTextBox.GetResult();
            string CustomNameValue = await stylizedTextBox.Result();
            if (CustomNameValue.Trim().Length > 0)
            {
                CustomName = "CustomName:" + (CurrentMinVersion >= 1130 ? "'[" : @"\\\\\\\""") + CustomNameValue.TrimEnd(',') + (CurrentMinVersion >= 1130 ? "]'" : @"\\\\\\\""") + ",";
                CustomName = CustomName.Replace(@"\\n", "");
            }

            #region 拼接指令数据

            #region Animation
            
            #endregion

            #region Result
            string nbt = CustomName + BoolNBTList + EquipmentList + DisabledValue + CustomNameVisibleString + TagList + PoseString;
            nbt = nbt.TrimEnd(',');
            if (CurrentMinVersion >= 1130)
                result = "summon armor_stand ~ ~ ~" + (nbt != "" ? " {" + nbt + "}" : "");
            else
                result = @"give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:""{\""text\"":\""右击执行\"",\""clickEvent\"":{\""action\"":\""run_command\"",\""value\"":\""/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:\\\""summon armor_stand ~ ~ ~ {" + nbt + @"}\\\""}\""}}""}}";
            #endregion

            #endregion

            #region 生成结果
            if (ShowGeneratorResult)
            {
                DisplayerView displayerView = _container.Resolve<DisplayerView>();
                if (displayerView is not null && displayerView.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerView.Show();
                    displayerViewModel.GeneratorResult(result, "盔甲架", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(result);
                Message.PushMessage("盔甲架生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        private void Return(Window win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 重置所有动作
        /// </summary>
        private void ResetAllPose()
        {
            if (CanResetAllPose)
            {
                HeadX = HeadY = HeadZ = BodyX = BodyY = BodyZ = LArmX = LArmY = LArmZ = RArmX = RArmY = RArmZ = LLegX = LLegY = LLegZ = RLegX = RLegY = RLegZ = 0f;
                CanResetAllPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置头部动作
        /// </summary>
        private void ResetHeadPose()
        {
            if (CanResetHeadPose)
            {
                HeadX = HeadY = HeadZ = 0f;
                CanResetHeadPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置身体动作
        /// </summary>
        private void ResetBodyPose()
        {
            if (CanResetBodyPose)
            {
                BodyX = BodyY = BodyZ = 0f;
                CanResetBodyPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置左臂动作
        /// </summary>
        private void ResetLeftArmPose()
        {
            if (CanResetLArmPose)
            {
                LArmX = LArmY = LArmZ = 0f;
                CanResetLArmPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置右臂动作
        /// </summary>
        private void ResetRightArmPose()
        {
            if (CanResetRArmPose)
            {
                RArmX = RArmY = RArmZ = 0f;
                CanResetRArmPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置左腿动作
        /// </summary>
        private void ResetLeftLegPose()
        {
            if (CanResetLLegPose)
            {
                LLegX = LLegY = LLegZ = 0f;
                CanResetLLegPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 重置右腿动作
        /// </summary>
        private void ResetRightLegPose()
        {
            if (CanResetRLegPose)
            {
                RLegX = RLegY = RLegZ = 0f;
                CanResetRLegPose = false;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 头部三轴合一
        /// </summary>
        private void HeadThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;
            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("HeadX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("HeadY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("HeadZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 身体三轴合一
        /// </summary>
        private void BodyThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;
            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("BodyX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("BodyY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("BodyZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 左臂三轴合一
        /// </summary>
        private void LeftArmThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;
            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("LArmX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("LArmY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("LArmZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 右臂三轴合一
        /// </summary>
        private void RightArmThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;
            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("RArmX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("RArmY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("RArmZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 左腿三轴合一
        /// </summary>
        private void LeftLegThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;

            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("LLegX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("LLegY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("LLegZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 右腿三轴合一
        /// </summary>
        private void RightLegThreeAxis(Button btn)
        {
            UsingThreeAxis = !UsingThreeAxis;
            btn.Cursor = UsingThreeAxis ? Cursors.None : Cursors.Arrow;

            if (UsingThreeAxis)
            {
                Binding x_binder = new()
                {
                    Path = new PropertyPath("RLegX"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding y_binder = new()
                {
                    Path = new PropertyPath("RLegY"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding z_binder = new()
                {
                    Path = new PropertyPath("RLegZ"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                XAxis.SetBinding(FrameworkElement.TagProperty, x_binder);
                YAxis.SetBinding(FrameworkElement.TagProperty, y_binder);
                ZAxis.SetBinding(FrameworkElement.TagProperty, z_binder);
            }
        }

        public void ThreeAxisMouseMove(object sender, MouseEventArgs e)
        {
            if (UsingThreeAxis)
            {
                Button btn = sender as Button;
                CurrentButtonCenter = Mouse.GetPosition(btn);
                LastCursorPosition = new Point(LastCursorPosition.X - CurrentButtonCenter.X, LastCursorPosition.Y - CurrentButtonCenter.Y);

                if (LastCursorPosition.X > 0)
                    XAxisValue += -1;
                else
                    if (LastCursorPosition.X < 0)
                    XAxisValue += 1;
                else
                    if (LastCursorPosition.Y > 0)
                    ZAxisValue += -1;
                else
                if (LastCursorPosition.Y < 0)
                    ZAxisValue += 1;

                XAxisValue = XAxisValue > 180 ? 180 : (XAxisValue < -180) ? -180 : XAxisValue;
                ZAxisValue = ZAxisValue > 180 ? 180 : (ZAxisValue < -180) ? -180 : ZAxisValue;

                XAxis.Tag = XAxisValue;
                ZAxis.Tag = ZAxisValue;

                //值复位
                if (!OutOfThreeAxis)
                    LastCursorPosition = CurrentButtonCenter;
            }
            OutOfThreeAxis = false;
        }

        public void ThreeAxisMouseLeave(object sender, MouseEventArgs e)
        {
            if (UsingThreeAxis)
            {
                OutOfThreeAxis = true;
                Button btn = sender as Button;
                CurrentButtonCenter = btn.PointToScreen(new Point(0, 0));
                CurrentButtonCenter.X += btn.Width / 2;
                CurrentButtonCenter.Y += btn.Height / 2;
                int point_x = (int)CurrentButtonCenter.X;
                int point_y = (int)CurrentButtonCenter.Y;
                SetCursorPos(point_x, point_y);
            }
        }

        public void ThreeAxisMouseWheel(object sender, MouseWheelEventArgs e)
        {
            YAxisValue += e.Delta > 0 ? 2.5f : (-2.5f);
            YAxisValue = YAxisValue > 180 ? 180 : (YAxisValue < -180) ? -180 : YAxisValue;
            YAxis.Tag = YAxisValue;
        }

        /// <summary>
        /// 隐藏盔甲架手臂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideArmsInModel(object sender, RoutedEventArgs e)
        {
            //把纹理设置为空
            ArmImage = null;
        }

        /// <summary>
        /// 显示盔甲架的手臂
        /// </summary>
        public void ShowArmsInModel(object sender, RoutedEventArgs e)
        {
            //把纹理设置回来
            ArmImage = WoodenImage;
        }

        /// <summary>
        /// 显示盔甲架底座
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HaveBasePlateInModel(object sender, RoutedEventArgs e)
        {
            Stone = new BitmapImage(stoneUri);
            StoneSide = new BitmapImage(stoneSideUri);
        }

        /// <summary>
        /// 不显示盔甲架的底座
        /// </summary>
        public void NoBasePlateInModel(object sender, RoutedEventArgs e)
        {
            Stone = null;
            StoneSide = null;
        }

        /// <summary>
        /// NBT未勾选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NBTUnchecked(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs current = sender as TextCheckBoxs;
            BoolTypeNBT.Remove(current.HeaderText);
        }

        /// <summary>
        /// NBT已勾选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NBTChecked(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs current = sender as TextCheckBoxs;
            BoolTypeNBT.Add(current.HeaderText);
        }

        #region 处理3D模型

        /// <summary>
        /// 载入盔甲架模型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ArmorStandViewer_Loaded(object sender, RoutedEventArgs e)
        {
            #region 载入盔甲架
            ArmorStandViewer = sender as Viewport3D;
            //载入盔甲架整体容器，用于旋转
            ModelGroup = ArmorStandViewer.Children[2] as ModelVisual3D;
            MainCamera = ArmorStandViewer.Camera as PerspectiveCamera;
            #endregion

            #region 载入初始坐标和朝向
            initHorizontalAngle = HorizontalAngle;
            initVerticalAngle = VerticalAngle;
            #endregion
        }

        public void ArmorStandViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (_armorStandCameraMovementType is ArmorStandCameraMovementType.Free)
            {
                #region 前后左右
                if (e.Key is Key.A || e.Key is Key.D)
                {
                    Vector3D lookDirection = MainCamera.LookDirection;
                    lookDirection.Normalize();
                    lookDirection *= -0.25;
                    if (e.Key is Key.A)
                        lookDirection = new(-lookDirection.Z, 0, lookDirection.X);
                    else
                        lookDirection = new(lookDirection.Z, 0, -lookDirection.X);
                    double posX = MainCamera.Position.X;
                    double posY = MainCamera.Position.Y;
                    double posZ = MainCamera.Position.Z;
                    MainCamera.Position = new(posX + lookDirection.X, posY, posZ + lookDirection.Z);
                }

                if (e.Key is Key.W || e.Key is Key.S)
                {
                    Vector3D lookDirection = MainCamera.LookDirection;
                    lookDirection.Normalize();
                    lookDirection *= 0.25;
                    double posX = MainCamera.Position.X;
                    double posY = MainCamera.Position.Y;
                    double posZ = MainCamera.Position.Z;
                    MainCamera.Position = new(posX + lookDirection.X * (e.Key is Key.W ? 1 : -1), posY, posZ + lookDirection.Z * (e.Key is Key.W ? 1 : -1));
                }
                #endregion
                #region 上下
                if (e.Key is Key.Space || e.Key is Key.LeftShift)
                {
                    MainCamera.Position = new(MainCamera.Position.X, MainCamera.Position.Y + (e.Key is Key.Space ? 0.25 : -0.25), MainCamera.Position.Z);
                }
                #endregion
            }
        }

        /// <summary>
        /// 处理模型视图中鼠标按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ModelViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePosX = e.GetPosition(ArmorStandViewer).X;
            lastMousePosY = e.GetPosition(ArmorStandViewer).Y;

            if(sender is TabControl tabControl)
            {
                (tabControl.Items[0] as TabItem).Focus();
            }
        }

        /// <summary>
        /// 处理模型视图中鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ModelViewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _armorStandCameraMovementType is ArmorStandCameraMovementType.Watch)
            {
                deltaMoveX = 0.5 * (e.GetPosition(ArmorStandViewer).X - lastMousePosX);
                deltaMoveY = 0.5 * (e.GetPosition(ArmorStandViewer).Y - lastMousePosY);

                horizontalAngleDelta += deltaMoveX;
                if (horizontalAngleDelta > 360)
                    horizontalAngleDelta = 0;
                if (horizontalAngleDelta < -360)
                    horizontalAngleDelta = 0;
                HorizontalAngle = -horizontalAngleDelta;

                verticalAngleDelta += deltaMoveY;
                if (verticalAngleDelta > 45)
                    verticalAngleDelta = 45;
                if (verticalAngleDelta < -135)
                    verticalAngleDelta = -135;

                VerticalAngle = -verticalAngleDelta;
                lastMousePosX = e.GetPosition(ArmorStandViewer).X;
                lastMousePosY = e.GetPosition(ArmorStandViewer).Y;
            }

            if (e.LeftButton == MouseButtonState.Pressed && _armorStandCameraMovementType is ArmorStandCameraMovementType.Free)
            {
                deltaMoveX = e.GetPosition(ArmorStandViewer).X - lastMousePosX;
                deltaMoveY = e.GetPosition(ArmorStandViewer).Y - lastMousePosY;

                Vector3D lookDirection = MainCamera.LookDirection;
                lookDirection.Normalize();
                double pitch = Math.Asin(lookDirection.Y) * (180 / Math.PI);
                double yaw = Math.Atan2(lookDirection.Z, lookDirection.X) * (180 / Math.PI);
                Vector3D result = GetLookDirection(pitch + (0.1 * (deltaMoveY > 0 ? 1 : -1)), yaw + (0.1 * (deltaMoveX > 0 ? -1 : 1)));

                result.X = Math.Clamp(result.X, -89, 89);

                MainCamera.LookDirection = result;

                lastMousePosX = e.GetPosition(ArmorStandViewer).X;
                lastMousePosY = e.GetPosition(ArmorStandViewer).Y;
            }
        }

        /// <summary>
        /// 处理模型视图中鼠标滚轮的滚动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ModelViewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(_armorStandCameraMovementType is not ArmorStandCameraMovementType.Watch)
            {
                return;
            }
            if (e.Delta > 0)
                Move(2);
            else if (e.Delta < 0)
                Move(-2);
        }
        #endregion

        #endregion
    }
}