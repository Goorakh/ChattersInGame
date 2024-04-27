using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using UnityEngine;

namespace ChattersInGame.Alerts
{
    public static class UserAlert
    {
        public static void AccessTokenInvalid()
        {
            Show(new AlertMessageConstant("User access token expired or was revoked, please re-authenticate"));
        }

        public static void AccessTokenAboutToExpire(TimeStamp expires)
        {
            Show(new AlertMessageTimeRemaining("Warning: Your access token will expire in {0}, it is recommended that you re-authenticate to refresh it", "Warning: Your saved access token expired {0} ago, please authenticate again to renew it", expires));
        }

        public static void HttpResponseError(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                Log.Warning($"Status code {responseMessage.StatusCode} is not an error code");
                return;
            }

            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    AccessTokenInvalid();
                    break;
                default:
                    break;
            }
        }

        public static void Show(AlertMessage message)
        {
            AsyncUtils.RunNextUnityUpdate(() =>
            {
                if (!RoR2Application.loadFinished)
                {
                    IEnumerator waitForLoadedThenShow()
                    {
                        yield return new WaitUntil(() => RoR2Application.loadFinished);

                        Show(message);
                    }

                    void showOnLoadFinished()
                    {
                        RoR2Application.instance.StartCoroutine(waitForLoadedThenShow());
                    }

                    if (RoR2Application.instance)
                    {
                        showOnLoadFinished();
                    }
                    else
                    {
                        RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, showOnLoadFinished);
                    }

                    return;
                }

                if (Run.instance)
                {
                    Chat.AddMessage($"<size=150%><color=#FF0000>{message.ConstructAlertString()}</color></size>");
                }
                else
                {
                    SimpleDialogBox dialogBox = SimpleDialogBox.Create();

                    dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("Alert");
                    dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(message.ConstructAlertString());

                    dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                }
            });
        }
    }
}
