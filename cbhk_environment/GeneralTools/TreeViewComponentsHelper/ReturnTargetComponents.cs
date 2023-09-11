using cbhk_environment.CustomControls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 处理每一层控件由Tag转向控件的实际逻辑
    /// </summary>
    public class ReturnTargetComponents
    {
        /// <summary>
        /// 普通文本块的灰刷
        /// </summary>
        private static SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#838383"));
        /// <summary>
        /// 白色刷子
        /// </summary>
        private static SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        /// <summary>
        /// 黑色刷子
        /// </summary>
        private static SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));

        public static void SetHeader(ref DockPanel container,List<string> requestList, ObservableCollection<string> enumList = null)
        {
            #region 一般的数据控件
            IconTextButtons description = null;
            //枚举框标记
            bool AddedEnum = false;
            foreach (string item in requestList)
            {
                #region 取出固定组件数据
                string value = item;
                if (item.Contains('-'))
                    value = item[(item.IndexOf('-') + 1)..];
                #endregion

                #region key
                if (item.StartsWith("key"))
                {
                    TextBlock key = new()
                    {
                        Uid = "key",
                        Text = value,
                        Foreground = whiteBrush,
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    container.Children.Add(key);
                }
                #endregion

                #region 封装用于切换子结构的ComboBox
                if (enumList != null && enumList.Count > 1 && !AddedEnum)
                {
                    AddedEnum = true;
                    ComboBox subStructureSwitchBox = new()
                    {
                        Uid = "enum",
                        Style = Application.Current.Resources["TextComboBoxStyle"] as Style,
                        ItemsSource = enumList,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        SelectedIndex = 0,
                        FontSize = 15,
                        Width = 100
                    };
                    subStructureSwitchBox.SelectionChanged += SubStructureSwitchBox_SelectionChanged;
                    container.Children.Add(subStructureSwitchBox);
                }
                #endregion

                #region description
                if (item.StartsWith("description"))
                {
                    description = new()
                    {
                        Uid = "description",
                        Height = 18,
                        Width = 18,
                        Margin = new Thickness(5, 0, 0, 0),
                        Style = Application.Current.Resources["IconTextButton"] as Style,
                        Foreground = blackBrush,
                        Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/Issue.png", UriKind.RelativeOrAbsolute))),
                        ToolTip = value
                    };
                    ToolTipService.SetInitialShowDelay(description,0);
                    ToolTipService.SetBetweenShowDelay(description,0);
                }
                #endregion

                #region boolean
                if(item == "bool")
                {
                    TextToggleButtons trueButton = new()
                    {
                        IsChecked = false,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Style = Application.Current.Resources["TextToggleButtonsStyle"] as Style,
                        Foreground = blackBrush,
                        Padding = new Thickness(5),
                        Content = "True"
                    };
                    TextToggleButtons falseButton = new()
                    {
                        IsChecked = false,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Style = Application.Current.Resources["TextToggleButtonsStyle"] as Style,
                        Foreground = blackBrush,
                        Padding = new Thickness(5),
                        Content = "False"
                    };
                    container.Children.Add(trueButton);
                    container.Children.Add(falseButton);
                }

                //ComboBox booleanBox = new()
                //{
                //    Style = Application.Current.Resources["TextComboBoxStyle"] as Style,
                //    SelectedIndex = 0,
                //    FontSize = 12
                //};
                //booleanBox.Items.Add("True");
                //booleanBox.Items.Add("False");
                //container.Children.Add(booleanBox);
                #endregion

                #region number
                if(item.StartsWith("number"))
                {
                    double minValue = 0;
                    double maxValue = 0;
                    if (item.EndsWith("Short"))
                    {
                        minValue = short.MinValue;
                        maxValue = short.MaxValue;
                    }
                    else
                    if (item.EndsWith("Float"))
                    {
                        minValue = float.MinValue;
                        maxValue = float.MaxValue;
                    }
                    else
                    if (item.EndsWith("Double"))
                    {
                        minValue = double.MinValue;
                        maxValue = double.MaxValue;
                    }
                    else
                    if (item.EndsWith("Byte"))
                    {
                        minValue = byte.MinValue;
                        maxValue = byte.MaxValue;
                    }
                    else
                    {
                        minValue = int.MinValue;
                        maxValue = int.MaxValue;
                    }
                    Slider numberBox = new()
                    {
                        Height = 25,
                        Width = 100,
                        Value = 0,
                        Minimum = minValue,
                        Maximum = maxValue,
                        Style = Application.Current.Resources["NumberBoxStyle"] as Style,
                        FontSize = 12
                    };
                    container.Children.Add(numberBox);
                }
                #endregion

                #region string
                if(item == "String")
                {
                    TextBox textBox = new()
                    {
                        Width = 200,
                        Foreground = whiteBrush,
                        FontSize = 12
                    };
                    container.Children.Add(textBox);
                }
                #endregion

                #region compound
                if(item.StartsWith("Compound"))
                {
                    #region 判断应该添加什么插入型组件
                    switch (value)
                    {
                        case "EntityGenerator":
                            IconTextButtons generatorButton = new()
                            {
                                Uid = value,
                                Padding = new Thickness(5),
                                Style = Application.Current.Resources["IconTextButton"] as Style,
                                Foreground = blackBrush,
                                Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonNormal.png", UriKind.RelativeOrAbsolute))),
                                PressedBackground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonPressed.png", UriKind.RelativeOrAbsolute))),
                                FontSize = 12,
                                ClickMode = ClickMode.Release,
                                Content = "+"
                            };
                            generatorButton.Click += GeneratorButton_Click;
                            container.Children.Add(generatorButton);
                            break;
                    }
                    #endregion
                }
                #endregion

                #region list
                if(item.StartsWith("List"))
                {
                    IconTextButtons addButton = new()
                    {
                        Uid = value,
                        Padding = new Thickness(5),
                        Style = Application.Current.Resources["IconTextButton"] as Style,
                        Foreground = blackBrush,
                        Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonNormal.png", UriKind.RelativeOrAbsolute))),
                        PressedBackground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonPressed.png", UriKind.RelativeOrAbsolute))),
                        FontSize = 12,
                        ClickMode = ClickMode.Release,
                        Content = "+"
                    };
                    addButton.Click += AddButton_Click;
                    container.Children.Add(addButton);
                }
                #endregion
            }
            if (description != null)
            container.Children.Add(description);
            #endregion
        }

        /// <summary>
        /// 添加子结构
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void AddButton_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons currentButton = sender as IconTextButtons;
            RichTreeViewItems currentItem = currentButton.FindParent<RichTreeViewItems>();
            string currentJson = currentItem.Tag.ToString();
            JArray children = JArray.Parse(JObject.Parse(currentJson)["children"].ToString());

            #region 遍历子级,添加新的成员

            #region 作为列表成员的父级显示节点
            RichTreeViewItems subObj = new()
            {
                ConnectingLineFill = grayBrush,
                Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                TextState = new TreeViewRun(),
            };
            DockPanel dockPanel = new();
            IconTextButtons deleteButton = new()
            {
                Padding = new Thickness(5),
                Style = currentButton.Style,
                Foreground = blackBrush,
                Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonNormal.png", UriKind.RelativeOrAbsolute))),
                PressedBackground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonPressed.png", UriKind.RelativeOrAbsolute))),
                FontSize = 12,
                ClickMode = ClickMode.Release,
                Content = "-"
            };
            string headText = JObject.Parse(currentJson)["key"].ToString();
            if (headText.EndsWith('s'))
                headText = headText.TrimEnd('s');
            else
                headText = (headText.StartsWith('a') || headText.StartsWith('e') || headText.StartsWith('i') || headText.StartsWith('o') || headText.StartsWith('u') ? "an " : "a ") + headText;
            TextBlock objHead = new()
            {
                Text = headText,
                Foreground = whiteBrush,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            deleteButton.Click += DeleteListItemButton_Click;
            dockPanel.Children.Add(deleteButton);
            dockPanel.Children.Add(objHead);
            subObj.Header = dockPanel;
            #endregion

            foreach (JObject item in children.Cast<JObject>())
            {
                #region 新建子结构
                RichTreeViewItems subItem = new()
                {
                    ConnectingLineFill = grayBrush,
                    Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                    Tag = item.ToString(),
                    TextState = new TreeViewRun(),
                };

                JArray subChildren = JArray.Parse(JObject.Parse(item.ToString())["children"].ToString());
                if (subChildren.Count > 0)
                {
                    subItem.Items.Add(new TreeViewItem() { Uid = "null" });
                    subItem.Expanded += TreeViewConveter.CompoundItemExpanded;
                }

                #region 绑定器
                Binding componentsBinder = new()
                {
                    Path = new PropertyPath("Tag"),
                    Mode = BindingMode.OneTime,
                    RelativeSource = RelativeSource.Self,
                    Converter = new TagToHeader(),
                    ConverterParameter = subItem
                };
                #endregion

                //绑定组件转换器
                BindingOperations.SetBinding(subItem, HeaderedItemsControl.HeaderProperty, componentsBinder);
                subObj.Items.Add(subItem);
                #endregion
            }
            currentItem.Items.Add(subObj);
            #endregion
        }

        /// <summary>
        /// 删除列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DeleteListItemButton_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons deleteButton = sender as IconTextButtons;
            RichTreeViewItems richTreeViewItems = deleteButton.FindParent<RichTreeViewItems>();
            RichTreeViewItems listNode = richTreeViewItems.Parent as RichTreeViewItems;
            listNode.Items.Remove(richTreeViewItems);
        }

        /// <summary>
        /// 生成按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void GeneratorButton_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons textButtons = sender as IconTextButtons;
            RichTreeViewItems parent = textButtons.FindParent<RichTreeViewItems>();

            switch (textButtons.Uid)
            {
                case "EntityGenerator":
                    Generators.EntityGenerator.Entity entity = new()
                    {
                        Owner = Window.GetWindow(parent)
                    };
                    if (entity.ShowDialog().Value)
                    {
                        JArray tags = JArray.Parse(JObject.Parse(parent.Tag.ToString())["tag"].ToString());
                        string tagType = tags[0].ToString();

                        #region 构建实体节点的Header
                        DockPanel dockPanel = new()
                        {
                            LastChildFill = false
                        };
                        Image entityHeader = new()
                        {
                            Source = new BitmapImage(new Uri("", UriKind.RelativeOrAbsolute))
                        };
                        IconTextButtons deleteButton = new()
                        {
                            Padding = new Thickness(3),
                            Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonNormal.png", UriKind.RelativeOrAbsolute))),
                            PressedBackground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/ButtonPressed.png", UriKind.RelativeOrAbsolute))),
                            Style = Application.Current.Resources["IconTextButton"] as Style,
                            FontSize = 12,
                            Foreground = whiteBrush,
                            ClickMode = ClickMode.Release,
                            Content = "X",
                        };
                        deleteButton.Click += DeleteButton_Click;
                        dockPanel.Children.Add(deleteButton);
                        dockPanel.Children.Add(entityHeader);
                        #endregion

                        //添加子节点
                        RichTreeViewItems subItem = new()
                        {
                            ConnectingLineFill = grayBrush,
                            Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                            Tag = entity.Background.ToString(),
                            Header = dockPanel
                        };

                        if (tagType.Contains("List") || parent.Items.Count == 0)
                            parent.Items.Add(subItem);
                        else
                            if (tagType.Contains("Compound"))
                            parent.Items[0] = subItem;
                    }
                    break;
            }
        }

        /// <summary>
        /// 删除普通成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons currentButton = sender as IconTextButtons;
            RichTreeViewItems parent = currentButton.FindParent<RichTreeViewItems>();
            RichTreeViewItems grandparent = parent.Parent as RichTreeViewItems;
            grandparent.Items.Remove(parent);
        }

        /// <summary>
        /// 下拉框切换子结构的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SubStructureSwitchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox currentBox = sender as ComboBox;
            RichTreeViewItems currentItem = currentBox.FindParent<RichTreeViewItems>();
            JObject subObj = JObject.Parse(currentItem.Tag.ToString());
            string description = subObj["description"].ToString();

            #region 处理单个切换和多个切换
            MatchCollection case1 = Regex.Matches(description, @"(?<=可以是)[a-zA-Z_]+(?=。)");
            MatchCollection case2 = Regex.Matches(description, @"(?<=如果是)[a-zA-Z_]+(?=。)");
            JArray children = JArray.Parse(subObj["children"].ToString());
            ObservableCollection<RichTreeViewItems> subStructureCollection = new();
            string selectedItem = currentBox.SelectedItem.ToString();

            //表示需要子结构
            if(selectedItem.EndsWith("List") || selectedItem.EndsWith("Compound") || selectedItem.EndsWith("Array"))
            {
                #region 隐藏不需要的节点成员控件
                DockPanel dockPanel = currentItem.Header as DockPanel;
                foreach (FrameworkElement item in dockPanel.Children)
                {
                    if (item.Uid.Length == 0)
                        item.Visibility = Visibility.Collapsed;
                }
                #endregion

                if ((case1.Count > 1 || case2.Count > 1) && children.Count > 1)
                {
                    JToken subStructure = JToken.Parse(children[currentBox.SelectedIndex].ToString());
                    RichTreeViewItems subItem = new()
                    {
                        Tag = subStructure.ToString(),
                        ConnectingLineFill = grayBrush,
                        Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                    };

                    #region 设置绑定器
                    Binding componentsBinder = new()
                    {
                        Path = new PropertyPath("Tag"),
                        Mode = BindingMode.OneTime,
                        RelativeSource = RelativeSource.Self,
                        Converter = new TagToHeader(),
                        ConverterParameter = subItem
                    };
                    BindingOperations.SetBinding(subItem, HeaderedItemsControl.HeaderProperty, componentsBinder);
                    #endregion

                    subStructureCollection.Add(subItem);
                }
                else
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        RichTreeViewItems subItem = new()
                        {
                            Tag = children[i].ToString(),
                            ConnectingLineFill = grayBrush,
                            Style = Application.Current.Resources["RichTreeViewItems"] as Style,
                        };

                        #region 设置绑定器
                        Binding componentsBinder = new()
                        {
                            Path = new PropertyPath("Tag"),
                            Mode = BindingMode.OneTime,
                            RelativeSource = RelativeSource.Self,
                            Converter = new TagToHeader(),
                            ConverterParameter = subItem
                        };
                        BindingOperations.SetBinding(subItem, HeaderedItemsControl.HeaderProperty, componentsBinder);
                        #endregion

                        subStructureCollection.Add(subItem);
                    }
                }

                #region 更新子结构数据
                currentItem.SubStructure ??= new Dictionary<string, ObservableCollection<RichTreeViewItems>>();
                if (!currentItem.SubStructure.ContainsKey(selectedItem))
                    currentItem.SubStructure.Add(selectedItem, subStructureCollection);
                else
                    if(currentItem.SubStructure[selectedItem].Count == 0)
                    currentItem.SubStructure[selectedItem] = subStructureCollection;
                currentItem.ItemsSource ??= currentItem.SubStructure[selectedItem];
                #endregion
            }
            else//不需要子结构
            {
                #region 显示需要的节点成员控件
                string NeedDisplyType = selectedItem;
                DockPanel dockPanel = currentItem.Header as DockPanel;
                foreach (FrameworkElement item in dockPanel.Children)
                {
                    if (item.Uid.Length == 0)
                    {
                        if(((NeedDisplyType == "Int" || NeedDisplyType == "Float" || NeedDisplyType == "Double" || NeedDisplyType == "Long" || NeedDisplyType == "Short" || NeedDisplyType == "Byte") && item is Slider) || (NeedDisplyType == "String" && item is TextBox) || (NeedDisplyType == "Boolean" && item is TextToggleButtons))
                        {
                            item.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                #endregion
                currentItem.ItemsSource = null;
                currentItem.Items.Clear();
            }

            #endregion
        }
    }
}
