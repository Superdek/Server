
namespace Containers
{

    public interface IReadOnlySet<K> where K : struct, System.IEquatable<K>
    {
        public bool Contains(K key);

        public System.Collections.Generic.IEnumerable<K> GetKeys();

    }

    public sealed class Set<K> : System.IDisposable, IReadOnlySet<K>
        where K : struct, System.IEquatable<K>
    {
        private bool _disposed = false;

        private const int _MIN_LENGTH = 16;
        private const int _EXPENSION_FACTOR = 2;
        private const float _LOAD_FACTOR = 0.75F;
        private const int _C = 5;

        private bool[] _flags = new bool[_MIN_LENGTH];
        private K[] _keys = new K[_MIN_LENGTH];
        private int _length = _MIN_LENGTH;
        private int _count = 0;

        public int Count
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);
                return _count;
            }
        }
        public bool Empty => (Count == 0);

        public Set() { }

        ~Set() => System.Diagnostics.Debug.Assert(false);

        private int Hash(K key)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            /*Debug.Assert(key != null);*/
            return System.Math.Abs(key.GetHashCode() * _C);
        }

        private void Resize(int newLength)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);

            bool[] oldFlags = _flags;
            K[] oldKeys = _keys;
            int oldLength = _length;

            bool[] newFlags = new bool[newLength];
            K[] newKeys = new K[newLength];

            for (int i = 0; i < oldLength; ++i)
            {
                if (!oldFlags[i])
                    continue;

                K key = oldKeys[i];
                int hash = Hash(key);
                for (int j = 0; j < newLength; ++j)
                {
                    int index = (hash + j) % newLength;

                    if (newFlags[index])
                        continue;

                    newFlags[index] = true;
                    newKeys[index] = key;

                    break;
                }

            }

            _flags = newFlags;
            _keys = newKeys;
            _length = newLength;

            oldFlags = null;
            oldKeys = null;

        }

        public void Insert(K key)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);

            int hash = Hash(key);
            for (int i = 0; i < _length; ++i)
            {
                int index = (hash + i) % _length;

                if (_flags[index])
                {
                    if (_keys[index].Equals(key))
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    continue;
                }

                _flags[index] = true;
                _keys[index] = key;
                _count++;

                float factor = (float)_count / (float)_length;
                if (factor >= _LOAD_FACTOR)
                {
                    Resize(_length * _EXPENSION_FACTOR);
                }

                break;
            }

        }

        private bool CanShift(int targetIndex, int currentIndex, int originIndex)
        {
            return (targetIndex < currentIndex && currentIndex < originIndex) ||
                (originIndex < targetIndex && targetIndex < currentIndex) ||
                (currentIndex < originIndex && originIndex < targetIndex) ||
                (originIndex == targetIndex);
        }

        public void Extract(K key)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);

            if (_count == 0)
            {
                System.Diagnostics.Debug.Assert(false);
            }

            int targetIndex = -1, nextI = -1;
            int hash = Hash(key);
            for (int i = 0; i < _length; ++i)
            {
                int index = (hash + i) % _length;

                if (!_flags[index])
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (!_keys[index].Equals(key))
                {
                    continue;
                }

                _count--;

                if (_MIN_LENGTH < _length)
                {
                    int reducedLength = _length / _EXPENSION_FACTOR;
                    float factor = (float)_count / (float)reducedLength;
                    if (factor < _LOAD_FACTOR)
                    {
                        _flags[index] = false;
                        Resize(reducedLength);

                        return;
                    }
                }

                targetIndex = index;
                nextI = i + 1;

                break;
            }

            System.Diagnostics.Debug.Assert(targetIndex >= 0);
            System.Diagnostics.Debug.Assert(nextI > 0);
            for (int i = nextI; i < _length; ++i)
            {
                int index = (hash + i) % _length;

                if (!_flags[index])
                {
                    break;
                }

                K shiftedKey = _keys[index];
                int originIndex = Hash(shiftedKey) % _length;
                if (!CanShift(targetIndex, index, originIndex))
                {
                    continue;
                }

                _keys[targetIndex] = shiftedKey;

                targetIndex = index;
            }

            _flags[targetIndex] = false;

            return;
        }

        public bool Contains(K key)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);
            if (_count == 0)
            {
                return false;
            }

            int hash = Hash(key);
            for (int i = 0; i < _length; ++i)
            {
                int index = (hash + i) % _length;

                if (!_flags[index])
                {
                    return false;
                }

                if (!_keys[index].Equals(key))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public void Reset()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _flags = new bool[_MIN_LENGTH];
            _keys = new K[_MIN_LENGTH];
            _length = _MIN_LENGTH;
            _count = 0;
        }

        public K[] Flush()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);

            if (_count == 0)
            {
                return [];
            }

            var keys = new K[_count];

            int i = 0;
            for (int j = 0; j < _length; ++j)
            {
                if (!_flags[j])
                {
                    continue;
                }

                keys[i++] = _keys[j];

                if (i == _count)
                {
                    break;
                }
            }

            _flags = new bool[_MIN_LENGTH];
            _keys = new K[_MIN_LENGTH];
            _length = _MIN_LENGTH;
            _count = 0;

            return keys;
        }

        public System.Collections.Generic.IEnumerable<K> GetKeys()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_flags.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_keys.Length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_length >= _MIN_LENGTH);
            System.Diagnostics.Debug.Assert(_count >= 0);

            if (_count == 0)
                yield break;

            int i = 0;
            for (int j = 0; j < _length; ++j)
            {
                if (!_flags[j]) continue;

                yield return _keys[j];

                if (++i == _count) break;
            }

        }

        public void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release reousrces.
            _flags = null;
            _keys = null;

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }
}
