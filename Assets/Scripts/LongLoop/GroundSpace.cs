using System;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Reads the currently bound street configuration for foot-space calculations.</summary>
    public static class GroundSpace
    {
        public static float GroundDistance(Vector3 footA, Vector3 footB)
        {
            return GroundSpaceMath.GroundDistance(footA, footB, Config().Depth);
        }

        public static Vector3 FollowPoint(Vector3 foot)
        {
            var focus = Config().Focus;
            if (!Finite(foot.x) || !Finite(foot.y) || !Finite(foot.z) || !Finite(focus))
                throw new ArgumentOutOfRangeException(nameof(foot), "Foot position and FOCUS must be finite.");
            double y = (double)foot.y + focus;
            if (y < -float.MaxValue || y > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(foot), "Follow point exceeds the finite float range.");
            return new Vector3(foot.x, (float)y, foot.z);
        }

        private static DepthScaleConfig Config()
        {
            var current = StreetConfigs.Current;
            if (current == null || current.DepthScale == null)
                throw new InvalidOperationException("StreetConfigs.Current.DepthScale is not bound.");
            return current.DepthScale;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
