

using Common;

namespace Protocol
{
    internal readonly struct ChunkLocation : System.IEquatable<ChunkLocation>
    {
        public const int WIDTH = BlockLocation.Width * 16;

        public static ChunkLocation Generate(Vector p)
        {
            int x = (int)(p.X / WIDTH),
                z = (int)(p.Z / WIDTH);

            double r1 = p.X % WIDTH,
                   r2 = p.Z % WIDTH;
            if (r1 < 0.0D)
            {
                --x;
            }

            if (r2 < 0.0D)
            {
                --z;
            }

            return new(x, z);
        }

        public static ChunkLocation Generate(BlockLocation loc)
        {
            int x = loc.X / WIDTH,
                z = loc.Z / WIDTH;

            double r1 = loc.X % (double)WIDTH,
                   r2 = loc.Z % (double)WIDTH;
            if (r1 < 0.0D)
            {
                --x;
            }
            if (r2 < 0.0D)
            {
                --z;
            }

            return new(x, z);
        }

        public readonly int X, Z;

        public ChunkLocation(int x, int z)
        {
            X = x; Z = z;
        }

        public override readonly string? ToString()
        {
            return $"( X: {X}, Z: {Z} )";
        }

        public readonly bool Equals(ChunkLocation other)
        {
            return (X == other.X) && (Z == other.Z);
        }

    }
}
