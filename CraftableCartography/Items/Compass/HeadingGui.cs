using Vintagestory.API.Client;

namespace CraftableCartography.Items.Compass
{
    internal class HeadingGui : HudElement
    {
        public HeadingGui(ICoreClientAPI capi) : base(capi)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds textBounds = ElementBounds.Fixed(0, 50, 300, 100);
            textBounds.WithAlignment(EnumDialogArea.CenterMiddle);

            SingleComposer = capi.Gui.CreateCompo("compassdisplay", dialogBounds)
                .AddDynamicText("test", CairoFont.WhiteMediumText(), textBounds, "text")
                .Compose();
        }

        public void SetText(string text)
        {
            GuiElementDynamicText elemText = SingleComposer.GetDynamicText("text");
            elemText.SetNewText(text);
        }
    }
}
