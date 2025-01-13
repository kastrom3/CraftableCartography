using CraftableCartography.MapLayers;
using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnGuiOpened))]
        public static void RecentreMap(GuiDialogWorldMap __instance)
        {
            GuiElementMap elemMap = __instance.SingleComposer.GetElement("mapElem") as GuiElementMap;

            Traverse traverse = Traverse.Create(__instance);

            ICoreClientAPI capi = traverse.Field("capi").GetValue<ICoreClientAPI>();

            BlockPos pos = capi.World.Player.Entity.Attributes.GetBlockPos(MapOpenCoordsAttr, capi.World.DefaultSpawnPosition.AsBlockPos);
            float zoom = capi.World.Player.Entity.Attributes.GetFloat(MapOpenZoomAttr, elemMap.ZoomLevel);

            elemMap.ZoomLevel = zoom;
            elemMap.CenterMapTo(pos);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.TryClose))]
        public static void StoreMapLocation(GuiDialogWorldMap __instance)
        {
            ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            GuiElementMap elemMap = __instance.SingleComposer.GetElement("mapElem") as GuiElementMap;
            Cuboidd curBlockViewBounds = elemMap.CurrentBlockViewBounds;
            BlockPos pos = new(
                (int)(curBlockViewBounds.X1 + curBlockViewBounds.X2) / 2,
                (int)(curBlockViewBounds.Y1 + curBlockViewBounds.Y2) / 2,
                (int)(curBlockViewBounds.Z1 + curBlockViewBounds.Z2) / 2
                );
            capi.ShowChatMessage("Map closed, centre was about " + pos.ToString() + " (" + pos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos).ToString() + ")\nZoom level: "+elemMap.ZoomLevel);

            capi.World.Player.Entity.Attributes.SetFloat(MapOpenZoomAttr, elemMap.ZoomLevel);
            capi.World.Player.Entity.Attributes.SetBlockPos(MapOpenCoordsAttr, pos);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GuiElementMap), nameof(GuiElementMap.PostRenderInteractiveElements))]
        public static void StopMapTrackingPlayer(GuiElementMap __instance)
        {
            if (__instance != null)
            {
                Traverse traverse = Traverse.Create(__instance);

                if (traverse != null)
                {
                    ICoreClientAPI capi = __instance.Api;
                    if (capi != null)
                    {
                        if (capi.World != null)
                        {
                            if (capi.World.Player != null)
                            {
                                if (capi.World.Player.Entity != null)
                                {
                                    Vec3d playerPos = capi.World.Player.Entity.Pos.XYZ;
                                    traverse.Field("prevPlayerPos").GetValue<Vec3d>().Set(playerPos.X, playerPos.Y, playerPos.Z);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
