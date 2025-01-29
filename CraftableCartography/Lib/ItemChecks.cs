using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CraftableCartography.Lib
{
    public static class ItemChecks
    {
        public const string jpsCode = "jps";
        public const string compassCode = "compasstemporal";
        public const string temporalSextantCode = "sextanttemporal";
        public const string mapCode = "map";
        public const string modDomain = "craftablecartography";

        public static bool GenericItemCheck(ICoreClientAPI capi, string itemCode, string domain)
        {
            foreach (ItemSlot slot in capi.World.Player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code.FirstCodePart() == itemCode && slot.Itemstack.Item.Code.Domain == domain)
                            return true;
            return false;
        }
        public static bool GenericItemsCheck(ICoreClientAPI capi, string[] itemCodes, string domain)
        {
            foreach (ItemSlot slot in capi.World.Player.InventoryManager.GetHotbarInventory())
                if (slot.Itemstack != null)
                    if (slot.Itemstack.Item != null)
                        if (slot.Itemstack.Item.Code != null)
                            foreach (string itemCode in itemCodes)
                                if (slot.Itemstack.Item.Code.FirstCodePart() == itemCode && slot.Itemstack.Item.Code.Domain == domain)
                                    return true;

            return false;
        }
        public static bool HasTemporalSextant(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, temporalSextantCode, modDomain);
        }

        public static bool HasTemporalCompass(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, compassCode, modDomain);
        }

        public static bool HasMap(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, mapCode, modDomain);
        }
        public static bool HasJPS(ICoreClientAPI capi)
        {
            return GenericItemCheck(capi, jpsCode, modDomain);
        }
    }
}
