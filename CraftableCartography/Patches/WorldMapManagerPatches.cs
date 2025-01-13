using CraftableCartography.MapLayers;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal static class WorldMapManagerPatches
    {
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldMapManager), nameof(WorldMapManager.ToggleMap))]
        public static void ToggleMap(WorldMapManager __instance)
        {
            GuiDialogWorldMap mapDlg = __instance.worldMapDlg;
            if (mapDlg != null && mapDlg.IsOpened())
            {
                if (mapDlg.DialogType == EnumDialogType.Dialog)
                {
                    ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    //capi.ShowChatMessage("big map closing, storing location");

                    GuiElementMap elemMap = mapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap;
                    Cuboidd curBlockViewBounds = elemMap.CurrentBlockViewBounds;
                    BlockPos pos = new(
                        (int)(curBlockViewBounds.X1 + curBlockViewBounds.X2) / 2,
                        (int)(curBlockViewBounds.Y1 + curBlockViewBounds.Y2) / 2,
                        (int)(curBlockViewBounds.Z1 + curBlockViewBounds.Z2) / 2
                        );

                    capi.World.Player.Entity.Attributes.SetFloat(MapOpenZoomAttr, elemMap.ZoomLevel);
                    capi.World.Player.Entity.Attributes.SetBlockPos(MapOpenCoordsAttr, pos);

                    //capi.ShowChatMessage("Stored centre: " + pos.ToString() + " (" + pos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos).ToString() + ")\nZoom level: " + elemMap.ZoomLevel);
                }
            }
        }
    }
}
