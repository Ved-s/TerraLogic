using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    public abstract class Pos
    {
        public abstract int Calculate(UIElement element);

        public static Pos X(string element = null) => new ReferencePos(element, RefProp.X);
        public static Pos Y(string element = null) => new ReferencePos(element, RefProp.Y);
        public static Pos Width(string element = null, bool ignoreInvisibility = false) => new ReferencePos(element, RefProp.Width,  false, ignoreInvisibility);
        public static Pos Height(string element = null, bool ignoreInvisibility = false) => new ReferencePos(element, RefProp.Height, false, ignoreInvisibility);

        public static Pos X(string element, float value)      => new RelativePos(element, RefProp.X, value);
        public static Pos Y(string element, float value)      => new RelativePos(element, RefProp.Y, value);
        public static Pos Width(string element, float value)  => new RelativePos(element, RefProp.Width, value);
        public static Pos Height(string element, float value) => new RelativePos(element, RefProp.Height, value);


        public static Pos Right (string element = null, bool ignoreInvisibility = false) => new ReferencePos(element, RefProp.Width,  true, ignoreInvisibility);
        public static Pos Bottom(string element = null, bool ignoreInvisibility = false) => new ReferencePos(element, RefProp.Height, true, ignoreInvisibility);
        

        public static implicit operator Pos(int value) => new ConstPos(value); 
        public static Pos operator +(Pos a, Pos b) => new CombinedPos(a, b, false); 
        public static Pos operator -(Pos a, Pos b) => new CombinedPos(a, b, true);

        public class ConstPos : Pos
        {
            int value;

            public ConstPos(int value) { this.value = value; }

            public override int Calculate(UIElement e) => value;
        }
        public class CombinedPos : Pos
        {
            Pos a, b;
            bool sub;

            public CombinedPos(Pos a, Pos b, bool subtract) 
            {
                this.a = a;
                this.b = b;
                sub = subtract;
            }

            public override int Calculate(UIElement e) => sub ? a.Calculate(e) - b.Calculate(e) : a.Calculate(e) + b.Calculate(e);
        }
        public class ReferencePos : Pos 
        {
            UIElement resolvedElement;
            string elementName;
            RefProp p;
            bool end, ignoreInvisibility;

            public ReferencePos(string element, RefProp prop, bool end = false, bool ignoreInvisibility = false) 
            {
                this.ignoreInvisibility = ignoreInvisibility;
                elementName = element;
                p = prop;
                this.end = end;
            }

            public override int Calculate(UIElement e)
            {
                if (resolvedElement is null) resolvedElement = elementName is null? e : e.GetElement(elementName);
                switch (p) 
                {
                    case RefProp.X: return resolvedElement.X.Calculate(resolvedElement);
                    case RefProp.Y: return resolvedElement.Y.Calculate(resolvedElement);
                    case RefProp.Width:  return (resolvedElement.Visible || ignoreInvisibility)? (end ? resolvedElement.Width.Calculate (resolvedElement) + resolvedElement.X.Calculate(resolvedElement) : resolvedElement.Width .Calculate(resolvedElement)) : (end? resolvedElement.X.Calculate(resolvedElement) : 0 );
                    case RefProp.Height: return (resolvedElement.Visible || ignoreInvisibility)? (end ? resolvedElement.Height.Calculate(resolvedElement) + resolvedElement.Y.Calculate(resolvedElement) : resolvedElement.Height.Calculate(resolvedElement)) : (end? resolvedElement.Y.Calculate(resolvedElement) : 0 );
                }
                return 0;
            }

             
        }

        public class RelativePos : Pos
        {
            UIElement resolvedElement;
            string elementName;
            RefProp p;
            float value;
            bool includeSelf;

            public RelativePos(string element, RefProp prop, float value, bool includeSelfProp = true)
            {
                elementName = element;
                p = prop;
                this.value = value;
                includeSelf = includeSelfProp;
            }

            public override int Calculate(UIElement e)
            {
                if (resolvedElement is null) resolvedElement = e.GetElement(elementName);
                if (resolvedElement is null) return 0;
                switch (p)
                {
                    case RefProp.X:      return (int)((resolvedElement.X.Calculate(e)      - (includeSelf ? e.X.Calculate(e)      : 0)) * value);
                    case RefProp.Y:      return (int)((resolvedElement.Y.Calculate(e)      - (includeSelf ? e.Y.Calculate(e)      : 0)) * value);
                    case RefProp.Width:  return (int)((resolvedElement.Width.Calculate(e)  - (includeSelf ? e.Width.Calculate(e)  : 0)) * value);
                    case RefProp.Height: return (int)((resolvedElement.Height.Calculate(e) - (includeSelf ? e.Height.Calculate(e) : 0)) * value);
                }
                return 0;
            }


        }

        public enum RefProp { X, Y, Width, Height }

    }
}
