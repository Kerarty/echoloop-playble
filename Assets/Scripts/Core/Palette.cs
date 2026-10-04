using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Single source of truth for the game's look. Everything is built from flat,
    /// saturated colour blocks on a dark backdrop so the shapes read instantly
    /// against the noisy time-loop motion.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Backdrop = Hex(0x0B0E14);
        public static readonly Color Structure = Hex(0x2A3345);
        public static readonly Color StructureLight = Hex(0x3B4759);
        public static readonly Color StructureDark = Hex(0x1B2230);

        public static readonly Color Accent = Hex(0x33E0C8);      // echoes / system
        public static readonly Color AccentWarm = Hex(0xFFC24B);   // shards
        public static readonly Color Danger = Hex(0xFF4D6D);      // hazards
        public static readonly Color Power = Hex(0x7C6CFF);       // gates
        public static readonly Color Player = Hex(0xF2F5FF);

        public static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);

        public static Color Dim(Color c, float amount) =>
            Color.Lerp(c, Backdrop, Mathf.Clamp01(amount));
    }
}