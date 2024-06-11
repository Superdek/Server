using Common;

namespace Physics
{

    public readonly struct Vector : System.IEquatable<Vector>
    {
        public static Vector operator +(Vector v1, Vector v2)
        {
            return new(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector operator *(Vector v1, Vector v2)
        {
            return new(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public static Vector operator *(Vector v, double s)
        {
            return new(v.X * s, v.Y * s, v.Z * s);
        }

        public static Vector operator *(double s, Vector v)
        {
            return new(s * v.X, s * v.Y, s * v.Z);
        }

        public static Vector operator /(Vector v, double s)
        {
            return new(v.X / s, v.Y / s, v.Z / s);
        }

        public static Vector operator /(double s, Vector v)
        {
            return new(s / v.X, s / v.Y, s / v.Z);
        }

        public static double GetLengthSquared(Vector v1, Vector v2)
        {
            double dx = v1.X - v2.X,
                   dy = v1.Y - v2.Y,
                   dz = v1.Z - v2.Z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        public static double GetLength(Vector v1, Vector v2)
        {
            double s = GetLengthSquared(v1, v2);
            return System.Math.Sqrt(s);
        }

        public readonly double X, Y, Z;

        public Vector(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public override readonly string? ToString()
        {
            return $"( X: {X}, Y: {Y}, Z: {Z} )";
        }

        public bool Equals(Vector other)
        {
            return Comparing.IsEqualTo(X, other.X) &&
                Comparing.IsEqualTo(Y, other.Y) &&
                Comparing.IsEqualTo(Z, other.Z);
        }

    }

    /*public struct Ray
    {

    }*/

    public sealed class BoundingBox
    {
        public readonly Vector MAX, MIN;

        public BoundingBox(Vector max, Vector min)
        {
            System.Diagnostics.Debug.Assert(
                Comparing.IsGreaterThanOrEqualTo(max.X, min.X));
            System.Diagnostics.Debug.Assert(
                Comparing.IsGreaterThanOrEqualTo(max.Y, min.Y));
            System.Diagnostics.Debug.Assert(
                Comparing.IsGreaterThanOrEqualTo(max.Z, min.Z));

            MAX = max; MIN = min;
        }

        internal double F1(BoundingBox bb, double vx)
        {
            throw new System.NotImplementedException();
        }

        internal double F2(BoundingBox bb, double vy)
        {
            throw new System.NotImplementedException();
        }

        internal double F3(BoundingBox bb, double vz)
        {
            throw new System.NotImplementedException();
        }

        public override string? ToString()
        {
            return $"( MAX: {MAX}, MIN: {MIN} )";
        }

        public bool Equals(BoundingBox? other)
        {
            if (other == null)
            {
                return false;
            }

            return MAX.Equals(other.MAX) && MIN.Equals(other.MIN);
        }
    }

    public sealed class Shape
    {
        private readonly BoundingBox[] _PRIMITIVES;

        public Shape(params BoundingBox[] primitives)
        {
            _PRIMITIVES = primitives;
        }

        /*public Vector F1(BoundingBox bb, Vector v)
        {
            double vx = v.X;

            for (int i = 0; i < _PRIMITIVES.Length; ++i)
            {
                vx = _PRIMITIVES[i].F1(bb, vx);
            }

            return new(vx, v.Y, v.Z);
        }
        
        public double F2(BoundingBox bb, double vy)
        {
            for (int i = 0; i < _PRIMITIVES.Length; ++i)
            {
                vy = _PRIMITIVES[i].F2(bb, vy);
            }

            return vy;
        }

        public double F1(BoundingBox bb, double vx)
        {
            for (int i = 0; i < _PRIMITIVES.Length; ++i)
            {
                vx = _PRIMITIVES[i].F1(bb, vx);
            }

            return vx;
        }*/

    }
    
}
