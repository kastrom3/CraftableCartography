﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal class CoordinateHudPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HudElementCoordinates), "Every250ms")]
        public static bool Every250ms(HudElementCoordinates __instance, float dt)
        {
            ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            if (!__instance.IsOpened())
            {
                return false;
            }
            BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
            int ypos = pos.Y;
            pos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
            string facing = BlockFacing.HorizontalFromYaw(capi.World.Player.Entity.Pos.Yaw).ToString();
            facing = Lang.Get("facing-" + facing, Array.Empty<object>());

            string coords = "";

            if (HasTemporalCompass(capi.World.Player) || HasJPS(capi.World.Player)) coords += string.Concat(new string[]
            {
                pos.X.ToString(),
                ", ",
                ypos.ToString(),
                ", ",
                pos.Z.ToString(),
                "\n" });
            bool hasAdvancedCompass = HasCompass(capi.World.Player) || HasJPS(capi.World.Player) || HasTemporalCompass(capi.World.Player);

            if (!hasAdvancedCompass && HasPrimitiveCompass(capi.World.Player))
            {
                coords += facing; // Только направление, без градусов, если нет продвинутого компаса
            }
            else if (hasAdvancedCompass)
            {
                float yawDeg = (float)Math.Round(180 - capi.World.Player.Entity.Pos.Yaw * (180 / GameMath.PI));

                while (yawDeg < 0) yawDeg += 360;
                yawDeg %= 360;

                coords += yawDeg.ToString() + "° / " + facing;

                if (ClientSettings.ExtendedDebugInfo)
                {
                    string text;
                    if (!(facing == "North"))
                    {
                        if (!(facing == "East"))
                        {
                            if (!(facing == "South"))
                            {
                                if (!(facing == "West"))
                                {
                                    text = string.Empty;
                                }
                                else
                                {
                                    text = " / X-";
                                }
                            }
                            else
                            {
                                text = " / Z+";
                            }
                        }
                        else
                        {
                            text = " / X+";
                        }
                    }
                    else
                    {
                        text = " / Z-";
                    }
                    coords += text;
                }
            }
            __instance.SingleComposer.GetDynamicText("text").SetNewText(coords, false, false, false);
            List<ElementBounds> boundsList = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
            __instance.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;
            for (int i = 0; i < boundsList.Count; i++)
            {
                if (boundsList[i] != __instance.SingleComposer.Bounds)
                {
                    ElementBounds bounds = boundsList[i];
                    __instance.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + bounds.absY + bounds.OuterHeight;
                    return false;
                }
            }
            return false;
        }
    }
}
