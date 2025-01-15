using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal static class MapGuiPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnGuiOpened))]
        public static void RecentreMapToStoredLocation(GuiDialogWorldMap __instance)
        {
            GuiElementMap elemMap = __instance.SingleComposer.GetElement("mapElem") as GuiElementMap;

            Traverse traverse = Traverse.Create(__instance);

            ICoreClientAPI capi = traverse.Field("capi").GetValue<ICoreClientAPI>();

            BlockPos pos = capi.World.Player.Entity.Attributes.GetBlockPos(MapOpenCoordsAttr, CraftableCartographyModSystem.defaultPosition.pos);
            float zoom = capi.World.Player.Entity.Attributes.GetFloat(MapOpenZoomAttr, CraftableCartographyModSystem.defaultPosition.zoomLevel);

            elemMap.ZoomLevel = zoom;
            elemMap.CenterMapTo(pos);

            //capi.ShowChatMessage("Loaded centre: " + pos.ToString() + " (" + pos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos).ToString() + ")\nZoom level: " + zoom);
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