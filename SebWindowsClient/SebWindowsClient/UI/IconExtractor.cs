using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.IconLib;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SebWindowsClient.UI
{
    public static class Iconextractor
    {
        public static Bitmap ExtractHighResIconImage(string path, int? size = null)
        {
            var mi = new MultiIcon();
            mi.Load(path);
            var si = mi.FirstOrDefault();
            if (si != null)
            {
                IconImage icon;
                if (size != null)
                {
                    if (size.Value <= 32)
                    {
                        try
                        {
                            return Icon.ExtractAssociatedIcon(path).ToBitmap();
                        }
                        catch
                        {
                        }
                    }
                    icon = si.Where(x => x.Size.Height >= size.Value).OrderBy(x => x.Size.Height).FirstOrDefault();
                    if (icon != null)
                        return icon.Icon.ToBitmap();
                }
                var max = si.Max(_i => _i.Size.Height);
                icon = si.FirstOrDefault(i => i.Size.Height == max);
                if(icon != null)
                    return icon.Transparent;
            }
            return null;
        }
    }
}
