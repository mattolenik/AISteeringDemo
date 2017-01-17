using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

static class Extensions
{
    public static bool AlmostEquals(this float a, float b)
    {
        return Math.Abs(a - b) < 0.01;
    }

    public static float NextFloat(this Random random, float min, float max)
    {
        return random.NextFloat() * (max - min) + min;
    }

    public static float NextFloat(this Random random)
    {
        return (float)random.NextDouble();
    }
}