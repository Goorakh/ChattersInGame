using RoR2;

namespace ChattersInGame.Patches
{
    static class AllyChatterDeathMessagePatch
    {
        public static void Apply()
        {
            // Intentionally not using GlobalEventManager.onCharacterDeathGlobal since it's not actually global, it only gets invoked on the server
            On.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
        }

        public static void Undo()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath -= GlobalEventManager_OnCharacterDeath;
        }

        static void GlobalEventManager_OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);

            if (!damageReport.victimMaster || damageReport.victimTeamIndex != TeamIndex.Player)
                return;

            ChatName victimChatName = damageReport.victimMaster.GetComponent<ChatName>();
            if (!victimChatName)
                return;

            ChatterInfo vicitimChatterInfo = victimChatName.ChatterInfo;
            if (vicitimChatterInfo == null || !vicitimChatterInfo.UserDataIsReady)
                return;

            ChatterInfo attackerChatterInfo;
            if (damageReport.attackerMaster && damageReport.attackerMaster.TryGetComponent(out ChatName attackerChatName))
            {
                attackerChatterInfo = attackerChatName.ChatterInfo;
            }
            else
            {
                attackerChatterInfo = null;
            }

            string deathMessageToken;
            if ((damageReport.damageInfo.damageType & DamageType.VoidDeath) != DamageType.Generic)
            {
                deathMessageToken = "PLAYER_DEATH_QUOTE_VOIDDEATH";
            }
            else if (damageReport.isFallDamage)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                deathMessageToken = GlobalEventManager.fallDamageDeathQuoteTokens[UnityEngine.Random.Range(0, GlobalEventManager.fallDamageDeathQuoteTokens.Length)];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
            else if (damageReport.victimBody && damageReport.victimBody.inventory && damageReport.victimBody.inventory.GetItemCount(RoR2Content.Items.LunarDagger) > 0)
            {
                deathMessageToken = "PLAYER_DEATH_QUOTE_BRITTLEDEATH";
            }
            else
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                deathMessageToken = GlobalEventManager.standardDeathQuoteTokens[UnityEngine.Random.Range(0, GlobalEventManager.standardDeathQuoteTokens.Length)];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            Chat.AddMessage($"<style=cDeath><sprite name=\"Skull\" tint=1> {Language.GetStringFormatted(deathMessageToken, vicitimChatterInfo.UserDisplayName)} <sprite name=\"Skull\" tint=1></style>");
        }
    }
}
