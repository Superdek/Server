using Containers;

namespace Protocol
{
    internal sealed class EntityRendererManager : System.IDisposable
    {
        private bool _disposed = false;

        private bool _movement = false;

        private readonly Set<int> _IDENTIFIERS = new();  // Disposable
        private readonly Queue<EntityRenderer> _RENDERERS = new();  // Disposable

        public EntityRendererManager() { }

        ~EntityRendererManager() => System.Diagnostics.Debug.Assert(false);

        public bool Apply(EntityRenderer renderer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (_IDENTIFIERS.Contains(renderer.Id))
            {
                return true;
            }

            _IDENTIFIERS.Insert(renderer.Id);
            _RENDERERS.Enqueue(renderer);

            return false;
        }

        public void MoveAndRotate(
            int entityId,
            Vector p, Vector pPrev, Entity.Angles look, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (EntityRenderer renderer in _RENDERERS.GetValues())
            {
                renderer.MoveAndRotate(entityId, p, pPrev, look, onGround);
            }

            _movement = true;
        }

        public void Move(
            int entityId, Vector p, Vector pPrev, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (EntityRenderer renderer in _RENDERERS.GetValues())
            {
                renderer.Move(entityId, p, pPrev, onGround);
            }

            _movement = true;
        }

        public void Rotate(int entityId, Entity.Angles look, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (EntityRenderer renderer in _RENDERERS.GetValues())
            {
                renderer.Rotate(entityId, look, onGround);
            }
            
            _movement = true;
        }

        public void Stand(int entityId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (EntityRenderer renderer in _RENDERERS.GetValues())
            {
                renderer.Stand(entityId);
            }
        
            _movement = true;
        }

        public void HandleRendering(int entityId, Vector p)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            for (int i = 0; i < _RENDERERS.Count; ++i)
            {
                EntityRenderer renderer = _RENDERERS.Dequeue();

                if (renderer.IsDisconnected)
                {
                    _IDENTIFIERS.Extract(renderer.Id);

                    continue;
                }
                else if (!renderer.CanRender(p))
                {
                    _IDENTIFIERS.Extract(renderer.Id);
                    renderer.DestroyEntity(entityId);

                    continue;
                }

                _RENDERERS.Enqueue(renderer);
            }

        }

        public void FinishMovementRenderring()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_movement);

            _movement = false;
        }

        public void ChangeForms(int entityId, bool sneaking, bool sprinting)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (var renderer in _RENDERERS.GetValues())
            {
                renderer.ChangeForms(entityId, sneaking, sprinting);
            }
        }

        /*public void Teleport(
            int entityId, Entity.Vector pos, Entity.Angles look, bool onGround)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(!_IDENTIFIERS.Contains(entityId));

            foreach (var renderer in _RENDERERS.GetValues())
            {
                renderer.Teleport(entityId, pos, look, onGround);
            }
        }*/

        public void Flush(int entityId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            while (!_RENDERERS.Empty)
            {
                EntityRenderer renderer = _RENDERERS.Dequeue();
                renderer.DestroyEntity(entityId);
            }
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion
            System.Diagnostics.Debug.Assert(!_movement);
            System.Diagnostics.Debug.Assert(_RENDERERS.Empty);

            // Release  resources.
            /*_ID_LIST.Dispose();*/

            _RENDERERS.Dispose();

            // Finish
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }

}
