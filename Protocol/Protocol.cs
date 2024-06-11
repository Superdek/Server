using Common;
using Applications;
using Containers;

namespace Protocol
{
    internal static class SocketMethods
    {
        public static int GetLocalPort(System.Net.Sockets.Socket socket)
        {
            System.Net.IPEndPoint? localEndPoint = (System.Net.IPEndPoint?)socket.LocalEndPoint;
            System.Diagnostics.Debug.Assert(localEndPoint != null);
            return localEndPoint.Port;
        }

        public static System.Net.Sockets.Socket Establish(ushort port)
        {
            System.Net.Sockets.Socket socket = new(
                System.Net.Sockets.SocketType.Stream, 
                System.Net.Sockets.ProtocolType.Tcp);

            System.Net.IPEndPoint localEndPoint = new(System.Net.IPAddress.Any, port);
            socket.Bind(localEndPoint);
            socket.Listen();

            return socket;
        }

        /// <exception cref="TryAgainException"></exception>
        public static System.Net.Sockets.Socket Accept(System.Net.Sockets.Socket socket)
        {
            try
            {
                System.Net.Sockets.Socket newSocket = socket.Accept();

                return newSocket;
            }
            catch (System.Net.Sockets.SocketException e)
            {
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock)
                {
                    throw new TryAgainException();
                }

                throw;
            }

            System.Diagnostics.Debug.Assert(false);
        }

        public static bool Poll(System.Net.Sockets.Socket socket, System.TimeSpan span)  
        {
            // TODO: check the socket is binding and listening.

            if (IsBlocking(socket) &&
                socket.Poll(span, System.Net.Sockets.SelectMode.SelectRead) == false)
            {
                return false;
            }

            return true;
        }

        public static void SetBlocking(
            System.Net.Sockets.Socket socket, bool f)
        {
            socket.Blocking = f;
        }

        public static bool IsBlocking(
            System.Net.Sockets.Socket socket)
        {
            return socket.Blocking;
        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public static int RecvBytes(
            System.Net.Sockets.Socket socket, 
            byte[] buffer, int offset, int size)
        {
            try
            {
                int n = socket.Receive(buffer, offset, size, System.Net.Sockets.SocketFlags.None);
                if (n == 0)
                {
                    throw new DisconnectedClientException();
                }

                System.Diagnostics.Debug.Assert(n <= size);

                return n;
            }
            catch (System.Net.Sockets.SocketException e)
            {
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock)
                {
                    throw new TryAgainException();
                }

                throw;
            }

        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public static byte RecvByte(System.Net.Sockets.Socket socket)
        {
            byte[] buffer = new byte[1];

            int n = RecvBytes(socket, buffer, 0, 1);
            System.Diagnostics.Debug.Assert(n == 1);

            return buffer[0];
        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public static void SendBytes(System.Net.Sockets.Socket socket, byte[] data)
        {
            try
            {
                System.Diagnostics.Debug.Assert(data != null);
                int n = socket.Send(data);
                System.Diagnostics.Debug.Assert(n >= 0);
                System.Diagnostics.Debug.Assert(n == data.Length);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock)
                {
                    throw new TryAgainException();
                }
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted)
                {
                    throw new DisconnectedClientException();
                }

                throw;
            }

        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public static void SendByte(System.Net.Sockets.Socket socket, byte v)
        {
            SendBytes(socket, [v]);
        }

    }

    internal sealed class Client : System.IDisposable
    {
        private bool _disposed = false;

        private const int _TimeoutLimit = 100;
        private int _tryAgainCount = 0;

        private const byte _SEGMENT_BITS = 0x7F;
        private const byte _CONTINUE_BIT = 0x80;

        private int _x = 0, _y = 0;
        private byte[]? _data = null;

        private System.Net.Sockets.Socket _socket;

        public int LocalPort => SocketMethods.GetLocalPort(_socket);

        /// <exception cref="TryAgainException"></exception>
        internal static Client Accept(System.Net.Sockets.Socket socket)
        {
            //TODO: Check the socket is Binding and listening correctly.

            System.Net.Sockets.Socket newSocket = SocketMethods.Accept(socket);
            SocketMethods.SetBlocking(newSocket, false);

            /*Console.WriteLine($"socket: {socket.LocalEndPoint}");*/


            return new(newSocket);
        }

        private Client(System.Net.Sockets.Socket socket)
        {
            System.Diagnostics.Debug.Assert(SocketMethods.IsBlocking(socket) == false);
            _socket = socket;
        }

        ~Client() => System.Diagnostics.Debug.Assert(false);

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        private int RecvSize()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(SocketMethods.IsBlocking(_socket) == false);

            int size = _x;
            int position = _y;
            System.Diagnostics.Debug.Assert(size >= 0);
            System.Diagnostics.Debug.Assert(position >= 0);

            try
            {
                while (true)
                {
                    byte v = SocketMethods.RecvByte(_socket);

                    size |= (v & _SEGMENT_BITS) << position;
                    if ((v & _CONTINUE_BIT) == 0)
                    {
                        break;
                    }

                    position += 7;

                    if (position >= 32)
                    {
                        throw new InvalidEncodingException();
                    }    

                    System.Diagnostics.Debug.Assert(position > 0);
                }

            }
            finally
            {
                _x = size;
                _y = position;
                System.Diagnostics.Debug.Assert(_data == null);
            }

            return size;
        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        private void SendSize(int size)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            while (true)
            {
                if ((size & ~_SEGMENT_BITS) == 0)
                {
                    System.Diagnostics.Debug.Assert(size <= 0b_00000000_00000000_00000000_11111111);
                    SocketMethods.SendByte(_socket, (byte)size);
                    break;
                }

                int v = (size & _SEGMENT_BITS) | _CONTINUE_BIT;
                System.Diagnostics.Debug.Assert(v <= 0b_00000000_00000000_00000000_11111111);
                System.Diagnostics.Debug.Assert(((uint)255 ^ (byte)0b_11111111U) == 0);
                SocketMethods.SendByte(_socket, (byte)v);

                size >>= 7;
            }

        }

        /// <exception cref="UnexpectedClientBehaviorExecption"></exception>
        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public void Recv(Buffer buffer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(SocketMethods.IsBlocking(_socket) == false);

            try
            {
                if (_data == null)
                {
                    int size = RecvSize();
                    _x = size;
                    _y = 0;

                    if (size == 0) return;

                    System.Diagnostics.Debug.Assert(_data == null);
                    System.Diagnostics.Debug.Assert(size > 0);
                    _data = new byte[size];
                }

                int availSize = _x, offset = _y;

                do
                {
                    try
                    {
                        int n = SocketMethods.RecvBytes(_socket, _data, offset, availSize);
                        System.Diagnostics.Debug.Assert(n <= availSize);

                        availSize -= n;
                        offset += n;
                    }
                    finally
                    {
                        _x = availSize;
                        _y = offset;
                    }

                } while (availSize > 0);

                buffer.WriteData(_data);

                _x = 0;
                _y = 0;
                _data = null;

                _tryAgainCount = 0;
            }
            catch (TryAgainException)
            {
                /*Console.WriteLine($"count: {_count}");*/
                if (_TimeoutLimit < _tryAgainCount++)
                {
                    throw new DataRecvTimeoutException();
                }

                throw;
            }

        }

        /// <exception cref="DisconnectedClientException"></exception>
        /// <exception cref="TryAgainException"></exception>
        public void Send(Buffer buffer)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            byte[] data = buffer.ReadData();
            SendSize(data.Length);
            SocketMethods.SendBytes(_socket, data);
        }

        public void Dispose()
        {
            // Assertions.
            System.Diagnostics.Debug.Assert(!_disposed);

            // Release resources.
            _socket.Dispose();
            _data = null;

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }

    internal sealed class User(Client client, System.Guid userId, string username)
    {
        public readonly Client CLIENT = client;
        public readonly System.Guid USER_ID = userId;
        public readonly string USERNAME = username;
    }

    public sealed class ConnectionListener : System.IDisposable
    {
        private bool _disposed = false;

        private readonly ConcurrentQueue<User> _users = new();

        ~ConnectionListener() => System.Diagnostics.Debug.Assert(false);

        internal void AddUser(Client client, System.Guid userId, string username)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _users.Enqueue(new(client, userId, username));
        }

        public void Accept(World world, Queue<(Connection, Player)> connections)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            while (true)
            {
                User? user = _users.Dequeue();
                if (user == null) break;

                if (!world.CanJoinWorld())
                {
                    // TODO: send message why disconnected.
                    user.CLIENT.Dispose();
                    continue;
                }

                Player player = world.SpawnOrFindPlayer(user.USERNAME, user.USER_ID);
                Connection conn = new(world, player, user.CLIENT);
                connections.Enqueue((conn, player));
            }

        }

        public void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

    }

    public sealed class ClientListener
    {
        private static readonly System.TimeSpan _PendingTimeout = System.TimeSpan.FromSeconds(1);

        private readonly ConnectionListener _connListener;

        public ClientListener(ConnectionListener connListener)
        {
            _connListener = connListener;
        }

        ~ClientListener() => System.Diagnostics.Debug.Assert(false);

        private int HandleVisitors(Queue<Client> visitors, Queue<int> levelQueue)
        {
            /*Console.Write(".");*/

            int count = visitors.Count;
            System.Diagnostics.Debug.Assert(count == levelQueue.Count);
            if (count == 0) return 0;

            bool close, success;

            for (; count > 0; --count)
            {
                close = success = false;

                Client client = visitors.Dequeue();
                int level = levelQueue.Dequeue();

                /*Console.WriteLine($"count: {count}, level: {level}");*/

                System.Diagnostics.Debug.Assert(level >= 0);
                using Buffer buffer = new();

                try
                {
                    if (level == 0)
                    {
                        /*Console.WriteLine("Handshake!");*/
                        client.Recv(buffer);

                        int packetId = buffer.ReadInt(true);
                        if (ServerboundHandshakingPacket.SetProtocolPacketId != packetId)
                        {
                            throw new UnexpectedPacketException();
                        }

                        SetProtocolPacket packet = SetProtocolPacket.Read(buffer);

                        if (!buffer.Empty)
                        {
                            throw new BufferOverflowException();
                        }

                        switch (packet.NextState)
                        {
                            default:
                                throw new InvalidEncodingException();
                            case Packet.States.Status:
                                level = 1;
                                break;
                            case Packet.States.Login:
                                level = 3;
                                break;
                        }

                    }

                    if (level == 1)  // Request
                    {
                        /*Console.WriteLine("Request!");*/
                        client.Recv(buffer);

                        int packetId = buffer.ReadInt(true);
                        if (ServerboundStatusPacket.RequestPacketId != packetId)
                        {
                            throw new UnexpectedPacketException();
                        }

                        RequestPacket requestPacket = RequestPacket.Read(buffer);

                        if (!buffer.Empty)
                        {
                            throw new BufferOverflowException();
                        }

                        // TODO
                        ResponsePacket responsePacket = new(100, 10, "Hello, World!");
                        responsePacket.Write(buffer);
                        client.Send(buffer);

                        level = 2;
                    }

                    if (level == 2)  // Ping
                    {
                        /*Console.WriteLine("Ping!");*/
                        client.Recv(buffer);

                        int packetId = buffer.ReadInt(true);
                        if (ServerboundStatusPacket.PingPacketId != packetId)
                        {
                            throw new UnexpectedPacketException();
                        }

                        PingPacket inPacket = PingPacket.Read(buffer);

                        if (!buffer.Empty)
                        {
                            throw new BufferOverflowException();
                        }

                        PongPacket outPacket = new(inPacket.Payload);
                        outPacket.Write(buffer);
                        client.Send(buffer);
                    }

                    if (level == 3)  // Start Login
                    {
                        client.Recv(buffer);

                        int packetId = buffer.ReadInt(true);
                        if (ServerboundLoginPacket.StartLoginPacketId != packetId)
                        {
                            throw new UnexpectedPacketException();
                        }

                        StartLoginPacket inPacket = StartLoginPacket.Read(buffer);

                        if (!buffer.Empty)
                        {
                            throw new BufferOverflowException();
                        }

                        // TODO: Check username is empty or invalid.

                        System.Console.Write("Start http request!");

                        // TODO: Use own http client in common library.
                        using System.Net.Http.HttpClient httpClient = new();
                        string url = string.Format("https://api.mojang.com/users/profiles/minecraft/{0}", inPacket.Username);
                        System.Console.WriteLine(inPacket.Username);
                        System.Console.WriteLine($"url: {url}");
                        using System.Net.Http.HttpRequestMessage request = new(System.Net.Http.HttpMethod.Get, url);

                        // TODO: handle HttpRequestException
                        using System.Net.Http.HttpResponseMessage response = httpClient.Send(request);

                        using System.IO.Stream stream = response.Content.ReadAsStream();
                        using System.IO.StreamReader reader = new(stream);
                        string str = reader.ReadToEnd();
                        System.Collections.Generic.Dictionary<string, string>? dictionary =
                            System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(str);
                        System.Diagnostics.Debug.Assert(dictionary != null);

                        System.Guid userId = System.Guid.Parse(dictionary["id"]);
                        string username = dictionary["name"];  // TODO: check username is valid
                        System.Console.WriteLine($"userId: {userId}");
                        System.Console.WriteLine($"username: {username}");

                        // TODO: Handle to throw exception
                        System.Diagnostics.Debug.Assert(inPacket.Username == username);

                        System.Console.Write("Finish http request!");

                        LoginSuccessPacket outPacket1 = new(userId, username);
                        outPacket1.Write(buffer);
                        client.Send(buffer);

                        // TODO: Must dealloc id when connection is disposed.
                        _connListener.AddUser(client, userId, username);

                        success = true;
                    }


                    System.Diagnostics.Debug.Assert(buffer.Size == 0);

                    if (!success) close = true;

                }
                catch (TryAgainException)
                {
                    System.Diagnostics.Debug.Assert(success == false);
                    System.Diagnostics.Debug.Assert(close == false);

                    /*Console.Write($"TryAgain!");*/
                }
                catch (UnexpectedClientBehaviorExecption)
                {
                    System.Diagnostics.Debug.Assert(success == false);
                    System.Diagnostics.Debug.Assert(close == false);

                    close = true;

                    buffer.Flush();

                    if (level >= 3)  // >= Start Login level
                    {
                        System.Diagnostics.Debug.Assert(false);
                        // TODO: Handle send Disconnect packet with reason.
                    }

                }
                catch (DisconnectedClientException)
                {
                    System.Diagnostics.Debug.Assert(success == false);
                    System.Diagnostics.Debug.Assert(close == false);
                    close = true;

                    /*Console.Write("~");*/

                    buffer.Flush();

                    /*Console.Write($"EndofFileException");*/
                }

                System.Diagnostics.Debug.Assert(buffer.Empty);

                if (!success)
                {
                    if (close == false)
                    {
                        visitors.Enqueue(client);
                        levelQueue.Enqueue(level);
                    }
                    else
                    {
                        client.Dispose();
                    }

                    continue;
                }
                    
                System.Diagnostics.Debug.Assert(close == false);

            }

            return visitors.Count;
        }

        public void StartRoutine(ConsoleApplication app, ushort port)
        {
            using System.Net.Sockets.Socket socket = SocketMethods.Establish(port);

            using Queue<Client> visitors = new();
            /*
             * 0: Handshake
             * 1: Request
             * 2: Ping
             * 3: Start Login
             */
            using Queue<int> levelQueue = new();

            SocketMethods.SetBlocking(socket, true);

            while (app.Running)
            {
                System.Console.Write(">");

                try
                {
                    if (!SocketMethods.IsBlocking(socket) &&
                        HandleVisitors(visitors, levelQueue) == 0)
                    {
                        SocketMethods.SetBlocking(socket, true);
                    }

                    if (SocketMethods.Poll(socket, _PendingTimeout))
                    {
                        Client client = Client.Accept(socket);
                        visitors.Enqueue(client);
                        levelQueue.Enqueue(0);

                        SocketMethods.SetBlocking(socket, false);
                    }
                }
                catch (TryAgainException)
                {
                    /*Console.WriteLine("TryAgainException!");*/
                    continue;
                }

            }

            // TODO: Handle client, send Disconnet Packet if the client's step is after StartLogin;
        }

    }


}
