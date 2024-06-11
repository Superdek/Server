
using Common;

namespace Protocol
{
    public sealed class BoundingBox
    {

        private readonly Vector _MAX, _MIN;
        public Vector Max => _MAX;
        public Vector Min => _MIN;

        public BoundingBox(Vector max, Vector min)
        {
            System.Diagnostics.Debug.Assert(max.X >= min.X);
            System.Diagnostics.Debug.Assert(max.Y >= min.Y);
            System.Diagnostics.Debug.Assert(max.Z >= min.Z);

            _MAX = max; _MIN = min;
        }

        public double GetLengthX()
        {
            System.Diagnostics.Debug.Assert(_MAX.X >= _MIN.X);

            return _MAX.X -_MIN.X;
        }

        public double GetLengthY()
        {
            System.Diagnostics.Debug.Assert(_MAX.Y >= _MIN.Y);

            return _MAX.Y - _MIN.Y;
        }

        public double GetLengthZ()
        {
            System.Diagnostics.Debug.Assert(_MAX.Z >= _MIN.Z);

            return _MAX.Z - _MIN.Z;
        }

        public BoundingBox MoveX(double s)
        {
            Vector max = new(Max.X + s, Max.Y, Max.Z),
                   min = new(Min.X + s, Min.Y, Min.Z);

            return new(max, min);
        }

        public BoundingBox MoveY(double s)
        {
            Vector max = new(Max.X, Max.Y + s, Max.Z),
                   min = new(Min.X, Min.Y + s, Min.Z);

            return new(max, min);
        }

        public BoundingBox MoveZ(double s)
        {
            Vector max = new(Max.X, Max.Y, Max.Z + s),
                   min = new(Min.X, Min.Y, Min.Z + s);

            return new(max, min);
        }

        public BoundingBox Move(Vector v)
        {
            Vector
                max = new(Max.X + v.X, Max.Y + v.Y, Max.Z + v.Z),
                min = new(Min.X + v.X, Min.Y + v.Y, Min.Z + v.Z);

            return new(max, min);
        }

        public BoundingBox ExtendX(double s)
        {
            Vector max = Max, min = Min;

            if (s > 0.0D)
            {
                max = new(max.X + s, max.Y, max.Z);
            }
            else if (s < 0.0D)
            {
                min = new(min.X + s, min.Y, min.Z);
            }

            return new(max, min);
        }

        public BoundingBox ExtendY(double s)
        {
            Vector max = Max, min = Min;

            if (s > 0.0D)
            {
                max = new(max.X, max.Y + s, max.Z);
            }
            else if (s < 0.0D)
            {
                min = new(min.X, min.Y + s, min.Z);
            }

            return new(max, min);
        }

        public BoundingBox ExtendZ(double s)
        {
            Vector max = Max, min = Min;

            if (s > 0.0D)
            {
                max = new(max.X, max.Y, max.Z + s);
            }
            else if (s < 0.0D)
            {
                min = new(min.X, min.Y, min.Z + s);
            }

            return new(max, min);
        }

        public BoundingBox Extend(Vector v)
        {
            BoundingBox bb = this;
            bb = bb.ExtendX(v.X);
            bb = bb.ExtendY(v.Y);
            bb = bb.ExtendZ(v.Z);

            return bb;
        }

        public bool IsOverlappingX(BoundingBox bb)
        {
            return (bb.Min.X < Max.X) && (bb.Max.X > Min.X);
        }

        public bool IsOverlappingY(BoundingBox bb)
        {
            return (bb.Min.Y < Max.Y) && (bb.Max.Y > Min.Y);
        }

        public bool IsOverlappingZ(BoundingBox bb)
        {
            return (bb.Min.Z < Max.Z) && (bb.Max.Z > Min.Z);
        }

        public bool IsOverlapping(BoundingBox bb)
        {
            return IsOverlappingX(bb) && IsOverlappingY(bb) && IsOverlappingZ(bb);
        }

        public bool IsContactingX(BoundingBox bb)
        {
            return (bb.Min.X <= Max.X) && (bb.Max.X >= Min.X);
        }

        public bool IsContactingY(BoundingBox bb)
        {
            return (bb.Min.Y <= Max.Y) && (bb.Max.Y >= Min.Y);
        }
        
        public bool IsContactingZ(BoundingBox bb)
        {
            return (bb.Min.Z <= Max.Z) && (bb.Max.Z >= Min.Z);
        }

        public bool IsContacting(BoundingBox bb)
        {
            return IsContactingX(bb) && IsContactingY(bb) && IsContactingZ(bb);
        }

        public double AdjustX(BoundingBox bb, double s)
        {
            if (IsOverlappingY(bb) && IsOverlappingZ(bb))
            {
                double sPrime;
                if ((s > 0.0D) && (bb.Max.X <= Min.X))
                {
                    sPrime = Min.X - bb.Max.X;
                    System.Diagnostics.Debug.Assert(sPrime >= 0.0D);
                    if (sPrime < s)
                    {
                        s = sPrime;
                    }
                }
                else if((s < 0.0D) && (bb.Min.X >= Max.X))
                {
                    sPrime = Max.X - bb.Min.X;
                    System.Diagnostics.Debug.Assert(sPrime <= 0.0D);
                    if (sPrime > s)
                    {
                        s = sPrime;
                    }
                }
            }

            return s;
        }

        public double AdjustY(BoundingBox bb, double s)
        {
            if (IsOverlappingX(bb) && IsOverlappingZ(bb))
            {
                double sPrime;
                if ((s > 0.0D) && (bb.Max.Y <= Min.Y))
                {
                    sPrime = Min.Y - bb.Max.Y;
                    System.Diagnostics.Debug.Assert(sPrime >= 0.0D);
                    if (sPrime < s)
                    {
                        s = sPrime;
                    }
                }
                else if ((s < 0.0D) && (bb.Min.Y >= Max.Y))
                {
                    sPrime = Max.Y - bb.Min.Y;
                    System.Diagnostics.Debug.Assert(sPrime <= 0.0D);
                    if (sPrime > s)
                    {
                        s = sPrime;
                    }
                }
            }

            return s;
        }

        public double AdjustZ(BoundingBox bb, double s)
        {
            if (IsOverlappingX(bb) && IsOverlappingY(bb))
            {
                double sPrime;
                if ((s > 0.0D) && (bb.Max.Z <= Min.Z))
                {
                    sPrime = Min.Z - bb.Max.Z;
                    System.Diagnostics.Debug.Assert(sPrime >= 0.0D);
                    if (sPrime < s)
                    {
                        s = sPrime;
                    }
                }
                else if ((s < 0.0D) && (bb.Min.Z >= Max.Z))
                {
                    sPrime = Max.Z - bb.Min.Z;
                    System.Diagnostics.Debug.Assert(sPrime <= 0.0D);
                    if (sPrime > s)
                    {
                        s = sPrime;
                    }
                }
            }

            return s;
        }

        public Vector GetBottomCenter()
        {
            System.Diagnostics.Debug.Assert(_MAX.X >= _MIN.X);
            System.Diagnostics.Debug.Assert(_MAX.Y >= _MIN.Y);
            System.Diagnostics.Debug.Assert(_MAX.Z >= _MIN.Z);

            double x = (_MAX.X + _MIN.X) / 2.0D,
                   y = _MIN.Y,
                   z = (_MAX.Z + _MIN.Z) / 2.0D;

            return new Vector(x, y, z);
        }

        public override string? ToString()
        {
            return $"( Max: {Max}, Min: {Min} )";
        }

        public bool Equals(BoundingBox? other)
        {
            if (other == null)
            {
                return false;
            }

            return Max.Equals(other.Max) && Min.Equals(other.Min);
        }
    }
}
