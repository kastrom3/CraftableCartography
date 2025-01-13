using CraftableCartography.MapLayers;
using HarmonyLib;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography
{
    [HarmonyPatch]
    public class CraftableCartographyModSystem : ModSystem
    {
        public const string patchName = "com.profcupcake.craftablecartography";

        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        Harmony harmony;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            this.api = api;

            harmony = new(patchName);
            harmony.PatchAll();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;

            api.ChatCommands.Create("togglemapmarker")
                .WithDescription("Toggles showing of player's map marker")
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.root)
                .WithArgs(new ICommandArgumentParser[] { api.ChatCommands.Parsers.OnlinePlayer("player") })
                .HandleWith(ToggleMarker);
        }

        private TextCommandResult ToggleMarker(TextCommandCallingArgs args)
        {
            IPlayer player = args[0] as IPlayer;

            player.Entity.WatchedAttributes.SetBool(ShowOnMapAttr, !player.Entity.WatchedAttributes.GetBool(ShowOnMapAttr));

            return TextCommandResult.Success("Player marker toggled");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.World.Player.Entity.WatchedAttributes.RegisterModifiedListener(ShowOnMapAttr, OnMapShowToggle);
        }

        private void OnMapShowToggle() // currently borken :|
        {
            CCPlayerMapLayer mapLayer = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<CCPlayerMapLayer>().FirstOrDefault();
            mapLayer.OnMapOpenedClient();
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll(patchName);
        }
    }
}
