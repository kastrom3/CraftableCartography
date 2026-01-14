using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal class ProPickPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
        public static bool PrintProbeResults(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
        {
            if (HasJPS(splr)) return true;

            Traverse traverse = Traverse.Create(__instance);

            PropickReading results = traverse.Method("GenProbeResults", new Type[] { typeof(IWorldAccessor), typeof(BlockPos) }).GetValue<PropickReading>(world, pos);
            string textResults = results.ToHumanReadable(splr.LanguageCode, traverse.Field("ppws").GetValue<ProPickWorkSpace>().pageCodes);
            splr.SendMessage(GlobalConstants.InfoLogChatGroup, textResults, EnumChatType.Notification, null);

            splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("craftablecartography:enter-setreading"), EnumChatType.Notification);

            world.Api.ModLoader.GetModSystem<CraftableCartographyModSystem>().StoreLastReading(splr, results);

            return false;
        }
    }
}
