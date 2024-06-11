
namespace Protocol
{
    internal sealed class BoundingShape
    {
        private readonly BoundingBox[] _PRIMITIVES;
        
        public BoundingShape(params BoundingBox[] primitives)
        {
            _PRIMITIVES = primitives;
        }

        public double AdjustX(BoundingBox bb, double s)
        {
            int length = _PRIMITIVES.Length;
            System.Diagnostics.Debug.Assert(length >= 0);

            if (length == 0)
            {
                return s;
            }

            for (int i = 0; i < length; ++i)
            {
                BoundingBox bbFixed = _PRIMITIVES[i];
                s = bbFixed.AdjustX(bb, s);
            }

            return s;
        }

        public double AdjustY(BoundingBox bb, double s)
        {
            int length = _PRIMITIVES.Length;
            System.Diagnostics.Debug.Assert(length >= 0);

            if (length == 0)
            {
                return s;
            }

            for (int i = 0; i < length; ++i)
            {
                BoundingBox bbFixed = _PRIMITIVES[i];
                s = bbFixed.AdjustY(bb, s);
            }

            return s;
        }

        public double AdjustZ(BoundingBox bb, double s)
        {
            int length = _PRIMITIVES.Length;
            System.Diagnostics.Debug.Assert(length >= 0);

            if (length == 0)
            {
                return s;
            }

            for (int i = 0; i < length; ++i)
            {
                BoundingBox bbFixed = _PRIMITIVES[i];
                s = bbFixed.AdjustZ(bb, s);
            }

            return s;
        }

    }
}
