
using Common;

namespace Protocol
{
    public enum Blocks : int
    {
        Air,
        Stone, 
        Granite, 
        PolishedGranite, 
        Diorite, 
        PolishedDiorite, 
        Andesite, 
        PolishedAndesite, 
        Grass, 
        Dirt, 
        CoarseDirt, 
        Podzol, 

        EastBottomOakWoodStairs, 
        WestBottomOakWoodStairs, 
        SouthBottomOakWoodStairs, 
        NorthBottomOakWoodStairs, 
        EastTopOakWoodStairs, 
        WestTopOakWoodStairs, 
        SouthTopOakWoodStairs, 
        NorthTopOakWoodStairs, 
    }

    /*public sealed class Block : System.IDisposable
    {
        public struct Vector : System.IEquatable<Vector>
        {
            public static Vector Convert(Entity.Vector pos)
            {
                int x = (int)pos.X,
                    y = (int)pos.Y,
                    z = (int)pos.Z;

                double r1 = pos.X % 1,
                       r2 = pos.Y % 1,
                       r3 = pos.Z % 1;
                if (Comparing.IsLessThan(r1, 0))
                {
                    --x;
                }

                if (Comparing.IsLessThan(r2, 0))
                {
                    --y;
                }

                if (Comparing.IsLessThan(r3, 0))
                {
                    --z;
                }

                return new(x, y, z);
            }

            public int X, Y, Z;

            public Vector(int x, int y, int z)
            {
                X = x; Y = y; Z = z;
            }

            public override readonly string? ToString()
            {
                return $"( X: {X}, Y: {Y}, Z: {Z} )";
            }

            public readonly bool Equals(Vector other)
            {
                return (X == other.X) && (Y == other.Y) && (Z == other.Z);
            }

        }   

        public class Grid : System.IEquatable<Grid>
        {
            private readonly Vector _max, _min;

            public static Grid Generate(Entity.Grid grid)
            {
                Vector max = Vector.Convert(grid.MAX),
                       min = Vector.Convert(grid.MIN);

                Entity.BoundingBox bb = Entity.BoundingBox.GetBlockBB();

                double r1 = grid.MIN.X % bb.Width,
                       r2 = grid.MIN.Y % bb.Height,
                       r3 = grid.MIN.Z % bb.Width;
                int xMin = min.X, yMin = min.Y, zMin = min.Z;
                if (Comparing.IsEqualTo(r1, 0.0D))
                {
                    --xMin;
                }
                if (Comparing.IsEqualTo(r2, 0.0D))
                {
                    --yMin;
                }
                if (Comparing.IsEqualTo(r3, 0.0D))
                {
                    --zMin;
                }

                return new(max, new(xMin, yMin, zMin));
            }

            public static Grid Generate(Entity.Vector p, Entity.BoundingBox boundingBox)
            {
                System.Diagnostics.Debug.Assert(Comparing.IsGreaterThan(boundingBox.Width, 0));
                System.Diagnostics.Debug.Assert(Comparing.IsGreaterThan(boundingBox.Height, 0));

                Entity.BoundingBox bb = Entity.BoundingBox.GetBlockBB();

                double h = boundingBox.Width / 2.0D;
                System.Diagnostics.Debug.Assert(h > 0);

                double xEntityMax = p.X + h, yEntityMax = p.Y + boundingBox.Height, zEntityMax = p.Z + h,
                       xEntityMin = p.X - h, yEntityMin = p.Y, zEntityMin = p.Z - h;
                Entity.Vector 
                    maxEntity = new(xEntityMax, yEntityMax, zEntityMax),
                    minEntity = new(xEntityMin, yEntityMin, zEntityMin);
                Vector max = Vector.Convert(maxEntity),
                       min = Vector.Convert(minEntity);

                double r1 = xEntityMin % bb.Width,
                       r2 = yEntityMin % bb.Height,
                       r3 = zEntityMin % bb.Width;
                int xMin = min.X, yMin = min.Y, zMin = min.Z;
                if (Comparing.IsEqualTo(r1, 0.0D))
                {
                    --xMin;
                }
                if (Comparing.IsEqualTo(r2, 0.0D))
                {
                    --yMin;
                }
                if (Comparing.IsEqualTo(r3, 0.0D))
                {
                    --zMin;
                }

                return new(max, new(xMin, yMin, zMin));
            }

            public Grid(Vector max, Vector min)
            {
                System.Diagnostics.Debug.Assert(max.X >= min.X);
                System.Diagnostics.Debug.Assert(max.Y >= min.Y);
                System.Diagnostics.Debug.Assert(max.Z >= min.Z);

                _max = max; _min = min;
            }

            public bool Contains(Vector p)
            {
                return (
                    p.X <= _max.X && p.X >= _min.X &&
                    p.Y <= _max.Y && p.Y >= _min.Y &&
                    p.Z <= _max.Z && p.Z >= _min.Z);
            }

            public System.Collections.Generic.IEnumerable<Vector> GetVectors()
            {
                if (_max.X == _min.X && _max.Y == _min.Y && _max.Z == _min.Z)
                {
                    yield return new(_max.X, _max.Y, _max.Z);
                }
                else
                {
                    for (int y = _min.Y; y <= _max.Y; ++y)
                    {
                        for (int z = _min.Z; z <= _max.Z; ++z)
                        {
                            for (int x = _min.X; x <= _max.X; ++x)
                            {
                                yield return new(x, y, z);
                            }
                        }
                    }
                }

            }

            public override string? ToString()
            {
                return $"( Max: ({_max.X}, {_max.Z}), Min: ({_min.X}, {_min.Z}) )";
            }

            public bool Equals(Grid? other)
            {
                if (other == null)
                {
                    return false;
                }

                System.Diagnostics.Debug.Assert(other != null);
                return (other._max.Equals(_max) && other._min.Equals(_min));
            }

        }

        *//*public static Block GetBlockByGlobalPaletteId(ulong value)
        {
            uint metadata = Conversions.ToUint(value) & 0b_1111;
            uint id = Conversions.ToUint(value) >> 4;
            System.Diagnostics.Debug.Assert((id & ~Conversions.ToUint(0b1_1111_1111)) == 0);

            Types type = (Types)id;

            return new(type, metadata);
        }*//*

        public enum Types : uint
        {
            Air              = 0,
            Stone            = 1,
            Grass            = 2,
            Dirt             = 3,
        }

        private readonly Vector _P;
        public Vector Position => _P;

        private readonly Types _TYPE;
        public Types Type => _TYPE;

        private readonly uint _METADATA;
        public uint Metadata => _METADATA;

        public ulong GlobalPaletteId
        {
            get
            {
                byte metadata = (byte)_METADATA;
                System.Diagnostics.Debug.Assert((metadata & 0b_11110000) == 0);  // metadata is 4 bits

                ushort id = (ushort)_TYPE;
                System.Diagnostics.Debug.Assert((id & 0b_11111110_00000000) == 0);  // id is 9 bits
                return (ulong)(id << 4 | metadata);  // 13 bits
            }
        }


        public Block(Vector p, Types type, uint metadata)
        {
            _P = p;
            _TYPE = type;
            _METADATA = metadata;
        }

        *//*public Block(Types type)
        {
            _TYPE = type;
            _METADATA = 0;
        }*//*

    }*/

}
