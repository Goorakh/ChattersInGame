using ChattersInGame.Alerts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ChattersInGame.Twitch
{
    public static class AuthenticationAPI
    {
        public const string CLIENT_ID = "2bdoatvwgwkvy7qql8d7y8aqpe3xf7";

        static readonly byte[] _authRedirectResponseBytes = Encoding.ASCII.GetBytes("""
                     <!DOCTYPE html>
                     <html lang="en">
                     <head>
                         <meta charset="UTF-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     </head>
                     <body>
                     Authentication complete. You may close this window.
                        <script>
                            var url = window.location;
                            url.replace(window.location.hash, "");
                            fetch(url, {
                               method: 'GET',
                               headers: {
                                  'fragment': window.location.hash
                               }
                            });
                        </script>
                     </body>
                     """);

        static bool _isWaitingForAuthenticationResponse = false;

        static bool _isRunningTokenValidationLoop = false;

        public static async Task GenerateNewAuthenticationTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_isWaitingForAuthenticationResponse)
                return;

            string accessToken = await GetUserAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
                return;

            TwitchDataStorage.StoreToken(accessToken);

            await SetAccessTokenAsync(accessToken, cancellationToken);
        }

        public static async Task SetAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            if (!_isRunningTokenValidationLoop)
            {
                _isRunningTokenValidationLoop = true;

                _ = accessTokenValidationLoop();
            }

            await TwitchWebSocketManager.Instance.Connect(new Uri("wss://eventsub.wss.twitch.tv/ws"), cancellationToken);
        }

        static async Task accessTokenValidationLoop()
        {
            bool hasInvokedTokenAboutToExpireWarning = false;

            while (true)
            {
                if (!TwitchDataStorage.HasAccessToken)
                {
                    await Task.Delay(1000 * 5);
                    continue;
                }

                AuthenticationTokenValidationResponse validationResponse = await GetAccessTokenValidationAsync(TwitchDataStorage.AccessToken);

                if (validationResponse == null)
                {
                    // Token may have been cleared while waiting for response, if so, no need to display alert
                    if (TwitchDataStorage.HasAccessToken)
                    {
                        TwitchDataStorage.ClearToken();

                        UserAlert.AccessTokenInvalid();
                    }

                    await Task.Delay(1000 * 5);
                }
                else
                {
#if DEBUG
                    Log.Debug($"Token validated: expires {validationResponse.Expires}");
#endif

                    if (validationResponse.Expires.TimeUntil.TotalDays <= 1)
                    {
                        UserAlert.AccessTokenAboutToExpire(validationResponse.Expires);
                        hasInvokedTokenAboutToExpireWarning = true;
                    }
                    else if (hasInvokedTokenAboutToExpireWarning)
                    {
                        hasInvokedTokenAboutToExpireWarning = false;
                    }

                    // Wait 10 minutes
                    await Task.Delay(1000 * 60 * 10);
                }
            }
        }

        public static async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            _isWaitingForAuthenticationResponse = true;

            try
            {
                const string AUTH_REDIRECT = "http://localhost:4000/chatnames/oauth/redirect";
                const string SCOPES = "user:read:chat";

                string authState;
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    const int AUTH_STATE_LENGTH = 16;

                    byte[] rngBytes = new byte[AUTH_STATE_LENGTH];
                    rng.GetBytes(rngBytes);

                    StringBuilder authStateBuilder = new StringBuilder(AUTH_STATE_LENGTH * 2);
                    for (int i = 0; i < AUTH_STATE_LENGTH; i++)
                    {
                        authStateBuilder.Append(rngBytes[i].ToString("x2"));
                    }

                    authState = authStateBuilder.ToString();
                }

                string authorizeUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={CLIENT_ID}&redirect_uri={AUTH_REDIRECT}&scope={HttpUtility.UrlEncode(SCOPES)}&state={authState}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = authorizeUrl,
                    UseShellExecute = true
                });

                using (HttpListener httpListener = new HttpListener())
                {
                    httpListener.Prefixes.Add(AUTH_REDIRECT + "/");
                    httpListener.Start();

                    HttpListenerContext context = await httpListener.GetContextAsync();

                    context.Response.ContentType = "text/html";
                    context.Response.ContentEncoding = Encoding.ASCII;
                    context.Response.ContentLength64 = _authRedirectResponseBytes.LongLength;

                    await context.Response.OutputStream.WriteAsync(_authRedirectResponseBytes, 0, _authRedirectResponseBytes.Length, cancellationToken);

                    context.Response.Close();

                    context = await httpListener.GetContextAsync();

                    string fragmentHeader = context.Request.Headers["fragment"];
                    Uri url = context.Request.Url;

                    Dictionary<string, string> queries = [];
                    if (url != null)
                    {
                        string query = url.Query;
                        if (!string.IsNullOrEmpty(query))
                        {
                            // Remove leading '?' character
                            query = query.Substring(1);

                            UrlUtils.SplitUrlQueries(query, queries);
                        }
                    }

                    Dictionary<string, string> fragments = [];
                    if (!string.IsNullOrEmpty(fragmentHeader))
                    {
                        // Remove leading '#' character
                        fragmentHeader = fragmentHeader.Substring(1);

                        UrlUtils.SplitUrlQueries(fragmentHeader, fragments);
                    }

                    context.Response.Close();

                    if (!fragments.TryGetValue("state", out string receicedAuthState) || !string.Equals(authState, receicedAuthState))
                    {
                        Log.Error("Invalid auth state");
                        return null;
                    }
                    else if (fragments.TryGetValue("access_token", out string accessToken))
                    {
                        return accessToken;
                    }
                    else
                    {
                        Log.Error("No access token was received");
                        return null;
                    }
                }
            }
            finally
            {
                _isWaitingForAuthenticationResponse = false;
            }
        }

        public static async Task<AuthenticationTokenValidationResponse> GetAccessTokenValidationAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            using HttpResponseMessage validationResponse = await client.GetAsync("https://id.twitch.tv/oauth2/validate", cancellationToken);

            if (!validationResponse.IsSuccessStatusCode)
            {
                UserAlert.HttpResponseError(validationResponse);
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<AuthenticationTokenValidationResponse>(await validationResponse.Content.ReadAsStringAsync());
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
