using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography
{
    public class MapChecker : ModSystem
    {
        ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.Event.RegisterGameTickListener(MapCheckTick, 1000);
        }

        public void MapCheckTick(float dt)
        {
            GuiDialogWorldMap map = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
            if (map != null)
            {
                if (map.DialogType == EnumDialogType.HUD)
                {
                    if (!IsMinimapAllowed())
                    {
                        map.TryClose();
                    }
                } else if (map.DialogType == EnumDialogType.Dialog)
                {
                    if (!IsMapAllowed())
                    {
                        map.TryClose();
                    }
                }
            }
        }

        public bool IsMinimapAllowed()
        {
            return HasJPS(capi);
        }

        public bool IsMapAllowed()
        {
            return (HasMap(capi) || HasJPS(capi));
        }
    }
}
