using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.ItemGenerator;
using cbhk.Generators.ItemGenerator.Components;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

namespace cbhk.Generators.ArmorStandGenerator
{
    public partial class ArmorStandDataContext: ObservableObject
    {
        #region 字段

        #region 是否拥有副手权限
        //版本切换锁,防止属性之间无休止更新
        private bool permission_switch_lock = false;
        private Visibility haveOffHandPermission = Visibility.Visible;
        public Visibility HaveOffHandPermission
        {
            get { return haveOffHandPermission; }
            set
            {
                haveOffHandPermission = value;
                if(!permission_switch_lock)
                {
                    permission_switch_lock = !permission_switch_lock;
                    UseMainHandPermission = SelectedVersion.Text == "1.9.0" ? Visibility.Collapsed : Visibility.Visible;
                    permission_switch_lock = false;
                }
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否与主手共用权限
        private Visibility useMainHandPermission = Visibility.Collapsed;
        public Visibility UseMainHandPermission
        {
            get { return useMainHandPermission; }
            set
            {
                useMainHandPermission = value;
                if(!permission_switch_lock)
                {
                    permission_switch_lock = !permission_switch_lock;
                    HaveOffHandPermission = SelectedVersion.Text == "1.8.0" ? Visibility.Collapsed : Visibility.Visible;
                    permission_switch_lock = false;
                }
                OnPropertyChanged();
            }
        }
        #endregion

        #region 为盔甲架和坐标轴映射纹理
        public BitmapImage woodenImage { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\oak_planks.png"));
        public BitmapImage horizontalImage { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\HorizontalPlanks.png"));
        public BitmapImage stone { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\smooth_stone.png"));
        public BitmapImage stoneSide { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\stoneSide.png"));
        public BitmapImage axisRed { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\axisRed.png"));
        public BitmapImage axisGreen { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\axisGreen.png"));
        public BitmapImage axisBlue { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\axisBlue.png"));
        public BitmapImage axisGray { get; set; } = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\ArmorStand\images\axisGray.png"));
        #endregion

        #region 预览功能菜单可见性
        private Visibility previewMenuVisibility = Visibility.Visible;
        public Visibility PreviewMenuVisibility
        {
            get => previewMenuVisibility;
            set => SetProperty(ref previewMenuVisibility,value);
        }
        #endregion

        #region Tags
        private string tags;

        public string Tags
        {
            get => tags;
            set => SetProperty(ref tags, value);
        }

        #endregion

        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;

        /// <summary>
        /// 当前视图模型
        /// </summary>
        private Viewport3D ArmorStandViewer = null;

        //as的所有NBT项
        List<string> as_nbts = [];

        /// <summary>
        /// 正在播放
        /// </summary>
        private bool IsPlaying = false;

        #region 显示结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        #region as名称
        private string custom_name;
        public string CustomName
        {
            get => custom_name;
            set => SetProperty(ref custom_name, value);
        }
        private string CustomNameString = "";
        #endregion

        #region as名称可见性
        private bool custom_name_visible;
        public bool CustomNameVisible
        {
            get { return custom_name_visible; }
            set
            {
                custom_name_visible = value;
                OnPropertyChanged();
            }
        }
        private string CustomNameVisibleString
        {
            get { return CustomNameVisible ?"CustomNameVisible:true,":""; }
        }
        #endregion

        #region as的tag
        private string tag;
        public string Tag
        {
            get 
            {
                if (tag != null && tag.Length > 0)
                {
                    string[] tag_string = tag.Split(',');
                    TagString = "Tags:[";
                    for (int i = 0; i < tag_string.Length; i++)
                    {
                        TagString += "\"" + tag_string[i] + "\",";
                    }
                    TagString = TagString.TrimEnd(',') + "],";
                }
                return tag;
            }
            set => SetProperty(ref tag, value);
        }
        private string TagString = "";
        #endregion

        #region BoolNBTs
        private string BoolNBTs
        {
            get
            {
                string result = "";
                foreach (string item in BoolTypeNBT)
                {
                    result += item+":true,";
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
                return int.Parse(result) > 0 ? "DisabledSlots:" + result+"," : "";
            }
        }
        #endregion

        #region PoseString
        private string PoseString
        {
            get
            {
                string result = "";
                bool have_value = HeadXValue || HeadYValue || HeadZValue || BodyXValue || BodyYValue || BodyZValue || LArmXValue || LArmYValue || LArmZValue || RArmXValue || RArmYValue || RArmZValue || LLegXValue || LLegYValue || LLegZValue || RLegXValue || RLegYValue || RLegZValue;

                result = (have_value) ?(HeadXValue || HeadYValue || HeadZValue ? "Head:[" + HeadX + "f," + HeadY + "f," + HeadZ + "f]," : "") + (BodyXValue || BodyYValue || BodyZValue ? "Body:[" + BodyX + "f," + BodyY + "f," + BodyZ + "f]," : "")
                      + (LArmXValue || LArmYValue || LArmZValue ? "LeftArm:[" + LArmX + "f," + LArmY + "f," + LArmZ + "f]," : "")
                      + (RArmXValue || RArmYValue || RArmZValue ? "RightArm:[" + RArmX + "f," + RArmY + "f," + RArmZ + "f]," : "")
                      + (LLegXValue || LLegYValue || LLegZValue ? "LeftLeg:[" + LLegX + "f," + LLegY + "f," + LLegZ + "f]," : "")
                      + (RLegXValue || RLegYValue || RLegZValue ? "RightLeg:[" + RLegX + "f," + RLegY + "f," + RLegZ + "f]," : "") : "";

                result = result.ToString() !="" ? "Pose:{" + result.TrimEnd(',') + "}" : "";
                return result.ToString();
            }
        }
        #endregion

        #region 重置动作的按钮前景颜色对象
        //灰色
        static SolidColorBrush gray_brush = new((Color)ColorConverter.ConvertFromString("#8F8F8F"));
        //白色
        static SolidColorBrush black_brush = new((Color)ColorConverter.ConvertFromString("#000000"));
        #endregion

        #region 摄像机初始坐标和朝向
        double initHorizontalAngle = 0;
        double initVerticalAngle = 0;
        #endregion

        #region 重置按钮属性
        #region 是否可以重置所有动作
        private bool can_reset_all_pose;
        public bool CanResetAllPose
        {
            get { return can_reset_all_pose; }
            set
            {
                can_reset_all_pose = value;
                ResetAllPoseButtonForeground = CanResetAllPose ? black_brush:gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置所有动作的按钮前景
        private Brush reset_all_pose_button_foreground = gray_brush;
        public Brush ResetAllPoseButtonForeground
        {
            get { return reset_all_pose_button_foreground; }
            set
            {
                reset_all_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置头部动作
        private bool can_reset_head_pose;
        public bool CanResetHeadPose
        {
            get { return can_reset_head_pose; }
            set
            {
                can_reset_head_pose = value;
                ResetHeadPoseButtonForeground = CanResetHeadPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置头部动作的按钮前景
        private Brush reset_head_pose_button_foreground = gray_brush;
        public Brush ResetHeadPoseButtonForeground
        {
            get { return reset_head_pose_button_foreground; }
            set
            {
                reset_head_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置身体动作
        private bool can_reset_body_pose;
        public bool CanResetBodyPose
        {
            get { return can_reset_body_pose; }
            set
            {
                can_reset_body_pose = value;
                ResetBodyPoseButtonForeground = CanResetBodyPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置身体动作的按钮前景
        private Brush reset_body_pose_button_foreground = gray_brush;
        public Brush ResetBodyPoseButtonForeground
        {
            get { return reset_body_pose_button_foreground; }
            set
            {
                reset_body_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置左臂动作
        private bool can_reset_larm_pose;
        public bool CanResetLArmPose
        {
            get { return can_reset_larm_pose; }
            set
            {
                can_reset_larm_pose = value;
                ResetLArmPoseButtonForeground = CanResetLArmPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置左臂动作的按钮前景
        private Brush reset_larm_pose_button_foreground = gray_brush;
        public Brush ResetLArmPoseButtonForeground
        {
            get { return reset_larm_pose_button_foreground; }
            set
            {
                reset_larm_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置右臂动作
        private bool can_reset_rarm_pose;
        public bool CanResetRArmPose
        {
            get { return can_reset_rarm_pose; }
            set
            {
                can_reset_rarm_pose = value;
                ResetRArmPoseButtonForeground = CanResetRArmPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置右臂动作的按钮前景
        private Brush reset_rarm_pose_button_foreground = gray_brush;
        public Brush ResetRArmPoseButtonForeground
        {
            get { return reset_rarm_pose_button_foreground; }
            set
            {
                reset_rarm_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置左腿动作
        private bool can_reset_lleg_pose;
        public bool CanResetLLegPose
        {
            get { return can_reset_lleg_pose; }
            set
            {
                can_reset_lleg_pose = value;
                ResetLLegPoseButtonForeground = CanResetLLegPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置左臂动作的按钮前景
        private Brush reset_lleg_pose_button_foreground = gray_brush;
        public Brush ResetLLegPoseButtonForeground
        {
            get { return reset_lleg_pose_button_foreground; }
            set
            {
                reset_lleg_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否可以重置右腿动作
        private bool can_reset_rleg_pose;
        public bool CanResetRLegPose
        {
            get { return can_reset_rleg_pose; }
            set
            {
                can_reset_rleg_pose = value;
                ResetRLegPoseButtonForeground = CanResetRLegPose ? black_brush : gray_brush;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 重置右臂动作的按钮前景
        private Brush reset_rleg_pose_button_foreground = gray_brush;
        public Brush ResetRLegPoseButtonForeground
        {
            get { return reset_rleg_pose_button_foreground; }
            set
            {
                reset_rleg_pose_button_foreground = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #endregion

        #region 整体水平旋转
        private float rotationX = 0f;
        public float RotationX
        {
            get => rotationX;
            set => SetProperty(ref rotationX, value);
        }
        #endregion

        #region 头部XYZ
        private bool HeadXValue;
        private float head_x = 0f;
        public float HeadX
        {
            get { return head_x; }
            set
            {
                head_x = value;
                HeadXValue = HeadX != 0f?true:false;
                OnPropertyChanged();
            }
        }

        private bool HeadYValue;
        private float head_y = 0f;
        public float HeadY
        {
            get { return head_y; }
            set
            {
                head_y = value;
                HeadYValue = HeadY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool HeadZValue;
        private float head_z = 0f;
        public float HeadZ
        {
            get { return head_z; }
            set
            {
                head_z = value;
                HeadZValue = HeadZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 身体XYZ
        private bool BodyXValue;
        private float body_x = 0f;
        public float BodyX
        {
            get { return body_x; }
            set
            {
                body_x = value;
                BodyXValue = BodyX != 0f ?true:false;
                //TurnModel(new Point3D(0.5, 9.5, 0.5), TopModel, 0.02, true, BodyX, BodyY, BodyZ);
                //TurnModel(new Point3D(0.5, 9.5, 0.5), BottomModel, 0.02, true, BodyX, BodyY, BodyZ);
                //TurnModel(new Point3D(0.5, 9.5, 0.5), LeftModel, 0.02, true, BodyX, BodyY, BodyZ);
                //TurnModel(new Point3D(0.5, 9.5, 0.5), RightModel, 0.02, true, BodyX, BodyY, BodyZ);
                OnPropertyChanged();
            }
        }

        private bool BodyYValue;
        private float body_y = 0f;
        public float BodyY
        {
            get { return body_y; }
            set
            {
                body_y = value;
                BodyYValue = BodyY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool BodyZValue;
        private float body_z = 0f;
        public float BodyZ
        {
            get { return body_z; }
            set
            {
                body_z = value;
                BodyZValue = BodyZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 左臂XYZ
        private bool LArmXValue;
        private float larm_x = 0f;
        public float LArmX
        {
            get { return larm_x; }
            set
            {
                larm_x = value;
                LArmXValue = LArmX != 0f;
                OnPropertyChanged();
            }
        }

        private bool LArmYValue;
        private float larm_y = 0f;
        public float LArmY
        {
            get { return larm_y; }
            set
            {
                larm_y = value;
                LArmYValue = LArmY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool LArmZValue;
        private float larm_z = 0f;
        public float LArmZ
        {
            get { return larm_z; }
            set
            {
                larm_z = value;
                LArmZValue = LArmZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 右臂XYZ
        private bool RArmXValue;
        private float rarm_x = 0f;
        public float RArmX
        {
            get { return rarm_x; }
            set
            {
                rarm_x = value;
                RArmXValue = RArmX != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool RArmYValue;
        private float rarm_y = 0f;
        public float RArmY
        {
            get { return rarm_y; }
            set
            {
                rarm_y = value;
                RArmYValue = RArmY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool RArmZValue;
        private float rarm_z = 0f;
        public float RArmZ
        {
            get { return rarm_z; }
            set
            {
                rarm_z = value;
                RArmZValue = RArmZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 左腿XYZ
        private bool LLegXValue;
        private float lleg_x = 0f;
        public float LLegX
        {
            get { return lleg_x; }
            set
            {
                lleg_x = value;
                LLegXValue = LLegX != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool LLegYValue;
        private float lleg_y = 0f;
        public float LLegY
        {
            get { return lleg_y; }
            set
            {
                lleg_y = value;
                LLegYValue = LLegY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool LLegZValue;
        private float lleg_z = 0f;
        public float LLegZ
        {
            get { return lleg_z; }
            set
            {
                lleg_z = value;
                LLegZValue = LLegZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion
        #region 右腿XYZ
        private bool RLegXValue;
        private float rleg_x = 0f;
        public float RLegX
        {
            get { return rleg_x; }
            set
            {
                rleg_x = value;
                RLegXValue = RLegX != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool RLegYValue;
        private float rleg_y = 0f;
        public float RLegY
        {
            get { return rleg_y; }
            set
            {
                rleg_y = value;
                RLegYValue = RLegY != 0f ?true:false;
                OnPropertyChanged();
            }
        }

        private bool RLegZValue;
        private float rleg_z = 0f;
        public float RLegZ
        {
            get { return rleg_z; }
            set
            {
                rleg_z = value;
                RLegZValue = RLegZ != 0f ?true:false;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 装备

        #region 合并装备数据
        private string Equipments
        {
            get
            {
                string result;
                string ArmorItems = (HeadItem.Length + BodyItem.Length + LegsItem.Length + FeetItem.Length) > 0 ? "ArmorItems:[" + (HeadItem + "," + BodyItem + "," + LegsItem + "," + FeetItem).Trim(',') + "]," : "";
                string HandItems = (LeftHandItem.Length + RightHandItem.Length) > 0 ? "HandItems:[" + (LeftHandItem + "," + RightHandItem).Trim(',') + "]," : "";
                result = ArmorItems + HandItems;
                return result;
            }
        }
        #endregion

        #region Head
        private string head_item = "";
        public string HeadItem
        {
            get { return head_item; }
            set
            {
                head_item = value;
            }
        }
        #endregion

        #region Body
        private string body_item = "";
        public string BodyItem
        {
            get { return body_item; }
            set
            {
                body_item = value;
            }
        }
        #endregion

        #region LeftHand
        private string left_hand_item = "";
        public string LeftHandItem
        {
            get { return left_hand_item; }
            set
            {
                left_hand_item = value;
            }
        }
        #endregion

        #region RightHand
        private string right_hand_item = "";
        public string RightHandItem
        {
            get { return right_hand_item; }
            set
            {
                right_hand_item = value;
            }
        }
        #endregion

        #region Legs
        private string leg_item = "";
        public string LegsItem
        {
            get { return leg_item; }
            set
            {
                leg_item = value;
            }
        }
        #endregion

        #region Boots
        private string feet_item = "";
        public string FeetItem
        {
            get { return feet_item; }
            set
            {
                feet_item = value;
            }
        }
        #endregion

        #endregion

        #region 控制右侧预览视图的双向旋转
        private double horizontalAngleDelta = 0;
        private double verticalAngleDelta = 0;
        private double horizontalAngle = 0;
        public double HorizontalAngle
        {
            get => horizontalAngle;
            set => SetProperty(ref horizontalAngle,value);
        }
        private double verticalAngle = 0;
        public double VerticalAngle
        {
            get => verticalAngle;
            set => SetProperty(ref verticalAngle, value);
        }
        #endregion

        #region 记录上一次的鼠标位置
        private Point last_cursor_position;
        private Point LastCursorPosition
        {
            get { return last_cursor_position; }
            set
            {
                last_cursor_position = value;
                OnPropertyChanged();
            }
        }
        #endregion

        /// <summary>
        /// 开始三轴合一
        /// </summary>
        private bool UsingThreeAxis = false;

        #region 三轴合一数据更新载体
        TextBlock XAxis = new();
        TextBlock YAxis = new();
        TextBlock ZAxis = new();
        
        //用于自增和自减
        float XAxisValue = 0f;
        float YAxisValue = 0f;
        float ZAxisValue = 0f;
        #endregion

        /// <summary>
        /// 超出三轴合一按钮范围
        /// </summary>
        private bool OutOfThreeAxis = false;

        /// <summary>
        /// 当前三轴合一按钮位置
        /// </summary>
        private Point CurrentButtonCenter;

        // 布尔型NBT链表
        List<string> BoolTypeNBT = [];

        //禁止移除或改变总值
        private int CannotTakeOrReplceSum;
        //禁止添加或改变总值
        private int CannotPlaceOrReplaceSum;
        //禁止添加总值
        private int CannotPlaceSum;

        #region 各部位装备图像源
        private ImageSource leftHandItemImage;

        public ImageSource LeftHandItemImage
        {
            get => leftHandItemImage;
            set => SetProperty(ref leftHandItemImage, value);
        }
        private ImageSource rightHandItemImage;

        public ImageSource RightHandItemImage
        {
            get => rightHandItemImage;
            set => SetProperty(ref rightHandItemImage, value);
        }
        private ImageSource headItemImage;

        public ImageSource HeadItemImage
        {
            get => headItemImage;
            set => SetProperty(ref headItemImage, value);
        }
        private ImageSource chestItemImage;

        public ImageSource ChestItemImage
        {
            get => chestItemImage;
            set => SetProperty(ref chestItemImage, value);
        }
        private ImageSource legItemImage;

        public ImageSource LegItemImage
        {
            get => legItemImage;
            set => SetProperty(ref legItemImage, value);
        }
        private ImageSource feetItemImage;

        public ImageSource FeetItemImage
        {
            get => feetItemImage;
            set => SetProperty(ref feetItemImage, value);
        }
        #endregion

        #region 禁止移除或改变头部、身体、手部、腿部、脚部装备
        private bool cannotTakeOrReplaceHead;
        public bool CannotTakeOrReplaceHead
        {
            get
            {
                return cannotTakeOrReplaceHead;
            }
            set
            {
                cannotTakeOrReplaceHead = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += cannotTakeOrReplaceHead ? 4096 : -4096;
            }
        }

        private bool cannotTakeOrReplaceBody;
        public bool CannotTakeOrReplaceBody
        {
            get { return cannotTakeOrReplaceBody; }
            set
            {
                cannotTakeOrReplaceBody = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += cannotTakeOrReplaceBody ? 2048 : -2048;
            }
        }

        private bool cannotTakeOrReplaceMainhand;
        public bool CannotTakeOrReplaceMainHand
        {
            get { return cannotTakeOrReplaceMainhand; }
            set
            {
                cannotTakeOrReplaceMainhand = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += cannotTakeOrReplaceMainhand ? 256 : -256;
            }
        }

        private bool cannotTakeOrReplaceOffHand;
        public bool CannotTakeOrReplaceOffHand
        {
            get { return cannotTakeOrReplaceOffHand; }
            set
            {
                cannotTakeOrReplaceOffHand = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += HaveOffHandPermission == Visibility.Visible && cannotTakeOrReplaceOffHand ? 8192 : -8192;
            }
        }

        private bool cannotTakeOrReplaceLegs;
        public bool CannotTakeOrReplaceLegs
        {
            get { return cannotTakeOrReplaceLegs; }
            set
            {
                cannotTakeOrReplaceLegs = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += cannotTakeOrReplaceLegs ? 1024 : -1024;
            }
        }

        private bool cannotTakeOrReplaceBoots;
        public bool CannotTakeOrReplaceBoots
        {
            get { return cannotTakeOrReplaceBoots; }
            set
            {
                cannotTakeOrReplaceBoots = value;
                OnPropertyChanged();
                CannotTakeOrReplceSum += cannotTakeOrReplaceBoots ? 512 : -512;
            }
        }
        #endregion

        #region 禁止添加或改变头部、身体、手部、腿部、脚部装备
        private bool cannotPlaceOrReplacehead;
        public bool CannotPlaceOrReplaceHead
        {
            get { return cannotPlaceOrReplacehead; }
            set
            {
                cannotPlaceOrReplacehead = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += cannotPlaceOrReplacehead ? 16 : -16;
            }
        }

        private bool cannotPlaceOrReplacebody;
        public bool CannotPlaceOrReplaceBody
        {
            get { return cannotPlaceOrReplacebody; }
            set
            {
                cannotPlaceOrReplacebody = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += cannotPlaceOrReplacebody ? 8 : -8;
            }
        }

        private bool cannotPlaceOrReplaceMainHand;
        public bool CannotPlaceOrReplaceMainHand
        {
            get { return cannotPlaceOrReplaceMainHand; }
            set
            {
                cannotPlaceOrReplaceMainHand = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceMainHand ? 1 : -1;
            }
        }

        private bool cannotPlaceOrReplaceOffHand;
        public bool CannotPlaceOrReplaceOffHand
        {
            get { return cannotPlaceOrReplaceOffHand; }
            set
            {
                cannotPlaceOrReplaceOffHand = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += HaveOffHandPermission == Visibility.Visible && cannotPlaceOrReplaceOffHand ? 32 : -32;
            }
        }

        private bool cannotPlaceOrReplaceLegs;
        public bool CannotPlaceOrReplaceLegs
        {
            get { return cannotPlaceOrReplaceLegs; }
            set
            {
                cannotPlaceOrReplaceLegs = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceLegs ? 4 : -4;
            }
        }

        private bool cannotPlaceOrReplaceBoots;
        public bool CannotPlaceOrReplaceBoots
        {
            get { return cannotPlaceOrReplaceBoots; }
            set
            {
                cannotPlaceOrReplaceBoots = value;
                OnPropertyChanged();
                CannotPlaceOrReplaceSum += cannotPlaceOrReplaceBoots ? 2 : -2;
            }
        }
        #endregion

        #region 禁止添加头部、身体、手部、腿部、脚部装备
        private bool cannotPlaceHead;
        public bool CannotPlaceHead
        {
            get
            {
                return cannotPlaceHead;
            }
            set
            {
                cannotPlaceHead = value;
                OnPropertyChanged();
                CannotPlaceSum += cannotPlaceHead ? 1048576 : -1048576;
            }
        }

        private bool cannotPlaceBody;
        public bool CannotPlaceBody
        {
            get { return cannotPlaceBody; }
            set
            {
                cannotPlaceBody = value;
                OnPropertyChanged();
                CannotPlaceSum += cannotPlaceBody ? 524288 : -524288;
            }
        }

        private bool cannotPlaceMainHand;
        public bool CannotPlaceMainHand
        {
            get { return cannotPlaceMainHand; }
            set
            {
                cannotPlaceMainHand = value;
                OnPropertyChanged();
                CannotPlaceSum += cannotPlaceMainHand ? 65536 : -65536;
            }
        }

        private bool cannotPlaceOffHand;
        public bool CannotPlaceOffHand
        {
            get { return cannotPlaceOffHand; }
            set
            {
                cannotPlaceOffHand = value;
                OnPropertyChanged();
                CannotPlaceSum += HaveOffHandPermission == Visibility.Visible && cannotPlaceOffHand ? 2097152 : -2097152;
            }
        }

        private bool cannotPlaceLegs;
        public bool CannotPlaceLegs
        {
            get { return cannotPlaceLegs; }
            set
            {
                cannotPlaceLegs = value;
                OnPropertyChanged();
                CannotPlaceSum += cannotPlaceLegs ? 262144 : -262144;
            }
        }

        private bool cannotPlaceBoots;
        public bool CannotPlaceBoots
        {
            get { return cannotPlaceBoots; }
            set
            {
                cannotPlaceBoots = value;
                OnPropertyChanged();
                CannotPlaceSum += cannotPlaceBoots ? 131072 : -131072;
            }
        }
        #endregion

        //布尔NBT集合
        StackPanel NBTList = null;

        #region 版本数据源
        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.5" },
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

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconArmorStand.png";
        #endregion

        #region 引用
        /// <summary>
        /// 前台窗体引用
        /// </summary>
        ArmorStand armorStand = null;
        /// <summary>
        /// 样式化文本框引用
        /// </summary>
        StylizedTextBox stylizedTextBox = null;
        /// <summary>
        /// 标签文本框
        /// </summary>
        TagRichTextBox tagRichTextBox = null;

        #region 所有3D视图对象
        public GeometryModel3D HeadModel { get; set; }
        public ModelVisual3D LeftArmModel { get; set; }
        public ModelVisual3D RightArmModel { get; set; }
        public GeometryModel3D LeftLegModel { get; set; }
        public GeometryModel3D RightLegModel { get; set; }
        public GeometryModel3D TopModel { get; set; }
        public GeometryModel3D BottomModel { get; set; }
        public GeometryModel3D LeftModel { get; set; }
        public GeometryModel3D RightModel { get; set; }
        public ModelVisual3D BasePlateModel { get; set; }
        public ModelVisual3D ArmGroup { get; set; }

        public ModelVisual3D ModelGroup { get; set; }

        private double lastMousePosX = 0.0;

        private double lastMousePosY = 0.0;

        private double deltaMoveX;

        private double deltaMoveY;
        #endregion

        #endregion

        public ArmorStandDataContext()
        {
            #region 连接三个轴的上下文
            XAxis.DataContext = this;
            YAxis.DataContext = this;
            ZAxis.DataContext = this;
            #endregion
        }

        #region Events
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
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender,RoutedEventArgs e) => await stylizedTextBox.Upgrade(CurrentMinVersion);

        /// <summary>
        /// 检测名称文本框内容为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StylizedTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if((e.Key == Key.Back || e.Key == Key.Delete))
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
        /// 禁用Gizmo自带的旋转
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HelixViewport3D_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

        /// <summary>
        /// 禁用Gizmo自带的缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HelixViewport3D_PreviewMouseWheel(object sender, MouseWheelEventArgs e) => e.Handled = true;

        [RelayCommand]
        /// <summary>
        /// 重置摄像机状态
        /// </summary>
        private void ResetCameraState()
        {
            HorizontalAngle = initHorizontalAngle;
            VerticalAngle = initVerticalAngle;
            lastMousePosX = lastMousePosY = deltaMoveX = deltaMoveY = horizontalAngleDelta = verticalAngleDelta = 0;
            armorStand.mainCamera.Position = new(0,0,0);
        }

        [RelayCommand]
        /// <summary>
        /// 设置盔甲架的装备
        /// </summary>
        private void SetItem(FrameworkElement element)
        {
            Button iconTextButtons = element as Button;
            Item item = new();
            ItemDataContext itemContext = item.DataContext as ItemDataContext;
            itemContext.IsCloseable = false;

            #region 设置已生成的数据
            string Result = "";
            if (item.ShowDialog().Value)
            {
                ItemPageDataContext pageContext = (itemContext.ItemPageList[0].Content as ItemPages).DataContext as ItemPageDataContext;
                itemContext.home = Window.GetWindow(element);

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
                string itemID = data.SelectToken("id").ToString().Replace("minecraft:","");
                string filePath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png";
                switch (iconTextButtons.Uid)
                {
                    case "Head":
                        if (File.Exists(filePath))
                            HeadItemImage = new BitmapImage(new Uri(filePath,UriKind.Absolute));
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
                        LegsItem = Result;
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
        /// 生成as
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
                CustomName = CustomName.Replace(@"\\n","");
            }

            #region 拼接指令数据

            #region HaveAnimation
            #endregion

            #region Result
            string nbt = CustomName + BoolNBTs + Equipments + DisabledValue + CustomNameVisibleString + Tags + PoseString;
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
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(result, "盔甲架", icon_path);
                displayer.Show();
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
            if(CanResetAllPose)
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
            if(CanResetHeadPose)
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
            if(CanResetBodyPose)
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
            if(CanResetLArmPose)
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
            if(CanResetRArmPose)
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
            if(CanResetLLegPose)
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
            if(CanResetRLegPose)
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

        /// <summary>
        /// 载入基础NBT列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NBTCheckboxListLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\ArmorStand\\data\\base_nbt.ini"))
                as_nbts = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\ArmorStand\\data\\base_nbt.ini", Encoding.UTF8).ToList();
            NBTList = sender as StackPanel;

            if (as_nbts.Count > 0)
            {
                foreach (string item in as_nbts)
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
                        Style = (NBTList.Children[0] as TextCheckBoxs).Style
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
                NBTList.Children.RemoveAt(0);
            }
        }

        /// <summary>   
        /// 设置鼠标的坐标   
        /// </summary>   
        /// <param name="x">横坐标</param>   
        /// <param name="y">纵坐标</param>   
        [DllImport("User32")]
        public extern static void SetCursorPos(int x, int y);

        public void ThreeAxisMouseMove(object sender, MouseEventArgs e)
        {
            if (UsingThreeAxis)
            {
                Button btn = sender as Button;
                CurrentButtonCenter = Mouse.GetPosition(btn);
                LastCursorPosition = new Point(LastCursorPosition.X - CurrentButtonCenter.X, LastCursorPosition.Y - CurrentButtonCenter.Y);

                if(LastCursorPosition.X > 0)
                XAxisValue += -1;
                else
                    if(LastCursorPosition.X < 0)
                    XAxisValue += 1;
                else
                    if(LastCursorPosition.Y > 0)
                    ZAxisValue += -1;
                else
                if (LastCursorPosition.Y < 0)
                    ZAxisValue += 1;

                XAxisValue = XAxisValue > 180 ? 180 : (XAxisValue < -180)? -180 : XAxisValue;
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
            YAxisValue = YAxisValue > 180 ?180:(YAxisValue<-180)?-180:YAxisValue;
            YAxis.Tag = YAxisValue;
        }

        private void play_animation(IconTextButtons btn)
        {
            IsPlaying = !IsPlaying;
            string pause_data = "F1 M191.397656 128.194684l191.080943 0 0 768.472256-191.080943 0 0-768.472256Z M575.874261 128.194684l192.901405 0 0 768.472256-192.901405 0 0-768.472256Z";
            string playing_data = "M870.2 466.333333l-618.666667-373.28a53.333333 53.333333 0 0 0-80.866666 45.666667v746.56a53.206667 53.206667 0 0 0 80.886666 45.666667l618.666667-373.28a53.333333 53.333333 0 0 0 0-91.333334z";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            btn.IconData = IsPlaying? converter.ConvertFrom(pause_data) as Geometry: converter.ConvertFrom(playing_data) as Geometry;
            btn.ContentData = IsPlaying?"暂停":"播放";
        }

        /// <summary>
        /// 隐藏盔甲架手臂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideArmsInModel(object sender, RoutedEventArgs e)
        {
            ArmGroup.Children.Remove(LeftArmModel);
            ArmGroup.Children.Remove(RightArmModel);
        }

        /// <summary>
        /// 显示盔甲架的手臂
        /// </summary>
        public void ShowArmsInModel(object sender, RoutedEventArgs e)
        {
            ArmGroup.Children.Add(LeftArmModel);
            ArmGroup.Children.Add(RightArmModel);
        }

        /// <summary>
        /// 显示盔甲架底座
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HaveBasePlateInModel(object sender, RoutedEventArgs e)
        {
            ModelGroup.Children.Add(BasePlateModel);
        }

        /// <summary>
        /// 不显示盔甲架的底座
        /// </summary>
        public void NoBasePlateInModel(object sender, RoutedEventArgs e)
        {
            ModelGroup.Children.Remove(BasePlateModel);
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
        public void ArmorStandLoaded(object sender, RoutedEventArgs e)
        {
            ArmorStandViewer = sender as Viewport3D;
            armorStand = Window.GetWindow(ArmorStandViewer) as ArmorStand;
            //载入盔甲架整体容器，用于旋转
            ModelGroup = armorStand.ArmorStandContainer;
            //载入左右臂
            LeftArmModel = armorStand.Arms.Children[0] as ModelVisual3D;
            RightArmModel = armorStand.Arms.Children[1] as ModelVisual3D;
            ArmGroup = armorStand.Arms;
            ArmGroup.Children.Clear();
            //载入底盘
            BasePlateModel = armorStand.BasePlate;

            #region 载入初始坐标和朝向
            initHorizontalAngle = HorizontalAngle;
            initVerticalAngle = VerticalAngle;
            #endregion

            #region 创建SceneGizmo
            //底面半径
            double baseRadius = 0.8;
            //高
            double height = 2;
            //离原点的距离
            double distance = 2.8;

            #region z正半轴
            var ZAxisPositiveBuilder = new MeshBuilder(false, false);
            ZAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var ZAxisPositiveMesh = ZAxisPositiveBuilder.ToMesh();
            Transform3DGroup ZAxisPositiveTransform3DGroup = new();
            TranslateTransform3D ZAxisPositiveTranslateTransform3D = new(0, 0, distance);
            RotateTransform3D ZAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(-1, 0, 0), 90));
            ZAxisPositiveTransform3DGroup.Children.Add(ZAxisPositiveRotateTransform3D);
            ZAxisPositiveTransform3DGroup.Children.Add(ZAxisPositiveTranslateTransform3D);
            var ZAxisPositiveModel = new GeometryModel3D
            {
                Geometry = ZAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Green),
                Transform = ZAxisPositiveTransform3DGroup
            };
            ModelVisual3D ZAxisPositiveVisual = new()
            {
                Content = ZAxisPositiveModel
            };
            armorStand.SceneGizmo.Children.Add(ZAxisPositiveVisual);
            #endregion
            #region z负半轴
            var ZAxisNegativeBuilder = new MeshBuilder(false, false);
            ZAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var ZAxisNegativeMesh = ZAxisNegativeBuilder.ToMesh();
            Transform3DGroup ZAxisNegativeTransform3DGroup = new();
            TranslateTransform3D ZAxisNegativeTranslateTransform3D = new(0, 0, -distance);
            RotateTransform3D ZAxisNegativeRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90));
            ZAxisNegativeTransform3DGroup.Children.Add(ZAxisNegativeRotateTransform3D);
            ZAxisNegativeTransform3DGroup.Children.Add(ZAxisNegativeTranslateTransform3D);
            var ZAxisNegativeModel = new GeometryModel3D
            {
                Geometry = ZAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = ZAxisNegativeTransform3DGroup
            };
            ModelVisual3D ZAxisNegativeVisual = new()
            {
                Content = ZAxisNegativeModel
            };
            armorStand.SceneGizmo.Children.Add(ZAxisNegativeVisual);
            #endregion

            #region y正半轴
            var YAxisPositiveBuilder = new MeshBuilder(false, false);
            YAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var YAxisPositiveMesh = YAxisPositiveBuilder.ToMesh();
            Transform3DGroup YAxisPositiveTransform3DGroup = new();
            TranslateTransform3D YAxisPositiveTranslateTransform3D = new(0, distance, 0);
            RotateTransform3D YAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));
            YAxisPositiveTransform3DGroup.Children.Add(YAxisPositiveRotateTransform3D);
            YAxisPositiveTransform3DGroup.Children.Add(YAxisPositiveTranslateTransform3D);
            var YAxisPositiveModel = new GeometryModel3D
            {
                Geometry = YAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Blue),
                Transform = YAxisPositiveTransform3DGroup
            };
            ModelVisual3D YAxisPositiveVisual = new()
            {
                Content = YAxisPositiveModel
            };
            armorStand.SceneGizmo.Children.Add(YAxisPositiveVisual);
            #endregion
            #region y负半轴
            var YAxisNegativeBuilder = new MeshBuilder(false, false);
            YAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var YAxisNegativeMesh = YAxisNegativeBuilder.ToMesh();
            Transform3DGroup YAxisNegativeTransform3DGroup = new();
            TranslateTransform3D YAxisNegativeTranslateTransform3D = new(0, -distance, 0);
            YAxisNegativeTransform3DGroup.Children.Add(YAxisNegativeTranslateTransform3D);
            var YAxisNegativeModel = new GeometryModel3D
            {
                Geometry = YAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = YAxisNegativeTransform3DGroup
            };
            ModelVisual3D YAxisNegativeVisual = new()
            {
                Content = YAxisNegativeModel
            };
            armorStand.SceneGizmo.Children.Add(YAxisNegativeVisual);
            #endregion

            #region x正半轴
            var XAxisPositiveBuilder = new MeshBuilder(false, false);
            XAxisPositiveBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var XAxisPositiveMesh = XAxisPositiveBuilder.ToMesh();
            Transform3DGroup XAxisPositiveTransform3DGroup = new();
            TranslateTransform3D XAxisPositiveTranslateTransform3D = new(distance, 0, 0);
            RotateTransform3D XAxisPositiveRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90));
            XAxisPositiveTransform3DGroup.Children.Add(XAxisPositiveRotateTransform3D);
            XAxisPositiveTransform3DGroup.Children.Add(XAxisPositiveTranslateTransform3D);
            var XAxisPositiveModel = new GeometryModel3D
            {
                Geometry = XAxisPositiveMesh,
                Material = new DiffuseMaterial(Brushes.Red),
                Transform = XAxisPositiveTransform3DGroup
            };
            ModelVisual3D XAxisPositiveVisual = new()
            {
                Content = XAxisPositiveModel
            };
            armorStand.SceneGizmo.Children.Add(XAxisPositiveVisual);
            #endregion
            #region x负半轴
            var XAxisNegativeBuilder = new MeshBuilder(false, false);
            XAxisNegativeBuilder.AddCone(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), baseRadius, 0, height, true, true, 32);
            var XAxisNegativeMesh = XAxisNegativeBuilder.ToMesh();
            Transform3DGroup XAxisNegativeTransform3DGroup = new();
            TranslateTransform3D XAxisNegativeTranslateTransform3D = new(-distance, 0, 0);
            RotateTransform3D XAxisNegativeRotateTransform3D = new(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90));
            XAxisNegativeTransform3DGroup.Children.Add(XAxisNegativeRotateTransform3D);
            XAxisNegativeTransform3DGroup.Children.Add(XAxisNegativeTranslateTransform3D);
            var XAxisNegativeModel = new GeometryModel3D
            {
                Geometry = XAxisNegativeMesh,
                Material = new DiffuseMaterial(Brushes.LightGray),
                Transform = XAxisNegativeTransform3DGroup
            };
            ModelVisual3D XAxisNegativeVisual = new()
            {
                Content = XAxisNegativeModel
            };
            armorStand.SceneGizmo.Children.Add(XAxisNegativeVisual);
            #endregion

            #endregion
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
        }

        /// <summary>
        /// 处理模型视图中鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ModelViewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
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
        }

        /// <summary>
        /// 处理模型视图中鼠标滚轮的滚动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ModelViewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                Move(2);
            else if (e.Delta < 0)
                Move(-2);
        }
        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// 移动摄像机与模型之间的相对距离
        /// </summary>
        /// <param name="d"></param>
        public void Move(double d)
        {
            double u = 0.05;
            Vector3D lookDirection = armorStand.mainCamera.LookDirection;
            Point3D position = armorStand.mainCamera.Position;
            lookDirection.Normalize();
            position += u * lookDirection * d;
            armorStand.mainCamera.Position = position;
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
        #endregion
    }
}