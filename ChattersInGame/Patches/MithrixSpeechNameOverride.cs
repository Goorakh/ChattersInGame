using RoR2;
using System.Linq;

namespace ChattersInGame.Patches
{
    static class MithrixSpeechNameOverride
    {
        public static void Apply()
        {
            On.RoR2.Chat.NpcChatMessage.ConstructChatString += NpcChatMessage_ConstructChatString;
        }

        public static void Undo()
        {
            On.RoR2.Chat.NpcChatMessage.ConstructChatString -= NpcChatMessage_ConstructChatString;
        }

        static bool isChatNamedMithrix(CharacterMaster master)
        {
            if (master.masterIndex != MasterCatalog.FindMasterIndex("BrotherMaster")
                && master.masterIndex != MasterCatalog.FindMasterIndex("BrotherHurtMaster"))
            {
                return false;
            }

            CharacterBody body = master.GetBody();
            if (!body || !BossGroup.FindBossGroup(body))
                return false;

            return master.TryGetComponent(out ChatName chatName) && chatName.ChatterInfo != null && chatName.ChatterInfo.UserDataIsReady;
        }

        static string NpcChatMessage_ConstructChatString(On.RoR2.Chat.NpcChatMessage.orig_ConstructChatString orig, Chat.NpcChatMessage self)
        {
            string result = orig(self);

            if (self.formatStringToken == "BROTHER_DIALOGUE_FORMAT")
            {
                CharacterMaster chatNamedMithrix = CharacterMaster.readOnlyInstancesList.FirstOrDefault(isChatNamedMithrix);
                if (chatNamedMithrix)
                {
                    int colonIndex = result.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        int mithrixNameEndIndex = colonIndex - 1;

                        int mithrixNameStartIndex;
                        for (mithrixNameStartIndex = colonIndex; mithrixNameStartIndex > 0; mithrixNameStartIndex--)
                        {
                            if (!char.IsLetter(result, mithrixNameStartIndex - 1))
                            {
                                break;
                            }
                        }

                        if (mithrixNameStartIndex <= mithrixNameEndIndex)
                        {
                            result = result.Remove(mithrixNameStartIndex, mithrixNameEndIndex - mithrixNameStartIndex + 1)
                                           .Insert(mithrixNameStartIndex, chatNamedMithrix.GetComponent<ChatName>().ChatterInfo.UserDisplayName);
                        }
                    }
                }
            }

            return result;
        }
    }
}
