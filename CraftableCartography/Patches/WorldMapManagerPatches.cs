using CraftableCartography.MapLayers;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;
using static CraftableCartography.Lib.ItemChecks;

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
        public static void StoreMapLocation(WorldMapManager __instance)
        {
            GuiDialogWorldMap mapDlg = __instance.worldMapDlg;
            if (mapDlg != null && mapDlg.IsOpened())
            {
                if (mapDlg.DialogType == EnumDialogType.Dialog)
                {
                    ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    //capi.ShowChatMessage("big map closing, storing location");

                    GuiElementMap elemMap = mapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap;

                    capi.ModLoader.GetModSystem<CraftableCartographyModSystem>().StoreMapPos(elemMap);

                    //capi.ShowChatMessage("Stored centre: " + pos.ToString() + " (" + pos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos).ToString() + ")\nZoom level: " + elemMap.ZoomLevel);
                }
            }
        }
    }
    //Отключение отображения существ на карте
    [HarmonyPatch(typeof(OwnedEntityMapLayer))]
    [HarmonyPatch("Render")]
    public static class OwnedEntityMapLayer_Render_Patch
    {
            public static bool Prefix(OwnedEntityMapLayer __instance, GuiElementMap mapElem, float dt)
        {
            ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            // Проверка HasJPS
            if (HasJPS(capi.World.Player))
            {
                return true; // Продолжить выполнение оригинального метода
            }

            return false; // Пропустить оригинальный метод, если проверка не пройдена
        }
    }
}
