using ChattersInGame.Twitch.Emotes;
using ChattersInGame.Twitch.User;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public static class TwitchAPI
    {
        public static async Task<GetUsersResponse> GetUsers(string[] userIds, string[] usernames, CancellationToken cancellationToken = default)
        {
            userIds ??= [];
            usernames ??= [];

            if (userIds.Length == 0 && usernames.Length == 0)
                return GetUsersResponse.Empty;

            if (userIds.Length + usernames.Length > 100)
                throw new ArgumentOutOfRangeException($"{nameof(userIds)}, {nameof(usernames)}", "Combined size of user ids and usernames cannot exceed 100");

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchDataStorage.AccessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", AuthenticationAPI.CLIENT_ID);

            string combinedUserIds = string.Join("&", Array.ConvertAll(userIds, id => $"id={id}"));
            string combinedUsernames = string.Join("&", Array.ConvertAll(usernames, username => $"login={username}"));

            string query;
            if (string.IsNullOrEmpty(combinedUsernames))
            {
                query = combinedUserIds;
            }
            else if (string.IsNullOrEmpty(combinedUserIds))
            {
                query = combinedUsernames;
            }
            else
            {
                query = string.Join("&", [combinedUserIds, combinedUsernames]);
            }

            using HttpResponseMessage getUsersResponseMessage = await client.GetAsync($"https://api.twitch.tv/helix/users?{query}");
            if (!getUsersResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"Twitch API responded with error code {getUsersResponseMessage.StatusCode}");
                return GetUsersResponse.Empty;
            }

            GetUsersResponse getUsersResponse;
            try
            {
                getUsersResponse = JsonConvert.DeserializeObject<GetUsersResponse>(await getUsersResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize user data: {e}");
                return GetUsersResponse.Empty;
            }

            return getUsersResponse;
        }

        public static async Task<GetEmoteSetResponse> GetEmoteSets(string[] setIds, CancellationToken cancellationToken = default)
        {
            if (setIds == null || setIds.Length == 0)
                return GetEmoteSetResponse.Empty;

            const int MAX_SET_IDS = 25;
            if (setIds.Length > MAX_SET_IDS)
            {
                Log.Warning($"Too many set ids specified: {setIds.Length}, max={MAX_SET_IDS}");
                Array.Resize(ref setIds, MAX_SET_IDS);
            }

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchDataStorage.AccessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", AuthenticationAPI.CLIENT_ID);

            string url = $"https://api.twitch.tv/helix/chat/emotes/set?{string.Join("&", Array.ConvertAll(setIds, setId => $"emote_set_id={setId}"))}";

            using HttpResponseMessage getEmoteSetResponseMessage = await client.GetAsync(url, cancellationToken);
            if (!getEmoteSetResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"Twitch API responded with error code {getEmoteSetResponseMessage.StatusCode}");
                return GetEmoteSetResponse.Empty;
            }

            GetEmoteSetResponse getEmoteSetResponse;
            try
            {
                getEmoteSetResponse = JsonConvert.DeserializeObject<GetEmoteSetResponse>(await getEmoteSetResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize emote set data: {e}");
                return GetEmoteSetResponse.Empty;
            }

            return getEmoteSetResponse;
        }
    }
}
