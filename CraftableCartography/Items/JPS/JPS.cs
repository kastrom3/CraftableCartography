using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CraftableCartography.Lib;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CraftableCartography.Items.JPS
{
    public class ItemJPSDevice : ItemWearable
    {
        protected float fuelHoursCapacity = 576f; // Максимальное количество часов топлива

        // Получение текущего количества топлива
        public double GetFuelHours(ItemStack stack)
        {
            return Math.Max(0.0, stack.Attributes.GetDecimal("fuelHours"));
        }

        // Установка количества топлива
        public void SetFuelHours(ItemStack stack, double fuelHours)
        {
            stack.Attributes.SetDouble("fuelHours", fuelHours);
        }

        // Добавление топлива
        public void AddFuelHours(ItemStack stack, double fuelHours)
        {
            stack.Attributes.SetDouble("fuelHours", Math.Max(0.0, fuelHours + GetFuelHours(stack)));
        }
        
        // Получение топлива из атрибутов предмета
        public float GetStackFuel(ItemStack stack)
        {
            return stack.ItemAttributes?["jpsFuelHours"].AsFloat() ?? 0f;
        }

        // Добавление топлива (определяет, сколько предметов может быть объединено с шлемом)
        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
        {
            if (priority == EnumMergePriority.DirectMerge)
            {
                if (GetStackFuel(sourceStack) == 0f)
                {
                    return base.GetMergableQuantity(sinkStack, sourceStack, priority);
                }

                return 1;
            }

            return base.GetMergableQuantity(sinkStack, sourceStack, priority);
        }

        // Добавление топлива (выполняет объединение двух предметов)
        public override void TryMergeStacks(ItemStackMergeOperation op)
        {
            if (op.CurrentPriority == EnumMergePriority.DirectMerge)
            {
                float stackFuel = GetStackFuel(op.SourceSlot.Itemstack);
                double fuelHours = GetFuelHours(op.SinkSlot.Itemstack);
                if (stackFuel > 0f && fuelHours + (double)stackFuel <= (double)fuelHoursCapacity)
                {
                    SetFuelHours(op.SinkSlot.Itemstack, (double)stackFuel + fuelHours);
                    op.MovedQuantity = 1;
                    op.SourceSlot.TakeOut(1);
                    op.SinkSlot.MarkDirty();
                }
                else if (api.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(this, "jpsfull", Lang.Get("craftablecartography:ingameerror-jps-full"));
                }
            }
        }

        // Переопределение метода для отображения информации о предмете
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            double fuelHours = GetFuelHours(inSlot.Itemstack);
            int days = (int)(fuelHours / 24);
            double remainingHours = fuelHours % 24;
            dsc.AppendLine(Lang.Get("Has fuel for {0} days and {1:0.#} hours", days, remainingHours));
            if (fuelHours <= 0.0)
            {
                dsc.AppendLine(Lang.Get("Add temporal gear to refuel"));
            }
        }
    }
}