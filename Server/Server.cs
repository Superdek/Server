using Applications;
using Containers;
using Protocol;
using Server;
using System.Threading;
using System.Threading.Tasks;

namespace Application
{
    public sealed class Server : ConsoleApplication
    {
        private bool _disposed = false;

        private readonly World _WORLD;

        private readonly Queue<(Connection, Player)> _CONNECTIONS1 = new();  // Disposable
        private readonly Queue<(Connection, Player)> _CONNECTIONS2 = new();  // Disposable
        private readonly Queue<Connection> _DISCONNECTIONS = new();  // Disposable

        private Server() 
        {
            _WORLD = new SuperWorld();
        }

        ~Server() => System.Diagnostics.Debug.Assert(false);

        private void Control(long serverTicks)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            Connection conn;
            Player player;
            while (true)
            {
                if (_CONNECTIONS1.Empty)
                {
                    break;
                }

                (conn, player) = _CONNECTIONS1.Dequeue();

                try
                {
                    conn.Control(serverTicks, _WORLD, player);
                }
                catch (DisconnectedClientException)
                {
                    _DISCONNECTIONS.Enqueue(conn);

                    continue;
                }

                _CONNECTIONS2.Enqueue((conn, player));
            }

        }

        private void HandleDisconnections()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            Connection conn;
            while (true)
            {
                if (_DISCONNECTIONS.Empty)
                {
                    break;
                }

                conn = _DISCONNECTIONS.Dequeue();

                conn.Flush(_WORLD);
                conn.Dispose();
            }
        }

        private void Render(long serverTicks)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            Connection conn;
            Player player;
            while (true)
            {
                if (_CONNECTIONS2.Empty)
                {
                    break;
                }

                (conn, player) = _CONNECTIONS2.Dequeue();

                conn.Render(_WORLD, player);

                _CONNECTIONS1.Enqueue((conn, player));
            }
        }

        private void StartGameRoutine(long serverTicks, ConnectionListener connListener)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Console.Write(".");

            Control(serverTicks);

            // Barrier

            HandleDisconnections();

            // Barrier

            _WORLD.DespawnEntities();

            // Barrier

            _WORLD.MoveEntities();

            // Barrier

            connListener.Accept(_WORLD, _CONNECTIONS2);

            // Barrier

            _WORLD.SpawnEntities();

            // Barrier

            Render(serverTicks);

            // Barrier

            _WORLD.StartRoutine(serverTicks);

            // Barrier

            _WORLD.StartEntitRoutines(serverTicks);

            // Barrier

        }

        private static long GetCurrentTime()
        {
            return (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMicrosecond);
        }

        private void StartCoreRoutine(ConnectionListener connListener)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            long interval, total, start, end, elapsed;

            long serverTicks = 0;

            interval = total = (long)System.TimeSpan.FromMilliseconds(50).TotalMicroseconds;
            start = GetCurrentTime();

            while (Running)
            {
                if (total >= interval)
                {
                    total -= interval;

                    StartGameRoutine(serverTicks++, connListener);
                }

                end = GetCurrentTime();
                elapsed = end - start;
                total += elapsed;
                start = end;

                if (elapsed > interval)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine($"The task is taking longer than expected. Elapsed Time: {elapsed}.");
                }
            }

            // Handle close routine...

        }

        public override void Dispose()
        {
            // Assertiong
            System.Diagnostics.Debug.Assert(!_disposed);
            
            System.Diagnostics.Debug.Assert(_CONNECTIONS1.Empty);
            System.Diagnostics.Debug.Assert(_CONNECTIONS2.Empty);

            // Release resources.
            _WORLD.Dispose();

            _CONNECTIONS1.Dispose();
            _CONNECTIONS2.Dispose();

            // Finish
            base.Dispose();
            _disposed = true;
        }

        public static void Main()
        {
            System.Console.WriteLine("Hello, World!");

            ushort port = 25565;

            using Server app = new();

            using ConnectionListener connListener = new();

            app.Run(() => app.StartCoreRoutine(connListener));

            ClientListener listener = new(connListener);
            app.Run(() => listener.StartRoutine(app, port));

            while (app.Running)
            {
                // Handle Barriers
                System.Threading.Thread.Sleep(1000);
            }

        }

    }

}

