using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TerraLogic
{
    static class ColorUils
    {
        public static KnownColor[] KnownColors { get; private set; }

        static ColorUils() 
        {
            List<KnownColor> colors = new List<KnownColor>();
            foreach (PropertyInfo prop in typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.PropertyType == typeof(Color)) 
                {
                    colors.Add(new KnownColor(prop.Name, (Color)prop.GetValue(null, null)));
                }
            }
            KnownColors = colors.ToArray();
        }

        public static KnownColor ClosestColor(Color c) 
        {
            float distance = float.MaxValue;
            KnownColor color = null;

            foreach (KnownColor known in KnownColors) 
            {
                float newDistance = ColorDistance(known.Value, c);
                if (color != null && newDistance >= distance) continue;

                distance = newDistance;
                color = known;
            }

            return color;
        }

        static float ColorDistance(Color a, Color b) 
        {
            return (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B)) / 3f;
        }
    }

    public class KnownColor 
    {


        public string Name { get; private set; }
        public Color Value { get; private set; }

        internal KnownColor(string name, Color value)
        {
            Name = name;
            Value = value;
        }
    }

    public struct Colors 
    {
        public Color Foreground;
        public Color Background;

        public Colors(Color fore, Color back) 
        {
            Foreground = fore;
            Background = back;
        }

        public Colors(byte foreR, byte foreG, byte foreB, Color back)
        {
            Foreground = new Color(foreR, foreG, foreB);
            Background = back;
        }
        public Colors(Color fore, byte backR, byte backG, byte backB)
        {
            Foreground = fore;
            Background = new Color(backR, backG, backB);
        }
        public Colors(byte foreR, byte foreG, byte foreB, byte backR, byte backG, byte backB)
        {
            Foreground = new Color(foreR, foreG, foreB);
            Background = new Color(backR, backG, backB);
        }
        public Colors(byte foreR, byte foreG, byte foreB, byte foreA, Color back)
        {
            Foreground = new Color(foreR, foreG, foreB, foreA);
            Background = back;
        }
        public Colors(Color fore, byte backR, byte backG, byte backB, byte backA)
        {
            Foreground = fore;
            Background = new Color(backR, backG, backB, backA);
        }
        public Colors(byte foreR, byte foreG, byte foreB, byte foreA, byte backR, byte backG, byte backB, byte backA)
        {
            Foreground = new Color(foreR, foreG, foreB, foreA);
            Background = new Color(backR, backG, backB, backA);
        }

        public Colors Swapped()
        {
            return new Colors(Background, Foreground);
        }

        public Colors WithForeground(Color fore)
        {
            return new Colors(fore, Background);
        }

        public Colors WithBackground(Color back)
        {
            return new Colors(Foreground, back);
        }
    }
}
