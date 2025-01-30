using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftableCartography.Items.Shared;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace CraftableCartography.Items.Compass
{
    public class Compass : Item
    {
        HudElementNavReading gui;

        float heading;
        float headingDelta;

        float damping = 0.9f;
        float accelerationQuot = 360;

        float lastUpdate;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                gui ??= new((ICoreClientAPI)byEntity.Api);
                gui.TryOpen();

                lastUpdate = 0;
            }
            
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                float dt = secondsUsed - lastUpdate;

                float yawDeg = 180 - byEntity.Pos.Yaw * (180 / GameMath.PI);

                float angleDiff = GameMath.AngleDegDistance(heading, yawDeg);

                headingDelta += (angleDiff / accelerationQuot) * dt;
                headingDelta *= damping;

                heading += headingDelta * dt;

                while (heading < 0) heading += 360;
                while (heading > 360) heading -= 360;

                string word = "\n";
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

                string text = Math.Round(heading).ToString() + "°";
                text += word;

                gui.SetText(text);
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
