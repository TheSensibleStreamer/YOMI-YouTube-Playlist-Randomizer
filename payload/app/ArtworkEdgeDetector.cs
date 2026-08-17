using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public static class ArtworkEdgeDetector
{
    const double ColorTolerance = 38.0;
    const double RequiredMatch = 0.90;
    const int MinimumCrop = 8;
    const double MaximumEdgeFraction = 0.30;
    const double MinimumRemainingFraction = 0.40;
    const int SamplesAcross = 72;
    const double TransitionMatchLimit = 0.84;

    sealed class Rgb
    {
        public int R;
        public int G;
        public int B;
    }

    static int[] SamplePositions(int length)
    {
        if (length <= 1) return new int[] { 0 };

        int count = Math.Min(SamplesAcross, length);
        List<int> values = new List<int>();

        // Use the central 60% of the perpendicular axis. This deliberately
        // avoids corners, where a top/bottom border may intersect a different
        // left/right border color.
        double start = (length - 1) * 0.20;
        double end = (length - 1) * 0.80;

        for (int i = 0; i < count; i++)
        {
            double t = count <= 1 ? 0.5 : (double)i / (double)(count - 1);
            int p = (int)Math.Round(start + ((end - start) * t));
            if (p < 0) p = 0;
            if (p >= length) p = length - 1;

            if (values.Count == 0 || values[values.Count - 1] != p)
                values.Add(p);
        }

        return values.ToArray();
    }

    static int Median(List<int> source)
    {
        if (source == null || source.Count == 0) return 0;
        int[] a = source.ToArray();
        Array.Sort(a);

        int n = a.Length;
        if ((n & 1) == 1) return a[n / 2];
        return (int)Math.Round((a[(n / 2) - 1] + a[n / 2]) / 2.0);
    }

    static Rgb Reference(Bitmap bmp, string side, int[] xs, int[] ys)
    {
        List<int> rs = new List<int>();
        List<int> gs = new List<int>();
        List<int> bs = new List<int>();

        int w = bmp.Width;
        int h = bmp.Height;

        if (side == "Top" || side == "Bottom")
        {
            for (int d = 0; d < 3; d++)
            {
                int y = side == "Top" ? d : (h - 1 - d);
                if (y < 0) y = 0;
                if (y >= h) y = h - 1;

                for (int i = 0; i < xs.Length; i++)
                {
                    Color c = bmp.GetPixel(xs[i], y);
                    rs.Add(c.R); gs.Add(c.G); bs.Add(c.B);
                }
            }
        }
        else
        {
            for (int d = 0; d < 3; d++)
            {
                int x = side == "Left" ? d : (w - 1 - d);
                if (x < 0) x = 0;
                if (x >= w) x = w - 1;

                for (int i = 0; i < ys.Length; i++)
                {
                    Color c = bmp.GetPixel(x, ys[i]);
                    rs.Add(c.R); gs.Add(c.G); bs.Add(c.B);
                }
            }
        }

        return new Rgb { R = Median(rs), G = Median(gs), B = Median(bs) };
    }

    static bool Matches(Color c, Rgb r)
    {
        int dr = c.R - r.R;
        int dg = c.G - r.G;
        int db = c.B - r.B;
        double distanceSquared = (double)(dr * dr) + (double)(dg * dg) + (double)(db * db);
        return distanceSquared <= (ColorTolerance * ColorTolerance);
    }

    static double LineMatch(Bitmap bmp, bool row, int index, Rgb reference, int[] xs, int[] ys)
    {
        int matches = 0;
        int total = 0;

        if (row)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                if (Matches(bmp.GetPixel(xs[i], index), reference)) matches++;
                total++;
            }
        }
        else
        {
            for (int i = 0; i < ys.Length; i++)
            {
                if (Matches(bmp.GetPixel(index, ys[i]), reference)) matches++;
                total++;
            }
        }

        return total == 0 ? 0.0 : (double)matches / (double)total;
    }

    static int EdgeBand(Bitmap bmp, string side, int[] xs, int[] ys)
    {
        Rgb reference = Reference(bmp, side, xs, ys);
        bool horizontal = side == "Top" || side == "Bottom";
        int length = horizontal ? bmp.Height : bmp.Width;
        int maxBand = (int)Math.Floor(length * MaximumEdgeFraction);

        if (maxBand < MinimumCrop) return 0;

        int band = 0;

        for (int offset = 0; offset < maxBand; offset++)
        {
            bool row = horizontal;
            int idx;

            if (side == "Top") idx = offset;
            else if (side == "Bottom") idx = bmp.Height - 1 - offset;
            else if (side == "Left") idx = offset;
            else idx = bmp.Width - 1 - offset;

            double ratio = LineMatch(bmp, row, idx, reference, xs, ys);
            if (ratio >= RequiredMatch) band++;
            else break;
        }

        if (band < MinimumCrop) return 0;

        // Require an actual content transition immediately inside the proposed
        // border. This keeps large naturally-flat image regions conservative.
        double transition = 0.0;
        int transitionCount = 0;

        for (int k = 0; k < 3; k++)
        {
            int offset = band + k;
            if (offset >= length) break;

            bool row = horizontal;
            int idx;

            if (side == "Top") idx = offset;
            else if (side == "Bottom") idx = bmp.Height - 1 - offset;
            else if (side == "Left") idx = offset;
            else idx = bmp.Width - 1 - offset;

            transition += LineMatch(bmp, row, idx, reference, xs, ys);
            transitionCount++;
        }

        if (transitionCount > 0 &&
            (transition / transitionCount) >= TransitionMatchLimit)
            return 0;

        return band;
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("NONE detector-error input-file-missing");
                return 0;
            }

            using (Image image = Image.FromFile(args[0]))
            using (Bitmap bmp = new Bitmap(image))
            {
                int ow = bmp.Width;
                int oh = bmp.Height;

                if (ow < 16 || oh < 16)
                {
                    Console.WriteLine("NONE detector-error image-too-small");
                    return 0;
                }

                int[] xs = SamplePositions(ow);
                int[] ys = SamplePositions(oh);

                int left = EdgeBand(bmp, "Left", xs, ys);
                int right = EdgeBand(bmp, "Right", xs, ys);
                int top = EdgeBand(bmp, "Top", xs, ys);
                int bottom = EdgeBand(bmp, "Bottom", xs, ys);

                int newW = ow - left - right;
                int newH = oh - top - bottom;

                int minimumW = (int)Math.Floor(ow * MinimumRemainingFraction);
                int minimumH = (int)Math.Floor(oh * MinimumRemainingFraction);

                if (newW < minimumW)
                {
                    left = 0; right = 0; newW = ow;
                }

                if (newH < minimumH)
                {
                    top = 0; bottom = 0; newH = oh;
                }

                bool meaningful =
                    left >= MinimumCrop ||
                    right >= MinimumCrop ||
                    top >= MinimumCrop ||
                    bottom >= MinimumCrop;

                if (!meaningful)
                {
                    Console.WriteLine(
                        "NONE no-confident-edge-band size={0}x{1} edges=L{2},T{3},R{4},B{5}",
                        ow, oh, left, top, right, bottom
                    );
                    return 0;
                }

                Console.WriteLine(
                    "CROP {0}:{1}:{2}:{3} L{4} T{5} R{6} B{7}",
                    newW, newH, left, top, left, top, right, bottom
                );
                return 0;
            }
        }
        catch (Exception ex)
        {
            string message = (ex.Message ?? "unknown").Replace("\r", " ").Replace("\n", " ");
            Console.WriteLine("NONE detector-error " + message);
            return 0;
        }
    }
}
