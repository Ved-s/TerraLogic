using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraLogic.Structures
{
    public struct Angle
    {
        public float Radians { get; set; }
        public float Degrees
        {
            get => Radians / (float)Math.PI * 180f;
            set => Radians = value / 180f * (float)Math.PI;
        }

        public static Angle FromRadians(float rad) => new Angle() { Radians = rad };
        public static Angle FromDegrees(float deg) => new Angle() { Degrees = deg };

        public static Angle operator +(Angle a, Angle b) => new Angle() { Radians = a.Radians + b.Radians };
        public static Angle operator -(Angle a, Angle b) => new Angle() { Radians = a.Radians - b.Radians };
    }
}
