using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Scripter.Models;

namespace Scripter.Services;

public static class ImageMatcher
{
    public static bool Matches(ActionCondition condition)
    {
        var kind = condition.Kind;
        if (kind == ConditionKind.Always)
        {
            return true;
        }

        var found = ImageFound(condition);
        return kind == ConditionKind.ImageFound ? found : !found;
    }

    public static bool ImageFound(ActionCondition condition)
    {
        var imagePath = ScriptStorage.ResolveImagePath(condition.ImagePath);
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            throw new InvalidOperationException("Image condition is missing a reference screenshot.");
        }

        if (!condition.HasRegion)
        {
            throw new InvalidOperationException("Image condition is missing a search region.");
        }

        using var template = new Bitmap(imagePath);
        var region = new Rectangle(condition.RegionX, condition.RegionY, condition.RegionW, condition.RegionH);
        if (template.Width > region.Width || template.Height > region.Height)
        {
            throw new InvalidOperationException("Reference image is larger than the search region. Snip a smaller image or enlarge the region.");
        }

        using var haystack = CaptureRegion(region);
        var score = BestMatchPercent(haystack, template);
        var threshold = Math.Clamp(condition.Threshold <= 0 ? 70 : condition.Threshold, 1, 100);
        return score >= threshold;
    }

    public static Bitmap CaptureRegion(Rectangle region)
    {
        var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(region.Location, Point.Empty, region.Size);
        return bmp;
    }

    public static Bitmap CaptureVirtualScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bmp;
    }

    private static double BestMatchPercent(Bitmap haystack, Bitmap needle)
    {
        var hay = ToGrayscale(haystack);
        var ned = ToGrayscale(needle);
        try
        {
            var maxDx = hay.Width - ned.Width;
            var maxDy = hay.Height - ned.Height;
            var best = double.NegativeInfinity;

            for (var y = 0; y <= maxDy; y++)
            {
                for (var x = 0; x <= maxDx; x++)
                {
                    var score = NormalizedCorrelation(hay, ned, x, y);
                    if (score > best)
                    {
                        best = score;
                    }
                }
            }

            if (double.IsNegativeInfinity(best))
            {
                return 0;
            }

            return Math.Clamp(best * 100.0, 0, 100);
        }
        finally
        {
            hay.Dispose();
            ned.Dispose();
        }
    }

    private static double NormalizedCorrelation(GrayImage hay, GrayImage ned, int ox, int oy)
    {
        var n = ned.Width * ned.Height;
        double sumH = 0, sumN = 0, sumHH = 0, sumNN = 0, sumHN = 0;

        for (var y = 0; y < ned.Height; y++)
        {
            var hayRow = (oy + y) * hay.Width + ox;
            var nedRow = y * ned.Width;
            for (var x = 0; x < ned.Width; x++)
            {
                var h = hay.Pixels[hayRow + x];
                var t = ned.Pixels[nedRow + x];
                sumH += h;
                sumN += t;
                sumHH += h * h;
                sumNN += t * t;
                sumHN += h * t;
            }
        }

        var num = n * sumHN - sumH * sumN;
        var den = Math.Sqrt((n * sumHH - sumH * sumH) * (n * sumNN - sumN * sumN));
        if (den < 0.0001)
        {
            return 1;
        }

        return num / den;
    }

    private static GrayImage ToGrayscale(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        using var clone = bmp.Clone(rect, PixelFormat.Format24bppRgb);
        var data = clone.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var pixels = new byte[bmp.Width * bmp.Height];
            var stride = data.Stride;
            var buffer = new byte[stride * bmp.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            for (var y = 0; y < bmp.Height; y++)
            {
                var row = y * stride;
                for (var x = 0; x < bmp.Width; x++)
                {
                    var i = row + x * 3;
                    pixels[y * bmp.Width + x] = (byte)((buffer[i] + buffer[i + 1] + buffer[i + 2]) / 3);
                }
            }

            return new GrayImage(bmp.Width, bmp.Height, pixels);
        }
        finally
        {
            clone.UnlockBits(data);
        }
    }

    private sealed class GrayImage(int width, int height, byte[] pixels) : IDisposable
    {
        public int Width { get; } = width;
        public int Height { get; } = height;
        public byte[] Pixels { get; } = pixels;

        public void Dispose()
        {
        }
    }
}
