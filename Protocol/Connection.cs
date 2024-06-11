
using Containers;

namespace Protocol
{
    public sealed class Connection : System.IDisposable
    {

        private sealed class LoadingHelper : System.IDisposable
        {
            private bool _disposed = false;

            private const int _MAX_LOAD_COUNT = 7;
            private const int _MAX_SEARCH_COUNT = 103;

            private readonly Set<ChunkLocation> _LOADED_CHUNKS = new();  // Disposable

            private ChunkGrid? _grid;
            private ChunkLocation _c;
            private int _d = - 1;

            private int _x, _z, _layer, _n;

            public LoadingHelper() { }

            ~LoadingHelper() => System.Diagnostics.Debug.Assert(false);

            public void Load(
                Queue<ChunkLocation> newChunks, Queue<ChunkLocation> outOfRangeChunks, 
                ChunkLocation locCenter, int d)
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                System.Diagnostics.Debug.Assert(d > 0);
                System.Diagnostics.Debug.Assert(_MAX_LOAD_COUNT > 0);
                System.Diagnostics.Debug.Assert(_MAX_SEARCH_COUNT >= _MAX_LOAD_COUNT);

                if (!locCenter.Equals(_c) || (d != _d))
                {
                    ChunkGrid grid = ChunkGrid.Generate(locCenter, d);
                    
                    if (_grid != null)
                    {
                        System.Diagnostics.Debug.Assert(!_grid.Equals(grid));

                        // TODO: Refactoring without brute-force approch.
                        foreach (ChunkLocation p in _grid.GetLocations())
                        {
                            if (grid.Contains(p))
                            {
                                continue;
                            }

                            if (_LOADED_CHUNKS.Contains(p))
                            {
                                _LOADED_CHUNKS.Extract(p);
                                outOfRangeChunks.Enqueue(p);
                            }
                        }
                    }
                    
                    _grid = grid;
                    _d = d;
                    _c = locCenter;

                    _layer = 0;
                    _n = 0;

                }

                if (_layer > d)
                {
                    return;
                }

                {
                    int i = 0, j = 0;

                    int w;
                    ChunkLocation p;

                    do
                    {
                        w = _layer * 2;

                        System.Diagnostics.Debug.Assert(_layer >= 0);

                        if (_layer == 0)
                        {
                            p = locCenter;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(_n >= 0);
                            if (_n < w)
                            {
                                p = new(--_x, _z);
                            }
                            else if (_n < w * 2)
                            {
                                p = new(_x, --_z);
                            }
                            else if (_n < w * 3)
                            {
                                p = new(++_x, _z);
                            }
                            else if (_n < w * 4)
                            {
                                p = new(_x, ++_z);
                            }
                            else
                            {
                                throw new System.NotImplementedException();
                            }

                            ++_n;
                        }

                        if (!_LOADED_CHUNKS.Contains(p))
                        {
                            newChunks.Enqueue(p);
                            _LOADED_CHUNKS.Insert(p);
                            ++i;
                        }

                        System.Diagnostics.Debug.Assert(_n <= w * 4);
                        if (_n == w * 4)
                        {
                            ++_layer;
                            _n = 0;

                            _x = locCenter.X + _layer; _z = locCenter.Z + _layer;
                        }

                        if (_layer > d)
                        {
                            break;
                        }

                        System.Diagnostics.Debug.Assert(j <= _MAX_SEARCH_COUNT);
                        if (++j == _MAX_SEARCH_COUNT)
                        {
                            break;
                        }

                    } while (i < _MAX_LOAD_COUNT);
                }

                System.Diagnostics.Debug.Assert(_LOADED_CHUNKS.Count <= (d + d + 1) * (d + d + 1));

            }

            public void Dispose()
            {
                // Assertions.
                System.Diagnostics.Debug.Assert(!_disposed);

                // Release resources.
                _LOADED_CHUNKS.Dispose();

                // Finish.
                System.GC.SuppressFinalize(this);
                _disposed = true;
            }

        }

        private sealed class TeleportationRecord
        {
            private const long _MAX_TICKS = 20;  // 1 seconds, 20 ticks

            public readonly int _payload;
            private long _ticks = 0;

            private readonly Vector _p;
            public Vector Position => _p;

            public TeleportationRecord(int payload, Vector p)
            {
                _payload = payload;
                _p = p;
            }

            public void Confirm(int payload)
            {
                if (payload != _payload)
                    throw new UnexpectedValueException("ConfirmSelfPlayerTeleportationPacket.Payload");
            }

            public void Update()
            {
                System.Diagnostics.Debug.Assert(_ticks >= 0);

                if (_ticks++ > _MAX_TICKS)
                {
                    throw new TeleportationConfirmTimeoutException();
                }

            }

        }

        private sealed class KeepAliveRecord
        {
            private const long TickLimit = 20 * 30;  // 30 seconds, 20 * 30 ticks

            public readonly long _payload;
            private long _ticks = 0;

            public KeepAliveRecord(long payload)
            {
                _payload = payload;
            }

            public void Confirm(long payload)
            {
                if (payload != _payload)
                    throw new UnexpectedValueException("ResponseKeepAlivePacketId.Payload");
            }

            public void Update()
            {
                System.Diagnostics.Debug.Assert(_ticks >= 0);

                if (_ticks++ > TickLimit)
                {
                    throw new ResponseKeepAliveTimeoutException();
                }

            }

        }

        private bool _disposed = false;

        private readonly Client _CLIENT;  // Dispoasble

        private bool _init = false;

        private bool _disconnected = false;

        private readonly Queue<LoadChunkPacket> _LOAD_CHUNK_PACKETS = new();  // Dispoasble
        private readonly Queue<ClientboundPlayingPacket> _OUT_PACKETS = new();  // Dispoasble

        private const int _MAX_ENTITY_RENDER_DISTANCE = 7;
        private const int _MIN_RENDER_DISTANCE = 2, _MAX_RENDER_DISTANCE = 32;
        private int _dEntityRendering = _MIN_RENDER_DISTANCE;
        private int _dChunkRendering = _MIN_RENDER_DISTANCE;

        private readonly EntityRenderer _ENTITY_RENDERER;
        private readonly SelfPlayerRenderer _SELF_RENDERER;

        private readonly LoadingHelper _LOADING_HELPER = new();  // Dispoasble
        private readonly Queue<TeleportationRecord> _TELEPORTATION_RECORDS = new();  // Dispoasble
        private KeepAliveRecord? _keepAliveRecord = null;

        private Window _window;  // disposable

        internal Connection(World world, Player player, Client client)
        {
            System.Diagnostics.Debug.Assert(_MAX_ENTITY_RENDER_DISTANCE >= _MIN_RENDER_DISTANCE);
            System.Diagnostics.Debug.Assert(_MAX_RENDER_DISTANCE >= _MAX_ENTITY_RENDER_DISTANCE);

            _CLIENT = client;

            _SELF_RENDERER = new(_OUT_PACKETS, client);

            using Buffer buffer = new();

            _window = new Window(_OUT_PACKETS, player._selfInventory);

            ChunkLocation loc = ChunkLocation.Generate(player.Position);
            _ENTITY_RENDERER = new EntityRenderer(player.Id, _OUT_PACKETS, loc, _dEntityRendering);
        }

        ~Connection() => System.Diagnostics.Debug.Assert(false);
        
        /*private void StartInitProcess(Buffer buffer, World world, Player player)
        {
            if (_initLevel == 0)
            {
                *//*Console.WriteLine("JoinGame!");*//*

                System.Diagnostics.Debug.Assert(_renderDistance == -1);

                // TODO: If already player exists, use id of that player object, not new alloc id.
                JoinGamePacket packet = new(player.Id, 0, 0, 0, "default", false);  // TODO
                packet.Write(buffer);
                _CLIENT.Send(buffer);

                _initLevel++;
            }

            if (_initLevel == 1)
            {
                *//*Console.WriteLine("ClientSettings!");*//*

                System.Diagnostics.Debug.Assert(_renderDistance == -1);

                _CLIENT.Recv(buffer);

                int packetId = buffer.ReadInt(true);
                if (ServerboundPlayingPacket.SetClientSettingsPacketId != packetId)
                    throw new UnexpectedPacketException();

                SetClientSettingsPacket packet = SetClientSettingsPacket.Read(buffer);

                if (buffer.Size > 0)
                    throw new BufferOverflowException();

                _renderDistance = packet.RenderDistance;
                if (_renderDistance < _MIN_RENDER_DISTANCE ||
                    _renderDistance > _MAX_RENDER_DISTANCE)
                    throw new UnexpectedValueException("SetClientSettingsPacket.RenderDistance");

                _initLevel++;
            }

            if (_initLevel == 2)
            {
                *//*Console.WriteLine("PluginMessage!");*//*

                System.Diagnostics.Debug.Assert(_renderDistance >= _MIN_RENDER_DISTANCE);
                System.Diagnostics.Debug.Assert(_renderDistance <= _MAX_RENDER_DISTANCE);

                _CLIENT.Recv(buffer);

                int packetId = buffer.ReadInt(true);
                if (0x09 != packetId)
                    throw new UnexpectedPacketException();

                buffer.Flush();

                if (buffer.Size > 0)
                    throw new BufferOverflowException();

                _initLevel++;
            }

            System.Diagnostics.Debug.Assert(IsInit);

            System.Diagnostics.Debug.Assert(_window == null);
            _window = new(_OUT_PACKETS, player._selfInventory);
        }*/

        private void RecvDataAndHandle(
            Buffer buffer, long serverTicks, World world, Player player)
        {
            if (_disconnected)
            {
                throw new DisconnectedClientException();
            }

            _CLIENT.Recv(buffer);

            int packetId = buffer.ReadInt(true);
            switch (packetId)
            {
                default:
                    System.Console.WriteLine($"packetId: 0x{packetId:X}");
                    /*throw new NotImplementedException();*/
                    buffer.Flush();
                    break;
                case ServerboundPlayingPacket.ConfirmSelfPlayerTeleportationPacketId:
                    {
                        ConfirmSelfPlayerTeleportationPacket packet = ConfirmSelfPlayerTeleportationPacket.Read(buffer);

                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            throw new UnexpectedPacketException();
                        }

                        TeleportationRecord record = _TELEPORTATION_RECORDS.Dequeue();
                        record.Confirm(packet.Payload);

                        Vector p = new(record.Position.X, record.Position.Y, record.Position.Z);
                        ChunkLocation locChunk = ChunkLocation.Generate(p);
                        _ENTITY_RENDERER.Update(locChunk);
                    }
                    break;
                case ServerboundPlayingPacket.SetClientSettingsPacketId:
                    {
                        SetClientSettingsPacket packet = SetClientSettingsPacket.Read(buffer);

                        int d = packet.RenderDistance;
                        
                        if (d < _MIN_RENDER_DISTANCE || d > _MAX_RENDER_DISTANCE)
                        {
                            throw new UnexpectedValueException("SetClientSettingsPacket.RenderDistance");
                        }

                        _dChunkRendering = d;
                        _dEntityRendering = System.Math.Min(d, _MAX_ENTITY_RENDER_DISTANCE);

                        _ENTITY_RENDERER.Update(_dEntityRendering);
                    }
                    break;
                case ServerboundPlayingPacket.ServerboundConfirmTransactionPacketId:
                    {
                        ServerboundConfirmTransactionPacket packet =
                            ServerboundConfirmTransactionPacket.Read(buffer);

                        System.Console.WriteLine(
                            $"WindowId: {packet.WindowId}, " +
                            $"ActionNumber: {packet.ActionNumber}, " +
                            $"Accepted: {packet.Accepted}, ");

                        throw new System.NotImplementedException();
                    }
                    break;
                case ServerboundPlayingPacket.ClickWindowPacketId:
                    {
                        ClickWindowPacket packet = ClickWindowPacket.Read(buffer);

                        {
                            System.Console.WriteLine();
                            System.Console.WriteLine(
                                $"WindowId: {packet.WINDOW_ID}, " +
                                $"SlotNumber: {packet.SLOT}, " +
                                $"ButtonNumber: {packet.BUTTON}, " +
                                $"ActionNumber: {packet.ACTION}, " +
                                $"ModeNumber: {packet.MODE}, " +
                                $"SlotData.Id: {packet.SLOT_DATA.Id}, " +
                                $"SlotData.Count: {packet.SLOT_DATA.Count}, ");
                        }
                        System.Diagnostics.Debug.Assert(_window != null);

                        if (packet.MODE == 5)
                        {
                            Queue<ClickWindowPacket> packets = new Queue<ClickWindowPacket>();
                            while (true)
                            {
                                _CLIENT.Recv(buffer);
                                packetId = buffer.ReadInt(true);
                                if (packetId != ServerboundPlayingPacket.ClickWindowPacketId)
                                {
                                    throw new UnexpectedValueException("ClickWindowPacketId");
                                }

                                ClickWindowPacket dragPacket = ClickWindowPacket.Read(buffer);
                                {
                                    System.Console.WriteLine();
                                    System.Console.WriteLine(
                                        $"WindowId: {dragPacket.WINDOW_ID}, " +
                                        $"SlotNumber: {dragPacket.SLOT}, " +
                                        $"ButtonNumber: {dragPacket.BUTTON}, " +
                                        $"ActionNumber: {dragPacket.ACTION}, " +
                                        $"ModeNumber: {dragPacket.MODE}, " +
                                        $"SlotData.Id: {dragPacket.SLOT_DATA.Id}, " +
                                        $"SlotData.Count: {dragPacket.SLOT_DATA.Count}, ");
                                }

                                if (dragPacket.SLOT == -999)
                                {
                                    break;
                                }

                                packets.Enqueue(dragPacket);
                            }

                            switch (packet.BUTTON)
                            {
                                case 0:
                                    _window.HandleLeftDrag(packets, _OUT_PACKETS);
                                    break;
                                case 4:
                                    _window.HandleRightDrag(packets, _OUT_PACKETS);
                                    break;
                                case 8:
                                    _window.HandleMiddleDrag(packets, _OUT_PACKETS);
                                    break;
                            }
                            packets.Dispose();
                        }
                        else
                        {
                            _window.Handle(packet, _OUT_PACKETS);
                        }

                        _OUT_PACKETS.Enqueue(new ClientboundConfirmTransactionPacket(
                                (sbyte)packet.WINDOW_ID, packet.ACTION, true));

                        /*{
                            SlotData slotData = new(280, 1, 0);
                            _outPackets.Enqueue(new SetSlotPacket(0, 27, slotData.WriteData()));
                        }
                        {
                            SlotData slotData = new();
                            _outPackets.Enqueue(new SetSlotPacket(-1, 27, slotData.WriteData()));
                        }*/
                    }
                    break;
                case ServerboundPlayingPacket.ServerboundCloseWindowPacketId:
                    {
                        ServerboundCloseWindowPacket packet =
                            ServerboundCloseWindowPacket.Read(buffer);

                        if (packet.WindowId < 0)
                        {
                            throw new UnexpectedValueException($"ClickWindowPacket.WindowId");
                        }

                        /*_outPackets.Enqueue(new SetSlotPacket(-1, 0, new(280, 10)));*/

                        System.Diagnostics.Debug.Assert(_window != null);
                        _window.ResetWindow(packet.WindowId, _OUT_PACKETS);
                    }
                    break;
                case 0x09:
                    {
                        buffer.Flush();
                    }
                    break;
                case ServerboundPlayingPacket.ResponseKeepAlivePacketId:
                    {
                        ResponseKeepAlivePacket packet = ResponseKeepAlivePacket.Read(buffer);

                        if (_keepAliveRecord == null)
                        {
                            throw new UnexpectedPacketException();
                        }

                        _keepAliveRecord.Confirm(packet.Payload);
                        _keepAliveRecord = null;

                        world.KeepAlivePlayer(serverTicks, player);

                    }
                    break;
                case ServerboundPlayingPacket.PlayerPacketId:
                    {
                        PlayerPacket packet = PlayerPacket.Read(buffer);

                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            player.Control(packet.OnGround);
                        }

                    }
                    break;
                case ServerboundPlayingPacket.PlayerPositionPacketId:
                    {
                        PlayerPositionPacket packet = PlayerPositionPacket.Read(buffer);
                
                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            Vector p = new(packet.X, packet.Y, packet.Z);
                            player.Control(p, packet.OnGround);

                            ChunkLocation locChunk = ChunkLocation.Generate(p);
                            _ENTITY_RENDERER.Update(locChunk);
                        }

                    }
                    break;
                case ServerboundPlayingPacket.PlayerPosAndLookPacketId:
                    {
                        PlayerPosAndLookPacket packet = PlayerPosAndLookPacket.Read(buffer);

                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            Vector p = new(packet.X, packet.Y, packet.Z);
                            player.Control(p, packet.OnGround);

                            ChunkLocation locChunk = ChunkLocation.Generate(p);
                            _ENTITY_RENDERER.Update(locChunk);
                        }

                        player.Rotate(new(packet.Yaw, packet.Pitch));
                    }
                    break;
                case ServerboundPlayingPacket.PlayerLookPacketId:
                    {
                        PlayerLookPacket packet = PlayerLookPacket.Read(buffer);

                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            player.Control(packet.OnGround);
                        }

                        player.Rotate(new(packet.Yaw, packet.Pitch));
                    }
                    break;
                case ServerboundPlayingPacket.EntityActionPacketId:
                    {
                        EntityActionPacket packet = EntityActionPacket.Read(buffer);

                        switch (packet.ActionId)
                        {
                            default:
                                throw new UnexpectedValueException("EntityAction.ActoinId");
                            case 0:
                                /*System.Console.Write("Seanking!");*/
                                if (player.IsSneaking)
                                {
                                    throw new UnexpectedValueException("EntityActionPacket.ActionId");
                                }

                                player.Sneak();
                                break;
                            case 1:
                                /*System.Console.Write("Unseanking!");*/
                                if (!player.IsSneaking)
                                {
                                    throw new UnexpectedValueException("EntityActionPacket.ActionId");
                                }

                                player.Unsneak();
                                break;
                            case 3:
                                if (player.IsSprinting)
                                {
                                    throw new UnexpectedValueException("EntityActionPacket.ActionId");
                                }

                                player.Sprint();
                                break;
                            case 4:
                                if (!player.IsSprinting)
                                {
                                    throw new UnexpectedValueException("EntityActionPacket.ActionId");
                                }

                                player.Unsprint();
                                break;
                        }

                        if (packet.JumpBoost > 0)
                        {
                            throw new UnexpectedValueException("EntityActionPacket.JumpBoost");
                        }

                    }
                    break;
            }

        }

        /// <exception cref="DisconnectedClientException">TODO: Why it's thrown.</exception>
        public void Control(
            long serverTicks, World world, Player player)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (!_init)
            {
                return;
            }

            if (serverTicks == 100)  // 5 seconds
            {
                System.Diagnostics.Debug.Assert(_window != null);
                _window.OpenWindowWithPublicInventory(
                    _OUT_PACKETS,
                    player._selfInventory,
                    world._Inventory);
            }

            using Buffer buffer = new();

            try
            {
                try
                {
                    try
                    {
                        while (true)
                        {
                            RecvDataAndHandle(buffer, serverTicks, world, player);
                        }
                    }
                    catch (TryAgainException)
                    {

                    }

                    foreach (TeleportationRecord record in _TELEPORTATION_RECORDS.GetValues())
                    {
                        record.Update();
                    }

                    if (_keepAliveRecord != null)
                    {
                        _keepAliveRecord.Update();
                    }
                }
                catch (UnexpectedClientBehaviorExecption e)
                {
                    // TODO: send disconnected message to client.

                    System.Console.WriteLine(e.Message);

                    throw new DisconnectedClientException();
                }
                
            }
            catch (DisconnectedClientException)
            {
                buffer.Flush();

                world.DisconnectPlayer(player);
                player.Disconnect();

                _disconnected = true;

                throw;
            }
            
        }

        private void LoadWorld(World world, Player player)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_dChunkRendering >= _MIN_RENDER_DISTANCE);
            System.Diagnostics.Debug.Assert(_dChunkRendering <= _MAX_RENDER_DISTANCE);
            System.Diagnostics.Debug.Assert(_dEntityRendering >= _MIN_RENDER_DISTANCE);
            System.Diagnostics.Debug.Assert(_dEntityRendering <= _MAX_ENTITY_RENDER_DISTANCE);

            ChunkLocation locCenter = ChunkLocation.Generate(player.Position);

            ChunkGrid grid = ChunkGrid.Generate(locCenter, _dEntityRendering);
            foreach (ChunkLocation loc in grid.GetLocations())
            {
                foreach (Entity entity in world.GetEntities(loc))
                {
                    if (entity.Id == player.Id)
                    {
                        continue;
                    }

                    entity.ApplyRenderer(_OUT_PACKETS, _ENTITY_RENDERER);
                }
            }

            using Queue<ChunkLocation> newChunkPositions = new();
            using Queue<ChunkLocation> outOfRangeChunks = new();

            _LOADING_HELPER.Load(newChunkPositions, outOfRangeChunks, locCenter, _dChunkRendering);

            int mask; byte[] data;
            while (!newChunkPositions.Empty)
            {
                ChunkLocation locChunk = newChunkPositions.Dequeue();

                (mask, data) = world.GetChunkData(locChunk);

                _LOAD_CHUNK_PACKETS.Enqueue(new LoadChunkPacket(
                    locChunk.X, locChunk.Z, true, mask, data));
            }

            while (!outOfRangeChunks.Empty)
            {
                ChunkLocation p = outOfRangeChunks.Dequeue();

                _OUT_PACKETS.Enqueue(new UnloadChunkPacket(p.X, p.Z));
            }

        }
        
        public void Render(World world, Player player)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_disconnected);

            using Buffer buffer = new();

            try
            {
                if (!_init)
                {
                    JoinGamePacket packet = new(player.Id, 0, 0, 0, "default", false);
                    packet.Write(buffer);
                    _CLIENT.Send(buffer);

                    world.ConnectPlayer(player, _OUT_PACKETS);
                    player.Connect(_SELF_RENDERER);

                    _init = true;
                }

                LoadWorld(world, player);

                while (!_LOAD_CHUNK_PACKETS.Empty)
                {
                    LoadChunkPacket packet = _LOAD_CHUNK_PACKETS.Dequeue();

                    packet.Write(buffer);
                    _CLIENT.Send(buffer);

                    System.Diagnostics.Debug.Assert(buffer.Empty);
                }

                while (!_OUT_PACKETS.Empty)
                {
                    ClientboundPlayingPacket packet = _OUT_PACKETS.Dequeue();

                    if (packet is TeleportSelfPlayerPacket teleportPacket)
                    {
                        Vector p = new(teleportPacket.X, teleportPacket.Y, teleportPacket.Z);

                        TeleportationRecord report = new(teleportPacket.Payload, p);
                        _TELEPORTATION_RECORDS.Enqueue(report);
                    }
                    else if (packet is ClientboundCloseWindowPacket)
                    {
                        System.Diagnostics.Debug.Assert(_window != null);
                        _window.ResetWindowForcibly(player._selfInventory, _OUT_PACKETS, false);
                    }
                    else if (packet is RequestKeepAlivePacket requestKeepAlivePacket)
                    {
                        // TODO: save payload and check when recived.
                        System.Diagnostics.Debug.Assert(_keepAliveRecord == null);
                        _keepAliveRecord = new KeepAliveRecord(requestKeepAlivePacket.Payload);
                    }
                    /*else if (packet is DestroyEntitiesPacket destroyEntitiesPacket)
                    {
                        int[] entityIds = destroyEntitiesPacket.EntityIds;
                        foreach (int id in entityIds)
                        {
                            _ENTITY_TO_RENDERERS.Extract(id);
                        }
                    }*/

                    packet.Write(buffer);
                    _CLIENT.Send(buffer);

                    System.Diagnostics.Debug.Assert(buffer.Empty);
                }

                System.Diagnostics.Debug.Assert(_OUT_PACKETS.Empty);
            }
            catch (DisconnectedClientException)
            {
                buffer.Flush();

                _disconnected = true;
            }

        }

        public void Flush(World world)
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_disconnected);

            _ENTITY_RENDERER.Disconnect();

            _window.Flush(world);
        }

        public void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_disconnected);

            // Release Resources.
            _CLIENT.Dispose();

            _LOAD_CHUNK_PACKETS.Dispose();
            _OUT_PACKETS.Dispose();

            _LOADING_HELPER.Dispose();
            _TELEPORTATION_RECORDS.Dispose();
            _keepAliveRecord = null;

            _window.Dispose();

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }
}
