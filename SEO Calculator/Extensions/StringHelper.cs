using System;
using System.Drawing;
using System.Linq;
using Microsoft.VisualBasic;

namespace SEO_Calculator.Extensions
{
    public static class StringHelper
    {
        // RGB To HTML
        public static string RgbToHtml(short r, short g, short b)
        {
            return ColorTranslator.ToHtml(Color.FromArgb(r, g, b));
        }

        // Format Number
        public static string FormatNumber(object number)
        {
            if (number is short || number is int ||
                number is long)
                return Strings.FormatNumber(number, IncludeLeadingDigit: TriState.False);
            return Strings.FormatNumber(number, IncludeLeadingDigit: TriState.False);
        }

        // Number Abbreviation
        public static string NumberAbbreviation(string quantity, bool rounded = true)
        {
            var abbreviation = string.Empty;

            switch (quantity.Count(character => character == Convert.ToChar(".")))
            {
                case 0:
                    return quantity;

                case 1:
                    abbreviation = "kilos";
                    break;

                case 2:
                    abbreviation = "Millions";
                    break;

                case 3:
                    abbreviation = "Billions";
                    break;

                case 4:
                    abbreviation = "Trillions";
                    break;

                case 5:
                    abbreviation = "Quadrillions";
                    break;

                case 6:
                    abbreviation = "Quintuillions";
                    break;

                case 7:
                    abbreviation = "Sextillions";
                    break;

                case 8:
                    abbreviation = "Septillions";
                    break;

                default:
                    return quantity;
            }

            return rounded
                ? $"{Strings.StrReverse(Strings.StrReverse(quantity).Substring(Strings.StrReverse(quantity).LastIndexOf(".", StringComparison.Ordinal) + 1))} {abbreviation}"
                : $"{Strings.StrReverse(Strings.StrReverse(quantity).Substring(Strings.StrReverse(quantity).LastIndexOf(".", StringComparison.Ordinal) - 1))} {abbreviation}";
        }
    }
}