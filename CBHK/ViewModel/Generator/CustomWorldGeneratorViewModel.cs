using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dimension（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class CustomWorldGeneratorViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "dimension")
    {
        #region Field
        private string generatorFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\CustomWorldGenerator";
        #endregion

        #region Property
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> generatorList = [];
        [ObservableProperty]
        private VectorTextComboBoxItem currentGenerator = new();
        #endregion

        public void CustomWorldGenerator_Loaded(object sender,RoutedEventArgs e)
        {
            if (Directory.Exists(generatorFolderPath))
            {
                string[] directoryArray = Directory.GetDirectories(generatorFolderPath);
                for (int i = 0; i < directoryArray.Length; i++)
                {
                    var generatorName = Path.GetFileName(directoryArray[i]);
                    GeneratorList.Add(new VectorTextComboBoxItem() { Text = generatorName, ItemID = directoryArray[i] });
                }
            }
        }
    }
}
