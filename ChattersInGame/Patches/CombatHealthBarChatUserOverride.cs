using ChattersInGame.Twitch;
using RoR2;
using RoR2.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChattersInGame.Patches
{
    static class CombatHealthBarChatUserOverride
    {
        public static void Apply()
        {
            On.RoR2.UI.CombatHealthBarViewer.GetHealthBarInfo += CombatHealthBarViewer_GetHealthBarInfo; ;
        }

        public static void Undo()
        {
            On.RoR2.UI.CombatHealthBarViewer.GetHealthBarInfo -= CombatHealthBarViewer_GetHealthBarInfo;
        }

        static object CombatHealthBarViewer_GetHealthBarInfo(On.RoR2.UI.CombatHealthBarViewer.orig_GetHealthBarInfo orig, CombatHealthBarViewer self, HealthComponent victimHealthComponent)
        {
            object boxedHealthBarInfo = orig(self, victimHealthComponent);

            if (boxedHealthBarInfo is CombatHealthBarViewer.HealthBarInfo healthBarInfo && healthBarInfo.healthBarRootObject)
            {
                if (!healthBarInfo.healthBarRootObject.GetComponent<HealthBarChatNameController>())
                    healthBarInfo.healthBarRootObject.AddComponent<HealthBarChatNameController>();
            }

            return boxedHealthBarInfo;
        }

        class HealthBarChatNameController : MonoBehaviour
        {
            HealthBar _healthBar;
            HealthComponent _lastHealthBarSource;

            ChatName _cachedChatName;

            HGTextMeshProUGUI _nameTextLabel;
            Image _iconImage;

            TimeStamp _displayingEmoteStartTime;
            EmoteImage _displayingEmoteImage;

            void Awake()
            {
                _healthBar = GetComponent<HealthBar>();

                GameObject nameTextObject = new GameObject("NameText");
                RectTransform nameTextTransform = nameTextObject.AddComponent<RectTransform>();

                nameTextTransform.SetParent(transform);
                nameTextTransform.localScale = Vector3.one;
                nameTextTransform.anchoredPosition3D = new Vector3(0f, 45f, 0f);

                _nameTextLabel = nameTextObject.AddComponent<HGTextMeshProUGUI>();
                _nameTextLabel.alignment = TextAlignmentOptions.Center;
                _nameTextLabel.overflowMode = TextOverflowModes.Overflow;
                _nameTextLabel.fontSize = 15;

                _nameTextLabel.enabled = false;

                GameObject iconObject = new GameObject("Icon");
                RectTransform iconTransform = iconObject.AddComponent<RectTransform>();

                iconTransform.SetParent(transform);
                iconTransform.localScale = Vector3.one;

                iconTransform.sizeDelta = new Vector2(45f, 45f);
                iconTransform.anchoredPosition3D = new Vector3(0f, 80f, 0f);

                _iconImage = iconObject.AddComponent<Image>();

                _iconImage.preserveAspect = true;
                _iconImage.enabled = false;
            }

            void Update()
            {
                HealthComponent healthBarSource = _healthBar.source;
                if (healthBarSource != _lastHealthBarSource)
                {
                    _lastHealthBarSource = healthBarSource;

                    _cachedChatName = null;

                    if (_lastHealthBarSource)
                    {
                        CharacterMaster master = _lastHealthBarSource.body.master;
                        if (master && master.TryGetComponent(out ChatName chatName))
                        {
                            _cachedChatName = chatName;
                        }
                    }
                }

                updateDisplayInfo();
            }

            void updateDisplayInfo()
            {
                string name = null;
                Color nameColor = Color.white;
                Sprite iconSprite = null;

                if (_cachedChatName)
                {
                    ChatterInfo chatterInfo = _cachedChatName.ChatterInfo;
                    if (chatterInfo != null && chatterInfo.UserDataIsReady)
                    {
                        name = chatterInfo.UserDisplayName;

                        if (chatterInfo.NameColor.HasValue && Main.UseChatterColors.Value)
                        {
                            nameColor = chatterInfo.NameColor.Value;
                        }

                        TimeStamp? currentEmoteLastUsedTime = chatterInfo.LastUsedEmoteTime;
                        if (_displayingEmoteStartTime < currentEmoteLastUsedTime)
                        {
                            _displayingEmoteStartTime = currentEmoteLastUsedTime.Value;
                        }

                        EmoteImage currentEmoteImage = chatterInfo.LastUsedEmote?.Image;
                        if (_displayingEmoteImage != currentEmoteImage && (chatterInfo.LastUsedEmote == null || (currentEmoteImage != null && currentEmoteImage.IsLoaded)))
                        {
                            _displayingEmoteImage = currentEmoteImage;
                        }

                        if (_displayingEmoteImage != null && _displayingEmoteStartTime.TimeSince.TotalSeconds <= 5 * 60 && Main.ShowChatterEmotes.Value)
                        {
                            iconSprite = _displayingEmoteImage.GetCurrentFrame().Sprite;
                        }
                    }
                }

                if (_nameTextLabel)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        _nameTextLabel.text = name;
                        _nameTextLabel.faceColor = nameColor;
                        _nameTextLabel.enabled = true;
                    }
                    else
                    {
                        _nameTextLabel.enabled = false;
                    }
                }

                if (_iconImage)
                {
                    if (iconSprite)
                    {
                        _iconImage.sprite = iconSprite;
                        _iconImage.enabled = true;

                        Rect iconRect = iconSprite.rect;
                        float spriteAspect = iconRect.width / iconRect.height;

                        RectTransform iconTransform = _iconImage.rectTransform;

                        Vector2 imageSize = iconTransform.sizeDelta;
                        imageSize.x = imageSize.y * Mathf.Max(1f, spriteAspect);
                        iconTransform.sizeDelta = imageSize;
                    }
                    else
                    {
                        _iconImage.enabled = false;
                    }
                }
            }
        }
    }
}
