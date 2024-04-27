using Newtonsoft.Json.Linq;

namespace ChattersInGame.Twitch
{
    public class TwitchWebSocketMessage
    {
        public JToken JToken { get; }

        public TwitchWebSocketMessage(JToken jToken)
        {
            JToken = jToken;
        }
    }
}
