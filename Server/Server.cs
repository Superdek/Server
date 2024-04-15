﻿using Applications;
using Containers;
using Protocol;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.AccessControl;
using System.Threading;

namespace Application
{
    

    public sealed class Server : ConsoleApplication
    {
        private bool _disposed = false;

        private readonly ConcurrentNumList _idList = new();

        private readonly Table<int, Connection> _connTable = new();
        private readonly Queue<Connection> _connections = new();
        private readonly Queue<Connection> _abortedConnections = new();
        private readonly Queue<(Connection, string)> _unexpectedConnections = new();

        private readonly Table<int, Player> _playerTable = new();
        private readonly Queue<Player> _players = new();

        private readonly Table<int, Queue<Control>> _controlsTable = new();
        private readonly Table<int, Queue<Report>> _reportsTable = new();

        private readonly Table<Chunk.Position, Chunk> _chunks = new();

        private Server() { }

        ~Server() => Dispose(false);

        private void RecvData(ulong currentTicks)
        {
            if (_connections.Empty) return;

            bool close;

            int count = _connections.Count;
            Debug.Assert(count > 0);
            for (int i = 0; i < count; ++i)
            {
                close = false;

                Connection conn = _connections.Dequeue();
                int id = conn.PlayerId;

                Queue<Control> controls = _controlsTable.Lookup(id);

                try
                {
                    conn.Recv(controls);
                }
                catch (UnexpectedClientBehaviorExecption e)
                {
                    Debug.Assert(close == false);

                    _unexpectedConnections.Enqueue((conn, e.Message));

                    close = true;
                }
                catch (DisconnectedClientException)
                {
                    Debug.Assert(close == false);

                    _abortedConnections.Enqueue(conn);

                    close = true;
                }

                if (close)
                {
                    continue;
                }

                _connections.Enqueue(conn);
            }

        }
        
        private void HandlePlayers()
        {
            if (_players.Empty) return;

            int count = _players.Count;
            Debug.Assert(count > 0);
            for (int i = 0; i < count; ++i)
            {
                Player player = _players.Dequeue();
                int id = player.Id;

                if (!_connTable.Contains(id))
                {
                    // TODO: Release resources of player object.

                    _playerTable.Extract(id);
                    _idList.Dealloc(id);
                    /*Console.WriteLine("Disconnected!");*/
                    continue;

                }

                Queue<Control> controls = _controlsTable.Lookup(id);

                while (!controls.Empty)
                {
                    Control _c = controls.Dequeue();

                    if (_c is PlayerOnGroundControl playerOnGroundControl)
                    {
                        player.onGround = playerOnGroundControl.OnGround;
                    }
                    else if (_c is PlayerPositionControl playerMovementControl)
                    {
                        player.posPrev = player.pos;

                        player.pos = playerMovementControl.Pos;
                    }
                    else if (_c is PlayerLookControl playerLookControl)
                    {
                        player.look = playerLookControl.Look;
                    }
                    else
                        throw new NotImplementedException();

                }

                _players.Enqueue(player);
            }

        }

        private void SendData()
        {
            if (_connections.Empty) return;

            bool close;
            int count = _connections.Count;
            Debug.Assert(count > 0);
            for (int i = 0; i < count; ++i)
            {
                close = false;

                Connection conn = _connections.Dequeue();

                int id = conn.PlayerId;

                try
                {
                    conn.Send();
                }
                catch (DisconnectedClientException)
                {
                    Debug.Assert(close == false);

                    _abortedConnections.Enqueue(conn);

                    close = true;
                }

                if (close)
                {
                    continue;
                }

                _connections.Enqueue(conn);

            }
        }

        private void HandleUnexpectedConnections()
        {
            if (_unexpectedConnections.Empty) return;

            int count = _unexpectedConnections.Count;
            Debug.Assert(count > 0);
            for (int i = 0; i < count; ++i)
            {
                (Connection conn, string msg) = _unexpectedConnections.Dequeue();

                int id = conn.PlayerId;

                _connTable.Extract(id);

                Queue<Control> controls = _controlsTable.Extract(id);
                Queue<Report> reports = _reportsTable.Extract(id);

                // TODO: Send message why disconnected.

                // TODO: Handle flush and release garbage.
                Debug.Assert(controls.Empty);
                reports.Flush();

                controls.Close();
                reports.Close();
            }
        }

        private void HandleAbortedConnections()
        {
            if (_abortedConnections.Empty) return;

            int count = _abortedConnections.Count;
            Debug.Assert(count > 0);
            for (int i = 0; i < count; ++i)
            {
                Connection conn = _abortedConnections.Dequeue();

                int id = conn.PlayerId;

                _connTable.Extract(id);

                Queue<Control> controls = _controlsTable.Extract(id);
                Queue<Report> reports = _reportsTable.Extract(id);

                // TODO: Handle flush and release garbage.
                Debug.Assert(controls.Empty);
                reports.Flush();

                controls.Close();
                reports.Close();

            }
        }

        private void StartGameRoutine(
            ulong currentTicks,
            ConnectionListener connListener)
        {
            Console.Write(".");
            /*Console.Write($"{currentTicks}");*/

            RecvData(currentTicks);

            // Barrier

            HandlePlayers();

            // Barrier

            connListener.Accept(
                _idList, 
                _connTable, _connections, 
                _playerTable, _players, 
                _controlsTable, _reportsTable, 
                _chunks, 
                new(0, 60, 0), new(0, 0));

            // Barrier

            SendData();

            // Barrier

            HandleUnexpectedConnections();

            // Barrier

            HandleAbortedConnections();

            // Barrier

        }

        private static ulong GetCurrentTime()
        {
            return (ulong)(DateTime.Now.Ticks / TimeSpan.TicksPerMicrosecond);
        }

        private void StartCoreRoutine(ConnectionListener connListener)
        {
            ulong interval, total, start, end, elapsed, currentTicks;

            currentTicks = 0;
            interval = total = (ulong)TimeSpan.FromMilliseconds(50).TotalMicroseconds;
            start = GetCurrentTime();

            while (Running)
            {
                if (total >= interval)
                {
                    total -= interval;

                    StartGameRoutine(currentTicks++, connListener);
                }

                end = GetCurrentTime();
                elapsed = end - start;
                total += elapsed;
                start = end;

                if (elapsed > interval)
                {
                    Console.WriteLine();
                    Console.WriteLine($"The task is taking longer than expected. Elapsed Time: {elapsed}.");
                }
            }

            // Handle close routine...

        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing == true)
                {
                    // Release managed resources.
                    _idList.Dispose();

                    _connTable.Dispose();
                    _connections.Dispose();
                    _abortedConnections.Dispose();
                    _unexpectedConnections.Dispose();

                    _playerTable.Dispose();
                    _players.Dispose();

                    _controlsTable.Dispose();
                    _reportsTable.Dispose();

                    _chunks.Dispose();
                }

                // Release unmanaged resources.

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        public static void Main()
        {
            Console.WriteLine("Hello, World!");

            ushort port = 25565;

            using Server app = new();

            ConnectionListener connListener = new();

            app.Run(() => app.StartCoreRoutine(connListener));

            GlobalListener listener = new(connListener);
            app.Run(() => 
                listener.StartRoutine(app, port));

            while (app.Running)
            {

                // Handle Barriers
                Thread.Sleep(1000);
            }


        }

    }

}
