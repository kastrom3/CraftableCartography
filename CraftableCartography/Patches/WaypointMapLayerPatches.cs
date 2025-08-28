using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    internal static class WaypointMapLayerPatches
    {
        // Патч для отключения Death Waypoints, если нет JPS
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaypointMapLayer), "Event_PlayerDeath")]
        public static bool Prefix_Event_PlayerDeath(WaypointMapLayer __instance, IServerPlayer byPlayer, DamageSource damageSource)
        {
            // Если у игрока нет JPS — блокируем создание Death Waypoint
            if (!HasJPS(byPlayer))
            {
                return false; // Отменяем оригинальный метод
            }
            return true; // Продолжаем выполнение
        }
    }
}
