using ChattersInGame.Alerts;
using ChattersInGame.Twitch.Chat;
using ChattersInGame.Twitch.Chat.Message;
using ChattersInGame.Twitch.Chat.Notification;
using ChattersInGame.Twitch.ThirdParty;
using ChattersInGame.Twitch.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public class TwitchWebSocketManager
    {
#if DEBUG
        const string DEBUG_BROADCASTER_USERNAME_OVERRIDE = "admiralbahroo";
#endif

        public static TwitchWebSocketManager Instance { get; } = new TwitchWebSocketManager();

        readonly HashSet<string> _handledMessageIDs = [];

        readonly HashSet<string> _activeSubscriptions = [];

        readonly ConcurrentQueue<TwitchWebSocketMessage> _notificationsQueue = [];

        CancellationTokenSource _disconnectedTokenSource = new CancellationTokenSource();

        TwitchWebSocketClientConnection _mainConnection;
        TwitchWebSocketClientConnection _migratingConnection;

        string _sessionID;

        public bool IsConnecting => _mainConnection != null && _mainConnection.State == WebSocketState.Connecting;

        public bool IsConnected => _mainConnection != null && _mainConnection.State == WebSocketState.Open;

        public bool IsMigrating => _migratingConnection != null;

        private TwitchWebSocketManager()
        {
        }

        public async Task Connect(Uri uri, CancellationToken cancellationToken)
        {
            _disconnectedTokenSource?.Dispose();
            _disconnectedTokenSource = new CancellationTokenSource();

            using CancellationTokenSource cancelledOrDisconnectedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disconnectedTokenSource.Token);

            if (_mainConnection == null)
            {
                _mainConnection = new TwitchWebSocketClientConnection(uri, this);
                await _mainConnection.Connect(cancelledOrDisconnectedSource.Token);
            }
            else
            {
                _mainConnection.ConnectionUrl = uri;
                await _mainConnection.Reconnect(TimeSpan.FromSeconds(0.5), cancelledOrDisconnectedSource.Token);
            }

            _ = Task.Run(() => handleNotificationsLoop(_disconnectedTokenSource.Token), _disconnectedTokenSource.Token);
        }

        public async Task Disconnect(CancellationToken cancellationToken)
        {
            _disconnectedTokenSource.Cancel();

            Task mainDisconnectTask;
            if (_mainConnection != null)
            {
                mainDisconnectTask = _mainConnection.Disconnect(cancellationToken).ContinueWith(_ =>
                {
                    _mainConnection?.Dispose();
                    _mainConnection = null;
                });
            }
            else
            {
                mainDisconnectTask = Task.CompletedTask;
            }

            Task migratingDisconnectTask;
            if (_migratingConnection != null)
            {
                migratingDisconnectTask = _migratingConnection.Disconnect(cancellationToken).ContinueWith(_ =>
                {
                    _migratingConnection?.Dispose();
                    _migratingConnection = null;
                });
            }
            else
            {
                migratingDisconnectTask = Task.CompletedTask;
            }

            await Task.WhenAll(mainDisconnectTask, migratingDisconnectTask);

            if (_activeSubscriptions.Count > 0)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchDataStorage.AccessToken}");
                    httpClient.DefaultRequestHeaders.Add("Client-Id", AuthenticationAPI.CLIENT_ID);

                    Task[] deleteSubscriptionTasks = new Task[_activeSubscriptions.Count];

                    int taskIndex = 0;
                    foreach (string subscriptionId in _activeSubscriptions)
                    {
                        deleteSubscriptionTasks[taskIndex++] = Task.Run(async () =>
                        {
                            await Task.Yield();

                            HttpResponseMessage deleteResult;
                            try
                            {
                                deleteResult = await httpClient.DeleteAsync($"https://api.twitch.tv/helix/eventsub/subscriptions?id={subscriptionId}", cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                Log.Error("Subscription delete request timed out");
                                return;
                            }

                            using HttpResponseMessage subscriptionDeleteResult = deleteResult;

                            if (!subscriptionDeleteResult.IsSuccessStatusCode)
                            {
                                Log.Warning($"Unable to delete subscription {subscriptionId}: {subscriptionDeleteResult.StatusCode}");
                            }
#if DEBUG
                            else
                            {
                                Log.Debug($"Removed subscription {subscriptionId}");
                            }
#endif
                        }, cancellationToken);
                    }

                    _activeSubscriptions.Clear();

                    await Task.WhenAll(deleteSubscriptionTasks);
                }
            }
        }

        void beginMigration(string reconnectUrl)
        {
            _migratingConnection = new TwitchWebSocketClientConnection(new Uri(reconnectUrl), this);
            _ = _migratingConnection.Connect();
        }

        public async Task HandleJsonMessageAsync(JToken jsonObject, CancellationToken cancellationToken)
        {
            JToken messageIdToken = jsonObject.SelectToken("metadata.message_id", false);
            if (messageIdToken == null)
            {
                Log.Error("Could not deserialize message_id property");
                return;
            }

            if (!_handledMessageIDs.Add(messageIdToken.ToObject<string>()))
                return;

            JToken messageTypeToken = jsonObject.SelectToken("metadata.message_type", false);
            if (messageTypeToken == null)
            {
                Log.Error("Could not deserialize message_type property");
                return;
            }

            using CancellationTokenSource cancelledOrDisconnectedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disconnectedTokenSource.Token);

            string messageType = messageTypeToken.ToObject<string>();
            switch (messageType)
            {
                case "session_welcome":
                    await handleSessionWelcomeMessage(jsonObject, cancelledOrDisconnectedSource.Token);
                    break;
                case "session_keepalive":
                    // This message has no payload, no handling needs to be done. All relevant processing this needs is done above.
                    break;
                case "notification":
                    _notificationsQueue.Enqueue(new TwitchWebSocketMessage(jsonObject));
                    break;
                case "session_reconnect":
                    handleSessionReconnectMessage(jsonObject);
                    break;
                case "revocation":
                    await handleRevokeMessageAsync(jsonObject, cancelledOrDisconnectedSource.Token);
                    break;
                default:
                    Log.Warning($"Unhandled message type: {messageType}");
                    break;
            }
        }

        void handleSessionReconnectMessage(JToken jsonObject)
        {
            JToken sessionDataToken = jsonObject.SelectToken("payload.session", false);
            if (sessionDataToken == null)
            {
                Log.Error("Could not deserialize session data");
                return;
            }

            WebSocketSessionData sessionData;
            try
            {
                sessionData = sessionDataToken.ToObject<WebSocketSessionData>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize session object: {e}");
                return;
            }

#if DEBUG
            Log.Debug($"Starting WebSocket migration {_mainConnection.ConnectionUrl}->{sessionData.ReconnectUrl}");
#endif

            beginMigration(sessionData.ReconnectUrl);
        }

        async Task handleRevokeMessageAsync(JToken jsonObject, CancellationToken cancellationToken)
        {
            // TODO: Handle this properly
            UserAlert.Show(new AlertMessageConstant("Access revoked, this is not handled, please restart the game or re-authenticate"));

            await Disconnect(default);
        }

        async Task handleSessionWelcomeMessage(JToken jsonObject, CancellationToken cancellationToken)
        {
            JToken sessionDataToken = jsonObject.SelectToken("payload.session", false);
            if (sessionDataToken == null)
            {
                Log.Error("Could not deserialize session data");
                return;
            }

            WebSocketSessionData sessionData;
            try
            {
                sessionData = sessionDataToken.ToObject<WebSocketSessionData>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize session object: {e}");
                return;
            }

            if (IsMigrating)
            {
                if (_mainConnection != null)
                {
                    _mainConnection.Dispose();
                    _mainConnection = null;
                }

                _mainConnection = _migratingConnection;

#if DEBUG
                Log.Debug("Completed WebSocket migration");
#endif
                return;
            }

            _sessionID = sessionData.SessionID;

            if (!TwitchDataStorage.HasAccessToken)
            {
                Log.Error("No authentication token stored");
                return;
            }

            AuthenticationTokenValidationResponse tokenValidationResponse = await AuthenticationAPI.GetAccessTokenValidationAsync(TwitchDataStorage.AccessToken, cancellationToken);
            if (tokenValidationResponse == null)
            {
                Log.Error("Authentication token is not valid");
                return;
            }

            async Task sendSubscription<T>(T message, CancellationToken cancellationToken)
            {
                using HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchDataStorage.AccessToken}");
                client.DefaultRequestHeaders.Add("Client-Id", AuthenticationAPI.CLIENT_ID);

                StringContent content = new StringContent(JsonConvert.SerializeObject(message));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using HttpResponseMessage subscribeResponseMessage = await client.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content, cancellationToken);

                if (!subscribeResponseMessage.IsSuccessStatusCode)
                {
                    Log.Error($"Subscribe failed: {subscribeResponseMessage.StatusCode}");
                    return;
                }

                using StreamReader responseReader = new StreamReader(await subscribeResponseMessage.Content.ReadAsStreamAsync(), Encoding.UTF8);

#if DEBUG
                Log.Debug(await responseReader.ReadToEndAsync());
                responseReader.BaseStream.Position = 0;
#endif

                using JsonReader responseJsonReader = new JsonTextReader(responseReader);

                JToken responseObject;
                try
                {
                    responseObject = await JToken.ReadFromAsync(responseJsonReader, cancellationToken);
                }
                catch (JsonException e)
                {
                    Log.Error($"Failed to deserialize subscribe response: {e}");
                    return;
                }

                JArray responseDataArrayToken = responseObject.SelectToken("data", false) as JArray;
                if (responseDataArrayToken == null || responseDataArrayToken.Count <= 0)
                {
                    Log.Error($"Subscribe response contained invalid data");
                    return;
                }

                JToken subscriptionIdToken = responseDataArrayToken[0].SelectToken("id");
                if (subscriptionIdToken == null)
                {
                    Log.Error($"Could not find subscription id");
                    return;
                }

                _activeSubscriptions.Add(subscriptionIdToken.ToObject<string>());
            }

            string broadcasterId = tokenValidationResponse.UserID;

#if DEBUG
            if (DEBUG_BROADCASTER_USERNAME_OVERRIDE != null)
            {
                GetUsersResponse broadcasterUsersResponse = await TwitchAPI.GetUsers([], [DEBUG_BROADCASTER_USERNAME_OVERRIDE], cancellationToken);

                if (broadcasterUsersResponse != null && broadcasterUsersResponse.Users.Length > 0)
                {
                    broadcasterId = broadcasterUsersResponse.Users[0].UserId;
                }
            }
#endif

            string userId = tokenValidationResponse.UserID;

            _ = ThirdPartyEmoteManager.RefreshThirdPartyEmotes(broadcasterId);

            await sendSubscription(new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = broadcasterId,
                    user_id = userId
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionID
                }
            }, cancellationToken);

            await sendSubscription(new
            {
                type = "channel.chat.notification",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = broadcasterId,
                    user_id = userId
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionID
                }
            }, cancellationToken);

            await sendSubscription(new
            {
                type = "channel.chat.clear_user_messages",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = broadcasterId,
                    user_id = userId
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionID
                }
            }, cancellationToken);
        }

        void handleNotificationsLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_notificationsQueue.TryDequeue(out TwitchWebSocketMessage webSocketMessage))
                {
                    handleNotificationMessage(webSocketMessage.JToken);
                }
            }
        }

        void handleNotificationMessage(JToken jsonObject)
        {
            JToken subscriptionTypeToken = jsonObject.SelectToken("payload.subscription.type", false);
            if (subscriptionTypeToken == null)
            {
                Log.Error("Failed to find subscription type");
                return;
            }

            string subscriptionType = subscriptionTypeToken.ToObject<string>();
            switch (subscriptionType)
            {
                case "channel.chat.message":
                    handleChannelChatMessageNotification(jsonObject);
                    break;
                case "channel.chat.notification":
                    handleChannelChatNotificationNotification(jsonObject);
                    break;
                case "channel.chat.clear_user_messages":
                    handleChannelChatClearUserMessagesNotification(jsonObject);
                    break;
                default:
                    Log.Warning($"Unhandled notification message: {subscriptionType}");
                    break;
            }
        }

        void handleChannelChatMessageNotification(JToken jsonObject)
        {
            JToken eventToken = jsonObject.SelectToken("payload.event");
            if (eventToken == null)
            {
                Log.Error("Failed to find event object");
                return;
            }

            ChannelChatMessageEvent chatMessageEvent;
            try
            {
                chatMessageEvent = eventToken.ToObject<ChannelChatMessageEvent>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize chat message: {e}");
                return;
            }

            if (string.IsNullOrEmpty(chatMessageEvent.ChatterUserId))
            {
                Log.Error("No user Id speficied for chat message");
                return;
            }

            ChatterInfo chatterInfo = ChatterManager.BumpChatter(chatMessageEvent.ChatterUserId);
            chatterInfo.ColorCode = chatMessageEvent.UserColor;

            chatterInfo.OnUserMessage(chatMessageEvent.Message);
        }

        void handleChannelChatNotificationNotification(JToken jsonObject)
        {
            JToken eventToken = jsonObject.SelectToken("payload.event");
            if (eventToken == null)
            {
                Log.Error("Failed to find event object");
                return;
            }

            ChannelChatNotificationEvent chatNotificationEvent;
            try
            {
                chatNotificationEvent = eventToken.ToObject<ChannelChatNotificationEvent>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize chat notification: {e}");
                return;
            }

            if (chatNotificationEvent.AnonymousChatter)
                return;

            if (string.IsNullOrEmpty(chatNotificationEvent.ChatterUserId))
            {
                Log.Error("No user Id speficied for chat notification");
                return;
            }

            ChatterInfo chatterInfo = ChatterManager.BumpChatter(chatNotificationEvent.ChatterUserId);
            chatterInfo.ColorCode = chatNotificationEvent.ChatterNameColor;

            if (chatNotificationEvent.Message != null)
            {
                chatterInfo.OnUserMessage(chatNotificationEvent.Message);
            }
        }

        void handleChannelChatClearUserMessagesNotification(JToken jsonObject)
        {
            JToken eventToken = jsonObject.SelectToken("payload.event");
            if (eventToken == null)
            {
                Log.Error("Failed to find event object");
                return;
            }

            ChannelChatClearUserMessagesEvent clearUserMessagesEvent;
            try
            {
                clearUserMessagesEvent = eventToken.ToObject<ChannelChatClearUserMessagesEvent>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize chat clear-user-messages event: {e}");
                return;
            }

            // A chatter was banned or timed out, remove them from the chatters list
            ChatterManager.RemoveChatter(clearUserMessagesEvent.TargetUserId);
        }
    }
}
