using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles.Gates
{
    class XnorGate : LogicGate
    {
        protected override string GateId => "Xnor";

        protected override bool Compute(bool[] lamps)
        {
            return lamps.Length > 0 && lamps.Sum(b => b?1:0) != 1;
        }
    }
}
