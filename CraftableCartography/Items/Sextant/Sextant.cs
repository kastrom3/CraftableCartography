using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace CraftableCartography.Items.Sextant
{
    public class Sextant : Item
    {
        CoordinatesGui gui;

        NatFloat randomX;
        NatFloat randomY;
        NatFloat randomZ;

        float maxVar = 25000f;

        double moveVar;

        EntityPos lastPos;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
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

            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            moveVar += lastPos.DistanceTo(byEntity.Pos) * 2;

            double varThisStep = (float)Math.Min(maxVar / Math.Pow(secondsUsed - moveVar, 5), maxVar);

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

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            gui.TryClose();
        }
    }
}
