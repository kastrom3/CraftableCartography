using CraftableCartography.Items.Shared;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace CraftableCartography.Items.Sextant
{
    public class Sextant : Item
    {
        HudElementNavReading gui;

        NatFloat randomX;
        NatFloat randomY;
        NatFloat randomZ;

        float maxVar = 25000f;

        double moveVar;

        EntityPos lastPos;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                gui ??= new((ICoreClientAPI)byEntity.Api);
                gui.TryOpen();

                moveVar = 0;

                lastPos = byEntity.Pos.Copy();

                Vec3d pos = byEntity.Pos.AsBlockPos.ToVec3d();
                pos.X -= api.World.DefaultSpawnPosition.AsBlockPos.X;
                pos.Z -= api.World.DefaultSpawnPosition.AsBlockPos.Z;

                randomX = NatFloat.createGauss((float)pos.X, maxVar);
                randomY = NatFloat.createGauss((float)pos.Y, maxVar);
                randomZ = NatFloat.createGauss((float)pos.Z, maxVar);
            }

            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                // Получаем позицию игрока
                BlockPos playerPos = byEntity.Pos.AsBlockPos;

                // Получаем уровень солнечного света
                int sunLightLevel = api.World.BlockAccessor.GetLightLevel(playerPos, EnumLightLevelType.OnlySunLight);

                // Проверяем, находится ли игрок в тени
                bool isInShadow = sunLightLevel < 22; // Порог тени (можно настроить)

                float timeOfDay = api.World.Calendar.HourOfDay;
                bool isDayTime = timeOfDay >= 6 && timeOfDay < 19; // День с 6 утра до 6 вечера

                // Проверка на дождь
                bool isRaining = api.World.BlockAccessor.GetClimateAt(playerPos, EnumGetClimateMode.NowValues).Rainfall > 0.1f; // Предполагаем, что дождь идет, если Rainfall больше 0.1

                if (isInShadow || !isDayTime || isRaining)
                {
                    gui.SetText("???, ???, ???");
                }
                else
                {
                    // Проверяем движение игрока
                    double distanceMoved = lastPos.DistanceTo(byEntity.Pos);
                    bool isMoving = distanceMoved > 0.1; // Порог движения (можно настроить)

                    // Увеличиваем moveVar при движении (замедляет определение координат)
                    if (isMoving)
                    {
                        moveVar += distanceMoved * 2; // Увеличиваем moveVar пропорционально движению
                    }

                    // Уменьшаем moveVar со временем, если игрок неподвижен
                    if (!isMoving)
                    {
                        moveVar = Math.Max(0, moveVar - 0.1); // Постепенно уменьшаем moveVar
                    }

                    // Учитываем moveVar при расчете погрешности
                    double varThisStep = (float)Math.Min(maxVar / Math.Pow(secondsUsed - moveVar, 5), maxVar);

                    // Убедимся, что varThisStep не становится отрицательным
                    if (varThisStep < 0) varThisStep = maxVar;

                    randomX.avg = (float)(byEntity.Pos.X - api.World.DefaultSpawnPosition.X);
                    randomY.avg = (float)byEntity.Pos.Y;
                    randomZ.avg = (float)(byEntity.Pos.Z - api.World.DefaultSpawnPosition.Z);

                    randomX.var = (float)varThisStep;
                    randomY.var = (float)varThisStep;
                    randomZ.var = (float)varThisStep;

                    string text =
                        Math.Round(randomX.nextFloat()).ToString() + ", " +
                        Math.Round(randomY.nextFloat()).ToString() + ", " +
                        Math.Round(randomZ.nextFloat());

                    gui.SetText(text);

                    lastPos = byEntity.Pos.Copy();
                }
            }

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
                gui.TryClose();
        }
    }
}