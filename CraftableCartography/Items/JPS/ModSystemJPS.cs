using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CraftableCartography.Items.JPS
{
    class ModSystemJPS : ModSystem
    {
        private ICoreServerAPI sapi;
        private double lastCheckTotalHours;

        // Массив слотов, которые нужно проверять
        private readonly int[] slotsNumber = { 12, 23, 24, 31, 32 };

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            // Загружаем систему только на сервере
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            // Регистрируем обработчик, который будет вызываться каждую секунду
            api.Event.RegisterGameTickListener(OnTickServer1s, 1000);
        }

        private void OnTickServer1s(float dt)
        {
            // Получаем текущее игровое время в часах
            double totalHours = sapi.World.Calendar.TotalHours;
            // Вычисляем, сколько времени прошло с последней проверки
            double timePassed = totalHours - lastCheckTotalHours;

            // Если прошло меньше 0.05 часов (3 минуты), ничего не делаем
            if (timePassed <= 0.05)
            {
                return;
            }

            // Получаем всех онлайн-игроков
            IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
            for (int i = 0; i < allOnlinePlayers.Length; i++)
            {
                // Получаем инвентарь игрока
                IInventory ownInventory = allOnlinePlayers[i].InventoryManager.GetOwnInventory("character");
                if (ownInventory != null)
                {
                    // Проходим по всем слотам, которые нужно проверить
                    foreach (int slotNumber in slotsNumber)
                    {
                        // Проверяем слот
                        ItemSlot itemSlot = ownInventory[slotNumber];
                        if (itemSlot?.Itemstack?.Collectible is ItemJPSDevice ItemJPSDevice)
                        {
                            // Уменьшаем заряд устройства на величину timePassed
                            ItemJPSDevice.AddFuelHours(itemSlot.Itemstack, -timePassed);
                            // Помечаем слот как измененный для синхронизации с клиентом
                            itemSlot.MarkDirty();
                        }
                    }
                }
            }

            // Обновляем время последней проверки
            lastCheckTotalHours = totalHours;
        }
    }
}
