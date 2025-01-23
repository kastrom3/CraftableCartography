using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CraftableCartography.Lib
{
    public static class ItemChecks
    {
        public const string jpsCode = "craftablecartography:jps";
        public const string compassCode = "craftablecartography:compasstemporal";
        public const string temporalSextantCode = "craftablecartography:sextanttemporal";
        public const string mapCode = "craftablecartography:map";

        public static bool GenericItemCheck(ICoreClientAPI capi, string itemCode)
        {
            foreach (ItemSlot slot in capi.World.Player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code == itemCode)
                            return true;
            return false;
        }
        public static bool GenericItemsCheck(ICoreClientAPI capi, string[] itemCodes)
        {
            foreach (ItemSlot slot in capi.World.Player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code != null)
                            foreach (string itemCode in itemCodes)
                                if (slot.Itemstack.Item.Code == itemCode)
                                    return true;

            return false;
        }
        public static bool HasTemporalSextant(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, temporalSextantCode);
        }

        public static bool HasTemporalCompass(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, compassCode);
        }

        public static bool HasMap(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, mapCode);
        }
        public static bool HasJPS(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, jpsCode);
        }
    }
}
