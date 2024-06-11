

using Common;
using Containers;
using static System.Collections.Specialized.BitVector32;

namespace Protocol
{
    internal static class BlockExtensions
    {
        private static Table<Blocks, int> _BLOCK_ENUM_TO_ID_MAP = new();
        private static Table<int, Blocks> _BLOCK_ID_TO_ENUM_MAP = new();

        static BlockExtensions()
        {
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Air, (0 << 4) | 0);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Stone, (1 << 4) | 0);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Granite, (1 << 4) | 1);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.PolishedGranite, (1 << 4) | 2);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Diorite, (1 << 4) | 3);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.PolishedDiorite, (1 << 4) | 4);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Andesite, (1 << 4) | 5);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.PolishedAndesite, (1 << 4) | 6);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Grass, (2 << 4) | 0);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Dirt, (3 << 4) | 0);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.CoarseDirt, (3 << 4) | 1);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.Podzol, (3 << 4) | 2);

            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.EastBottomOakWoodStairs, (53 << 4) | 0);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.WestBottomOakWoodStairs, (53 << 4) | 1);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.SouthBottomOakWoodStairs, (53 << 4) | 2);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.NorthBottomOakWoodStairs, (53 << 4) | 3);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.EastTopOakWoodStairs, (53 << 4) | 4);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.WestTopOakWoodStairs, (53 << 4) | 5);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.SouthTopOakWoodStairs, (53 << 4) | 6);
            _BLOCK_ENUM_TO_ID_MAP.Insert(Blocks.NorthTopOakWoodStairs, (53 << 4) | 7);

            foreach ((Blocks block, int id) in _BLOCK_ENUM_TO_ID_MAP.GetElements())
            {
                _BLOCK_ID_TO_ENUM_MAP.Insert(id, block);
            }

            System.Diagnostics.Debug.Assert(_BLOCK_ENUM_TO_ID_MAP.Count == _BLOCK_ID_TO_ENUM_MAP.Count);

        }

        public static Blocks ToBlock(int id)
        {
            System.Diagnostics.Debug.Assert(_BLOCK_ID_TO_ENUM_MAP.Contains(id));

            return _BLOCK_ID_TO_ENUM_MAP.Lookup(id);
        }

        public static int GetId(this Blocks block)
        {
            System.Diagnostics.Debug.Assert(_BLOCK_ENUM_TO_ID_MAP.Contains(block));

            return _BLOCK_ENUM_TO_ID_MAP.Lookup(block);
        }
    }

    internal sealed class BlockContext : System.IDisposable
    {
        private enum Directions
        {
            DOWN,
            UP,
            NORTH,
            SOUTH,
            WEST,
            EAST,
        }

        private static Directions ToCCWDirection(Directions d)
        {
            switch (d)
            {
                default:
                    throw new System.NotImplementedException();
                case Directions.NORTH:
                    return Directions.WEST;
                case Directions.EAST:
                    return Directions.NORTH;
                case Directions.SOUTH:
                    return Directions.EAST;
                case Directions.WEST:
                    return Directions.SOUTH;
            }
        }

        private static Directions ToCWDirection(Directions d)
        {
            switch (d)
            {
                default:
                    throw new System.NotImplementedException();
                case Directions.NORTH:
                    return Directions.EAST;
                case Directions.EAST:
                    return Directions.SOUTH;
                case Directions.SOUTH:
                    return Directions.WEST;
                case Directions.WEST:
                    return Directions.NORTH;
            }
        }

        private static Directions ToOppositeDirection(Directions d)
        {
            switch (d)
            {
                default:
                    throw new System.NotImplementedException();
                case Directions.UP:
                    return Directions.DOWN;
                case Directions.DOWN:
                    return Directions.UP;
                case Directions.NORTH:
                    return Directions.SOUTH;
                case Directions.EAST:
                    return Directions.WEST;
                case Directions.SOUTH:
                    return Directions.NORTH;
                case Directions.WEST:
                    return Directions.EAST;
            }
        }

        private static bool IsStairsBlock(Blocks block)
        {
            switch (block)
            {
                default:
                    return false;
                case Blocks.EastBottomOakWoodStairs:
                    return true;
                case Blocks.WestBottomOakWoodStairs:
                    return true;
                case Blocks.SouthBottomOakWoodStairs:
                    return true;
                case Blocks.NorthBottomOakWoodStairs:
                    return true;
                case Blocks.EastTopOakWoodStairs:
                    return true;
                case Blocks.WestTopOakWoodStairs:
                    return true;
                case Blocks.SouthTopOakWoodStairs:
                    return true;
                case Blocks.NorthTopOakWoodStairs:
                    return true;

            }
        }

        private static Directions GetStairsDirection(Blocks block)
        {
            System.Diagnostics.Debug.Assert(IsStairsBlock(block));

            int id = block.GetId();
            int metadata = id & 0b_1111;
            switch (metadata % 4)
            {
                default:
                    throw new System.NotImplementedException();
                case 0:
                    return Directions.EAST;
                case 1:
                    return Directions.WEST;
                case 2:
                    return Directions.SOUTH;
                case 3:
                    return Directions.NORTH;
            }
        }

        private static bool IsBottomStairsBlock(Blocks block)
        {
            System.Diagnostics.Debug.Assert(IsStairsBlock(block));

            int id = block.GetId();
            int metadata = id & 0b_1111;
            return metadata < 4;
        }

        private static bool IsVerticalStairsBlock(Blocks block)
        {
            System.Diagnostics.Debug.Assert(IsStairsBlock(block));

            int id = block.GetId();
            int metadata = id & 0b_1111;
            return metadata == 0 || metadata == 1 || metadata == 4 || metadata == 5;
        }

        private sealed class ChunkData : System.IDisposable
        {
            private sealed class SectionData : System.IDisposable
            {
                public static void Write(Buffer buffer, SectionData sectionData)
                {
                    byte bitCount = sectionData._bitsPerBlock;
                    buffer.WriteByte(bitCount);

                    (int, int)[] palette = sectionData._palette;
                    System.Diagnostics.Debug.Assert(palette != null);
                    if (bitCount == 13)
                    {
                        buffer.WriteInt(0, true);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(bitCount >= 4 && bitCount <= 8);

                        int length = palette.Length;
                        buffer.WriteInt(length, true);

                        for (int i = 0; i < length; ++i)
                        {
                            (int id, _) = palette[i];
                            buffer.WriteInt(id, true);
                        }
                    }

                    long[] data = sectionData._data;
                    buffer.WriteInt(data.Length, true);
                    for (int i = 0; i < data.Length; ++i)
                    {
                        buffer.WriteLong(data[i]);
                    }

                    buffer.WriteData(sectionData._blockLights);
                    buffer.WriteData(sectionData._skyLights);

                }

                private bool _disposed = false;

                public const int WIDTH = _WIDTH;
                public const int HEIGHT = WIDTH;
                private const int _TOTAL_BLOCK_COUNT = WIDTH * WIDTH * HEIGHT;

                private byte _bitsPerBlock;

                private (int, int)[] _palette;

                private const int _BITS_PER_DATA_UNIT = sizeof(long) * 8; // TODO: Change to appropriate name.
                private long[] _data;

                private byte[] _blockLights, _skyLights;

                private static int GetDataLength(int _bitsPerBlock)
                {
                    int a = _TOTAL_BLOCK_COUNT * _bitsPerBlock;
                    System.Diagnostics.Debug.Assert(a % _BITS_PER_DATA_UNIT == 0);
                    int length = a / _BITS_PER_DATA_UNIT;
                    return length;
                }

                public SectionData(int defaultId)
                {
                    _bitsPerBlock = 4;

                    _palette = [(defaultId, _TOTAL_BLOCK_COUNT)];

                    {
                        int length = GetDataLength(_bitsPerBlock);
                        _data = new long[length];

                        int i;
                        long value = (long)defaultId;

                        int start, offset, end;

                        for (int y = 0; y < HEIGHT; ++y)
                        {
                            for (int z = 0; z < WIDTH; ++z)
                            {
                                for (int x = 0; x < WIDTH; ++x)
                                {
                                    i = (((y * HEIGHT) + z) * WIDTH) + x;

                                    start = (i * _bitsPerBlock) / _BITS_PER_DATA_UNIT;
                                    offset = (i * _bitsPerBlock) % _BITS_PER_DATA_UNIT;
                                    end = (((i + 1) * _bitsPerBlock) - 1) / _BITS_PER_DATA_UNIT;

                                    System.Diagnostics.Debug.Assert(
                                        (value & ~((1L << _bitsPerBlock) - 1L)) == 0);
                                    _data[start] |= (value << offset);

                                    if (start != end)
                                    {
                                        _data[end] = value >> (_BITS_PER_DATA_UNIT - offset);
                                    }

                                }
                            }
                        }

                    }

                    {
                        int length = _TOTAL_BLOCK_COUNT / 2;
                        _blockLights = new byte[length];
                        System.Array.Fill<byte>(_blockLights, byte.MaxValue);
                        _skyLights = new byte[length];
                        System.Array.Fill<byte>(_skyLights, byte.MaxValue);
                    }

                }

                ~SectionData() => System.Diagnostics.Debug.Assert(false);

                public int GetId(int x, int y, int z)
                {
                    System.Diagnostics.Debug.Assert(!_disposed);

                    System.Diagnostics.Debug.Assert(x >= 0 && x <= WIDTH);
                    System.Diagnostics.Debug.Assert(z >= 0 && z <= WIDTH);
                    System.Diagnostics.Debug.Assert(y >= 0 && y <= HEIGHT);

                    long mask = (1L << _bitsPerBlock) - 1L;

                    int i;
                    long value;

                    int start, offset, end;

                    i = (((y * HEIGHT) + z) * WIDTH) + x;
                    start = (i * _bitsPerBlock) / _BITS_PER_DATA_UNIT;
                    offset = (i * _bitsPerBlock) % _BITS_PER_DATA_UNIT;
                    end = ((i + 1) * _bitsPerBlock - 1) / _BITS_PER_DATA_UNIT;

                    if (start == end)
                    {
                        value = (_data[start] >> offset);
                    }
                    else
                    {
                        value = (_data[start] >> offset | _data[end] << (_BITS_PER_DATA_UNIT - offset));
                    }

                    value &= mask;

                    if (_bitsPerBlock == 13)
                    {
                        System.Diagnostics.Debug.Assert(value <= int.MaxValue);
                        return (int)value;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(_palette != null);

                        (int id, var _) = _palette[value];
                        return id;
                    }
                }

                private void ExpandData(byte bitsPerBlock)
                {
                    System.Diagnostics.Debug.Assert(!_disposed);

                    System.Diagnostics.Debug.Assert(_palette != null);
                    System.Diagnostics.Debug.Assert(bitsPerBlock > _bitsPerBlock);

                    int length = GetDataLength(bitsPerBlock);
                    var data = new long[length];

                    long mask = (1L << bitsPerBlock) - 1L;

                    int i;
                    long value;

                    int start, offset, end;

                    if (bitsPerBlock == 13)
                    {
                        int id;

                        for (int y = 0; y < HEIGHT; ++y)
                        {
                            for (int z = 0; z < WIDTH; ++z)
                            {
                                for (int x = 0; x < WIDTH; ++x)
                                {
                                    i = (((y * HEIGHT) + z) * WIDTH) + x;

                                    {
                                        start = (i * _bitsPerBlock) / _BITS_PER_DATA_UNIT;
                                        offset = (i * _bitsPerBlock) % _BITS_PER_DATA_UNIT;
                                        end = (((i + 1) * _bitsPerBlock) - 1) / _BITS_PER_DATA_UNIT;

                                        if (start == end)
                                        {
                                            value = (_data[start] >> offset);
                                        }
                                        else
                                        {
                                            value = 
                                                (_data[start] >> offset) | 
                                                (_data[end] << (_BITS_PER_DATA_UNIT - offset));
                                        }

                                        value &= mask;
                                        (id, int count) = _palette[value];
                                        System.Diagnostics.Debug.Assert(count > 0);
                                    }

                                    {
                                        start = (i * bitsPerBlock) / _BITS_PER_DATA_UNIT;
                                        offset = (i * bitsPerBlock) % _BITS_PER_DATA_UNIT;
                                        end = (((i + 1) * bitsPerBlock) - 1) / _BITS_PER_DATA_UNIT;

                                        value = (long)id;

                                        System.Diagnostics.Debug.Assert(
                                            (value & ~((1L << bitsPerBlock) - 1L)) == 0);
                                        data[start] |= (value << offset);

                                        if (start != end)
                                        {
                                            data[end] = (value >> (_BITS_PER_DATA_UNIT - offset));
                                        }
                                    }

                                }
                            }
                        }

                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(bitsPerBlock > 4 && bitsPerBlock <= 8);

                        for (int y = 0; y < HEIGHT; ++y)
                        {
                            for (int z = 0; z < WIDTH; ++z)
                            {
                                for (int x = 0; x < WIDTH; ++x)
                                {
                                    i = (((y * HEIGHT) + z) * WIDTH) + x;

                                    {
                                        start = (i * _bitsPerBlock) / _BITS_PER_DATA_UNIT;
                                        offset = (i * _bitsPerBlock) % _BITS_PER_DATA_UNIT;
                                        end = (((i + 1) * _bitsPerBlock) - 1) / _BITS_PER_DATA_UNIT;

                                        if (start == end)
                                        {
                                            value = (_data[start] >> offset);
                                        }
                                        else
                                        {
                                            value = 
                                                (_data[start] >> offset | 
                                                _data[end] << (_BITS_PER_DATA_UNIT - offset));
                                        }

                                        value &= mask;
                                    }

                                    {
                                        start = (i * bitsPerBlock) / _BITS_PER_DATA_UNIT;
                                        offset = (i * bitsPerBlock) % _BITS_PER_DATA_UNIT;
                                        end = (((i + 1) * bitsPerBlock) - 1) / _BITS_PER_DATA_UNIT;

                                        System.Diagnostics.Debug.Assert(
                                            (value & ~((1L << bitsPerBlock) - 1L)) == 0);
                                        data[start] |= (value << offset);

                                        if (start != end)
                                        {
                                            data[end] = value >> (_BITS_PER_DATA_UNIT - offset);
                                        }
                                    }

                                }
                            }
                        }
                    }

                    _bitsPerBlock = bitsPerBlock;
                    _data = data;
                }

                private long GetValue(int id)
                {
                    System.Diagnostics.Debug.Assert(!_disposed);

                    long value;

                    if (_bitsPerBlock == 13)
                    {
                        System.Diagnostics.Debug.Assert(_palette == null);

                        value = (long)id;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(_palette != null);
                        System.Diagnostics.Debug.Assert(_bitsPerBlock >= 4 && _bitsPerBlock <= 8);
                        System.Diagnostics.Debug.Assert(_palette.Length > 0);

                        int indexPalette = -1;

                        System.Diagnostics.Debug.Assert(_palette != null);
                        for (int i = 0; i < _palette.Length; ++i)
                        {
                            (int idPalette, int count) = _palette[i];
                            System.Diagnostics.Debug.Assert(count >= 0);

                            if (id == idPalette)
                            {
                                indexPalette = i;
                                _palette[i] = (idPalette, ++count);
                            }
                        }

                        if (indexPalette >= 0)
                        {
                            value = (long)indexPalette;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(indexPalette == -1);

                            int length = _palette.Length;
                            int lengthNew = length + 1;

                            byte bitsPerBlock;
                            if (lengthNew <= 0b_00001111U)
                            {
                                bitsPerBlock = 4;
                            }
                            else if (lengthNew <= 0b_00011111U)
                            {
                                bitsPerBlock = 5;
                            }
                            else if (lengthNew <= 0b_00111111U)
                            {
                                bitsPerBlock = 6;
                            }
                            else if (lengthNew <= 0b_01111111U)
                            {
                                bitsPerBlock = 7;
                            }
                            else if (lengthNew <= 0b_11111111U)
                            {
                                bitsPerBlock = 8;
                            }
                            else
                            {
                                bitsPerBlock = 13;
                            }

                            if (bitsPerBlock > _bitsPerBlock)
                            {
                                ExpandData(bitsPerBlock);
                            }

                            {
                                var paletteNew = new (int, int)[lengthNew];

                                System.Array.Copy(_palette, paletteNew, length);
                                paletteNew[length] = (id, 1);

                                _palette = paletteNew;
                            }

                            if (bitsPerBlock == 13)
                            {
                                value = (long)id;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(bitsPerBlock >= 4 && bitsPerBlock <= 8);

                                value = (long)length;
                            }

                        }
                    }

                    return value;
                }

                public void SetId(int x, int y, int z, int id)
                {
                    System.Diagnostics.Debug.Assert(!_disposed);

                    System.Diagnostics.Debug.Assert(x >= 0 && x <= WIDTH);
                    System.Diagnostics.Debug.Assert(y >= 0 && y <= WIDTH);
                    System.Diagnostics.Debug.Assert(z >= 0 && z <= HEIGHT);

                    long value = GetValue(id);

                    int i = (((y * HEIGHT) + z) * WIDTH) + x;
                    int start = (i * _bitsPerBlock) / _BITS_PER_DATA_UNIT;
                    int offset = (i * _bitsPerBlock) % _BITS_PER_DATA_UNIT;
                    int end = ((i + 1) * _bitsPerBlock - 1) / _BITS_PER_DATA_UNIT;

                    System.Diagnostics.Debug.Assert(
                        (value & ~((1L << _bitsPerBlock) - 1L)) == 0L);
                    _data[start] |= (value << offset);

                    if (start != end)
                    {
                        _data[end] = (value >> (_BITS_PER_DATA_UNIT - offset));
                    }
                }

                public void Dispose()
                {
                    // Assertion.
                    System.Diagnostics.Debug.Assert(!_disposed);

                    // Release resources.
                    _palette = null;

                    _data = null;

                    _blockLights = null; _skyLights = null;

                    // Finish.
                    System.GC.SuppressFinalize(this);
                    _disposed = true;
                }


            }

            private const int _WIDTH = ChunkLocation.WIDTH;
            private const int _MAX_SECTION_COUNT = 16;

            public static (int, byte[]) Write(ChunkData chunkData)
            {
                using Buffer buffer = new();

                int mask = 0;
                for (int i = 0; i < _MAX_SECTION_COUNT; ++i)
                {
                    SectionData? section = chunkData._sections[i];
                    if (section == null) continue;

                    mask |= (1 << i);  // TODO;
                    SectionData.Write(buffer, section);
                }

                // TODO
                for (int z = 0; z < _WIDTH; ++z)
                {
                    for (int x = 0; x < _WIDTH; ++x)
                    {
                        buffer.WriteByte(127);  // Void Biome
                    }
                }

                return (mask, buffer.ReadData());
            }

            public static (int, byte[]) Write()
            {
                using Buffer buffer = new();

                int mask = 0;
                System.Diagnostics.Debug.Assert(_MAX_SECTION_COUNT == 16);

                // TODO: biomes
                for (int z = 0; z < _WIDTH; ++z)
                {
                    for (int x = 0; x < _WIDTH; ++x)
                    {
                        buffer.WriteByte(127);  // Void Biome
                    }
                }

                return (mask, buffer.ReadData());
            }

            private bool _disposed = false;

            private int _count = 0;
            private SectionData?[] _sections = new SectionData?[_MAX_SECTION_COUNT];  // from bottom to top

            public ChunkData() { }

            public void SetId(int defaultId, int x, int y, int z, int id)
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                System.Diagnostics.Debug.Assert(x >= 0 && x < SectionData.WIDTH);
                System.Diagnostics.Debug.Assert(z >= 0 && z < SectionData.WIDTH);

                System.Diagnostics.Debug.Assert(y >= 0);

                int ySection = y / SectionData.HEIGHT;
                System.Diagnostics.Debug.Assert(ySection < _MAX_SECTION_COUNT);

                int yPrime = y - (ySection * SectionData.HEIGHT);
                System.Diagnostics.Debug.Assert(yPrime >= 0 && yPrime < SectionData.HEIGHT);

                SectionData? section = _sections[ySection];
                if (section == null)
                {
                    if (id == defaultId)
                    {
                        return;
                    }

                    section = new SectionData(defaultId);
                    _sections[ySection] = section;
                }

                section.SetId(x, yPrime, z, id);
            }

            public int GetId(int defaultId, int x, int y, int z)
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                System.Diagnostics.Debug.Assert(x >= 0 && x < SectionData.WIDTH);
                System.Diagnostics.Debug.Assert(z >= 0 && z < SectionData.WIDTH);

                if (y < 0)
                {
                    return defaultId;
                }

                int ySection = y / SectionData.HEIGHT;
                if (ySection >= _MAX_SECTION_COUNT)
                {
                    return defaultId;
                }

                int yPrime = y - (ySection * SectionData.HEIGHT);
                System.Diagnostics.Debug.Assert(yPrime >= 0 && yPrime < SectionData.HEIGHT);

                SectionData? section = _sections[ySection];
                if (section == null)
                {
                    return defaultId;
                }

                return section.GetId(x, yPrime, z);
            }

            public void Dispose()
            {
                // Assertion.
                System.Diagnostics.Debug.Assert(!_disposed);

                // Release resources.
                for (int i = 0; i < _sections.Length; ++i)
                {
                    SectionData? data = _sections[i];
                    if (data == null)
                    {
                        continue;
                    }

                    data.Dispose();
                }
                _sections = null;

                // Finish.
                System.GC.SuppressFinalize(this);
                _disposed = true;
            }

        }

        private bool _disposed = false;

        public static readonly Blocks _DEFAULT_BLOCK = Blocks.Air;

        private readonly Table<ChunkLocation, ChunkData> _CHUNKS = new();  // Disposable

        public BlockContext() { }

        public void SetBlock(BlockLocation loc, Blocks block)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            ChunkData chunk;

            ChunkLocation locChunk = ChunkLocation.Generate(loc);
            if (!_CHUNKS.Contains(locChunk))
            {
                if (block == _DEFAULT_BLOCK)
                {
                    return;
                }

                chunk = new ChunkData();
                _CHUNKS.Insert(locChunk, chunk);
            }
            else
            {
                chunk = _CHUNKS.Lookup(locChunk);
            }

            int x = loc.X - (locChunk.X * ChunkLocation.WIDTH),
                y = loc.Y,
                z = loc.Z - (locChunk.Z * ChunkLocation.WIDTH);
            chunk.SetId(_DEFAULT_BLOCK.GetId(), x, y, z, block.GetId());
        }

        public Blocks GetBlock(BlockLocation loc)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            ChunkLocation locChunk = ChunkLocation.Generate(loc);
            if (!_CHUNKS.Contains(locChunk))
            {
                return _DEFAULT_BLOCK;
            }

            ChunkData chunk = _CHUNKS.Lookup(locChunk);
            int x = loc.X - (locChunk.X * ChunkLocation.WIDTH),
                y = loc.Y,
                z = loc.Z - (locChunk.Z * ChunkLocation.WIDTH);
            int id = chunk.GetId(_DEFAULT_BLOCK.GetId(), x, y, z);
            return BlockExtensions.ToBlock(id);
        }

        private Blocks GetBlock(BlockLocation loc, Directions d, int s)
        {
            BlockLocation locPrime;
            switch (d)
            {
                default:
                    throw new System.NotImplementedException();
                case Directions.EAST:
                    locPrime = new BlockLocation(loc.X + s, loc.Y, loc.Z);
                    break;
                case Directions.WEST:
                    locPrime = new BlockLocation(loc.X - s, loc.Y, loc.Z);
                    break;
                case Directions.SOUTH:
                    locPrime = new BlockLocation(loc.X, loc.Y, loc.Z + s);
                    break;
                case Directions.NORTH:
                    locPrime = new BlockLocation(loc.X, loc.Y, loc.Z - s);
                    break;
                case Directions.UP:
                    locPrime = new BlockLocation(loc.X, loc.Y + s, loc.Z);
                    break;
                case Directions.DOWN:
                    locPrime = new BlockLocation(loc.X, loc.Y - s, loc.Z);
                    break;
            }

            return GetBlock(locPrime);
        }
        private static BoundingShape GenerateNone()
        {
            return new BoundingShape();
        }

        private static BoundingShape GenerateCube(BlockLocation loc)
        {
            Vector min = loc.Convert(),
                   max = new(min.X + 1.0D, min.Y + 1.0D, min.Z + 1.0D);
            BoundingBox bb = new(max, min);
            return new BoundingShape(bb);
        }

        private (Directions, bool, int) DetermineStairsShape(
            BlockLocation loc, Blocks block)
        {
            System.Diagnostics.Debug.Assert(IsStairsBlock(block));

            Directions d = GetStairsDirection(block);
            bool bottom = IsBottomStairsBlock(block);

            Blocks block2 = GetBlock(loc, d, 1);
            if (IsStairsBlock(block2) &&
                bottom == IsBottomStairsBlock(block2))
            {
                if (IsVerticalStairsBlock(block2) != IsVerticalStairsBlock(block))
                {
                    Directions d2 = GetStairsDirection(block2);
                    Blocks block3 = GetBlock(loc, ToOppositeDirection(d2), 1);
                    if (!IsStairsBlock(block3) ||
                        GetStairsDirection(block3) != d ||
                        IsBottomStairsBlock(block3) != bottom)
                    {
                        if (d2 == ToCCWDirection(d))
                        {
                            // outer left
                            return (d, bottom, 1);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(d2 == ToCWDirection(d));
                            // outer right
                            return (d, bottom, 2);
                        }
                    }
                }

            }

            Blocks block4 = GetBlock(loc, ToOppositeDirection(d), 1);
            if (IsStairsBlock(block4) &&
                bottom == IsBottomStairsBlock(block4))
            {
                if (IsVerticalStairsBlock(block4) != IsVerticalStairsBlock(block))
                {
                    Directions d4 = GetStairsDirection(block4);
                    Blocks block5 = GetBlock(loc, d4, 1);
                    if (!IsStairsBlock(block5) ||
                        GetStairsDirection(block5) != d ||
                        IsBottomStairsBlock(block5) != bottom)
                    {
                        if (d4 == ToCCWDirection(d))
                        {
                            // inner left
                            return (d, bottom, 3);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(d4 == ToCWDirection(d));
                            // inner right
                            return (d, bottom, 4);
                        }
                    }
                }
            }


            // straight
            return (d, bottom, 0);
        }

        private BoundingShape GenerateStairs(BlockLocation loc, Blocks block)
        {
            (Directions d, bool bottom, int b) = DetermineStairsShape(loc, block);

            throw new System.NotImplementedException();
        }

        private BoundingShape Generate(BlockLocation loc)
        {
            Blocks block = GetBlock(loc);

            /**
             * 0: None (Air)
             * 1: Cube
             * 2: Slab
             * 3: Stairs
             */
            int a = 0;

            switch (block)
            {
                default:
                    throw new System.NotImplementedException();
                case Blocks.Air:
                    a = 0;
                    break;
                case Blocks.Stone:
                    a = 1;
                    break;
                case Blocks.Granite:
                    a = 1;
                    break;
                case Blocks.PolishedGranite:
                    a = 1;
                    break;
                case Blocks.EastBottomOakWoodStairs:
                    a = 3;
                    break;
            }

            switch (a)
            {
                default:
                    throw new System.NotImplementedException();
                case 0:
                    return GenerateNone();
                case 1:
                    return GenerateCube(loc);
                case 3:
                    return GenerateStairs(loc, block);
            }
        }

        public BoundingShape[] GetBlockShapes(BoundingBox bb)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            BlockGrid grid = BlockGrid.Generate(bb);

            int count = grid.GetCount(), i = 0;
            var shapes = new BoundingShape[count];

            for (int y = grid.Min.Y; y <= grid.Max.Y; ++y)
            {
                for (int z = grid.Min.Z; z <= grid.Max.Z; ++z)
                {
                    for (int x = grid.Min.X; x <= grid.Max.X; ++x)
                    {
                        BlockLocation loc = new(x, y, z);

                        BoundingShape shape = Generate(loc);
                        shapes[i++] = shape;
                    }
                }
            }
            System.Diagnostics.Debug.Assert(i == count);

            return shapes;
        }

        internal (int, byte[]) GetChunkData(ChunkLocation loc)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (_CHUNKS.Contains(loc))
            {
                ChunkData data = _CHUNKS.Lookup(loc);
                return ChunkData.Write(data);
            }
            else
            {
                return ChunkData.Write();
            }
        }

        public void Dispose()
        {
            // Assertion.
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release resources.
            (ChunkLocation, ChunkData)[]_chunks = _CHUNKS.Flush();
            for (int i = 0; i < _chunks.Length; ++i)
            {
                (var _, ChunkData data) = _chunks[i];
                data.Dispose();
            }
            _CHUNKS.Dispose();

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }
}
