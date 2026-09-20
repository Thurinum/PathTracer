using System.Numerics;

namespace PathTracerCore;

public static class QuaternionExtensions
{
    extension(Quaternion quat)
    {
        public static Quaternion FromEuler(Vector3 degrees)
        {
            return Quaternion.CreateFromYawPitchRoll(ToRad(degrees.Y), ToRad(degrees.X), ToRad(degrees.Z));

            float ToRad(float deg)
            {
                return MathF.PI / 180.0f * deg;
            }
        }
    }
}