using CraftableCartography.Items.Compass;
﻿using CraftableCartography.MapLayers;
using HarmonyLib;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
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
        private string dataPath;
        
        public const string patchName = "com.profcupcake.craftablecartography";

        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

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
            api.RegisterItemClass("compass", typeof(Compass));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;
        }

        [ProtoContract]
        public class MapShowUpdatePacket { }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.ChatCommands.Create("recentremap")
                .WithDescription("Recentres your map on the given coordinates")
                .RequiresPlayer()
                .WithArgs(new ICommandArgumentParser[] { api.ChatCommands.Parsers.OptionalInt("x"), api.ChatCommands.Parsers.OptionalInt("y"), api.ChatCommands.Parsers.OptionalInt("z") })
                .HandleWith(RecentreMapCommand);

            api.ChatCommands.Create("setJPSchannel")
                .WithDescription("Sets your JPS channel (for sharing location with other players)")
                .RequiresPlayer()
                .WithArgs(new ICommandArgumentParser[] {api.ChatCommands.Parsers.OptionalWord("channel")})
                .HandleWith(SetChannelCommand);
        }

        private TextCommandResult SetChannelCommand(TextCommandCallingArgs args)
        {
            string channel;
            if (args[0] != null) channel = ((string)args[0]).ToLower();
            else channel = "";
            args.Caller.Player.Entity.WatchedAttributes.SetString(JPSChannelAttr, (string)args[0]);
            return TextCommandResult.Success("Channel changed to '"+channel+"'.");
        }

        private TextCommandResult RecentreMapCommand(TextCommandCallingArgs args)
        {
            BlockPos pos = new((int)args[0], (int)args[1], (int)args[2]);
            BlockPos absPos = new BlockPos(pos.X, pos.Y, pos.Z).Add(api.World.DefaultSpawnPosition.AsBlockPos);
            GuiElementMap mapElem = api.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap;

            mapElem.CenterMapTo(absPos);

            SavedPositions saved = LoadMapPos();
            saved.pos = absPos;
            StoreMapPos(saved);

            return TextCommandResult.Success("Map centred on " + pos.X + " " + pos.Y + " " + pos.Z);
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
    }
}
