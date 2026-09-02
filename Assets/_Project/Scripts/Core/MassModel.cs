using UnityEngine;

namespace CleanPath.Core
{
    public struct Vec3
    {
        public float x, y, z;
        public Vec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static float Dist(Vec3 a, Vec3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public static class MassModel
    {
        public static float Radius(float mass, float radiusPerMassCbrt) =>
            radiusPerMassCbrt * Mathf.Pow(mass, 1f / 3f);

        public static float Mass(float radius, float radiusPerMassCbrt) =>
            Mathf.Pow(radius / radiusPerMassCbrt, 3f);
    }
}
