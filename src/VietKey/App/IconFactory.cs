using System.Drawing.Drawing2D;
using System.Drawing.Text;
using AdsunVietKey.Native;

namespace AdsunVietKey.App;

/// <summary>Vẽ icon khay hệ thống: "V" nền đỏ (tiếng Việt) hoặc "E" nền xanh (tiếng Anh).</summary>
internal static class IconFactory
{
    public static Icon Create(bool vietnamese)
    {
        Size size = SystemInformation.SmallIconSize;
        using var bmp = new Bitmap(size.Width, size.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            Color back = vietnamese ? Color.FromArgb(218, 37, 29) : Color.FromArgb(30, 90, 200);
            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, 0, 0, size.Width, size.Height);

            using var font = new Font("Segoe UI", size.Height * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(vietnamese ? "V" : "E", font, Brushes.White, new RectangleF(0, 0, size.Width, size.Height + 1), format);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone(); // bản sao tự quản lý handle
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }
}
