using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using ProtoBuf;
using CraftableCartography.Items.Map;

namespace CraftableCartography.Systems
{
    public class ModSystemDrawableMap : ModSystem
    {
        public static int mapSize = DrawMapGui.mapSize;
        public static int MaxMapDataSize = mapSize * mapSize;

        private Dictionary<string, ItemSlot> nowEditing = new Dictionary<string, ItemSlot>();
        private ICoreAPI api;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public void BeginEdit(IPlayer player, ItemSlot slot)
        {
            // Вызывается на обеих сторонах
            nowEditing[player.PlayerUID] = slot;
        }

        public void EndEdit(IPlayer player, byte[] mapData, string mapTitle = null, int mapScale = 32)
        {
            if (mapData != null && mapData.Length > MaxMapDataSize)
            {
                return;
            }

            if (nowEditing.TryGetValue(player.PlayerUID, out var slot))
            {
                // Обновляем данные локально на клиенте
                if (api.Side == EnumAppSide.Client && slot?.Itemstack?.Attributes != null)
                {
                    slot.Itemstack.Attributes.SetBytes("mapData", mapData);

                    // Обновляем название, если оно предоставлено
                    if (!string.IsNullOrEmpty(mapTitle))
                    {
                        slot.Itemstack.Attributes.SetString("title", mapTitle);
                    }

                    // Обновляем масштаб, если оно предоставлено
                    if (mapScale > 0)
                    {
                        slot.Itemstack.Attributes.SetInt("mapSize", mapScale);
                    }
                    slot.MarkDirty();
                }

                // Отправляем на сервер
                if (api is ICoreClientAPI capi)
                {
                    capi.Network.GetChannel("drawablemap")
                        .SendPacket(new MapDataPacket
                        {
                            PlayerUID = player.PlayerUID,
                            MapData = mapData,
                            MapTitle = mapTitle,
                            MapScale = mapScale
                        });
                }
            }

            nowEditing.Remove(player.PlayerUID);

            if (api.Side == EnumAppSide.Client)
            {
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), player.Entity);
            }
        }

        public void CancelEdit(IPlayer player)
        {
            nowEditing.Remove(player.PlayerUID);
            if (api is ICoreClientAPI capi)
            {
                capi.Network.GetChannel("drawablemap")
                    .SendPacket(new MapDataPacket
                    {
                        PlayerUID = player.PlayerUID,
                        DidSave = false
                    });
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;
            api.Network.RegisterChannel("drawablemap")
                .RegisterMessageType<MapDataPacket>();
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.Network.GetChannel("drawablemap")
                .SetMessageHandler<MapDataPacket>(OnMapDataPacket);
        }

        private void OnMapDataPacket(IServerPlayer fromPlayer, MapDataPacket packet)
        {
            if (packet.DidSave && packet.MapData != null)
            {
                // Находим слот игрока
                ItemSlot targetSlot = null;

                // Ищем в активном слоте горячей панели
                var activeSlot = fromPlayer.InventoryManager.ActiveHotbarSlot;
                if (activeSlot?.Itemstack?.Item is ItemDrawableMap)
                {
                    targetSlot = activeSlot;
                }
                else
                {
                    // Ищем в инвентаре
                    fromPlayer.Entity.WalkInventory((slot) =>
                    {
                        if (slot?.Itemstack?.Item is ItemDrawableMap)
                        {
                            targetSlot = slot;
                            return false;
                        }
                        return true;
                    });
                }

                if (targetSlot != null && targetSlot.Itemstack?.Attributes != null)
                {
                    // Обновляем данные на сервере
                    targetSlot.Itemstack.Attributes.SetBytes("mapData", packet.MapData);

                    // Обновляем название, если оно предоставлено
                    if (!string.IsNullOrEmpty(packet.MapTitle))
                    {
                        targetSlot.Itemstack.Attributes.SetString("title", packet.MapTitle);
                    }

                    // Сохраняем масштаб на сервере
                    if (packet.MapScale > 0)
                    {
                        targetSlot.Itemstack.Attributes.SetInt("mapSize", packet.MapScale);
                    }

                    targetSlot.MarkDirty();
                }
            }

            // Убираем из редактирования в любом случае
            nowEditing.Remove(packet.PlayerUID);
        }
    }

    [ProtoContract]
    public class MapDataPacket
    {
        [ProtoMember(1)]
        public string PlayerUID { get; set; }

        [ProtoMember(2)]
        public byte[] MapData { get; set; }

        [ProtoMember(3)]
        public bool DidSave { get; set; } = true;

        [ProtoMember(4)]
        public string MapTitle { get; set; }

        [ProtoMember(5)]
        public int MapScale { get; set; }
    }
}