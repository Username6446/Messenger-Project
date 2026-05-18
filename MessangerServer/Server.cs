using MessengerServer.Data;
using MessengerServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessengerServer;

public class Server
{
    private readonly TcpListener _listener;
    private readonly int _port;
    private bool _isRunning;
    private List<ClientHandler> _connectedClients = new();
    private readonly object _clientLock = new();
    private readonly DbService _dbService;
    private readonly AuthService _authService;
    private readonly ChatService _chatService;
    private readonly UserService _userService;

    public Server(int port = 5000)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _dbService = new DbService();
        _authService = new AuthService(_dbService);
        _chatService = new ChatService(_dbService);
        _userService = new UserService(_dbService);
    }

    public async Task StartAsync()
    {
        try
        {
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"🚀 Server started on port {_port}");
            Console.WriteLine($"📡 Waiting for connections...\n");
            Console.WriteLine("════════════════════════════════════════");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                    Console.WriteLine($"\n✅ New client connected: {remoteEndPoint}");
                    Console.WriteLine($"   Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"   Active connections: {_connectedClients.Count + 1}");

                    var handler = new ClientHandler(client, _authService, _chatService, _userService, this);
                    lock (_clientLock)
                    {
                        _connectedClients.Add(handler);
                    }

                    _ = handler.HandleAsync();
                }
                catch (SocketException ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"⚠️ Socket error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Server fatal error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        try
        {
            _listener.Stop();
        }
        catch { }

        Console.WriteLine("\n🛑 Server stopped");
    }


    public List<ClientHandler> GetClientsByUserId(int userId)
    {
        lock (_clientLock)
        {
            return _connectedClients.Where(c => c.CurrentUserId == userId).ToList();
        }
    }

    public List<ClientHandler> GetClientsInChat(int chatId)
    {
        lock (_clientLock)
        {
            return _connectedClients
                .Where(c => c.CurrentUserId > 0)
                .Where(c => _chatService.IsUserInChatAsync(c.CurrentUserId, chatId).Result)
                .ToList();
        }
    }

    public void RemoveClient(ClientHandler handler)
    {
        lock (_clientLock)
        {
            _connectedClients.Remove(handler);
            Console.WriteLine($"❌ Client disconnected: {handler.ClientEndPoint}");
            Console.WriteLine($"   Active connections: {_connectedClients.Count}");
        }
    }

    public async Task BroadcastToChatAsync(int chatId, object message)
    {
        try
        {

            var memberIds = await _chatService.GetChatMemberIdsAsync(chatId);

            Console.WriteLine($"📡 Broadcasting to chat {chatId}");
            Console.WriteLine($"   Members to notify: {string.Join(", ", memberIds)}");

      
            lock (_clientLock)
            {
                int sentCount = 0;
                foreach (var client in _connectedClients)
                {

                    if (client.CurrentUserId > 0 && memberIds.Contains(client.CurrentUserId))
                    {
                        Console.WriteLine($"   ✓ Sending to user {client.CurrentUserId} ({client.ClientEndPoint})");
                        _ = client.SendMessageAsync(message); 
                        sentCount++;
                    }
                }
                Console.WriteLine($"   Total sent: {sentCount}/{memberIds.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BroadcastToChatAsync error: {ex.Message}");
        }
    }


    public async Task BroadcastToUserAsync(int userId, object message)
    {
        try
        {
            lock (_clientLock)
            {
                var userClients = _connectedClients.Where(c => c.CurrentUserId == userId).ToList();

                foreach (var client in userClients)
                {
                    Console.WriteLine($"📤 Sending to user {userId}");
                    _ = client.SendMessageAsync(message); 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BroadcastToUserAsync error: {ex.Message}");
        }
    }

    public async Task BroadcastToAllAsync(object message)
    {
        try
        {
            lock (_clientLock)
            {
                foreach (var client in _connectedClients)
                {
                    if (client.CurrentUserId > 0)
                    {
                        _ = client.SendMessageAsync(message); 
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BroadcastToAllAsync error: {ex.Message}");
        }
    }
}