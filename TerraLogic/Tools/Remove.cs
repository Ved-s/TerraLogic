using Microsoft.Xna.Framework;
using TerraLogic.GuiElements;

namespace TerraLogic.Tools
{
    public class Remove : Tool
    {
        public override string Id => "remove";
        public override string DisplayName => "Remove tile";

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event != EventType.Released)
                Gui.Logics.HoverWorld.SetTile(Gui.Logics.HoverWorld.ScreenToTiles(pos).ToPoint(), "");
        }
    }

}
