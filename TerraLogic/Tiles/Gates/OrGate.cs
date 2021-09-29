using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles.Gates
{
    class OrGate : LogicGate
    {
        protected override string GateId => "Or";

        protected override bool Compute(bool[] lamps)
        {
            return lamps.Length > 0 && lamps.Any(b => b);
        }
    }
}
