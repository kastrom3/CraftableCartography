using Vintagestory.API.Client;
using Vintagestory.API.Common;
using static CraftableCartography.Lib.ItemChecks;
using CraftableCartography.Systems;
using System.Text;
using Vintagestory.API.Config;

namespace CraftableCartography.Items.Map
{
    public class ItemDrawableMap : Item
    {
        private ModSystemDrawableMap mapModSys;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            mapModSys = api.ModLoader.GetModSystem<ModSystemDrawableMap>();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            // Проверяем конфигурацию мода - если функция рисования отключена, прерываем выполнение
            if (byEntity.Api.ModLoader.GetModSystem<CraftableCartographyModSystem>().Config.modConfig.DrawnMap == false)
            {
                handling = EnumHandHandling.NotHandled;
                return;
            }

            // Вызываем на обеих сторонах
            if (firstEvent)
            {
                IPlayer player = (byEntity as EntityPlayer)?.Player;
                if (player == null) return;

                // Начинаем редактирование на обеих сторонах
                mapModSys.BeginEdit(player, slot);

                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = (ICoreClientAPI)byEntity.Api;

                    if (!HasMap(capi.World.Player))
                    {
                        mapModSys.CancelEdit(player);
                        return;
                    }

                    var activeSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;

                    if (activeSlot?.Itemstack == null || !IsOurMap(activeSlot.Itemstack))
                    {
                        mapModSys.CancelEdit(player);
                        return;
                    }

                    if (capi.Gui.OpenedGuis.Find(dlg => dlg is DrawMapGui) == null)
                    {
                        DrawMapGui mapGui = new DrawMapGui(capi, activeSlot, mapModSys);
                        mapGui.TryOpen();
                        handling = EnumHandHandling.PreventDefault;
                    }
                }
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        private bool IsOurMap(ItemStack itemstack)
        {
            return itemstack?.Item != null &&
                   itemstack.Item.Code.FirstCodePart() == "map" &&
                   itemstack.Item.Code.Domain == "craftablecartography";
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string @string = itemStack.Attributes.GetString("title");
            if (@string != null && @string.Length > 0)
            {
                return @string;
            }

            return base.GetHeldItemName(itemStack);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            // Если функция рисования отключена в конфиге, не показываем подсказку
            if (api?.ModLoader.GetModSystem<CraftableCartographyModSystem>().Config.modConfig.DrawnMap == false)
            {
                return new WorldInteraction[0];
            }

            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = Lang.Get("craftablecartography:heldhelp-drawmap"),
                    MouseButton = EnumMouseButton.Right
                }
            };
        }

        // Добавляем метод для отображения информации о карте
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot?.Itemstack?.Attributes?.HasAttribute("mapData") == true)
            {
                dsc.AppendLine(Lang.Get("craftablecartography:drawn-map"));
            }
            else
            {
                // Если данных карты нет - показываем, что карта пустая
                dsc.AppendLine(Lang.Get("craftablecartography:empty-map"));
            }
        }
    }
}