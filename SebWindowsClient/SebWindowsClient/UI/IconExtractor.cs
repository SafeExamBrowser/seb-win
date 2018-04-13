using System;
using System.Drawing;
using System.Linq;

namespace SebWindowsClient.UI
{
    public static class Iconextractor
    {
        public static Bitmap ExtractHighResIconImage(string path, int? size = null)
        {
            try
                {
                if (size <= 32)
                        {
                    var tempIcon = Icon.ExtractAssociatedIcon(path);
                    if (tempIcon != null)
                        return tempIcon.ToBitmap();
                }
                var iconExtractor = new IconExtractor.IconExtractor(path);
                var extractedIcon = iconExtractor.GetIcon(0);
                var splitIcons = IconExtractor.IconUtil.Split(extractedIcon);
                var searchSize = size ?? splitIcons.Max(i => i.Height);
                var icon = splitIcons.Where(x => x.Height >= searchSize).OrderBy(x => x.Size.Height).FirstOrDefault();
                return icon?.ToBitmap();
                        }
            catch (Exception)
                        {
            return null;
        }
    }
}
}