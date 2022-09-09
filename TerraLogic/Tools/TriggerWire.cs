using Microsoft.Xna.Framework;
using TerraLogic.Gui;
using TerraLogic.GuiElements;

namespace TerraLogic.Tools
{
    public class TriggerWire : Tool
    {
        public override string Id => "trigger";
        public override string DisplayName => "Trigger selected wires";

        public override bool AllowWireSelection => true;
        public override bool DrawMouseIcon => false;
        public override bool ShowWires => true;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed)
            {
                Point worldpos = Logics.HoverWorld.ScreenToTiles(pos).ToPoint();

                int wires = Logics.SelectedWire;
                if (wires == 0)
                    wires = Logics.HoverWorld.Wires[worldpos];
                if (wires == 0)
                    return;

                Logics.HoverWorld.SignalWire(worldpos, wires);
            }
        }
    }

}
