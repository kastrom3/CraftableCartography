using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace CraftableCartography.Patches
{
    [HarmonyPatch]
    public static class HudToolbarPatches
    {
        public delegate void OnMouseWheelDelegate(ref MouseWheelEventArgs args);

        public static event OnMouseWheelDelegate OnMouseWheel;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HudHotbar), "OnMouseWheel")]
        public static bool OnMouseWheelPatch(MouseWheelEventArgs args)
        {
            if (OnMouseWheel is not null)
            {
                foreach (OnMouseWheelDelegate d in OnMouseWheel.GetInvocationList())
                {
                    d.Invoke(ref args);
                    if (args.IsHandled) return false;
                }
            }
            
            return true;
        }
    }
}
