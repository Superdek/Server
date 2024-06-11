using Common;
using Containers;

namespace Protocol
{
    internal abstract class Renderer
    {
        private readonly Queue<ClientboundPlayingPacket> _OUT_PACKETS;

        public Renderer(Queue<ClientboundPlayingPacket> outPackets)
        {
            _OUT_PACKETS = outPackets;
        }

        protected void Render(ClientboundPlayingPacket packet)
        {
            _OUT_PACKETS.Enqueue(packet);
        }
        
    }

    internal sealed class EntityRenderer : Renderer
    {
        private bool _disconnected = false;
        public bool IsDisconnected => _disconnected;

        private ChunkLocation _loc;

        private int _dEntityRendering = -1;

        public readonly int Id;

        public EntityRenderer(
            int id,
            Queue<ClientboundPlayingPacket> outPackets,
            ChunkLocation loc, int dEntityRendering) 
            : base(outPackets) 
        {
            Id = id;

            _loc = loc;

            _dEntityRendering = dEntityRendering;
        }

        public void Disconnect()
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            _disconnected = true;
        }

        public void Update(ChunkLocation loc)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            _loc = loc;
        }

        public void Update(int dEntityRendering)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);
            System.Diagnostics.Debug.Assert(dEntityRendering > 0);

            _dEntityRendering = dEntityRendering;
        }

        public bool CanRender(Vector p)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);
            System.Diagnostics.Debug.Assert(_dEntityRendering > 0);

            ChunkGrid grid = ChunkGrid.Generate(_loc, _dEntityRendering);
            ChunkLocation loc = ChunkLocation.Generate(p);
            return grid.Contains(loc);
        }

        public void MoveAndRotate(
            int entityId, 
            Vector p, Vector pPrev, Entity.Angles look, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            double dx = (p.X - pPrev.X) * (32 * 128),
                dy = (p.Y - pPrev.Y) * (32 * 128),
                dz = (p.Z - pPrev.Z) * (32 * 128);
            System.Diagnostics.Debug.Assert(dx >= short.MinValue && dx <= short.MaxValue);
            System.Diagnostics.Debug.Assert(dy >= short.MinValue && dy <= short.MaxValue);
            System.Diagnostics.Debug.Assert(dz >= short.MinValue && dz <= short.MaxValue);

            (byte x, byte y) = look.ConvertToPacketFormat();
            Render(new EntityLookAndRelMovePacket(
                entityId,
                (short)dx, (short)dy, (short)dz,
                x, y,
                onGround));
            Render(new EntityHeadLookPacket(entityId, x));
        
        }

        public void Move(int entityId, Vector p, Vector pPrev, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            double dx = (p.X - pPrev.X) * (32 * 128), 
                dy = (p.Y - pPrev.Y) * (32 * 128), 
                dz = (p.Z - pPrev.Z) * (32 * 128);
            System.Diagnostics.Debug.Assert((dx >= short.MinValue) && (dx <= short.MaxValue));
            System.Diagnostics.Debug.Assert((dy >= short.MinValue) && (dy <= short.MaxValue));
            System.Diagnostics.Debug.Assert((dz >= short.MinValue) && (dz <= short.MaxValue));

            Render(new EntityRelMovePacket(
                entityId,
                (short)dx, (short)dy, (short)dz,
                onGround));

        }

        public void Rotate(int entityId, Entity.Angles look, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            (byte x, byte y) = look.ConvertToPacketFormat();
            Render(new EntityLookPacket(entityId, x, y, onGround));
            Render(new EntityHeadLookPacket(entityId, x));
        }

        public void Stand(int entityId)
        {
            System.Diagnostics.Debug.Assert(!_disconnected);

            Render(new EntityPacket(entityId));
        }

        /*public void Teleport(
            int entityId, Entity.Vector pos, Entity.Angles look, bool onGround)
        {
            
            Render(new EntityTeleportPacket(
                entityId,
                pos.X, pos.Y, pos.Z,
                look.Yaw, look.Pitch,
                onGround));
        }*/

        public void ChangeForms(int entityId, bool sneaking, bool sprinting)
        {
            byte flags = 0x00;

            if (sneaking)
            {
                flags |= 0x02;
            }
            if (sprinting)
            {
                flags |= 0x08;
            }

            using EntityMetadata metadata = new();
            metadata.AddByte(0, flags);

            Render(new EntityMetadataPacket(entityId, metadata.WriteData()));
        }

        public void DestroyEntity(int entityId)
        {
            Render(new DestroyEntitiesPacket(entityId));
        }

    }

    internal sealed class SelfPlayerRenderer : Renderer
    {
        private readonly Client _CLIENT;

        public SelfPlayerRenderer(
            Queue<ClientboundPlayingPacket> outPackets, Client client) 
            : base(outPackets)
        {
            _CLIENT = client;
        }

        public void Init(int entityId, Vector p, Entity.Angles look)
        {
            {
                int payload = new System.Random().Next();
                Render(new TeleportSelfPlayerPacket(
                    p.X, p.Y, p.Z,
                    look.Yaw, look.Pitch,
                    false, false, false, false, false,
                    payload));
            }

            {
                Render(new SetPlayerAbilitiesPacket(
                    false, false, true, false, 0.1F, 0.0F));
            }

            /*{
                Render(new EntityVelocityPacket(
                    entityId,
                    Conversions.ToShort(v.X * 8000),
                    Conversions.ToShort(v.Y * 8000),
                    Conversions.ToShort(v.Z * 8000)));
            }*/

        }

        /*public void Teleport(Entity.Vector p, Entity.Angles look)
        {
            int payload = new System.Random().Next();
            Render(new TeleportSelfPlayerPacket(
                p.X, p.Y, p.Z,
                look.Yaw, look.Pitch,
                false, false, false, false, false,
                payload));
        }*/

        public void ApplyVelocity(int entityId, Vector v)
        {
            using Buffer buffer = new();

            var packet = new EntityVelocityPacket(
                entityId,
                (short)(v.X * 8000),
                (short)(v.Y * 8000),
                (short)(v.Z * 8000));
            packet.Write(buffer);
            _CLIENT.Send(buffer);
        }
        
    }

}
