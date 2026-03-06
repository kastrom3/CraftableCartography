using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal class CoordinateHudPatches
    {
        private static double lastRotationTime;
        private static float previousYaw;
        private static float currentAmplitude;
        private static float currentDecayDuration;
        private static readonly Random fluctuationRandom = new Random();

        // Коэффициенты
        private const float MAX_AMPLITUDE = 60f; // максимальная амплитуда колебаний
        private const float ROTATION_SENSITIVITY = 1.2f; // чувствительность к скорости поворота
        private const float MIN_TRIGGER_ANGLE = 0.5f; // минимальный угол для начала колебаний
        private const float MAX_DECAY_DURATION = 5000f; // максимальное время затухания 5000 = 5 сек
        private const float AMPLITUDE_BLEND_FACTOR = 0.7f; // сила влияния новых колебаний (0.0-1.0)
        private const float DECAY_BLEND_FACTOR = 0.9f; // сила продления времени затухания (0.0-1.0)

        [HarmonyPatch(typeof(HudElementCoordinates), "OnBlockTexturesLoaded")]
        private static class HudElementCoordinates_OnBlockTexturesLoaded_Patch
        {

            private static void Postfix(HudElementCoordinates __instance)
            {
                // Получаем доступ к приватным полям через Traverse
                var traverse = Traverse.Create(__instance);
                var capi = traverse.Field("capi").GetValue<ICoreClientAPI>();
                var world = capi.World;

                // Оригинальная логика проверки конфига
                if (!world.Config.GetBool("allowCoordinateHud", true))
                {
                    (world as ClientMain).EnqueueMainThreadTask(() =>
                    {
                        (world as ClientMain).UnregisterDialog(__instance);
                        capi.Input.SetHotKeyHandler("coordinateshud", null);
                        __instance.Dispose();
                    }, "unreg");
                    return;
                }

                // Регистрируем обновление с интервалом 150 мс
                capi.Event.RegisterGameTickListener(dt =>
                {
                    // Вызываем приватный метод через отражение
                    traverse.Method("Every250ms", new[] { typeof(float) }).GetValue(dt);
                }, 150);

                // Оригинальная логика подписки на настройки
                ClientSettings.Inst.AddWatcher<bool>("showCoordinateHud", on =>
                {
                    if (on) __instance.TryOpen();
                    else __instance.TryClose();
                });
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HudElementCoordinates), "Every250ms")]
        public static bool Every250ms(HudElementCoordinates __instance, float dt)
        {
            ICoreClientAPI capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            if (!__instance.IsOpened()) return false;

            var player = capi.World.Player;
            EntityPlayer entity = player.Entity;
            float currentYaw = entity.Pos.Yaw;

            // Общие данные для всех компасов
            BlockPos pos = entity.Pos.AsBlockPos;
            int ypos = pos.Y;
            pos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
            
            string facing = BlockFacing.HorizontalFromYaw(currentYaw).ToString();
            facing = Lang.Get("facing-" + facing);

            string coords = "";

            // Отдельная обработка для TemporalCompass и JPS
            if (HasTemporalCompass(player) || HasJPS(player))
                coords += $"{pos.X}, {ypos}, {pos.Z}\n";

            bool hasCompass = capi.ModLoader.GetModSystem<CraftableCartographyModSystem>().Config.modConfig.CompassTextBox & HasCompass(player);
            bool hasStableCompass = HasJPS(player) || HasTemporalCompass(player);

            if (hasCompass || hasStableCompass)
            {
                float yawDeg = (float)Math.Round(180 - currentYaw * GameMath.RAD2DEG);
                
                // Логика колебаний только для обычного компаса
                if (hasCompass)
                {
                    float deltaYaw = GetNormalizedDeltaYaw(currentYaw, previousYaw);

                    if (Math.Abs(deltaYaw) > MIN_TRIGGER_ANGLE)
                    {
                        float rotationStrength = Math.Min(Math.Abs(deltaYaw * ROTATION_SENSITIVITY), MAX_AMPLITUDE);
                        float newDecayDuration = (rotationStrength / MAX_AMPLITUDE) * MAX_DECAY_DURATION;

                        // Смешиваем новую амплитуду с текущей вместо замены
                        currentAmplitude = Math.Max(currentAmplitude,
                            rotationStrength * AMPLITUDE_BLEND_FACTOR + currentAmplitude * (1 - AMPLITUDE_BLEND_FACTOR));

                        // Продлеваем время затухания с учетом нового воздействия
                        currentDecayDuration = Math.Max(currentDecayDuration,
                            newDecayDuration * DECAY_BLEND_FACTOR + currentDecayDuration * (1 - DECAY_BLEND_FACTOR));

                        // Обновляем таймер только если воздействие значительное
                        if (rotationStrength > currentAmplitude * 0.8f)
                        {
                            lastRotationTime = capi.World.ElapsedMilliseconds;
                        }
                    }

                    float timePassed = (float)(capi.World.ElapsedMilliseconds - lastRotationTime);

                    if (timePassed < currentDecayDuration)
                    {
                        float decayFactor = 1 - (timePassed / currentDecayDuration);
                        float effectiveAmplitude = currentAmplitude * decayFactor * decayFactor;
                        yawDeg += (float)(fluctuationRandom.NextDouble() * effectiveAmplitude * 2 - effectiveAmplitude);
                    }
                    else
                    {
                        // Плавный сброс остаточной амплитуды
                        currentAmplitude *= 0.95f;
                        currentDecayDuration *= 0.95f;
                    }
                }

                previousYaw = currentYaw;
                yawDeg = (yawDeg % 360 + 360) % 360;
                coords += $"{yawDeg:0}° / {facing}";
            }
            else if (capi.ModLoader.GetModSystem<CraftableCartographyModSystem>().Config.modConfig.CompassTextBox & HasPrimitiveCompass(player))
            {
                coords += facing;
            }

            // Добавление debug-информации
            if (ClientSettings.ExtendedDebugInfo && (hasCompass || hasStableCompass))
            {
                coords += facing switch
                {
                    "North" => " / Z-",
                    "East" => " / X+",
                    "South" => " / Z+",
                    "West" => " / X-",
                    _ => string.Empty
                };
            }

            __instance.SingleComposer.GetDynamicText("text").SetNewText(coords);
            
            // Оригинальная логика позиционирования
            var boundsList = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
            __instance.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;
            
            foreach (var bounds in boundsList)
            {
                if (bounds != __instance.SingleComposer.Bounds)
                {
                    __instance.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + bounds.absY + bounds.OuterHeight;
                    break;
                }
            }

            return false;
        }

        private static float GetNormalizedDeltaYaw(float current, float previous)
        {
            float diff = (current - previous) * GameMath.RAD2DEG;
            if (diff > 180) diff -= 360;
            if (diff < -180) diff += 360;
            return diff;
        }
    }
}