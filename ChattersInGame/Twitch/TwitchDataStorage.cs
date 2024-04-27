using ChattersInGame.Alerts;
using R2API.Utils;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public static class TwitchDataStorage
    {
        static readonly string _storagePath = PersistentDataStorage.GetAbsoluteSavePath("twitch/");

        static readonly string _authFilePath = Path.Combine(_storagePath, "usertoken");
        public static string EmotesCachePath { get; } = Path.Combine(_storagePath, "cache/emotes/");

        public static string AccessToken { get; private set; }
        public static bool HasAccessToken => !string.IsNullOrEmpty(AccessToken);

        public static async Task LoadAndValidateAccessToken(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_authFilePath))
                return;

            string accessToken = File.ReadAllText(_authFilePath);

            AuthenticationTokenValidationResponse validationResponse = await AuthenticationAPI.GetAccessTokenValidationAsync(accessToken, cancellationToken);
            if (validationResponse == null)
            {
                UserAlert.AccessTokenInvalid();
                ClearToken();
                return;
            }

            AccessToken = accessToken;

#if DEBUG
            Log.Debug($"Loaded stored access token {accessToken}");
#endif

            await AuthenticationAPI.SetAccessTokenAsync(accessToken, cancellationToken);
        }

        public static bool TryGetStoredToken(out string accessToken)
        {
            if (File.Exists(_authFilePath))
            {
                accessToken = File.ReadAllText(_authFilePath);
                return true;
            }

            accessToken = null;
            return false;
        }

        public static void ClearToken()
        {
            AccessToken = null;

            if (File.Exists(_authFilePath))
            {
                File.Delete(_authFilePath);
            }
        }

        public static void StoreToken(string accessToken)
        {
            AccessToken = accessToken;

            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);

            File.WriteAllText(_authFilePath, accessToken);

#if DEBUG
            Log.Debug($"Stored access token: {accessToken}");
#endif
        }

        public static bool TryGetEmoteData(string emoteId, out EmoteData emoteData)
        {
            string emoteDirectory = Path.Combine(EmotesCachePath, emoteId);
            if (!Directory.Exists(emoteDirectory))
            {
                emoteData = default;
                return false;
            }

            string metadataPath = Path.Combine(emoteDirectory, "meta");
            if (!File.Exists(metadataPath))
            {
                emoteData = default;
                return false;
            }

            string imagePath = Path.Combine(emoteDirectory, "img.png");
            if (!File.Exists(imagePath))
            {
                emoteData = default;
                return false;
            }

            emoteData = new EmoteData();

            using (FileStream fileStream = File.Open(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                emoteData.Metadata = new EmoteMetadata();

                using BinaryReader fileReader = new BinaryReader(fileStream);
                emoteData.Metadata.Deserialize(fileReader);
            }

            using (FileStream fileStream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long fileLength = fileStream.Length;

                emoteData.ImageBytes = new byte[fileLength];
                int numReadBytes = fileStream.Read(emoteData.ImageBytes, 0, (int)fileLength);

                if (numReadBytes < fileLength)
                {
                    Log.Error($"Didn't read full image file, read {numReadBytes}/{fileLength} bytes");
                    return false;
                }
            }

            return true;
        }

        public static void StoreEmoteData(string emoteId, EmoteData emoteData)
        {
            string emoteDirectory = Path.Combine(EmotesCachePath, emoteId);
            if (!Directory.Exists(emoteDirectory))
            {
                Directory.CreateDirectory(emoteDirectory);
            }

            string metadataPath = Path.Combine(emoteDirectory, "meta");
            using (FileStream fileStream = File.Open(metadataPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using BinaryWriter writer = new BinaryWriter(fileStream);

                emoteData.Metadata.Serialize(writer);
            }

            string imagePath = Path.Combine(emoteDirectory, "img.png");
            using (FileStream fileStream = File.Open(imagePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileStream.Write(emoteData.ImageBytes, 0, emoteData.ImageBytes.Length);
            }
        }
    }
}
