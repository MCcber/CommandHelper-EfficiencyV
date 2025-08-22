using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Interface;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Entity
{
    public partial class AreaEffectCloudEffectViewModel: ObservableObject, IComponentBuilder
    {
        #region Field
        private CBHKDataContext _context = null;
        #endregion

        #region Property
        public float FloatMinValue
        {
            get => float.MinValue;
        }
        public float FloatMaxValue
        {
            get => float.MaxValue;
        }
        public IEventAggregator EventAggregator { get; private set; }
        public RemoveComponentEvent RemoveComponentEvent { get; private set; }
        public string SelectedVersion { get; private set; }

        private string oldID = "";

        public StringBuilder Result { get; private set; }

        [ObservableProperty]
        private ObservableCollection<IconComboBoxItem> _effectIDList = [];

        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }

        [ObservableProperty]
        private bool ambient = false;

        [ObservableProperty]
        private bool showIcon = false;

        [ObservableProperty]
        private bool showParticles = false;

        [ObservableProperty]
        private bool amplifier = false;

        [ObservableProperty]
        private bool duration = false;

        [ObservableProperty]
        private IconComboBoxItem selectedID;

        [ObservableProperty]
        private bool had_effect_last_tick = false;

        [ObservableProperty]
        private double effect_changed_timestamp = 0;

        [ObservableProperty]
        private double padding_duration = 0;

        [ObservableProperty]
        private double factor_current = 0;

        [ObservableProperty]
        private double factor_previous_frame = 0;

        [ObservableProperty]
        private double factor_start = 0;

        [ObservableProperty]
        private double factor_target = 0;
        #endregion

        #region Method
        public AreaEffectCloudEffectViewModel(CBHKDataContext context, IEventAggregator eventAggregator)
        {
            _context = context;
            EventAggregator = eventAggregator;
            RemoveComponentEvent = EventAggregator.GetEvent<RemoveComponentEvent>();
        }
        #endregion

        #region Event
        /// <summary>
        /// 删除此状态效果
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void Delete(FrameworkElement obj)
        {
            RemoveComponentEvent.Publish(obj);
        }

        /// <summary>
        /// 载入状态效果数据源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Component_Loaded(object sender, RoutedEventArgs e)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (var item in _context.MobEffectSet)
            {
                string id = item.ID;
                string imagePath = "";
                if (File.Exists(currentPath + id + ".png"))
                    imagePath = currentPath + id + ".png";
                EffectIDList.Add(new IconComboBoxItem()
                {
                    ComboBoxItemIcon = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                    ComboBoxItemId = id,
                    ComboBoxItemText = item.Name
                });
            }
            SelectedID = EffectIDList[0];
        }

        public void UpdateVersion()
        {
            if (VersionComparer.IsInRange(SelectedVersion, "1.16"))
            {
                string currentID = SelectedID.ComboBoxItemId;
                string number = _context.MobEffectSet.First(item => item.ID == currentID).Number.ToString();
                if (number is not null)
                    oldID = number;
            }
            else
            {
                oldID = "\"minecraft:" + SelectedID.ComboBoxItemId + "\"";
            }
        }

        public void Create()
        {
            Result = new();
        }

        public void CollectionData()
        {
            List<string> Data = [];
            List<string> FactorCalculationList = [];
            Result = new("{" + string.Join(",", Data) + (FactorCalculationList.Count > 0 ? ",FactorCalculationData:{" + string.Join(",", FactorCalculationList) + "}" : "") + "}");
            if (Result.ToString() == "{}")
            {
                Result.Clear();
            }
        }

        public StringBuilder Build()
        {
            return Result;
        }
        #endregion
    }
}
