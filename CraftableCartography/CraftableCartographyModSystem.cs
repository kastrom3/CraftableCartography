using CraftableCartography.Config;
using CraftableCartography.Items.Compass;
using CraftableCartography.Items.JPS;
using CraftableCartography.Items.Sextant;
using HarmonyLib;
using Newtonsoft.Json;
using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;
using static CraftableCartography.Lib.ItemChecks;

namespace CraftableCartography
{
    public partial class CraftableCartographyModSystem : ModSystem
    {
        public ConfigManager<CraftableCartographyModConfig> Config;

        private string dataPath;

        public const string patchName = "com.profcupcake.craftablecartography";

        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        Dictionary<IServerPlayer, PropickReading> LastPropickReading;

        Harmony harmony;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            this.api = api;

            dataPath = Path.Combine(new string[] { GamePaths.DataPath, "ModData", api.World.SavegameIdentifier, "craftablecartography", "map-settings.json" });

            harmony = new(patchName);
            harmony.PatchAll();
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            LastPropickReading = new();

            api.RegisterItemClass("compass", typeof(Compass));

            api.RegisterItemClass("sextant", typeof(Sextant));

            api.RegisterItemClass("ItemJPSDevice", typeof(ItemJPSDevice));

            api.Network.RegisterChannel(NetChannel)
                .RegisterMessageType<SetChannelPacket>();

            Config = new(api, "craftablecartography");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;

            sapi.Network.GetChannel(NetChannel)
                .SetMessageHandler<SetChannelPacket>(SetChannelCommandServer);

            sapi.Event.RegisterGameTickListener(ServerJPSCheck, 5000);

            sapi.ChatCommands.Create("setreading")
                .WithAlias("sr")
                .WithDescription("Set the coordinates of your last prospecting pick reading to add it to the map")
                .RequiresPlayer()
                .WithArgs(sapi.ChatCommands.Parsers.Int("x"), sapi.ChatCommands.Parsers.Int("y"), sapi.ChatCommands.Parsers.Int("z"))
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(SetReadingCommand);
        }

        private TextCommandResult SetReadingCommand(TextCommandCallingArgs args)
        {
            int x = (int)args[0];
            int y = (int)args[1];
            int z = (int)args[2];

            BlockPos pos = new BlockPos(x, y, z);

            pos.Add(api.World.DefaultSpawnPosition.AsBlockPos);

            if (AddLastReadingToMap((IServerPlayer)args.Caller.Player, pos))
            {
                return TextCommandResult.Success($"Added last ProPick reading to map at {x}, {y}, {z}");
            }
            else
            {
                return TextCommandResult.Error("No reading to add!");
            }
        }

        private void ServerJPSCheck(float dt)
        {
            foreach (IServerPlayer player in sapi.World.AllPlayers.Cast<IServerPlayer>())
            {
                if (player.ConnectionState != EnumClientState.Playing || player.Entity == null) continue;

                player.Entity.WatchedAttributes.SetBool(hasJPSAttr, HasJPS(player));
            }    
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.ChatCommands.Create("setJPSchannel")
                .WithDescription("Sets your JPS channel (for sharing location with other players)")
                .RequiresPlayer()
                .WithArgs(new ICommandArgumentParser[] { api.ChatCommands.Parsers.OptionalWord("channel") })
                .HandleWith(SetChannelCommand);
        }

        [ProtoContract(ImplicitFields=ImplicitFields.AllFields)]
        public class SetChannelPacket
        {
            public string channel;
        }

        private TextCommandResult SetChannelCommand(TextCommandCallingArgs args)
        {
            string setchannel;
            if (args[0] != null) setchannel = ((string)args[0]).ToLower();
            else setchannel = "";
            
            capi.Network.GetChannel(NetChannel).SendPacket<SetChannelPacket>(new() { channel = setchannel });

            return TextCommandResult.Success();
        }

        private void SetChannelCommandServer(IServerPlayer fromPlayer, SetChannelPacket packet)
        {
            fromPlayer.Entity.WatchedAttributes.SetString(JPSChannelAttr, packet.channel);
            fromPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "JPS channel changed to '" + packet.channel + "'", EnumChatType.CommandSuccess);
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll(patchName);
        }

        public void StoreMapPos(GuiElementMap elemMap)
        {
            Cuboidd curBlockViewBounds = elemMap.CurrentBlockViewBounds;
            BlockPos pos = new(
                (int)(curBlockViewBounds.X1 + curBlockViewBounds.X2) / 2,
                (int)(curBlockViewBounds.Y1 + curBlockViewBounds.Y2) / 2,
                (int)(curBlockViewBounds.Z1 + curBlockViewBounds.Z2) / 2
                );

            StoreMapPos(new SavedPositions(pos, elemMap.ZoomLevel));
        }

        public void StoreMapPos(SavedPositions mapPos)
        {
            if (!Directory.Exists(Directory.GetParent(dataPath).FullName)) Directory.CreateDirectory(Directory.GetParent(dataPath).FullName);
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(mapPos));
        }

        public SavedPositions LoadMapPos()
        {
            if (File.Exists(dataPath)) return JsonConvert.DeserializeObject<SavedPositions>(File.ReadAllText(dataPath));
            return new SavedPositions(api);
        }

        public void StoreLastReading(IServerPlayer player, PropickReading reading)
        {
            LastPropickReading[player] = reading;
        }

        public bool AddLastReadingToMap(IServerPlayer player, BlockPos pos)
        {
            PropickReading reading = LastPropickReading[player];

            if (reading == null) return false;

            reading.Position = pos.ToVec3d();

            ModSystemOreMap modSystem = api.ModLoader.GetModSystem<ModSystemOreMap>(true);
            if (modSystem == null)
            {
                return false;
            }
            modSystem.DidProbe(reading, player);

            LastPropickReading[player] = null;

            return true;
        }
    }
}
