using CBHK.CustomControls.Interfaces;
using CBHK.GeneralTools;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Generators.ItemGenerator.Components
{
    /// <summary>
    /// Data.xaml 的交互逻辑
    /// </summary>
    public partial class Data : UserControl
    {
        //属性数据源
        public ObservableCollection<AttributeItems> AttributeSource = [];
        public DataTable AttributeTable = null;
        public DataTable AttributeSlotTable = null;

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

        public Data()
        {
            InitializeComponent();
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
            AttributeItems attributeItems = new()
            {
                Margin = new(0, 2, 0, 0)
            };
            AttributeSource.Add(attributeItems);
            ItemPageViewModel itemPageDataContext = attributeItems.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(attributeItems);
        }

        /// <summary>
        /// 清空属性
        /// </summary>
        /// <param name="obj"></param>
        private void ClearAttributesComand(FrameworkElement obj)
        {
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
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
                        attributeItem.AttributeNameBox.SelectedValue = AttributeTable.Select("id='" + attributeId.ToString() + "'").First()["id"].ToString();
                    if (name != null)
                        attributeItem.NameBox.Text = name.ToString();
                    if (amount != null)
                        attributeItem.Amount.Value = int.Parse(amount.ToString());
                    if (operation != null)
                        attributeItem.Operations.SelectedIndex = int.Parse(operation.ToString());
                    if (slot != null)
                        attributeItem.Slot.SelectedValue = AttributeSlotTable.Select("id='" + slot.ToString() + "'").First()["name"].ToString();
                }
                ExternData.Remove("tag.Attributes");
            }
        }
    }
}