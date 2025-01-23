using Vintagestory.API.Client;

namespace CraftableCartography.Items.Sextant
{
    internal class CoordinatesGui : HudElement
    {
        public CoordinatesGui(ICoreClientAPI capi) : base(capi)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds textBounds = ElementBounds.Fixed(0, 50, 300, 100);
            textBounds.WithAlignment(EnumDialogArea.CenterMiddle);

            SingleComposer = capi.Gui.CreateCompo("sextantdisplay", dialogBounds)
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
