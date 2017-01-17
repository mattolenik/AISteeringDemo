using UnityEngine;
using Random = System.Random;

/// <summary>
/// Converted from Java answer located here:
/// http://stackoverflow.com/questions/470690/how-to-automatically-generate-n-distinct-colors
/// </summary>
public static class ColorUtils
{
    const float UOffset = .436f;
    const float VOffset = .615f;

    const int Seed = 0;
    static Random rand = new Random(Seed);

    /*
     * Returns an array of ncolors RGB triplets such that each is as unique from the rest as possible
     * and each color has at least one component greater than minComponent and one less than maxComponent.
     * Use min == 1 and max == 0 to include the full RGB color range.
     * 
     * Warning: O N^2 algorithm blows up fast for more than 100 colors.
     */
    public static Color[] GenerateDistinctColors(int ncolors, float minComponent, float maxComponent)
    {
        rand = new Random(Seed);

        var yuv = new float[ncolors][];

        // initialize array with random colors
        for (var i = 0; i < ncolors;)
        {
            yuv[i++] = RandYuVinRgbRange(minComponent, maxComponent);
        }

        // continually break up the worst-fit color pair until we get tired of searching
        for (var c = 0; c < ncolors * 1000; c++)
        {
            float worst = 8888;
            var worstId = 0;
            for (var i = 1; i < yuv.Length; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    var dist = Sqrdist(yuv[i], yuv[j]);
                    if (dist < worst)
                    {
                        worst = dist;
                        worstId = i;
                    }
                }
            }
            var best = RandYuvBetterThan(worst, minComponent, maxComponent, yuv);
            if (best == null)
            {
                break;
            }

            yuv[worstId] = best;
        }

        var rgbs = new Color[yuv.Length];
        for (var i = 0; i < yuv.Length; i++)
        {
            var rgb = Yuv2Rgb(yuv[i][0], yuv[i][1], yuv[i][2]);
            rgbs[i] = new Color(rgb[0], rgb[1], rgb[2]);
        }

        return rgbs;
    }


    // From http://en.wikipedia.org/wiki/YUV#Mathematical_derivations_and_formulas
    static float[] Yuv2Rgb(float y, float u, float v)
    {
        return new[]
        {
            1 * y + 0 * u + 1.13983f * v,
            1 * y + -.39465f * u + -.58060f * v,
            1 * y + 2.03211f * u + 0 * v
        };
    }

    static float[] RandYuVinRgbRange(float minComponent, float maxComponent)
    {
        while (true)
        {
            var y = (float)rand.NextDouble();
            var u = (float)(rand.NextDouble() * 2 * UOffset - UOffset);
            var v = (float)(rand.NextDouble() * 2 * VOffset - VOffset);
            var rgb = Yuv2Rgb(y, u, v);
            float r = rgb[0], g = rgb[1], b = rgb[2];
            if (0 <= r && r <= 1 &&
                0 <= g && g <= 1 &&
                0 <= b && b <= 1 &&
                (r > minComponent || g > minComponent || b > minComponent) && // don't want all dark components
                (r < maxComponent || g < maxComponent || b < maxComponent)) // don't want all light components
            {
                return new[] { y, u, v };
            }
        }
    }

    static float Sqrdist(float[] a, float[] b)
    {
        float sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }
        return sum;
    }

    static float[] RandYuvBetterThan(float bestDistSqrd, float minComponent, float maxComponent, float[][] input)
    {
        for (var attempt = 1; attempt < 100 * input.Length; attempt++) {
            var candidate = RandYuVinRgbRange(minComponent, maxComponent);
            var good = true;
            foreach (var t in input)
            {
                if (Sqrdist(candidate, t) < bestDistSqrd)
                    good = false;
            }

            if (good)
            {
                return candidate;
            }
        }
        return null; // after a bunch of passes, couldn't find a candidate that beat the best.
    }
}