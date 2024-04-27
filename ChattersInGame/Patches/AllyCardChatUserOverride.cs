using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ChattersInGame.Patches
{
    static class AllyCardChatUserOverride
    {
        public static void Apply()
        {
            On.RoR2.UI.AllyCardController.Awake += AllyCardController_Awake;
            On.RoR2.UI.AllyCardController.UpdateInfo += AllyCardController_UpdateInfo;
        }

        public static void Undo()
        {
            On.RoR2.UI.AllyCardController.Awake -= AllyCardController_Awake;
            On.RoR2.UI.AllyCardController.UpdateInfo -= AllyCardController_UpdateInfo;
        }

        static void AllyCardController_Awake(On.RoR2.UI.AllyCardController.orig_Awake orig, AllyCardController self)
        {
            orig(self);
            self.gameObject.AddComponent<AllyCardImageController>();
        }

        static void AllyCardController_UpdateInfo(On.RoR2.UI.AllyCardController.orig_UpdateInfo orig, AllyCardController self)
        {
            orig(self);

            if (self.TryGetComponent(out AllyCardImageController imageController) && imageController.AdditionalIcon)
            {
                if (self.sourceMaster && self.sourceMaster.TryGetComponent(out ChatName chatName) && chatName.ChatterInfo != null && chatName.ChatterInfo.ProfileImage)
                {
                    Texture characterPortrait = self.portraitIconImage.texture;

                    imageController.AdditionalIcon.texture = characterPortrait;
                    imageController.AdditionalIcon.enabled = characterPortrait;

                    self.portraitIconImage.texture = chatName.ChatterInfo.ProfileImage;
                    self.portraitIconImage.enabled = true;
                }
                else
                {
                    imageController.AdditionalIcon.enabled = false;
                }
            }
        }

        class AllyCardImageController : MonoBehaviour
        {
            public RawImage AdditionalIcon;

            void Awake()
            {
                Transform portraitRoot = transform.Find("Portrait");
                if (!portraitRoot)
                {
                    Log.Error("Could not find portrait root");
                    return;
                }

                GameObject characterIconObject = new GameObject("CharacterIcon");
                RectTransform characterIconTransform = characterIconObject.AddComponent<RectTransform>();
                AdditionalIcon = characterIconObject.AddComponent<RawImage>();

                characterIconTransform.SetParent(portraitRoot);

                characterIconTransform.anchorMin = new Vector2(0.5f, 0.5f);
                characterIconTransform.anchorMax = new Vector2(0.5f, 0.5f);

                const float FULL_SIZE = 36f;
                const float SIZE = 30f;

                characterIconTransform.sizeDelta = new Vector2(SIZE, SIZE);
                characterIconTransform.anchoredPosition3D = new Vector3(FULL_SIZE - SIZE, -(FULL_SIZE - SIZE), 0f);

                characterIconTransform.localScale = Vector3.one;
            }
        }
    }
}
