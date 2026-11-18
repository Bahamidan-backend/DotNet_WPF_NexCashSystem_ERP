using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace IconGen;

internal static class Program
{
    private static byte[] RenderPngBytes(Image src, int size)
    {
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);
        g.DrawImage(src, 0, 0, size, size);

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static void WriteIco(string pngPath, string icoPath)
    {
        if (!File.Exists(pngPath))
            throw new FileNotFoundException("PNG not found", pngPath);

        Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);

        var sizes = new[] { 16, 32, 48, 256 };
        using var src = Image.FromFile(pngPath);
        var images = sizes.Select(s => (size: s, bytes: RenderPngBytes(src, s))).ToList();

        using var fs = File.Open(icoPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // ICONDIR
        bw.Write((ushort)0);    // reserved
        bw.Write((ushort)1);    // type = icon
        bw.Write((ushort)images.Count);

        var dirSize = 6 + 16 * images.Count;
        var offset = dirSize;

        foreach (var (size, bytes) in images)
        {
            bw.Write((byte)(size == 256 ? 0 : size)); // width
            bw.Write((byte)(size == 256 ? 0 : size)); // height
            bw.Write((byte)0); // color count
            bw.Write((byte)0); // reserved
            bw.Write((ushort)1); // planes
            bw.Write((ushort)32); // bit count
            bw.Write((uint)bytes.Length); // bytes in resource
            bw.Write((uint)offset); // image offset
            offset += bytes.Length;
        }

        foreach (var (_, bytes) in images)
            bw.Write(bytes);
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: IconGen <input.png> <output.ico>");
                return 2;
            }

            WriteIco(args[0], args[1]);
            Console.WriteLine($"Wrote icon: {args[1]}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}

