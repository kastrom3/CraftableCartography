using HarmonyLib;
using System;
using System.Text;
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
    internal static class MapGuiPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnGuiOpened))]
        public static void OnMapOpened(GuiDialogWorldMap __instance)
        {
            Traverse traverse = Traverse.Create(__instance);

            ICoreClientAPI capi = traverse.Field("capi").GetValue<ICoreClientAPI>();

            MapChecker mapChecker = capi.ModLoader.GetModSystem<MapChecker>();

            if (mapChecker != null)
            {
                if (__instance.DialogType == EnumDialogType.HUD)
                {
                    if (!mapChecker.IsMinimapAllowed())
                    {
                        __instance.TryClose();
                    }
                } else if (__instance.DialogType == EnumDialogType.Dialog)
                {
                    if (!mapChecker.IsMapAllowed())
                    {
                        __instance.TryClose();
                    }
                }
            }
            
            GuiElementMap elemMap = __instance.SingleComposer.GetElement("mapElem") as GuiElementMap;

            SavedPositions saved = capi.ModLoader.GetModSystem<CraftableCartographyModSystem>().LoadMapPos();
            
            elemMap.ZoomLevel = saved.zoomLevel;
            if (HasJPS(capi.World.Player))
            {
                elemMap.CenterMapTo(capi.World.Player.Entity.Pos.AsBlockPos);
            }
            else
            {
                elemMap.CenterMapTo(saved.pos);
            }

            //capi.ShowChatMessage("Loaded centre: " + pos.ToString() + " (" + pos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos).ToString() + ")\nZoom level: " + zoom);
        }

        [HarmonyPatch(typeof(GuiDialogWorldMap))]
        [HarmonyPatch("OnMouseMove")]
        public static class GuiDialogWorldMap_OnMouseMove_Patch
        {
            static void Postfix(GuiDialogWorldMap __instance, MouseEvent args)
            {
                ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();

                // Если курсор вне границ карты, пропускаем обработку
                if (__instance.SingleComposer == null || !__instance.SingleComposer.Bounds.PointInside(args.X, args.Y))
                {
                    return;
                }

                // Ваша кастомная логика
                Vec3d hoveredWorldPos = new Vec3d();
                CustomLoadWorldPos(__instance, args.X, args.Y, ref hoveredWorldPos);

                // Получаем базовое смещение динамически
                double worldOffsetX = capi.World.DefaultSpawnPosition.X;
                double worldOffsetZ = capi.World.DefaultSpawnPosition.Z;
                hoveredWorldPos.X -= worldOffsetX;
                hoveredWorldPos.Z -= worldOffsetZ;

                StringBuilder stringBuilder = new StringBuilder();
                if (HasJPS(capi.World.Player) | HasTemporalCompass(capi.World.Player))
                {
                    stringBuilder.AppendLine($"{(int)hoveredWorldPos.X}, {(int)hoveredWorldPos.Y}, {(int)hoveredWorldPos.Z}");
                }
                else
                {
                    stringBuilder.AppendLine($"???, ???, ???");
                }

                GuiElementMap guiElementMap = __instance.SingleComposer.GetElement("mapElem") as GuiElementMap;
                GuiElementHoverText hoverText = __instance.SingleComposer.GetHoverText("hoverText");
                foreach (MapLayer mapLayer in guiElementMap.mapLayers)
                {
                    mapLayer.OnMouseMoveClient(args, guiElementMap, stringBuilder);
                }

                string newText = stringBuilder.ToString().TrimEnd();
                hoverText.SetNewText(newText);
            }

            public static void CustomLoadWorldPos(GuiDialogWorldMap instance, double mouseX, double mouseY, ref Vec3d worldPos)
            {
                // Ваша кастомная логика
                double num = mouseX - instance.SingleComposer.Bounds.absX;
                double num2 = mouseY - instance.SingleComposer.Bounds.absY - ((instance.DialogType == EnumDialogType.Dialog) ? GuiElement.scaled(30.0) : 0.0);
                (instance.SingleComposer.GetElement("mapElem") as GuiElementMap).TranslateViewPosToWorldPos(new Vec2f((float)num, (float)num2), ref worldPos);
                worldPos.Y += 1.0;
            }
        }

        [HarmonyPatch(typeof(GuiElementMap))]
        [HarmonyPatch("OnKeyDown")]
        public static class GuiElementMap_OnKeyDown_Patch
        {
            static void Postfix(GuiElementMap __instance, ICoreClientAPI api, KeyEvent args)
            {

                // Получаем смещение мира (double)
                double worldOffsetX = api.World.DefaultSpawnPosition.X;
                double worldOffsetZ = api.World.DefaultSpawnPosition.Z;

                // Преобразуем double в int с округлением
                int offsetX = (int)Math.Round(worldOffsetX);
                int offsetZ = (int)Math.Round(worldOffsetZ);

                if (args.KeyCode == 51)
                {
                    if (HasJPS(api.World.Player))
                    {
                        __instance.CenterMapTo(api.World.Player.Entity.Pos.AsBlockPos);
                    }
                    else
                    {
                        BlockPos zeroPos = new BlockPos(offsetX, 0, offsetZ);
                        __instance.CenterMapTo(zeroPos);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GuiElementMap))]
        public static class GuiElementMap_Patches
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(GuiElementMap.PostRenderInteractiveElements))]
            public static bool StopMapTrackingPlayer(GuiElementMap __instance)
            {
                ICoreClientAPI capi = __instance.Api;

                if (HasJPS(capi.World.Player))
                {
                    // Возвращаем true, чтобы вызвать оригинальный метод
                    return true;
                }
                else
                {
                    // Останавливаем отслеживание игрока
                    if (__instance != null)
                    {
                        Traverse traverse = Traverse.Create(__instance);

                        if (traverse != null)
                        {
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

                    // Возвращаем false, чтобы пропустить оригинальный метод
                    return false;
                }
            }
        }
    }
}