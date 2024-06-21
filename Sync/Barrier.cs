﻿
namespace Sync
{
    public sealed class Barrier : System.IDisposable
    {
        private bool _disposed = false;

        private readonly int N;
        private int _count;

        private readonly Locker Locker;  // Disposable
        private readonly Cond Cond;  // Disposable

        public Barrier(int n)
        {
            N = n;
            _count = n;

            Locker = new Locker();
            Cond = new Cond(Locker);
        }

        ~Barrier() => System.Diagnostics.Debug.Assert(false);

        public void SignalAndWait()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            Locker.Hold();

            System.Diagnostics.Debug.Assert(_count > 0 && _count <= N);
            if (_count > 1)
            {
                --_count;
                Cond.Wait();
            }
            else
            {
                System.Diagnostics.Debug.Assert(_count == 1);

                _count = 0;
                Cond.Broadcast();
            }

            Locker.Release();
        }

        public void Reset()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_count == 0);
            _count = N;
        }

        public void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(false);

            // Release resources.
            Locker.Dispose();
            Cond.Dispose();

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }
}
