using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace MoreLocales.Core
{
    /// <summary>
    /// The new added cultures. Enums can be freely cast into other enums without any errors. The enum underneath will keep the value.
    /// </summary>
    public enum CultureNamePlus
    {
        Japanese = 10,
        Korean = 11,
        TraditionalChinese = 12,
        Turkish = 13,
        Thai = 14,
        Ukrainian = 15,
        LatinAmericanSpanish = 16,
        Czech = 17,
        Hungarian = 18,
        PortugalPortuguese = 19,
        Swedish = 20,
        Dutch = 21,
        Danish = 22,
        Vietnamese = 23, // omg is this a mirrorman reference
        Finnish = 24,
        Romanian = 25,
        Indonesian = 26,
        Unknown = 9999,
    }
}
