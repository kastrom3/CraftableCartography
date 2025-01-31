using Cairo;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using static CraftableCartography.Lib.CCConstants;
using static CraftableCartography.Lib.ItemChecks;
using System;
using System.Threading.Tasks;

namespace CraftableCartography.MapLayers
{

    public class CCPlayerMapLayer : MarkerMapLayer
    {
        Dictionary<IPlayer, EntityMapComponent> MapComps = new();
        ICoreClientAPI capi;
        LoadedTexture ownTexture;
        LoadedTexture otherTexture;

        public override string Title => "Players";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public override string LayerGroupCode => "terrain";

        public CCPlayerMapLayer(ICoreAPI api, IWorldMapManager mapsink) : base(api, mapsink)
        {
            capi = (api as ICoreClientAPI);
        }

        private void Event_PlayerDespawn(IClientPlayer byPlayer)
        {
            HidePlayer(byPlayer);
        }

        private void Event_PlayerSpawn(IClientPlayer byPlayer)
        {
            if (ShouldShowPlayer(byPlayer))
            {
                ShowPlayer(byPlayer);
            } else
            {
                HidePlayer(byPlayer);
            }
        }

        public override void OnLoaded()
        {
            if (capi != null)
            {
                // Only client side
                capi.Event.PlayerEntitySpawn += Event_PlayerSpawn;
                capi.Event.PlayerEntityDespawn += Event_PlayerDespawn;

                capi.Event.RegisterGameTickListener(OnMapOpenedClient, 2000);
            }
        }

        private void OnMapOpenedClient(float _)
        {
            OnMapOpenedClient(); // TODO: figure out a better way of updating the player markers (asynchronously?)
        }

        public override void OnMapOpenedClient()
        {
            if (!Active) return;

            int size = (int)GuiElement.scaled(32);

            if (ownTexture == null)
            {
                ImageSurface surface = new(Format.Argb32, size, size);
                Context ctx = new(surface);
                ctx.SetSourceRGBA(0, 0, 0, 0);
                ctx.Paint();
                capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[] { 0, 0, 0, 1 }, new double[] { 1, 1, 1, 1 });

                ownTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, false), size / 2, size / 2);
                ctx.Dispose();
                surface.Dispose();
            }

            if (otherTexture == null)
            {
                ImageSurface surface = new(Format.Argb32, size, size);
                Context ctx = new(surface);
                ctx.SetSourceRGBA(0, 0, 0, 0);
                ctx.Paint();
                capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[] { 0.3, 0.3, 0.3, 1 }, new double[] { 0.7, 0.7, 0.7, 1 });
                otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, false), size / 2, size / 2);
                ctx.Dispose();
                surface.Dispose();
            }
            
            foreach (IPlayer player in capi.World.AllOnlinePlayers)
            {
                if (ShouldShowPlayer(player))
                {
                    ShowPlayer(player);
                } else
                {
                    HidePlayer(player);
                }
            }
        }

        public void HidePlayer(IPlayer player)
        {
            if (MapComps.TryGetValue(player, out EntityMapComponent cmp))
            {
                cmp?.Dispose();
                MapComps.Remove(player);
            }
        }

        public void ShowPlayer(IPlayer player)
        {
            if (MapComps.TryGetValue(player, out EntityMapComponent cmp))
            {
                cmp?.Dispose();
                MapComps.Remove(player);
            }

            cmp = new EntityMapComponent(capi, player == capi.World.Player ? ownTexture : otherTexture, player.Entity);

            MapComps[player] = cmp;
        }

        public bool ShouldShowPlayer(IPlayer player)
        {
            if (player.Entity == null)
            {
                capi.World.Logger.Warning("Can't add player {0} to world map, missing entity :<", player.PlayerUID);
                return false;
            }

            if (capi.World.Config.GetBool("mapHideOtherPlayers", false) && player.PlayerUID != capi.World.Player.PlayerUID)
            {
                return false;
            }

            if (!HasJPS(player)) return false;

            if (player != capi.World.Player &&
                player.Entity.WatchedAttributes.GetString(JPSChannelAttr, "") !=
                capi.World.Player.Entity.WatchedAttributes.GetString(JPSChannelAttr, ""))
            {
                return false;
            }

            return true;
        }


        public override void Render(GuiElementMap mapElem, float dt)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.Render(mapElem, dt);
            }
        }

        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.OnMouseMove(args, mapElem, hoverText);
            }
        }

        public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.OnMouseUpOnElement(args, mapElem);
            }
        }

        public override void OnMapClosedClient()
        {
            //Dispose();
            //MapComps.Clear();
        }


        public override void Dispose()
        {
            foreach (var val in MapComps)
            {
                val.Value?.Dispose();
            }

            ownTexture?.Dispose();
            ownTexture = null;
            otherTexture?.Dispose();
            otherTexture = null;
        }
    }
}