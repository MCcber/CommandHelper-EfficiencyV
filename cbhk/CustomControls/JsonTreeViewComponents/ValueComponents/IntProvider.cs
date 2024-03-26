using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace cbhk.CustomControls.JsonTreeViewComponents.ValueComponents
{
    public class IntProvider : JsonTreeViewItem
    {
        #region Property
        /// <summary>
        /// 是否为值提供器
        /// </summary>
        public bool IsValueProvider { get; } = true;
        public int TypeKeyStartOffset { get; set; }
        public int TypeKeyEndOffset { get; set; }
        public int TypeValueStartOffset { get; set; }
        public int TypeValueEndOffset { get; set; }
        #endregion

        #region Fields
        public const IntProviderStructures ParentItem = IntProviderStructures.Constant;
        public static Dictionary<IntProviderStructures, ObservableCollection<JsonTreeViewItem>> StructureChildren = [];
        public IntProviderStructures CurrentStructure = IntProviderStructures.Constant;
        #endregion

        public IntProvider(JsonTreeViewContext currentContext,int lineNumber, int lineStartPosition) : base(lineNumber, lineStartPosition)
        {
            LineNumber = lineNumber;
            LineStartPosition = lineStartPosition;

            if (StructureChildren.Count > 0)
            {
                return;
            }

            #region Constant
            JsonTreeViewItem constantItem = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0
            };
            StructureChildren.Add(IntProviderStructures.Constant, [constantItem]);
            #endregion

            #region ConstantPlus
            ObservableCollection<JsonTreeViewItem> constantPlusList = [];

            JsonTreeViewItem constantPlusValue = new(lineNumber, lineStartPosition)
            {
                DisplayText = "value",
                IsNumberType = true,
                Value = 0,
                Key = "value"
            };

            constantPlusList.Add(constantPlusValue);

            StructureChildren.Add(IntProviderStructures.ConstantPlus, constantPlusList);
            #endregion

            #region Uniform
            ObservableCollection<JsonTreeViewItem> uniformList = [];

            JsonTreeViewItem uniformMinInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "min_inclusive",
                DisplayText = "min_inclusive"
            };
            JsonTreeViewItem uniformMaxInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "max_inclusive",
                DisplayText = "max_inclusive"
            };

            uniformList.Add(uniformMinInclusive);
            uniformList.Add(uniformMaxInclusive);

            StructureChildren.Add(IntProviderStructures.Uniform, uniformList);
            #endregion

            #region BiasedToBottom
            ObservableCollection<JsonTreeViewItem> biasedToBottomList = [];
            JsonTreeViewItem biasedToBottomMinInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "min_inclusive",
                DisplayText = "min_inclusive"
            };
            JsonTreeViewItem biasedToBottomMaxInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "max_inclusive",
                DisplayText = "max_inclusive"
            };
            biasedToBottomList.Add(biasedToBottomMinInclusive);
            biasedToBottomList.Add(biasedToBottomMaxInclusive);

            StructureChildren.Add(IntProviderStructures.BiasedToBottom, biasedToBottomList);
            #endregion

            #region Clamped
            ObservableCollection<JsonTreeViewItem> clampedList = [];
            JsonTreeViewItem clampedMinInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "min_inclusive",
                DisplayText = "min_inclusive"
            };
            JsonTreeViewItem clampedMaxInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "max_inclusive",
                DisplayText = "max_inclusive"
            };
            JsonTreeViewItem source = new(lineNumber, lineStartPosition)
            {
                IsEnumType = true,
                Key = "source",
                DisplayText = "Source"
            };
            foreach (var item in Enum.GetValues<IntProviderStructures>())
            {
                source.EnumItemsSource.Add(new TextComboBoxItem() { Text = item.ToString() });
            }
            source.SelectedEnumItem = source.EnumItemsSource[0];
            clampedList.Add(clampedMinInclusive);
            clampedList.Add(clampedMaxInclusive);
            clampedList.Add(source);
            StructureChildren.Add(IntProviderStructures.Clamped,clampedList);
            #endregion

            #region ClampedNormal
            ObservableCollection<JsonTreeViewItem> clampedNormalList = [];
            JsonTreeViewItem clampedNormalMinInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "min_inclusive",
                DisplayText = "min_inclusive"
            };
            JsonTreeViewItem clampedNormalMaxInclusive = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "max_inclusive",
                DisplayText = "max_inclusive"
            };
            JsonTreeViewItem clampedNormalMean = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "mean",
                DisplayText = "Mean"
            };
            JsonTreeViewItem clampedNormalDeviation = new(lineNumber, lineStartPosition)
            {
                IsNumberType = true,
                Value = 0,
                Key = "deviation",
                DisplayText = "deviation"
            };
            clampedNormalList.Add(clampedNormalMinInclusive);
            clampedNormalList.Add(clampedNormalMaxInclusive);
            clampedNormalList.Add(clampedNormalMean);
            clampedNormalList.Add(clampedNormalDeviation);
            StructureChildren.Add(IntProviderStructures.ClampedNormal, clampedNormalList);
            #endregion

            #region WeightedList
            ObservableCollection<JsonTreeViewItem> weightedListList = [];

            JsonTreeViewItem distribution = new(lineNumber, lineStartPosition)
            {
                DisplayText = "distribution",
                IsArray = true,
                Value = "[]",
                Key = "distribution"
            };

            weightedListList.Add(distribution);

            StructureChildren.Add(IntProviderStructures.WeightedList, weightedListList);
            #endregion
        }

        /// <summary>
        /// 递归更新结构数据
        /// </summary>
        /// <param name="list"></param>
        /// <param name="plan"></param>
        private void RecursiveUpdateMemberData(ObservableCollection<JsonTreeViewItem> list,ICustomWorldUnifiedPlan plan)
        {
            foreach (var item in list)
            {
                item.UpdateSelfOffset(plan);
                if (item.Children is not null && item.Children.Count > 0)
                    RecursiveUpdateMemberData(item.Children,plan);
            }
        }

        public new void ComboBox_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Window window = Window.GetWindow(comboBox);
            CurrentStructure = SelectedEnumItem.Text switch
            {
                "ConstantPlus" => IntProviderStructures.ConstantPlus,
                "Uniform" => IntProviderStructures.Uniform,
                "BiasedToBottom" => IntProviderStructures.BiasedToBottom,
                "Clamped" => IntProviderStructures.Clamped,
                "ClampedNormal" => IntProviderStructures.ClampedNormal,
                "WeightedList" => IntProviderStructures.WeightedList,
                _ => IntProviderStructures.Constant
            };
            if (window.DataContext is ICustomWorldUnifiedPlan customWorldUnifiedPlan)
            {
                ValueProviderHelper.UpdateIntProviderType(customWorldUnifiedPlan, this, this);
                ObservableCollection<JsonTreeViewItem> currentItemList = StructureChildren[CurrentStructure];
                RecursiveUpdateMemberData(currentItemList, customWorldUnifiedPlan);
            }
        }
    }
}