using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace cbhk.Generators.CustomWorldGenerators
{
    public class CustomWorldBaseWindowDataContext:ObservableObject
    {
        #region Field
        public Window home = null;
        public string InitJson = "";
        public enum CustomWorldGeneratorTypes
        {
            DimentionType
        }
        #endregion

        #region Property
        private string jsonData;

		public string JsonData
        {
            get => jsonData;

            set => SetProperty(ref jsonData, value);
		}
        #endregion

        public CustomWorldBaseWindowDataContext()
        {
        }
    }
}
