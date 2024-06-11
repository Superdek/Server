

using Containers;

namespace Protocol
{
    internal class PlayerList : System.IDisposable
    {
        private class Info
        {
            public readonly System.Guid UniqueId;
            public readonly string Username;
            private long _laytencyInTicks = -1;
            public long LaytencyInMilliseconds
            {
                get
                {
                    System.Diagnostics.Debug.Assert(_laytencyInTicks <= long.MaxValue / 50);
                    return _laytencyInTicks * 50;
                }
            }

            public Info(System.Guid uniqueId, string username)
            {
                UniqueId = uniqueId;
                Username = username;
            }

            public void Connect()
            {
                System.Diagnostics.Debug.Assert(_laytencyInTicks == -1);
                _laytencyInTicks = 0;
            }

            public void UpdateLaytency(long ticks)
            {
                System.Diagnostics.Debug.Assert(ticks > 0);
                System.Diagnostics.Debug.Assert(_laytencyInTicks >= 0);
                _laytencyInTicks = ticks;
            }

            public void Disconnect()
            {
                System.Diagnostics.Debug.Assert(_laytencyInTicks == 0);
                _laytencyInTicks = -1;
            }

        }

        private bool _disposed = false;

        private readonly Table<System.Guid, Info> _INFORS = new();
        private readonly Table<System.Guid, Queue<ClientboundPlayingPacket>> _RENDERERS = new();

        internal PlayerList() { }

        ~PlayerList() => System.Diagnostics.Debug.Assert(false);

        private void RenderToAddPlayer(Info item)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            long ms = item.LaytencyInMilliseconds;
            System.Diagnostics.Debug.Assert(ms < 0);
            System.Diagnostics.Debug.Assert(ms <= int.MaxValue);

            foreach (var renderer in _RENDERERS.GetValues())
            {
                renderer.Enqueue(
                    new AddPlayerListItemPacket(item.UniqueId, item.Username, (int)ms));
            }
        }

        private void RenderToAddAllPlayers(Queue<ClientboundPlayingPacket> renderer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            foreach (Info info in _INFORS.GetValues())
            {
                long ms = info.LaytencyInMilliseconds;
                System.Diagnostics.Debug.Assert(ms <= int.MaxValue);

                renderer.Enqueue(new AddPlayerListItemPacket(
                    info.UniqueId, info.Username, (int)ms));
            }
        }

        private void RenderToRemovePlayer(System.Guid uniqueId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            foreach (var renderer in _RENDERERS.GetValues())
            {
                renderer.Enqueue(new RemovePlayerListItemPacket(uniqueId));
            }
        }

        private void RenderToUpdateLatency(System.Guid uniqueId, long ms)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(ms <= int.MaxValue);

            foreach (var renderer in _RENDERERS.GetValues())
            {
                System.Diagnostics.Debug.Assert(ms <= int.MaxValue);
                renderer.Enqueue(
                    new UpdatePlayerListItemLatencyPacket(uniqueId,(int)ms));
            }
        }

        private bool IsDisconnected(System.Guid uniqueId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return !_RENDERERS.Contains(uniqueId);
        }

        public void AddPlayer(System.Guid uniqueId, string username)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(IsDisconnected(uniqueId));

            Info item = new(uniqueId, username);
            System.Diagnostics.Debug.Assert(!_INFORS.Contains(uniqueId));
            _INFORS.Insert(uniqueId, item);

            RenderToAddPlayer(item);
        }

        public void ConnectPlayer(
            System.Guid uniqueId,
            Queue<ClientboundPlayingPacket> renderer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            /*System.Console.WriteLine($"UniqueId: {uniqueId} in Connect");*/

            System.Diagnostics.Debug.Assert(IsDisconnected(uniqueId));
            _RENDERERS.Insert(uniqueId, renderer);

            System.Diagnostics.Debug.Assert(_INFORS.Contains(uniqueId));
            Info info = _INFORS.Lookup(uniqueId);
            
            System.Diagnostics.Debug.Assert(uniqueId == info.UniqueId);
            info.Connect();

            RenderToAddAllPlayers(renderer);

            System.Diagnostics.Debug.Assert(uniqueId == info.UniqueId);
            RenderToUpdateLatency(info.UniqueId, info.LaytencyInMilliseconds);
        }

        public void UpdatePlayerLaytency(System.Guid uniqueId, long ticks)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_INFORS.Contains(uniqueId));
            Info item = _INFORS.Lookup(uniqueId);
            item.UpdateLaytency(ticks);

            System.Diagnostics.Debug.Assert(uniqueId == item.UniqueId);
            RenderToUpdateLatency(item.UniqueId, item.LaytencyInMilliseconds);
        }

        public void DisconnectPlayer(System.Guid uniqueId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!IsDisconnected(uniqueId));
            _RENDERERS.Extract(uniqueId);

            System.Diagnostics.Debug.Assert(_INFORS.Contains(uniqueId));
            Info info = _INFORS.Lookup(uniqueId);
            info.Disconnect();

            System.Diagnostics.Debug.Assert(uniqueId == info.UniqueId);
            RenderToUpdateLatency(info.UniqueId, info.LaytencyInMilliseconds);
        }

        public void RemovePlayer(System.Guid uniqueId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(IsDisconnected(uniqueId));

            Info info = _INFORS.Extract(uniqueId);
            System.Diagnostics.Debug.Assert(info.UniqueId == uniqueId);

            RenderToRemovePlayer(uniqueId);
        }

        public void Dispose()
        {
            // Assertion.
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_INFORS.Empty);
            System.Diagnostics.Debug.Assert(_RENDERERS.Empty);

            // Release resources.
            _INFORS.Dispose();
            _RENDERERS.Dispose();

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }


    }

}
