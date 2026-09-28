using System;

namespace Earshot.Core.Model
{
    /// <summary>Earshot's own sound categories. The numeric value is the priority: higher keeps its slot.</summary>
    public enum Category
    {
        Self = 0,
        Ambient = 1,
        World = 2,
        Wildlife = 3,
        Enemy = 4,
        Raid = 5,
        Boss = 6
    }

    /// <summary>Mirror of vanilla's ClosedCaptions.CaptionType, so the model needs no game types.</summary>
    public enum VanillaType
    {
        Default = 0,
        Wildlife = 1,
        Enemy = 2,
        Boss = 3
    }

    public static class Categories
    {
        public static bool IsThreat(Category c)
        {
            return c == Category.Enemy || c == Category.Raid || c == Category.Boss;
        }

        /// <summary>
        /// Vanilla's "Wildlife" also covers enemy idles ("Wildlife, Enemy Idles" in the inspector),
        /// so a Greydwarf growl lands in Wildlife. That is intended: idles rank and throttle as chatter.
        /// </summary>
        public static Category FromVanilla(VanillaType t)
        {
            switch (t)
            {
                case VanillaType.Boss: return Category.Boss;
                case VanillaType.Enemy: return Category.Enemy;
                case VanillaType.Wildlife: return Category.Wildlife;
                default: return Category.World;
            }
        }

        public static bool TryParse(string text, out Category c)
        {
            int ignored;
            if (string.IsNullOrEmpty(text) || int.TryParse(text, out ignored))
            {
                c = Category.Self;
                return false;
            }
            return Enum.TryParse(text, true, out c) && Enum.IsDefined(typeof(Category), c);
        }
    }
}
