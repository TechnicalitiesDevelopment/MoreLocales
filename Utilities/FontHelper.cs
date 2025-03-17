using MoreLocales.Core;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreLocales.Utilities
{
    /// <summary>
    /// For CombatText, 0 is regular combat text (small), and 1 is crit combat text (larger)
    /// </summary>
    public readonly record struct GameFonts(Asset<DynamicSpriteFont> ItemStack, Asset<DynamicSpriteFont> MouseText, Asset<DynamicSpriteFont> DeathText, Asset<DynamicSpriteFont>[] CombatText);
    public static class FontHelper
    {
        private static readonly GameFonts defaultFonts;
        private static GameFonts japaneseFonts;
        private static GameFonts koreanFonts;
        private static bool forcedFont = false;
        public static bool usingLocalizedFont = false;
        public static LocalizedFont currentLocalizedFont = LocalizedFont.None;
        static FontHelper()
        {
            if (Main.dedServ)
                return;

            defaultFonts = new
            (
                FontAssets.ItemStack, FontAssets.MouseText, FontAssets.DeathText, FontAssets.CombatText
            );
        }
        public static void InitLocalizedFonts()
        {
            if (Main.dedServ)
                return;

            static Asset<DynamicSpriteFont> GetFont(string name) => ModContent.Request<DynamicSpriteFont>($"MoreLocales/Assets/Fonts/{name}", AssetRequestMode.ImmediateLoad);

            japaneseFonts = new
            (
                GetFont("ItemStack-JP"), GetFont("MouseText-JP"), GetFont("DeathText-JP"), [GetFont("CombatText-JP"), GetFont("CritText-JP")]
            );
            koreanFonts = new
            (
                GetFont("ItemStack-KR"), GetFont("MouseText-KR"), GetFont("DeathText-KR"), [GetFont("CombatText-KR"), GetFont("CritText-KR")]
            );
        }
        public static void ResetFont(bool bypassForced = false)
        {
            if (Main.dedServ)
                return;
            if (forcedFont && !bypassForced)
                return;

            usingLocalizedFont = false;

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

            if (!usingLocalizedFont && font == LocalizedFont.Default)
                return;

            GameFonts target = font switch
            {
                LocalizedFont.Japanese => japaneseFonts,
                LocalizedFont.Korean => koreanFonts,
                _ => defaultFonts
            };

            usingLocalizedFont = font > LocalizedFont.Default;

            if (usingLocalizedFont)
                currentLocalizedFont = font;

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
        public static bool? LocalizedFontAvailable(this CultureNamePlus culture)
        {
            LocalizedFont font = culture.GetLocalizedFont();
            if (font == LocalizedFont.None)
                return null;
            return font < LocalizedFont.Thai;
        }
        public static bool IsUsingAppropriateFont(GameCulture culture)
        {
            if (culture.TryGetLocalizedFont(out LocalizedFont font))
                return IsUsingFont(font);
            return false;
        }
        public static bool IsUsingFont(LocalizedFont font)
        {
            if (currentLocalizedFont == font)
                return usingLocalizedFont;
            return false;
        }
        public static LocalizedFont GetLocalizedFont(this CultureNamePlus culture)
        {
            return culture switch
            {
                CultureNamePlus.Japanese => LocalizedFont.Japanese,
                CultureNamePlus.Korean => LocalizedFont.Korean,
                CultureNamePlus.Thai => LocalizedFont.Thai,
                CultureNamePlus.Vietnamese => LocalizedFont.Vietnamese,
                _ => LocalizedFont.None
            };
        }
    }
}
