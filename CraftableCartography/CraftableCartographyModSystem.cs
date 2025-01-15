using CraftableCartography.MapLayers;
using HarmonyLib;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography
{
    public partial class CraftableCartographyModSystem : ModSystem
    {
        private static string dataPath;
        private static SavedPositions _defaultPosition;
        public static SavedPositions defaultPosition
        {
            get
            {
                if (_defaultPosition == null)
                {
                    dataPath = Path.Combine(new string[] { GamePaths.DataPath, "ModData", api.World.SavegameIdentifier, "craftablecartography", "map-settings.json" });
                    if (File.Exists(dataPath))
                    {
                        _defaultPosition = JsonConvert.DeserializeObject<SavedPositions>(File.ReadAllText(dataPath));
                    }
                    else
                    {
                        _defaultPosition = new(api);
                    }
                }
                return _defaultPosition;
            }
        }
        
        public const string patchName = "com.profcupcake.craftablecartography";

        static ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        Harmony harmony;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            CraftableCartographyModSystem.api = api;

            

            harmony = new(patchName);
            harmony.PatchAll();
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Network.RegisterChannel(NetChannel)
                .RegisterMessageType<MapShowUpdatePacket>();
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

        [ProtoContract]
        public class MapShowUpdatePacket { }

        private TextCommandResult ToggleMarker(TextCommandCallingArgs args)
        {
            IPlayer player = args[0] as IPlayer;

            player.Entity.WatchedAttributes.SetBool(ShowOnMapAttr, !player.Entity.WatchedAttributes.GetBool(ShowOnMapAttr));

            sapi.Network.GetChannel(NetChannel).BroadcastPacket(new MapShowUpdatePacket());

            return TextCommandResult.Success("Player marker toggled");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.ChatCommands.Create("recentremap")
                .WithDescription("Recentres your map on the given coordinates")
                .RequiresPlayer()
                .WithArgs(new ICommandArgumentParser[] { api.ChatCommands.Parsers.OptionalInt("x"), api.ChatCommands.Parsers.OptionalInt("y"), api.ChatCommands.Parsers.OptionalInt("z") })
                .HandleWith(RecentreMapCommand);

            api.Network.GetChannel(NetChannel)
                .SetMessageHandler<MapShowUpdatePacket>(OnMapShowToggle);
        }

        private TextCommandResult RecentreMapCommand(TextCommandCallingArgs args)
        {
            BlockPos pos = new((int)args[0], (int)args[1], (int)args[2]);
            BlockPos absPos = new BlockPos(pos.X, pos.Y, pos.Z).Add(api.World.DefaultSpawnPosition.AsBlockPos);
            GuiElementMap mapElem = api.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap;

            mapElem.CenterMapTo(absPos);
            capi.World.Player.Entity.Attributes.SetBlockPos(MapOpenCoordsAttr, absPos);

            return TextCommandResult.Success("Map centred on " + pos.X + " " + pos.Y + " " + pos.Z);
        }

        private void OnMapShowToggle(MapShowUpdatePacket packet)
        {
            CCPlayerMapLayer mapLayer = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<CCPlayerMapLayer>().FirstOrDefault();
            mapLayer.OnMapOpenedClient();
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll(patchName);
        }

        internal void SavePositionToFile()
        {
            defaultPosition.pos = capi.World.Player.Entity.Attributes.GetBlockPos(MapOpenCoordsAttr);
            defaultPosition.zoomLevel = capi.World.Player.Entity.Attributes.GetFloat(MapOpenZoomAttr);
            if (!Directory.Exists(Directory.GetParent(dataPath).FullName)) Directory.CreateDirectory(Directory.GetParent(dataPath).FullName);
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(defaultPosition));
        }
    }
}
