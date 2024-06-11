
using Common;
using Containers;
using System;

namespace Protocol
{
    public abstract class Entity : System.IDisposable
    {

        public readonly struct Hitbox
        {
            public readonly double _W, _H;
            public double Width => _W;
            public double Height => _H;
            /*public readonly double EyeHeight, MaxStepHeight;*/

            public Hitbox(double w, double h)
            {
                _W = w;
                _H = h;
            }

            public BoundingBox Convert(Vector p)
            {
                System.Diagnostics.Debug.Assert(_W > 0.0D);
                System.Diagnostics.Debug.Assert(_H > 0.0D);

                double w = _W / 2.0D;

                Vector max = new(p.X + w, p.Y + _H, p.Z + w),
                       min = new(p.X - w, p.Y, p.Z - w);
                return new(max, min);
            }
        }

        public readonly struct Angles : System.IEquatable<Angles>
        {
            internal const float MaxYaw = 180, MinYaw = -180;
            internal const float MaxPitch = 90, MinPitch = -90;

            public readonly float Yaw, Pitch;

            private static float Frem(float angle)
            {
                float y = 360.0f;
                return angle - (y * (float)System.Math.Floor(angle / y));
            }

            public Angles(float yaw, float pitch)
            {
                // TODO: map yaw from 180 to -180.
                System.Diagnostics.Debug.Assert(pitch >= MinPitch);
                System.Diagnostics.Debug.Assert(pitch <= MaxPitch);

                Yaw = Frem(yaw);
                Pitch = pitch;
            }

            internal (byte, byte) ConvertToPacketFormat()
            {
                System.Diagnostics.Debug.Assert(Pitch >= MinPitch);
                System.Diagnostics.Debug.Assert(Pitch <= MaxPitch);
                
                float x = Frem(Yaw);
                float y = Frem(Pitch);

                return (
                    (byte)((byte.MaxValue * x) / 360),
                    (byte)((byte.MaxValue * y) / 360));
            }

            public override readonly string? ToString()
            {
                throw new System.NotImplementedException();
            }

            public bool Equals(Angles other)
            {
                return (Yaw == other.Yaw) && (Pitch == other.Pitch);
            }

        }

        private bool _disposed = false;

        private int _id;
        public int Id => _id;

        public System.Guid UniqueId;


        public abstract double GetMass();


        private readonly Queue<Vector> _FORCES;
        public Queue<Vector> Forces => _FORCES;

        private Vector _v;
        public Vector Velocity => _v;

        private Vector _p;
        public Vector Position => _p;


        private bool _rotated;
        private Angles _look;
        public Angles Look => _look;


        protected bool _sneaking, _sprinting;
        public bool IsSneaking => _sneaking;
        public bool IsSprinting => _sprinting;


        public abstract Hitbox GetHitbox();

        private BoundingBox _bb;
        public BoundingBox BB => _bb;

        /*protected bool _teleported;
        protected Vector _posTeleport;
        protected Angles _lookTeleport;*/


        private readonly EntityRendererManager _RENDERER_MANAGER = new();  // Disposable

        internal Entity(
            int id, System.Guid uniqueId, Vector p, Angles look)
        {
            _id = id;

            UniqueId = uniqueId;

            _FORCES = new Queue<Vector>();

            _v = new Vector(0, 0, 0);

            _p = p;

            _rotated = false;
            _look = look;

            _sneaking = _sprinting = false;

            Hitbox hitbox = GetHitbox();
            _bb = hitbox.Convert(_p);

            /*_teleported = false;*/
            /*_posTeleport = new(0, 0, 0);
            _lookTeleport = new(0, 0);*/
        }

        ~Entity() => System.Diagnostics.Debug.Assert(false);

        internal abstract void Spawn(Queue<ClientboundPlayingPacket> outPackets);

        internal void ApplyRenderer(
            Queue<ClientboundPlayingPacket> outPackets, 
            EntityRenderer renderer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            bool exists = _RENDERER_MANAGER.Apply(renderer);
            if (!exists)
            {
                Spawn(outPackets);
            }
        }

        public virtual void ApplyForce(Vector force)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _FORCES.Enqueue(force);
        }

        public virtual void ApplyGlobalForce(Vector force)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _FORCES.Enqueue(force);
        }

        public virtual bool IsDead()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return false;
        }

        protected internal virtual void StartRoutine(long serverTicks, World world)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

        }

        internal (BoundingBox, Vector) Integrate()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            Vector v = _v;
            /*System.Console.WriteLine();
            System.Console.WriteLine($"p: ({p.X}, {p.Y}, {p.Z})");*/

            while (!_FORCES.Empty)
            {
                Vector force = _FORCES.Dequeue();

                v += (force / GetMass());
            }

            /*p += v;*/

            /*System.Console.WriteLine($"v: {v}, p: {p}, ");*/

            Hitbox hitbox = GetHitbox();
            BoundingBox bb = hitbox.Convert(_p);

            return (bb, v);
        }
        
        public virtual void Move(BoundingBox bb, Vector v, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_FORCES.Empty);

            Vector p = bb.GetBottomCenter();

            _RENDERER_MANAGER.HandleRendering(Id, p);

            bool moved = !p.Equals(_p);  // TODO: Compare with machine epsilon.
            if (moved && _rotated)
            {
                _RENDERER_MANAGER.MoveAndRotate(Id, p, _p, _look, onGround);
            }
            else if (moved)
            {
                System.Diagnostics.Debug.Assert(!_rotated);

                _RENDERER_MANAGER.Move(Id, p, _p, onGround);
            }
            else if (_rotated)
            {
                System.Diagnostics.Debug.Assert(!moved);

                _RENDERER_MANAGER.Rotate(Id, _look, onGround);
            }
            else
            {
                System.Diagnostics.Debug.Assert(!moved);
                System.Diagnostics.Debug.Assert(!_rotated);

                _RENDERER_MANAGER.Stand(Id);
            }

            _v = v;
            _p = p;

            _bb = bb;

            _rotated = false;

            _RENDERER_MANAGER.FinishMovementRenderring();

        }

        /*public virtual void Teleport(Vector pos, Angles look)
        {
            _teleported = true;
            _posTeleport = pos;
            _lookTeleport = look;
        }*/

        public void Rotate(Angles look)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _rotated = true;
            _look = look;
        }

        private void RanderFormChanging()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            
            _RENDERER_MANAGER.ChangeForms(Id, _sneaking, _sprinting);
        }

        public void Sneak()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            
            System.Diagnostics.Debug.Assert(!_sneaking);
            _sneaking = true;

            RanderFormChanging();
        }

        public void Unsneak()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            
            System.Diagnostics.Debug.Assert(_sneaking);
            _sneaking = false;

            RanderFormChanging();
        }

        public void Sprint()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            
            System.Diagnostics.Debug.Assert(!_sprinting);
            _sprinting = true;

            RanderFormChanging();
        }

        public void Unsprint()
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            
            System.Diagnostics.Debug.Assert(_sprinting);
            _sprinting = false;

            RanderFormChanging();
        }
        
        public virtual void Flush()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _RENDERER_MANAGER.Flush(Id);
        }

        public virtual void Dispose()
        {
            // Assertion
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release resources.
            _FORCES.Dispose();
            _RENDERER_MANAGER.Dispose();

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }

    public sealed class ItemEntity : Entity
    {
        private bool _disposed = false;

        
        public const double MASS = 0.1D;
        public override double GetMass()
        {
            return MASS;
        }


        public static readonly Hitbox HITBOX = new(0.25D, 0.25D);
        public override Hitbox GetHitbox()
        {
            return HITBOX;
        }

        public ItemEntity(int id, Vector pos, Angles look) 
            : base(id, System.Guid.NewGuid(), pos, look) { }

        ~ItemEntity() => System.Diagnostics.Debug.Assert(false);

        internal override void Spawn(Queue<ClientboundPlayingPacket> outPackets)
        {
            (byte x, byte y) = Look.ConvertToPacketFormat();
            outPackets.Enqueue(new SpawnObjectPacket(
                Id, UniqueId, 2,
                Position.X, Position.Y, Position.Z,
                x, y,
                1, 0, 0, 0));

            using EntityMetadata metadata = new();
            metadata.AddBool(5, true);
            metadata.AddSlotData(6, new SlotData(280, 1));
            outPackets.Enqueue(new EntityMetadataPacket(
                Id, metadata.WriteData()));
        }

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

    }

    public abstract class LivingEntity : Entity
    {
        private bool _disposed = false;

        internal LivingEntity(int id, System.Guid uniqueId, Vector pos, Angles look) 
            : base(id, uniqueId, pos, look) { }

        ~LivingEntity() => System.Diagnostics.Debug.Assert(false);

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

    }

    public sealed class Player : LivingEntity
    {
        private bool _disposed = false;


        public const double MASS = 1.0D;
        public override double GetMass() => MASS;


        public override Hitbox GetHitbox()
        {
            double w = 0.6D, h;
            if (IsSneaking)
            {
                h = 1.65D;
            }
            else
            {
                h = 1.8D;
            }

            return new(w, h);
        }


        public readonly string Username;


        internal readonly SelfInventory _selfInventory;


        private bool _connected = false;
        public bool IsConnected => _connected;


        private SelfPlayerRenderer? _selfRenderer;

        private Vector _p;
        private bool _onGround;

        internal Player(
            int id, System.Guid uniqueId, Vector p, Angles look, string username) 
            : base(id, uniqueId, p, look)
        {

            Username = username;

            _selfInventory = new();

            _selfRenderer = null;

        }

        ~Player() => System.Diagnostics.Debug.Assert(false);

        internal override void Spawn(Queue<ClientboundPlayingPacket> outPackets)
        {
            byte flags = 0x00;

            if (IsSneaking)
                flags |= 0x02;
            if (IsSprinting)
                flags |= 0x08;

            using EntityMetadata metadata = new();
            metadata.AddByte(0, flags);

            (byte x, byte y) = Look.ConvertToPacketFormat();
            outPackets.Enqueue(new SpawnNamedEntityPacket(
                Id, UniqueId,
                Position.X, Position.Y, Position.Z,
                x, y,
                metadata.WriteData()));
        }

        internal void Connect(SelfPlayerRenderer renderer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!IsConnected);
            _connected = true;

            System.Diagnostics.Debug.Assert(_selfRenderer == null);
            _selfRenderer = renderer;

            _p = Position;

            renderer.Init(Id, _p, Look);
        }

        internal void Disconnect()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(IsConnected);
            _connected = false;

            System.Diagnostics.Debug.Assert(_selfRenderer != null);
            _selfRenderer = null;
        }

        public override void ApplyForce(Vector force)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (IsConnected)
            {
                System.Diagnostics.Debug.Assert(_selfRenderer != null);
                _selfRenderer.ApplyVelocity(Id, force / GetMass());
            }

            base.ApplyForce(force);
        }

        public override void ApplyGlobalForce(Vector force)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (IsConnected)
            {
                
            }

            base.ApplyGlobalForce(force);
        }

        /*public override void Teleport(Vector pos, Angles look)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            base.Teleport(pos, look);

            System.Diagnostics.Debug.Assert(_selfRenderer != null);
            _selfRenderer.Teleport(pos, look);
            // TODO: Check the velocity was reset in client when teleported.
        }*/

        public override void Move(BoundingBox bb, Vector v, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (IsConnected)
            {
                // TODO: Check the difference between _p and p. and predict movement....
                /*System.Console.WriteLine($"p: {p}, _p: {_p}, ");
                System.Console.WriteLine($"Length: {Vector.GetLength(p, _p)}");
                if (Vector.GetLength(p, _p) > k)
                {
                }
                */

                /*Vector v1 = bb.GetBottomCenter(), v2 = _p;
                double length = Vector.GetLength(v1, v2);
                System.Console.WriteLine($"length: {length}");*/

                bb = GetHitbox().Convert(_p);
                onGround = _onGround;
            }
            
            base.Move(bb, v, onGround);
        }

        internal void Control(Vector p, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(IsConnected);

            _p = p;
            _onGround = onGround;
        }

        internal void Control(bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(IsConnected);

            _onGround = onGround;
        }

        protected internal override void StartRoutine(long serverTicks, World world)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (IsConnected)
            {

            }
            else
            {

            }

            base.StartRoutine(serverTicks, world);
        }

        public override void Dispose()
        {
            // Assertion.
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }
    }

}
