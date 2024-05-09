﻿namespace Containers
{
    public class NumList : System.IDisposable
    {
        private bool _disposed = false;

        private const int _MinNum = 0;
        private const int _MaxNum = int.MaxValue;

        private class Node(int from, int to)
        {
            public int from = from, to = to;
            public Node? next = null;

        }

        private Node _nodeFirst;

        private int _count = 0;
        public int Count => _count;
        public bool Empty => (_count == 0);

        public NumList()
        {
            _nodeFirst = new(_MinNum, _MaxNum);
        }

        ~NumList() => Dispose(false);

        public int Alloc()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            int from = _nodeFirst.from, to = _nodeFirst.to;
            System.Diagnostics.Debug.Assert(from <= to);

            int num;

            if (from < to)
            {
                num = from++;
                _nodeFirst.from = from;
            }
            else
            {
                System.Diagnostics.Debug.Assert(from == to);

                num = from;
                Node? next = _nodeFirst.next;
                System.Diagnostics.Debug.Assert(next != null);
                _nodeFirst = next;
            }

            _count++;

            return num;
        }

        public void Dealloc(int num)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_nodeFirst != null);

            Node? prev;
            Node? current = _nodeFirst;

            int from = current.from,
                to = current.to;
            System.Diagnostics.Debug.Assert(from <= to);
            System.Diagnostics.Debug.Assert(!(from <= num && num <= to));

            if (num < from)
            {
                if (from > 0)
                {
                    if (num == (from - 1))
                    {
                        current.from--;
                    }
                    else
                    {
                        prev = new(num, num);
                        prev.next = current;
                        _nodeFirst = prev;
                    }
                }
                else
                    System.Diagnostics.Debug.Assert(false);
            }
            else
            {
                do
                {
                    System.Diagnostics.Debug.Assert(current.from <= current.to);
                    System.Diagnostics.Debug.Assert(!(current.from <= num && num <= current.to));
                    System.Diagnostics.Debug.Assert(current.to < num);

                    prev = current;
                    current = prev.next;
                    System.Diagnostics.Debug.Assert(current != null);
                }
                while (!(prev.to < num && num < current.from));

                to = prev.to;
                from = current.from;

                if ((to + 1) == (from - 1))
                {
                    System.Diagnostics.Debug.Assert((to + 1) == num);
                    prev.to = current.to;
                    prev.next = current.next;
                }
                else if ((to + 1) < num && num < (from - 1))
                {
                    Node between = new(num, num);
                    between.next = current;
                    prev.next = between;
                }
                else if ((to + 1) == num)
                {
                    System.Diagnostics.Debug.Assert((to + 1) + 1 < from);
                    prev.to++;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(to < (from - 1) - 1);
                    current.from--;
                }
            }

            _count--;

        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true) return;

            if (disposing == true)
            {
                // managed objects
                _nodeFirst = null;
            }

            // Release unmanaged objects

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

    }

    public class ConcurrentNumList : NumList
    {
        private readonly object _SharedResource = new();

        private bool _disposed = false;

        ~ConcurrentNumList() => Dispose(false);

        public new int Alloc()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedResource)
            {
                return base.Alloc();
            }
        }

        public new void Dealloc(int num)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedResource)
            {
                base.Dealloc(num);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing == true)
                {
                    // Release managed resources.
                }

                // Release unmanaged resources.

                _disposed = true;
            }

            base.Dispose(disposing);
        }

    }
}
