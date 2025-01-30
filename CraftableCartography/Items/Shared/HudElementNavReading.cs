using Vintagestory.API.Client;

namespace CraftableCartography.Items.Shared
{
    internal class HudElementNavReading : HudElement
    {
        string text;
        ElementBounds dialogBounds;
        ElementBounds textBounds;
        ElementBounds bgBounds;

        public HudElementNavReading(ICoreClientAPI capi) : base(capi)
        {
            dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedPosition(0, 150);

            textBounds = ElementBounds.Fixed(0, 0, 0, 0).WithAlignment(EnumDialogArea.CenterMiddle);

            bgBounds = new ElementBounds();
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithFixedPadding(5);

            text = "";

            SingleComposer = capi.Gui.CreateCompo("navdisplay", dialogBounds)
                .AddGameOverlay(bgBounds)
                .BeginChildElements(bgBounds)
                .AddStaticTextAutoBoxSize(text, CairoFont.WhiteMediumText(), EnumTextOrientation.Center, textBounds)
                .EndChildElements()
                .Compose();
        }

        private void Recompose()
        {
            SingleComposer.Clear(dialogBounds);

            SingleComposer.AddGameOverlay(bgBounds).BeginChildElements(bgBounds)
                .AddStaticTextAutoBoxSize(text, CairoFont.WhiteMediumText(), EnumTextOrientation.Center, textBounds)
                .EndChildElements()
                .Compose();
        }

        public void SetText(string inText)
        {
            text = inText;
            Recompose();
        }
    }
}
