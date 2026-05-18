using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger_Project.Services
{
    public class ServerService
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected = true;
        private int _currentUserId = 0;
        private string _currentUsername = "";

        public static readonly string ServerHost = "localhost";
        public static readonly int ServerPort = 5000;

        private readonly string _serverHost = ServerHost;
        private readonly int _serverPort = ServerPort;

        private Dictionary<string, TaskCompletionSource<ServerMessage>> _pendingRequests = new();
        private readonly object _pendingLock = new();
        private int _requestId = 0;

        private readonly SemaphoreSlim _streamLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _listenerCts;

        public event EventHandler<ServerMessageEventArgs>? OnMessageReceived;
        public event EventHandler<string>? OnConnectionStatusChanged;

        public int CurrentUserId => _currentUserId;
        public string CurrentUsername => _currentUsername;
        public bool IsConnected => _isConnected && _client?.Connected == true;

        public async Task ConnectAsync(string host = "localhost", int port = 5000)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _isConnected = true;

                _listenerCts = new CancellationTokenSource();

                OnConnectionStatusChanged?.Invoke(this, "✅ Connected to server");

                _ = ListenForMessagesAsync(_listenerCts.Token);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectionStatusChanged?.Invoke(this, $"❌ Connection failed: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            _isConnected = false;

            _listenerCts?.Cancel();
            _listenerCts?.Dispose();

            await Task.Delay(100);

            _stream?.Dispose();
            _client?.Dispose();
            _streamLock?.Dispose();

            _currentUserId = 0;
            _currentUsername = "";

            OnConnectionStatusChanged?.Invoke(this, "Disconnected");
        }

        public void Disconnect()
        {
            _isConnected = false;

            _listenerCts?.Cancel();
            _listenerCts?.Dispose();

            _stream?.Dispose();
            _client?.Dispose();
            _streamLock?.Dispose();

            _currentUserId = 0;
            _currentUsername = "";

            OnConnectionStatusChanged?.Invoke(this, "Disconnected");
        }

        public async Task<(bool success, string? error, JsonElement? data)> LoginAsync(string username, string password)
        {
            var request = new ServerMessage
            {
                Action = "login",
                Data = JsonSerializer.SerializeToElement(new
                {
                    username,
                    password
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> RegisterAsync(string username, string password)
        {
            var request = new ServerMessage
            {
                Action = "register",
                Data = JsonSerializer.SerializeToElement(new
                {
                    username,
                    password
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> SendMessageAsync(int chatId, string text)
        {
            var request = new ServerMessage
            {
                Action = "send_message",
                Data = JsonSerializer.SerializeToElement(new
                {
                    chat_id = chatId,
                    text,
                    type = "Text"
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> GetMessagesAsync(int chatId, int limit = 50)
        {
            var request = new ServerMessage
            {
                Action = "get_messages",
                Data = JsonSerializer.SerializeToElement(new
                {
                    chat_id = chatId,
                    limit
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> GetChatsAsync()
        {
            var request = new ServerMessage
            {
                Action = "get_chats"
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> SearchUsersAsync(string query)
        {
            var request = new ServerMessage
            {
                Action = "search_users",
                Data = JsonSerializer.SerializeToElement(new
                {
                    query
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> CreateGroupAsync(string name, string? description = null)
        {
            var request = new ServerMessage
            {
                Action = "create_group",
                Data = JsonSerializer.SerializeToElement(new
                {
                    name,
                    description
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> CreateDirectChatAsync(int contactUserId)
        {
            var request = new ServerMessage
            {
                Action = "create_direct_chat",
                Data = JsonSerializer.SerializeToElement(new
                {
                    contact_user_id = contactUserId
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> GetContactsAsync()
        {
            var request = new ServerMessage
            {
                Action = "get_contacts"
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> AddContactAsync(int contactUserId)
        {
            var request = new ServerMessage
            {
                Action = "add_contact",
                Data = JsonSerializer.SerializeToElement(new
                {
                    contact_user_id = contactUserId
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> GetUserProfileAsync(int userId)
        {
            var request = new ServerMessage
            {
                Action = "get_user_profile",
                Data = JsonSerializer.SerializeToElement(new
                {
                    user_id = userId
                })
            };

            return await SendRequestAsync(request);
        }

        public async Task<(bool success, string? error, JsonElement? data)> UpdateProfileAsync(
            string? username = null,
            string? bio = null,
            DateTime? birthDate = null,
            string? avatarPath = null)
        {
            var request = new ServerMessage
            {
                Action = "update_profile",
                Data = JsonSerializer.SerializeToElement(new
                {
                    username,
                    bio,
                    birth_date = birthDate,
                    avatar_path = avatarPath
                })
            };

            return await SendRequestAsync(request);
        }

        private async Task<(bool success, string? error, JsonElement? data)> SendRequestAsync(ServerMessage request, int timeoutMs = 10000)
        {
            if (!IsConnected || _stream == null)
                return (false, "❌ Not connected to server", null);

            try
            {
                string requestId = $"req_{++_requestId}";
                request.RequestId = requestId;

                var tcs = new TaskCompletionSource<ServerMessage>();
                lock (_pendingLock)
                {
                    _pendingRequests[requestId] = tcs;
                }

                await _streamLock.WaitAsync();

                try
                {
                    string json = JsonSerializer.Serialize(request);
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    await _stream.WriteAsync(buffer, 0, buffer.Length);
                    await _stream.FlushAsync();

                    Console.WriteLine($"📤 Sent [{requestId}]: {request.Action}");
                }
                finally
                {
                    _streamLock.Release();
                }

                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    try
                    {
                        var response = await tcs.Task.ConfigureAwait(false);
                        Console.WriteLine($"📥 Received [{requestId}]: {response.Action} - Success: {response.Success}");
                        return (response.Success, response.Error, response.Data);
                    }
                    catch (OperationCanceledException)
                    {
                        lock (_pendingLock)
                        {
                            _pendingRequests.Remove(requestId);
                        }
                        return (false, $"❌ Request timeout ({timeoutMs}ms)", null);
                    }
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                return (false, $"❌ {ex.Message}", null);
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isConnected && _client?.Connected == true && !cancellationToken.IsCancellationRequested)
                {
                    var message = await ReadMessageAsync(cancellationToken);
                    if (message == null)
                    {
                        _isConnected = false;
                        Console.WriteLine("❌ Connection lost - stream returned null");
                        break;
                    }

                    if (!string.IsNullOrEmpty(message.RequestId))
                    {
                        lock (_pendingLock)
                        {
                            if (_pendingRequests.TryGetValue(message.RequestId, out var tcs))
                            {
                                _pendingRequests.Remove(message.RequestId);
                                tcs.SetResult(message);
                                Console.WriteLine($"✅ Matched response to request {message.RequestId}");
                            }
                        }
                    }
                    else
                    {
                        OnMessageReceived?.Invoke(this, new ServerMessageEventArgs { Message = message });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("📡 Listener cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Listener error: {ex.Message}");
                _isConnected = false;
            }
        }

        private async Task<ServerMessage?> ReadMessageAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_stream == null)
                    return null;

                byte[] buffer = new byte[8192];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (bytesRead == 0)
                {
                    Console.WriteLine("❌ Server closed connection");
                    return null;
                }

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"🔍 Raw message: {json.Substring(0, Math.Min(100, json.Length))}...");

                var message = JsonSerializer.Deserialize<ServerMessage>(json);
                return message;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading message: {ex.Message}");
                return null;
            }
        }

        public void SetCurrentUserId(int userId, string username = "")
        {
            _currentUserId = userId;
            _currentUsername = username;
        }
    }

    public class ServerMessageEventArgs : EventArgs
    {
        public ServerMessage? Message { get; set; }
    }

    public class ServerMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public System.Text.Json.JsonElement? Data { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Error { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [System.Text.Json.Serialization.JsonPropertyName("request_id")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? RequestId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("broadcastTo")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? BroadcastTo { get; set; }
    }
}