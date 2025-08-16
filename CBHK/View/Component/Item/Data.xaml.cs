using CBHK.CustomControl.Interfaces;
using CBHK.Domain;
using CBHK.GeneralTool;
using CBHK.ViewModel.Component.Item;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Item
{
    /// <summary>
    /// Data.xaml 的交互逻辑
    /// </summary>
    public partial class Data : UserControl
    {
        //属性数据源
        public ObservableCollection<AttributeItems> AttributeSource = [];
        private CBHKDataContext _context = null;
        private DataService _dataService = null;
        private Dictionary<string, string> ItemIDAndNameMap;

        #region 合并结果
        public async Task<string> Result()
        {
            string result = "";
            if(AttributeSource.Count > 0)
            {
                StringBuilder modifierString = new();
                string modifierElement;
                for (int i = 0; i < AttributeSource.Count; i++)
                {
                    modifierElement = await (AttributeSource[i] as IVersionUpgrader).Result();
                    modifierString.Append(modifierElement + ",");
                }
                result = "AttributeModifiers:[" + modifierString.ToString().TrimEnd(',') + "],";
            }
            return result;
        }
        #endregion

        public Data(DataService dataService,CBHKDataContext context)
        {
            InitializeComponent();
            _dataService = dataService;
            _context = context;
            ItemIDAndNameMap = _dataService.GetItemIDAndNameGroupByVersionMap().SelectMany(item => item.Value).ToDictionary();
            Attributes.Modify = new RelayCommand<FrameworkElement>(AddAttributeCommand);
            Attributes.Fresh = new RelayCommand<FrameworkElement>(ClearAttributesComand);
            AttributesPanel.ItemsSource = AttributeSource;
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="obj"></param>
        private void AddAttributeCommand(FrameworkElement obj)
        {
            AttributeItems attributeItems = new(_context)
            {
                Margin = new(0, 2, 0, 0)
            };
            AttributeSource.Add(attributeItems);
            ItemPageViewModel itemPageDataContext = attributeItems.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(attributeItems);
        }

        /// <summary>
        /// 清空属性
        /// </summary>
        /// <param name="obj"></param>
        private void ClearAttributesComand(FrameworkElement obj)
        {
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            for (int i = 0; i < AttributeSource.Count; i++)
            {
                itemPageDataContext.VersionComponents.Remove(AttributeSource[i]);
                AttributeSource.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// 获取外来数据
        /// </summary>
        /// <param name="ExternData"></param>
        public void GetExternData(ref JObject ExternData)
        {
            JToken count = ExternData.SelectToken("Count");
            JToken Damage = ExternData.SelectToken("tag.Damage");
            Damage ??= ExternData.SelectToken("Damage");

            if (count != null)
            {
                ItemCount.Value = int.Parse(count.ToString());
                ExternData.Remove("Count");
            }
            if (Damage != null)
            {
                ItemDamage.Value = int.Parse(Damage.ToString());
                ExternData.Remove("tag.Damage");
                ExternData.Remove("Damage");
            }
            JToken Attributes = ExternData.SelectToken("tag.Attributes");
            Attributes ??= ExternData.SelectToken("Attributes");
            if (Attributes is JArray AttributeArray && AttributeArray.Count > 0)
            {
                foreach (JObject attribute in AttributeArray.Cast<JObject>())
                {
                    AddAttributeCommand(null);
                    JToken attributeId = attribute.SelectToken("AttributeName");
                    JToken name = attribute.SelectToken("Name");
                    JToken amount = attribute.SelectToken("Amount");
                    JToken operation = attribute.SelectToken("Operation");
                    JToken slot = attribute.SelectToken("Slot");
                    AttributeItems attributeItem = AttributeSource[^1];

                    if (attributeId != null)
                    {
                        string attributeIDString = attributeId.ToString();
                        attributeItem.AttributeNameBox.SelectedValue = _context.MobAttributeSet.First(item => item.ID == attributeIDString).ID;
                    }
                    if (name != null)
                        attributeItem.NameBox.Text = name.ToString();
                    if (amount != null)
                        attributeItem.Amount.Value = int.Parse(amount.ToString());
                    if (operation != null)
                        attributeItem.Operations.SelectedIndex = int.Parse(operation.ToString());
                    if (slot != null)
                    {
                        string slotString = slot.ToString();
                        attributeItem.Slot.SelectedValue = _context.AttributeSlotSet.First(item => item.ID == slotString).Value;
                    }
                }
                ExternData.Remove("tag.Attributes");
            }
        }
    }
}