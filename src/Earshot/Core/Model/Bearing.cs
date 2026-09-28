using System;

namespace Earshot.Core.Model
{
    /// <summary>
    /// Direction of a sound relative to where the camera faces, on the horizontal plane. Unity's x is
    /// east/right and z is north/forward, so atan2(x, z) is a compass heading; the difference of two
    /// headings is clockwise-positive, the same sign as Vector3.SignedAngle(forward, to, up) that
    /// vanilla's CaptionArrow uses.
    /// </summary>
    public static class Bearing
    {
        public static float Degrees(float forwardX, float forwardZ, float listenerX, float listenerZ, float soundX, float soundZ)
        {
            double facing = Math.Atan2(forwardX, forwardZ);
            double toSound = Math.Atan2(soundX - listenerX, soundZ - listenerZ);
            double d = (toSound - facing) * 180.0 / Math.PI;
            while (d > 180.0)
                d -= 360.0;
            while (d <= -180.0)
                d += 360.0;
            return (float)d;
        }

        /// <summary>0 = ahead, 1 = ahead-right, 2 = right, 3 = behind-right, 4 = behind, 5 = behind-left, 6 = left, 7 = ahead-left.</summary>
        public static int Sector8(float degrees)
        {
            double d = degrees % 360.0;
            if (d < 0.0)
                d += 360.0;
            return (int)Math.Floor((d + 22.5) / 45.0) % 8;
        }

        /// <summary>The angle to draw the arrow at: snapped to 45° steps, or passed through for smooth rotation.</summary>
        public static float ArrowDegrees(float degrees, bool snap)
        {
            return snap ? Sector8(degrees) * 45f : degrees;
        }
    }
}
