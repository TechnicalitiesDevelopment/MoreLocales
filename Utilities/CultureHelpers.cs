using MoreLocales.Core;
using System;
using Terraria.Localization;

namespace MoreLocales.Utilities
{
    public static class CultureHelpers
    {
        public static bool IsCustom(this GameCulture culture) => ExtraLocalesSupport.extraCultures.ContainsValue(culture);
        public static string LangCode(this CultureNamePlus culture)
        {
            return culture switch
            {
                CultureNamePlus.Japanese => "ja-JP",
                CultureNamePlus.Korean => "ko-KR",
                CultureNamePlus.TraditionalChinese => "zh-Hant",
                CultureNamePlus.Turkish => "tr-TR",
                CultureNamePlus.Thai => "th-TH",
                CultureNamePlus.Ukrainian => "uk-UA",
                CultureNamePlus.LatinAmericanSpanish => "es-LA",
                CultureNamePlus.Czech => "cs-CZ",
                CultureNamePlus.Hungarian => "hu-HU",
                CultureNamePlus.PortugalPortuguese => "pt-PT",
                CultureNamePlus.Swedish => "sv-SE",
                CultureNamePlus.Dutch => "nl-NL",
                CultureNamePlus.Danish => "da-DK",
                CultureNamePlus.Vietnamese => "vi-VN",
                CultureNamePlus.Finnish => "fi-FI",
                CultureNamePlus.Romanian => "ro-RO",
                CultureNamePlus.Indonesian => "id-ID",
                _ => null
            };
        }
        public static bool IsValid(this CultureNamePlus culture) => culture != CultureNamePlus.Unknown && Enum.IsDefined(culture);
    }
}
