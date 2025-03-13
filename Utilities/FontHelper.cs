using MoreLocales.Core;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace MoreLocales.Utilities
{
    /// <summary>
    /// For CombatText, 0 is regular combat text (small), and 1 is crit combat text (larger)
    /// </summary>
    public readonly record struct GameFonts(Asset<DynamicSpriteFont> ItemStack, Asset<DynamicSpriteFont> MouseText, Asset<DynamicSpriteFont> DeathText, Asset<DynamicSpriteFont>[] CombatText);
    public class FontHelper
    {
        private static GameFonts defaultFonts;
        private static GameFonts japaneseFonts;
        private static GameFonts koreanFonts;
        private static bool forcedFont = false;
        static FontHelper()
        {
            if (Main.dedServ)
                return;
            defaultFonts = new
            (
                FontAssets.ItemStack, FontAssets.MouseText, FontAssets.DeathText, FontAssets.CombatText
            );
        }
        public static void ResetFont(bool bypassForced = false)
        {
            if (Main.dedServ)
                return;
            if (forcedFont && !bypassForced)
                return;
            SwitchFontInner(defaultFonts);
        }
        public static void SwitchFont(LocalizedFont font, bool setAsForcedFont = false)
        {
            if (Main.dedServ)
                return;
            if (font == LocalizedFont.None)
            {
                forcedFont = false;
                return;
            }
            if (forcedFont)
                return;

            GameFonts target = font switch
            {
                LocalizedFont.Japanese => japaneseFonts,
                LocalizedFont.Korean => koreanFonts,
                _ => defaultFonts
            };
            SwitchFontInner(target);

            forcedFont = setAsForcedFont;
        }
        private static void SwitchFontInner(GameFonts target)
        {
            FontAssets.ItemStack = target.ItemStack;
            FontAssets.MouseText = target.MouseText;
            FontAssets.DeathText = target.DeathText;
            FontAssets.CombatText = target.CombatText;
        }
    }
}
