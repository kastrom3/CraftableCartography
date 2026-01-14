using CraftableCartography.Items.JPS;
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
        public const string gameDomain = "game";

        public static bool GenericItemCheck(IPlayer player, string itemCode, string domain)
        {
            try
            {
                if (player?.InventoryManager?.GetHotbarInventory() == null)
                    return false;

                foreach (ItemSlot slot in player.InventoryManager.GetHotbarInventory())
                {
                    if (IsItemMatch(slot?.Itemstack, itemCode, domain))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
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
            try
            {
                // Проверяем правую руку
                var activeSlot = player?.InventoryManager?.ActiveHotbarSlot;
                if (IsItemMatch(activeSlot?.Itemstack, itemCode, domain))
                    return true;

                // Проверяем левую руку (с проверкой Entity)
                var leftHandSlot = player?.Entity?.LeftHandItemSlot;
                if (IsItemMatch(leftHandSlot?.Itemstack, itemCode, domain))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Универсальный метод для проверки соответствия предмета
        private static bool IsItemMatch(ItemStack itemstack, string itemCode, string domain)
        {
            try
            {
                if (itemstack?.Item == null) return false;

                return itemstack.Item.Code.FirstCodePart() == itemCode && itemstack.Item.Code.Domain == domain;
            }
            catch
            {
                return false;
            }
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
            try
            {
                if (player?.Entity?.Api == null)
                    return false;

                if (player.Entity.Api.Side == EnumAppSide.Client)
                {
                    var clientApi = player.Entity.Api as ICoreClientAPI;
                    if (clientApi?.World?.Player == player)
                    {
                        return ItemCheckInHand(player, primitiveCompassCode, modDomain);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasCompass(IPlayer player)
        {
            try
            {
                if (player?.Entity?.Api == null)
                    return false;

                if (player.Entity.Api.Side == EnumAppSide.Client)
                {
                    var clientApi = player.Entity.Api as ICoreClientAPI;
                    if (clientApi?.World?.Player == player)
                    {
                        return ItemCheckInHand(player, CompassCode, modDomain);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasTemporalCompass(IPlayer player)
        {
            try
            {
                if (player?.Entity?.Api == null)
                    return false;

                if (player.Entity.Api.Side == EnumAppSide.Client)
                {
                    var clientApi = player.Entity.Api as ICoreClientAPI;
                    if (clientApi?.World?.Player == player)
                    {
                        return ItemCheckInHand(player, temporalCompassCode, modDomain);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasMap(IPlayer player)
        {
            try
            {
                if (player?.Entity?.Api == null)
                    return false;

                if (player.Entity.Api.Side == EnumAppSide.Client)
                {
                    var clientApi = player.Entity.Api as ICoreClientAPI;
                    if (clientApi?.World?.Player == player)
                    {
                        return ItemCheckInHand(player, mapCode, modDomain);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static bool HasJPS(IPlayer player)
        {
            try
            {
                if (player?.Entity?.Api == null)
                    return false;

                if (player.Entity.Api.Side == EnumAppSide.Client)
                {
                    var clientApi = player.Entity.Api as ICoreClientAPI;
                    if (clientApi?.World?.Player == player)
                    {
                        return CheckJPSWithFuel(player);
                    }
                    else
                    {
                        return player.Entity.WatchedAttributes.GetBool(hasJPSAttr, false);
                    }
                }
                else if (player.Entity.Api.Side == EnumAppSide.Server)
                {
                    return CheckJPSWithFuel(player);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckJPSWithFuel(IPlayer player)
        {
            try
            {
                var jpsItem = ItemCheckInHelmetSlot(player, jpsCode, modDomain);
                if (!jpsItem) return false;

                var jpsStack = GetJPSStack(player);
                if (jpsStack?.Item is ItemJPSDevice jps)
                {
                    return jps.GetFuelHours(jpsStack) > 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }


        private static ItemStack GetJPSStack(IPlayer player)
        {
            var inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null) return null;
            int[] slotIndexes = { 12, 23, 24, 31, 32 };
            foreach (int slotIndex in slotIndexes)
            {
                var slot = inventory[slotIndex];
                if (IsItemMatch(slot?.Itemstack, jpsCode, modDomain))
                {
                    return slot.Itemstack;
                }
            }
            return null;
        }
    }
}
