using RoR2;
using UnityEngine;

namespace ChattersInGame
{
    public class ChatName : MonoBehaviour
    {
        CharacterMaster _master;

        ChatterInfo _chatterInfo;
        public ChatterInfo ChatterInfo
        {
            get
            {
                return _chatterInfo;
            }
            set
            {
                if (_chatterInfo == value)
                    return;

                if (_chatterInfo != null)
                {
                    _chatterInfo.ReferenceCount--;
                    _chatterInfo.OnUserDataReady -= onChatterInfoDataReady;
                }

                _chatterInfo = value;

                if (_chatterInfo != null)
                {
                    _chatterInfo.ReferenceCount++;

                    _chatterInfo.OnUserDataReady += onChatterInfoDataReady;

                    if (_chatterInfo.UserDataIsReady)
                    {
                        onChatterInfoDataReady();
                    }
                }
            }
        }

        void Awake()
        {
            _master = GetComponent<CharacterMaster>();
        }

        void OnEnable()
        {
            if (_master)
            {
                _master.onBodyStart += overrideBodyName;

                CharacterBody currentBody = _master.GetBody();
                if (currentBody)
                {
                    overrideBodyName(currentBody);
                }
            }
        }

        void OnDisable()
        {
            if (_master)
            {
                _master.onBodyStart -= overrideBodyName;
            }
        }

        void OnDestroy()
        {
            ChatterInfo = null;
        }

        void onChatterInfoDataReady()
        {
            if (!_master)
                return;

            CharacterBody body = _master.GetBody();
            if (!body)
                return;
            
            overrideBodyName(body);
        }

        void overrideBodyName(CharacterBody body)
        {
            if (ChatterInfo == null || !ChatterInfo.UserDataIsReady)
                return;

            string name = ChatterInfo.UserDisplayName;

            bool isUmbralMithrix = ModCompat.UmbralMithrix.IsActive
                                   && PhaseCounter.instance
                                   && (body.bodyIndex == BodyCatalog.FindBodyIndex("BrotherBody") || body.bodyIndex == BodyCatalog.FindBodyIndex("BrotherHurtBody"));

            if (isUmbralMithrix)
            {
                name = "Umbral " + name;
            }

            body.baseNameToken = name;

            BossGroup bossGroup = BossGroup.FindBossGroup(body);
            if (bossGroup)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                bossGroup.bestObservedName = string.Empty;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
