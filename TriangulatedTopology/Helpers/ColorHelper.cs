﻿using OpenTK.Mathematics;
using System.Drawing;

namespace TriangulatedTopology.Helpers
{
    public static class ColorHelper
    {
        public static Vector3 RgbaToVector3(this Color color)
        {
            return new Vector3((float)color.R / 255, (float)color.G / 255, (float)color.B / 255);
        }

        public static Color Vector3ToRgba(this Vector3 color)
        {
            return Color.FromArgb(255, (byte)(255 * color.X), (byte)(255 * color.Y), (byte)(255 * color.Z));
        }
    }
}
