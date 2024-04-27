using RoR2;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChattersInGame.Patches
{
    static class ChatNameSelector
    {
        public static void Apply()
        {
            CharacterMaster.onStartGlobal += CharacterMaster_onStartGlobal;
        }

        public static void Undo()
        {
            CharacterMaster.onStartGlobal += CharacterMaster_onStartGlobal;
        }

        static void CharacterMaster_onStartGlobal(CharacterMaster master)
        {
            if (master.playerCharacterMasterController)
                return;

            ChatterInfo chatterInfo = ChatterManager.GetRandomChatter(RoR2Application.rng);
            if (chatterInfo == null)
                return;

            ChatName chatName = master.gameObject.AddComponent<ChatName>();
            chatName.ChatterInfo = chatterInfo;
        }
    }
}
