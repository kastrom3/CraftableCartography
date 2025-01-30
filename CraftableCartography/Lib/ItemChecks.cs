using Vintagestory.API.Client;
using Vintagestory.API.Common;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography.Lib
{
    public static class ItemChecks
    {
        public const string jpsCode = "jps";
        public const string temporalCompassCode = "compasstemporal";
        public const string temporalSextantCode = "sextanttemporal";
        public const string mapCode = "map";
        public const string modDomain = "craftablecartography";

        public static bool GenericItemCheck(IPlayer player, string itemCode, string domain)
        {
            foreach (ItemSlot slot in player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code.FirstCodePart() == itemCode && slot.Itemstack.Item.Code.Domain == domain)
                            return true;
            return false;
        }
        public static bool GenericItemsCheck(IPlayer player, string[] itemCodes, string domain)
        {
            foreach (ItemSlot slot in player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code != null)
                            foreach (string itemCode in itemCodes)
                                if (slot.Itemstack.Item.Code.FirstCodePart() == itemCode && slot.Itemstack.Item.Code.Domain == domain)
                                    return true;

            return false;
        }
        public static bool HasTemporalSextant(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return GenericItemCheck(player, temporalSextantCode, modDomain);
            return false;
        }

        public static bool HasTemporalCompass(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return GenericItemCheck(player, temporalCompassCode, modDomain);
            return false;
        }

        public static bool HasMap(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return GenericItemCheck(player, mapCode, modDomain);
            return false;
        }
        public static bool HasJPS(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
            {
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                {
                    return GenericItemCheck(player, jpsCode, modDomain);
                } else
                {
                    return player.Entity.WatchedAttributes.GetBool(hasJPSAttr);
                }
            } else if (player.Entity.Api.Side == EnumAppSide.Server)
            {
                return GenericItemCheck(player, jpsCode, modDomain);
            }
            return false;
        }
    }
}
