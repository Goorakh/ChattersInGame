using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public class TwitchWebSocketClientConnection : WebSocketClientConnection
    {
        readonly TwitchWebSocketManager _owner;

        public TwitchWebSocketClientConnection(Uri url, TwitchWebSocketManager owner) : base(url)
        {
            _owner = owner;
        }

        protected override async Task handleSocketMessageAsync(WebSocketMessage message, CancellationToken cancellationToken)
        {
            await base.handleSocketMessageAsync(message, cancellationToken);

            if (message.MessageType == WebSocketMessageType.Text)
            {
#if DEBUG
                Log.Debug($"Received message: {Encoding.UTF8.GetString(message.MessageData.Array, message.MessageData.Offset, message.MessageData.Count)}");
#endif
            }
            else
            {
#if DEBUG
                Log.Debug($"Received message: {message.MessageData.Count} byte(s)");
#endif

                Log.Warning($"Unhandled socket message type: {message.MessageType}");

                return;
            }

            using MemoryStream memoryStream = new MemoryStream(message.MessageData.Array, message.MessageData.Offset, message.MessageData.Count);
            using StreamReader streamReader = new StreamReader(memoryStream, Encoding.UTF8);
            using JsonTextReader jsonReader = new JsonTextReader(streamReader);

            JToken jsonObject;
            try
            {
                jsonObject = await JToken.ReadFromAsync(jsonReader, cancellationToken);
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize web socket message: {e}");
                return;
            }

            await _owner.HandleJsonMessageAsync(jsonObject, cancellationToken);
        }
    }
}
