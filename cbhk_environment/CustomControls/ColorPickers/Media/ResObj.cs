using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace cbhk_environment.CustomControls.ColorPickers.Media
{
    public class ResObj
    {
        static Stream Get(Assembly assembly, string path)
        {
            return assembly.GetManifestResourceStream(assembly.GetName().Name + "." + path);
        }

        public static string GetString(Assembly assembly, string path)
        {
            try
            {
                return StreamObj.ToString(Get(assembly, path));
            }
            catch
            {
                return null;
            }
        }

        public static ImageSource GetImageSource(Assembly assembly, string path)
        {
            try
            {
                return StreamObj.ToImageSource(Get(assembly, path));
            }
            catch
            {
                return null;
            }
        }
    }
}