using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static CraftableCartography.Lib.CCConstants;

namespace CraftableCartography.Lib
{
    public static class ItemChecks
    {
        public const string jpsCode = "jps";
        public const string primitiveCompassCode = "primitivecompass";
        public const string CompassCode = "compass";
        public const string temporalCompassCode = "compasstemporal";
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

        // Измененный метод для проверки предмета в руке
        private static bool ItemCheckInHand(IPlayer player, string itemCode, string domain)
        {
            // Проверяем правую руку
            var activeSlot = player.InventoryManager.ActiveHotbarSlot;
            if (IsItemMatch(activeSlot?.Itemstack, itemCode, domain))
            {
                return true;
            }

            // Проверяем левую руку
            var leftHandSlot = player.Entity.LeftHandItemSlot;
            if (IsItemMatch(leftHandSlot?.Itemstack, itemCode, domain))
            {
                return true;
            }

            return false;
        }

        // Универсальный метод для проверки соответствия предмета
        private static bool IsItemMatch(ItemStack itemstack, string itemCode, string domain)
        {
            if (itemstack?.Item == null) return false;

            return itemstack.Item.Code.FirstCodePart() == itemCode && itemstack.Item.Code.Domain == domain;
        }

        // Метод для проверки наличия предмета в слоте шлема
        private static bool ItemCheckInHelmetSlot(IPlayer player, string itemCode, string domain)
        {
            var inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null) return false;
            int[] slotIndexes = { 12, 23, 24, 31, 32 };
            // Слоты: Снаружи, Средний, Внутренний
            // Шлем     31        23        ?
            // Маска    32        24        ?
            // Шея      33        25        ?
            foreach (int slotIndex in slotIndexes)
            {
                var slot = inventory[slotIndex];
                if (IsItemMatch(slot?.Itemstack, itemCode, domain))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasPrimitiveCompass(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return ItemCheckInHand(player, primitiveCompassCode, modDomain);
            return false;
        }

        public static bool HasCompass(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return ItemCheckInHand(player, CompassCode, modDomain);
            return false;
        }

        public static bool HasTemporalCompass(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                    return ItemCheckInHand(player, temporalCompassCode, modDomain);
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
                    return ItemCheckInHelmetSlot(player, jpsCode, modDomain);
                } else
                {
                    return player.Entity.WatchedAttributes.GetBool(hasJPSAttr);
                }
            } else if (player.Entity.Api.Side == EnumAppSide.Server)
            {
                return ItemCheckInHelmetSlot(player, jpsCode, modDomain);
            }
            return false;
        }

        // Метод для Combat Overhaul
        private static bool ItemCheckInHelmetSlot_CO(IPlayer player, string itemCode, string domain)
        {
            var inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName); // Слот шлема
            if (inventory == null) return false;
            for (int slotIndex = 24; slotIndex <= 26; slotIndex++)
            {
                var slot = inventory[slotIndex];
                if (IsItemMatch(slot?.Itemstack, itemCode, domain))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool HasJPS_CO(IPlayer player)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
            {
                if (((ICoreClientAPI)player.Entity.Api).World.Player == player)
                {
                    return ItemCheckInHelmetSlot_CO(player, jpsCode, modDomain);
                }
                else
                {
                    return player.Entity.WatchedAttributes.GetBool(hasJPSAttr);
                }
            }
            else if (player.Entity.Api.Side == EnumAppSide.Server)
            {
                return ItemCheckInHelmetSlot_CO(player, jpsCode, modDomain);
            }
            return false;
        }
    }
}
