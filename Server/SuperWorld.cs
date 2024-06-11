using Protocol;

namespace Server
{
    internal sealed class SuperWorld : World
    {
        private bool _disposed = false;

        public SuperWorld() : base(new(0, 101, 0), new(0,0)) { }

        ~SuperWorld() => System.Diagnostics.Debug.Assert(false);

        public override bool CanJoinWorld()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return true;
        }

        protected override bool DetermineToDespawnPlayerOnDisconnect()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return false;
        }

        public override void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

    }
}
