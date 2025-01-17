using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace CraftableCartography.Items.Compass
{
    public class Sextant : Item
    {
        HeadingGui gui;

        float heading;
        float headingDelta;

        float damping = 0.9f;
        float accelerationQuot = 30;

        float lastUpdate;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            gui ??= new((ICoreClientAPI)byEntity.Api);
            gui.TryOpen();

            lastUpdate = 0;

            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            float dt = secondsUsed - lastUpdate;

            float yawDeg = 180 - byEntity.Pos.Yaw * (180 / GameMath.PI);

            float angleDiff = GameMath.AngleDegDistance(heading, yawDeg);

            headingDelta += angleDiff / accelerationQuot * dt;
            headingDelta *= damping;

            heading += headingDelta * dt;

            while (heading < 0) heading += 360;
            while (heading > 360) heading -= 360;

            string word;
            if (heading < 45)
            {
                word = "\nNorth";
            }
            else if (heading < 135)
            {
                word = "\nEast";
            }
            else if (heading < 225)
            {
                word = "\nSouth";
            }
            else if (heading < 315)
            {
                word = "\nWest";
            }
            else
            {
                word = "\nNorth";
            }

            string text = Math.Round(heading).ToString();
            text += word;

            gui.SetText(text);

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            gui.TryClose();
        }
    }
}
