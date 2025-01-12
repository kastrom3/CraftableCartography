using CraftableCartography.MapLayers;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.GameContent;
using HarmonyLib;
using Vintagestory.API.Server;
using System;

namespace CraftableCartography
{
    [HarmonyPatch]
    public class CraftableCartographyModSystem : ModSystem
    {
        public const string patchName = "com.profcupcake.craftablecartography";

        ICoreAPI api;
        
        Harmony harmony; 
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            this.api = api;
            
            harmony = new(patchName);
            harmony.PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            api.ChatCommands.Create("togglemapmarker")
                .WithDescription("Toggles showing of local player's map marker")
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.root)
                .HandleWith(ToggleMarker);
        }

        private TextCommandResult ToggleMarker(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            CCPlayerMapLayer mapLayer;
            mapLayer = api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<CCPlayerMapLayer>().FirstOrDefault();

            if (mapLayer == null)
            {
                return TextCommandResult.Error("Could not find Player map layer!");
            }

            mapLayer.SetPlayerShown(player, !mapLayer.GetPlayerShown(player));

            return TextCommandResult.Success("Player marker toggled");
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
