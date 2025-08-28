using System;
using CraftableCartography.Items.Shared;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using CraftableCartography.Patches;

namespace CraftableCartography.Items.Compass
{
    public class Compass : Item
    {
        ICoreClientAPI capi;
        
        HudElementNavReading gui;
        HudCompassNeedleRenderer needleRenderer;

        float heading;
        float headingDelta;

        float damping = 0.96f;
        float accelerationMult = 5.6f;

        long lastUpdate;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                capi ??= byEntity.Api as ICoreClientAPI;

                needleRenderer = new(capi, this);
                needleRenderer.heading = heading;

                capi.Event.KeyDown += Event_KeyDown;

                HudToolbarPatches.OnMouseWheel += HudToolbarPatches_OnMouseWheel;
            }
            
            handling = EnumHandHandling.PreventDefault;
        }

        private void HudToolbarPatches_OnMouseWheel(ref MouseWheelEventArgs args)
        {
            needleRenderer.compassZoom += (needleRenderer.compassZoom * 0.02f * args.deltaPrecise);
            args.SetHandled(true);
        }

        private void Event_KeyDown(KeyEvent e)
        {
            if (e.KeyCode == (int)GlKeys.Up)
            {
                needleRenderer.compassZoom *= 1.02f;
                e.Handled = true;
            } else if (e.KeyCode == (int)GlKeys.Down)
            {
                needleRenderer.compassZoom *= 0.98f;
                e.Handled = true;
            }
        }

        private void DoMoveStep(Entity byEntity)
        {
            if (lastUpdate == 0) lastUpdate = byEntity.World.ElapsedMilliseconds;

            float dt = (byEntity.World.ElapsedMilliseconds - lastUpdate) / 1000f;
            
            float yawDeg = 180 - byEntity.SidedPos.Yaw * (180 / GameMath.PI);

            float angleDiff = GameMath.AngleDegDistance(heading, yawDeg);

            headingDelta += (angleDiff * accelerationMult) * dt;
            headingDelta *= damping;

            heading += headingDelta * dt;

            while (heading < 0) heading += 360;
            while (heading > 360) heading -= 360;

            lastUpdate = byEntity.World.ElapsedMilliseconds;
        }

        private string GetText()
        {
            string word = "";
            if (heading < 67.5 || heading > 292.5)
            {
                word += "N";
            }
            else if (heading > 112.5 && heading < 247.5)
            {
                word += "S";
            }

            if (heading > 22.5 && heading < 157.5)
            {
                word += "E";
            }
            else if (heading > 202.5 && heading < 337.5)
            {
                word += "W";
            }

            string text = "";

            if (Code.FirstCodePart() == "compass") text += Math.Round(heading).ToString() + "°\n";
            text += word;

            return text;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                DoMoveStep(byEntity);

                gui?.SetText(GetText());

                needleRenderer.heading = heading;
            }

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                gui?.TryClose();

                needleRenderer.Dispose();
                needleRenderer = null;

                ((ICoreClientAPI)api).Event.KeyDown -= Event_KeyDown;

                HudToolbarPatches.OnMouseWheel -= HudToolbarPatches_OnMouseWheel;
            }
        }

        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            DoMoveStep(byEntity);
        }

        public override bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
        {
            foreach (ItemSlot slot in slots)
            {
                slot.TakeOut(1);
            }
            
            return true;
        }

        /*
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

            Shape shape = Vintagestory.API.Common.Shape.TryGet(api, Code);

            shape.GetElementByName("needle").RotationY = heading;

            MeshData compassMesh;

            capi.Tesselator.TesselateShape(this, shape, out compassMesh);

            renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(compassMesh);
        }
        */
    }
}
