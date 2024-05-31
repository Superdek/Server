﻿

using Common;

namespace Protocol
{
    internal readonly struct BlockLocation : System.IEquatable<BlockLocation>
    {
        public const int Width = 1;
        public const int Height = 1;

        public static BlockLocation Generate(Vector p)
        {
            int x = Conversions.ToInt(p.X),
                y = Conversions.ToInt(p.Y),
                z = Conversions.ToInt(p.Z);

            double r1 = p.X % 1.0D,
                   r2 = p.Y % 1.0D,
                   r3 = p.Z % 1.0D;
            if (Comparing.IsLessThan(r1, 0.0D))
            {
                --x;
            }

            if (Comparing.IsLessThan(r2, 0.0D))
            {
                --y;
            }

            if (Comparing.IsLessThan(r3, 0.0D))
            {
                --z;
            }

            return new(x, y, z);
        }

        public static BlockLocation Genrate(ChunkLocation loc)
        {
            throw new System.NotImplementedException();
        }

        public readonly int X, Y, Z;

        public BlockLocation(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }

        public Vector Convert()
        {
            double x = Conversions.ToDouble(X),
                y = Conversions.ToDouble(Y),
                z = Conversions.ToDouble(Z);
            return new(x, y, z);
        }

        

        public override readonly string? ToString()
        {
            return $"( X: {X}, Y: {Y}, Z: {Z} )";
        }

        public readonly bool Equals(BlockLocation other)
        {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }

    }
}
