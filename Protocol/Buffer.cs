
using Common;

namespace Protocol
{

    // TODO: Check system is little- or big-endian.
    internal sealed class Buffer : System.IDisposable
    {
        private const int _EXPANSION_FACTOR = 2;
        private const float _LOAD_FACTOR = 0.7F;

        private const byte _SEGMENT_BITS = 0x7F;
        private const byte _CONTINUE_BIT = 0x80;

        /*private const int _BOOL_DATATYPE_SIZE = 1;
        private const int _SBYTE_DATATYPE_SIZE = 1;
        private const int _BYTE_DATATYPE_SIZE = 1;*/
        private const int _SHORT_DATATYPE_SIZE = 2;
        private const int _USHORT_DATATYPE_SIZE = 2;
        private const int _INT_DATATYPE_SIZE = 4;
        private const int _LONG_DATATYPE_SIZE = 8;
        private const int _FLOAT_DATATYPE_SIZE = 4;
        private const int _DOUBLE_DATATYPE_SIZE = 8;
        private const int _GUID_DATATYPE_SIZE = 16;

        private const int _INIT_DATASIZE = 16;

        private bool _disposed = false;

        private int _size;
        private byte[] _data;

        private int _first = 0, _last = 0;
        public int Size
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                System.Diagnostics.Debug.Assert(_first >= 0);
                System.Diagnostics.Debug.Assert(_last >= _first);
                return _last - _first;
            }
        }

        public bool Empty => (Size == 0);

        public Buffer()
        {
            _size = _INIT_DATASIZE;
            _data = new byte[_INIT_DATASIZE];
        }

        ~Buffer() => System.Diagnostics.Debug.Assert(false);

        public bool IsEmpty()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(Size >= 0);
            return (Size == 0);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        private byte ExtractByte()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_last >= _first);
            if (_first == _last)
            {
                throw new EmptyBufferException();
            }

            return _data[_first++];
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        private byte[] ExtractBytes(int size)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(size >= 0);
            if (size == 0)
            {
                return [];
            }
            else if (size == 1)
            {
                return [ExtractByte()];
            }

            System.Diagnostics.Debug.Assert(_last >= _first);

            if (_first + size > _last)
            {
                throw new EmptyBufferException();
            }

            byte[] data = new byte[size];
            System.Array.Copy(_data, _first, data, 0, size);
            _first += size;

            return data;
        }

        private void ExpandData(int addedSize)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(addedSize >= 0);
            if (addedSize == 0)
            {
                return;
            }

            int prevSize = _size,
                newSize = prevSize,

                requiredSize = _last + addedSize;

            if (addedSize > 1)
            {
                while (((float)requiredSize / (float)newSize) >= _LOAD_FACTOR)
                {
                    newSize *= _EXPANSION_FACTOR;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(addedSize == 1);

                if (((float)requiredSize / (float)newSize) >= _LOAD_FACTOR)
                {
                    newSize *= _EXPANSION_FACTOR;
                }
            }

            System.Diagnostics.Debug.Assert(prevSize <= newSize);

            _size = newSize;

            var newData = new byte[newSize];
            if (Size > 0)
            {
                System.Array.Copy(_data, _first, newData, _first, Size);
            }
            _data = newData;
        }

        private void InsertByte(byte data)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_last >= _first);

            ExpandData(1);
            _data[_last++] = data;
        }

        private void InsertBytes(byte[] data)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_last >= _first);

            int size = data.Length;
            ExpandData(size);
            System.Array.Copy(data, 0, _data, _last, size);
            _last += size;
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public bool ReadBool()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte data = ExtractByte();
            System.Diagnostics.Debug.Assert(data != 0x01 | data != 0x00);
            return (data > 0x00);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public sbyte ReadSbyte()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return (sbyte)ExtractByte();
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public byte ReadByte()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return (byte)ExtractByte();
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public short ReadShort()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_SHORT_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _SHORT_DATATYPE_SIZE);

            return (short)(
                ((short)data[0] << 8) |
                ((short)data[1] << 0));
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public ushort ReadUshort()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_USHORT_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _USHORT_DATATYPE_SIZE);

            return (ushort)(
                ((ushort)data[0] << 8) |
                ((ushort)data[1] << 0));
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public int ReadInt()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_INT_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _INT_DATATYPE_SIZE);

            return (int)(
                ((int)data[0] << 24) |
                ((int)data[1] << 16) |
                ((int)data[2] << 8) |
                ((int)data[3] << 0));
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public long ReadLong()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_LONG_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _LONG_DATATYPE_SIZE);

            return (long)(
                ((long)data[0] << 56) |
                ((long)data[1] << 48) |
                ((long)data[2] << 40) |
                ((long)data[3] << 32) |
                ((long)data[4] << 24) |
                ((long)data[5] << 16) |
                ((long)data[6] << 8) |
                ((long)data[7] << 0));
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public float ReadFloat()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_FLOAT_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _FLOAT_DATATYPE_SIZE);
            System.Array.Reverse(data);
            return System.BitConverter.ToSingle(data);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public double ReadDouble()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_DOUBLE_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _DOUBLE_DATATYPE_SIZE);
            System.Array.Reverse(data);
            return System.BitConverter.ToDouble(data);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public int ReadInt(bool decode)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (decode == false)       
            {
                return ReadInt();
            }

            uint uvalue = 0;
            int position = 0;

            while (true)
            {
                byte data = ExtractByte();

                uvalue |= (uint)(data & _SEGMENT_BITS) << position;
                if ((data & _CONTINUE_BIT) == 0)
                {
                    break;
                }

                position += 7;

                if (position >= 32)
                {
                    throw new InvalidEncodingException();
                }

                System.Diagnostics.Debug.Assert(position > 0);
            }

            return (int)uvalue;
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public long ReadLong(bool decode)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (decode == false)     
            {
                return ReadLong();
            }

            ulong uvalue = 0;
            int position = 0;

            while (true)
            {
                byte data = ExtractByte();

                uvalue |= (ulong)(data & _SEGMENT_BITS) << position;
                if ((data & _CONTINUE_BIT) == 0)
                    break;

                position += 7;

                if (position >= 64)
                {
                    throw new InvalidEncodingException();
                }

                System.Diagnostics.Debug.Assert(position > 0);
            }

            return (long)uvalue;
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public string ReadString()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
        

            int size = ReadInt(true);
            System.Diagnostics.Debug.Assert(size >= 0);

            byte[] data = ExtractBytes(size);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        public System.Guid ReadGuid()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = ExtractBytes(_GUID_DATATYPE_SIZE);
            System.Diagnostics.Debug.Assert(data.Length == _GUID_DATATYPE_SIZE);

            return new System.Guid(data);
        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        internal byte[] ReadData()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return ExtractBytes(Size);
        }

        public void WriteBool(bool value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (value == true)
            {
                InsertByte(1);
            }
            else
            {
                InsertByte(0);
            }
        }

        public void WriteSbyte(sbyte value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            InsertByte((byte)value);
        }

        public void WriteByte(byte value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            InsertByte((byte)value);
        }

        public void WriteShort(short value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _SHORT_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteUshort(ushort value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _USHORT_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteInt(int value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _INT_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteLong(long value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _LONG_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteFloat(float value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _FLOAT_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteDouble(double value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = System.BitConverter.GetBytes(value);
            System.Diagnostics.Debug.Assert(data.Length == _DOUBLE_DATATYPE_SIZE);
            System.Array.Reverse(data);
            InsertBytes(data);
        }

        public void WriteInt(int value, bool encode)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (encode == false)
            {
                WriteInt(value);
                return;
            }

            uint uvalue = (uint)value;

            while (true)
            {
                if ((uvalue & ~_SEGMENT_BITS) == 0)
                {
                    InsertByte((byte)uvalue);
                    break;
                }

                InsertByte((byte)((uvalue & _SEGMENT_BITS) | _CONTINUE_BIT));
                uvalue >>= 7;
            }

        }

        public void WriteLong(long value, bool encode)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (encode == false)
            {
                WriteLong(value);
                return;
            }

            uint uvalue = (uint)value;
            while (true)
            {
                if ((uvalue & ~_SEGMENT_BITS) == 0)
                {
                    InsertByte((byte)uvalue);
                    break;
                }

                InsertByte((byte)((uvalue & _SEGMENT_BITS) | _CONTINUE_BIT));
                uvalue >>= 7;
            }
        }

        public void WriteString(string value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            int size = value.Length;
            WriteInt(size, true);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
            InsertBytes(data);
        }

        public void WriteGuid(System.Guid value)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = value.ToByteArray();
            System.Diagnostics.Debug.Assert(data.Length == _GUID_DATATYPE_SIZE);
            InsertBytes(data);
        }

        internal void WriteData(byte[] data)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            InsertBytes(data);
        }

        public void Flush()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_size >= _INIT_DATASIZE);
            if (Size == 0)
            {
                return;
            }

            System.Diagnostics.Debug.Assert(_last >= _first);
            _first = _last;
        }

        public void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(!_disposed);

            if (!Empty)
            {
                throw new BufferOverflowException();
            }

            // Release resources.
            _data = null;

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }

}
