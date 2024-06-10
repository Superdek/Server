
using Containers;
using System;
using System.Numerics;
using System.Reflection;

namespace Protocol
{
    public sealed class Connection : System.IDisposable
    {

        private sealed class LoadingHelper : System.IDisposable
        {
            private bool _disposed = false;

            private const int _MAX_LOAD_COUNT = 7;
            private const int _MAX_SEARCH_COUNT = 103;

            private readonly Set<Chunk.Vector> _LOADED_CHUNKS = new();  // Disposable

            private Chunk.Grid? _grid;
            private Chunk.Vector _c;
            private int _d = - 1;

            private int _x, _z, _layer, _n;

            public LoadingHelper()
            {

            }

            ~LoadingHelper() => System.Diagnostics.Debug.Assert(false);

            public void Load(
                Queue<Chunk.Vector> newChunks,
                Queue<Chunk.Vector> outOfRangeChunks, 
                Chunk.Vector c, int d)
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                System.Diagnostics.Debug.Assert(d > 0);
                System.Diagnostics.Debug.Assert(_MAX_LOAD_COUNT > 0);
                System.Diagnostics.Debug.Assert(_MAX_SEARCH_COUNT >= _MAX_LOAD_COUNT);

                if (!c.Equals(_c) || (d != _d))
                {
                    Chunk.Grid grid = Chunk.Grid.Generate(c, d);
                    
                    if (_grid != null)
                    {
                        System.Diagnostics.Debug.Assert(!_grid.Equals(grid));

                        // TODO: Refactoring without brute-force approch.
                        foreach (Chunk.Vector p in _grid.GetVectors())
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
                    _c = c;

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
                    Chunk.Vector p;

                    do
                    {
                        w = _layer * 2;

                        System.Diagnostics.Debug.Assert(_layer >= 0);

                        if (_layer == 0)
                        {
                            p = c;
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

                            _x = c.X + _layer; _z = c.Z + _layer;
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

            private void Dispose(bool disposing)
            {
                System.Diagnostics.Debug.Assert(!_disposed);

                // Assertion

                if (disposing == true)
                {
                    // managed objects
                    _LOADED_CHUNKS.Dispose();

                }

                // unmanaged objects

                _disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                System.GC.SuppressFinalize(this);
            }

        }

        private sealed class TeleportationRecord
        {
            private const long TickLimit = 20;  // 1 seconds, 20 ticks

            public readonly int _payload;
            private long _ticks = 0;

            public TeleportationRecord(int payload)
            {
                _payload = payload;
            }

            public void Confirm(int payload)
            {
                if (payload != _payload)
                    throw new UnexpectedValueException("ConfirmSelfPlayerTeleportationPacket.Payload");
            }

            public void Update()
            {
                System.Diagnostics.Debug.Assert(_ticks >= 0);

                if (_ticks++ > TickLimit)
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

        private readonly int Id;

        private readonly Client _CLIENT;  // Dispoasble


        /*private int _initLevel = 0;
        private bool IsInit => _initLevel >= 3;*/
        private bool _init = false;

        private readonly Queue<LoadChunkPacket> _LOAD_CHUNK_PACKETS = new();  // Dispoasble
        private readonly Queue<ClientboundPlayingPacket> _OUT_PACKETS = new();  // Dispoasble

        private const int _MAX_ENTITY_RENDER_DISTANCE = 7;
        private const int _MIN_RENDER_DISTANCE = 2, _MAX_RENDER_DISTANCE = 32;
        private int _renderDistance = _MIN_RENDER_DISTANCE;

        private readonly LoadingHelper _LOADING_HELPER = new();
        private readonly Table<int, EntityRenderer> _ENTITY_TO_RENDERERS = new();  // Disposable

        private readonly SelfPlayerRenderer _SELF_RENDERER;  // Disposable

        private readonly Queue<TeleportationRecord> _TELEPORTATION_RECORDS = new();  // Dispoasble
        private KeepAliveRecord? _keepAliveRecord = null;

        private Window? _window = null;  // disposable

        internal Connection(Client client)
        {
            Id = client.LocalPort;

            _CLIENT = client;

            _SELF_RENDERER = new(_OUT_PACKETS, client);
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
                    }
                    break;
                case ServerboundPlayingPacket.SetClientSettingsPacketId:
                    {
                        SetClientSettingsPacket packet = SetClientSettingsPacket.Read(buffer);

                        if (_renderDistance != packet.RenderDistance)
                        {
                            _renderDistance = packet.RenderDistance;
                            if (_renderDistance < _MIN_RENDER_DISTANCE || _renderDistance > _MAX_RENDER_DISTANCE)
                            {
                                throw new UnexpectedValueException("SetClientSettingsPacket.RenderDistance");
                            }

                            int entityRenderDistance = System.Math.Min(_renderDistance, _MAX_ENTITY_RENDER_DISTANCE);
                            foreach (var renderer in _ENTITY_TO_RENDERERS.GetValues())
                            {
                                renderer.Update(entityRenderDistance);
                            }
                        }
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

                        world.KeepAlivePlayer(serverTicks, player.UniqueId);

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
                            Entity.Vector p = new(packet.X, packet.Y, packet.Z);
                            player.Control(p, packet.OnGround);

                            Chunk.Vector pChunk = Chunk.Vector.Convert(p);
                            foreach (var renderer in _ENTITY_TO_RENDERERS.GetValues())
                            {
                                renderer.Update(pChunk);
                            }
                        }

                    }
                    break;
                case ServerboundPlayingPacket.PlayerPosAndLookPacketId:
                    {
                        PlayerPosAndLookPacket packet = PlayerPosAndLookPacket.Read(buffer);

                        if (_TELEPORTATION_RECORDS.Empty)
                        {
                            Entity.Vector p = new(packet.X, packet.Y, packet.Z);
                            player.Control(p, packet.OnGround);

                            Chunk.Vector pChunk = Chunk.Vector.Convert(p);
                            foreach (var renderer in _ENTITY_TO_RENDERERS.GetValues())
                            {
                                renderer.Update(pChunk);
                            }
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
                                    throw new UnexpectedValueException("Entity.Sneaking");
                                }

                                player.Sneak();
                                break;
                            case 1:
                                /*System.Console.Write("Unseanking!");*/
                                if (!player.IsSneaking)
                                {
                                    throw new UnexpectedValueException("Entity.Sneaking");
                                }

                                player.Unsneak();
                                break;
                            case 3:
                                if (player.IsSprinting)
                                {
                                    throw new UnexpectedValueException("Entity.Sprinting");
                                }

                                player.Sprint();
                                break;
                            case 4:
                                if (!player.IsSprinting)
                                {
                                    throw new UnexpectedValueException("Entity.Sprinting");
                                }

                                player.Unsprint();
                                break;
                        }

                        if (packet.JumpBoost > 0)
                        {
                            throw new UnexpectedValueException("EntityAction.JumpBoost");
                        }

                    }
                    break;
            }

            if (!buffer.Empty)
                throw new BufferOverflowException();
        }

        /// <summary>
        /// TODO: Add description.
        /// </summary>
        /// <returns>TODO: Add description.</returns>
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
                    while (true)
                    {
                        RecvDataAndHandle(buffer, serverTicks, world, player);
                    }
                }
                catch (TryAgainException)
                {

                }
                catch (UnexpectedClientBehaviorExecption e)
                {
                    // TODO: send disconnected message to client.

                    System.Console.WriteLine(e.Message);

                    throw new DisconnectedClientException();
                }

                if (_renderDistance == -1)
                {
                    throw new UnexpectedPacketException();
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
            catch (DisconnectedClientException)
            {
                buffer.Flush();
                world.DisconnectPlayer(player);

                throw;
            }

            
        }

        private void LoadWorld(World world, Player player)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_renderDistance >= _MIN_RENDER_DISTANCE);
            System.Diagnostics.Debug.Assert(_renderDistance <= _MAX_RENDER_DISTANCE);

            Chunk.Vector c = Chunk.Vector.Convert(player.Position);
            /*System.Console.WriteLine($"c: {c}");*/

            {
                int renderDistance = System.Math.Min(_renderDistance, _MAX_ENTITY_RENDER_DISTANCE);

                using Queue<Entity> newEntities = new();

                Chunk.Grid grid = Chunk.Grid.Generate(c, renderDistance);
                foreach (Chunk.Vector p in grid.GetVectors())
                {
                    if (world.ContainsEntities(p))
                    {
                        foreach (Entity entity in world.GetEntities(p))
                        {
                            if (entity.Id == player.Id) continue;

                            if (_ENTITY_TO_RENDERERS.Contains(entity.Id)) continue;

                            newEntities.Enqueue(entity);

                            EntityRenderer renderer = entity.ApplyForRenderer(
                                _OUT_PACKETS, c, renderDistance);

                            _ENTITY_TO_RENDERERS.Insert(entity.Id, renderer);

                        }
                    }
                }

                while (!newEntities.Empty)
                {
                    Entity entity = newEntities.Dequeue();
                    System.Diagnostics.Debug.Assert(entity.Id != player.Id);

                    switch (entity)
                    {
                        default:
                            throw new System.NotImplementedException();
                        case Player spawnedPlayer:
                            {
                                byte flags = 0x00;

                                if (spawnedPlayer.IsSneaking)
                                    flags |= 0x02;
                                if (spawnedPlayer.IsSprinting)
                                    flags |= 0x08;

                                using EntityMetadata metadata = new();
                                metadata.AddByte(0, flags);

                                (byte x, byte y) = spawnedPlayer.Look.ConvertToPacketFormat();
                                _OUT_PACKETS.Enqueue(new SpawnNamedEntityPacket(
                                    spawnedPlayer.Id,
                                    spawnedPlayer.UniqueId,
                                    spawnedPlayer.Position.X, spawnedPlayer.Position.Y, spawnedPlayer.Position.Z,
                                    x, y,
                                    metadata.WriteData()));
                            }
                            break;
                        case ItemEntity itemEntity:
                            {
                                (byte x, byte y) = itemEntity.Look.ConvertToPacketFormat();
                                _OUT_PACKETS.Enqueue(new SpawnObjectPacket(
                                    itemEntity.Id, itemEntity.UniqueId,
                                    2,
                                    itemEntity.Position.X, itemEntity.Position.Y, itemEntity.Position.Z,
                                    x, y,
                                    1,
                                    0, 0, 0));

                                using EntityMetadata metadata = new();
                                metadata.AddBool(5, true);
                                metadata.AddSlotData(6, new SlotData(280, 1));
                                _OUT_PACKETS.Enqueue(new EntityMetadataPacket(
                                    itemEntity.Id, metadata.WriteData()));
                            }
                            break;
                    }
                }

            }

            {
                using Queue<Chunk.Vector> newChunkPositions = new();
                using Queue<Chunk.Vector> outOfRangeChunks = new();

                _LOADING_HELPER.Load(newChunkPositions, outOfRangeChunks, c, _renderDistance);

                int mask; byte[] data;
                while (!newChunkPositions.Empty)
                {
                    Chunk.Vector p = newChunkPositions.Dequeue();

                    if (world.ContainsChunk(p))
                    {
                        Chunk chunk = world.GetChunk(p);
                        (mask, data) = Chunk.Write(chunk);
                    }
                    else
                    {
                        (mask, data) = Chunk.Write();
                    }

                    _LOAD_CHUNK_PACKETS.Enqueue(new LoadChunkPacket(p.X, p.Z, true, mask, data));
                }

                while (!outOfRangeChunks.Empty)
                {
                    Chunk.Vector p = outOfRangeChunks.Dequeue();

                    _OUT_PACKETS.Enqueue(new UnloadChunkPacket(p.X, p.Z));
                }
            }

        }

        /// <summary>
        /// TODO: Add description.
        /// </summary>
        /// <exception cref="DisconnectedClientException">TODO: Why it's thrown.</exception>
        public void Render(World world, Player player)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            using Buffer buffer = new();

            try
            {
                if (!_init)
                {
                    /*try
                    {
                        StartInitProcess(buffer, world, player);
                    }
                    catch (UnexpectedClientBehaviorExecption e)
                    {
                        // TODO: send disconnected message to client.

                        System.Console.WriteLine(e.Message);

                        throw new DisconnectedClientException();
                    }*/

                    JoinGamePacket packet = new(player.Id, 0, 0, 0, "default", false);
                    packet.Write(buffer);
                    _CLIENT.Send(buffer);

                    world.ConnectPlayer(player, _OUT_PACKETS);
                    player.Connect(_SELF_RENDERER);

                    System.Diagnostics.Debug.Assert(_window == null);
                    _window = new(_OUT_PACKETS, player._selfInventory);

                    _init = true;
                }

                System.Diagnostics.Debug.Assert(_init);

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
                        TeleportationRecord report = new(teleportPacket.Payload);
                        _TELEPORTATION_RECORDS.Enqueue(report);

                        Entity.Vector p = new(teleportPacket.X, teleportPacket.Y, teleportPacket.Z);
                        Chunk.Vector pChunk = Chunk.Vector.Convert(p);
                        foreach (var renderer in _ENTITY_TO_RENDERERS.GetValues())
                        {
                            renderer.Update(pChunk);
                        }
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
                        _keepAliveRecord = new(requestKeepAlivePacket.Payload);
                    }
                    else if (packet is DestroyEntitiesPacket destroyEntitiesPacket)
                    {
                        int[] entityIds = destroyEntitiesPacket.EntityIds;
                        foreach (int id in entityIds)
                        {
                            _ENTITY_TO_RENDERERS.Extract(id);
                        }
                    }

                    packet.Write(buffer);
                    _CLIENT.Send(buffer);

                    System.Diagnostics.Debug.Assert(buffer.Empty);
                }

                System.Diagnostics.Debug.Assert(_OUT_PACKETS.Empty);
            }
            catch (DisconnectedClientException)
            {
                buffer.Flush();
                world.DisconnectPlayer(player);

                throw;
            }
        }

        public void Flush(World world)
        {
            foreach (ClientboundPlayingPacket packet in _OUT_PACKETS.Flush())
            {
                if (packet is DestroyEntitiesPacket destroyEntitiesPacket)
                {
                    foreach (int entityId in destroyEntitiesPacket.EntityIds)
                    {
                        _ENTITY_TO_RENDERERS.Extract(entityId);
                    }
                }
            }

            foreach ((int entityId, var renderer) in _ENTITY_TO_RENDERERS.Flush())
            {
                renderer.Disconnect();
            }

            if (_window != null)
            {
                _window.Flush(world);
            }
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion
            System.Diagnostics.Debug.Assert(_OUT_PACKETS.Empty);

            System.Diagnostics.Debug.Assert(_ENTITY_TO_RENDERERS.Empty);

            // Release Resources
            _CLIENT.Dispose();

            _LOAD_CHUNK_PACKETS.Dispose();
            _OUT_PACKETS.Dispose();

            _LOADING_HELPER.Dispose();
            _ENTITY_TO_RENDERERS.Dispose();

            _SELF_RENDERER.Dispose();

            _TELEPORTATION_RECORDS.Dispose();
            _keepAliveRecord = null;

            if (_window != null)
            {
                _window.Dispose();
                _window = null;
            }

            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }
}
