using CraftableCartography.MapLayers;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.GameContent;
using HarmonyLib;

namespace CraftableCartography
{
    [HarmonyPatch]
    public class CraftableCartographyModSystem : ModSystem
    {
        public const string patchName = "com.profcupcake.craftablecartography";
        Harmony harmony; 
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            
            harmony = new(patchName);
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll(patchName);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldMapManager), nameof(WorldMapManager.RegisterDefaultMapLayers))]
        public static bool RegisterDefaultMapLayers(WorldMapManager __instance)
        {
            __instance.RegisterMapLayer<ChunkMapLayer>("chunks", 0.0);
            __instance.RegisterMapLayer<CCPlayerMapLayer>("players", 0.5);
            __instance.RegisterMapLayer<EntityMapLayer>("entities", 0.5);
            __instance.RegisterMapLayer<WaypointMapLayer>("waypoints", 1.0);
            return false;
        }
    }
}
