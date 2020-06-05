using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEO_Calculator.Enums
{
    // [ENUM] SearchEngines
    public enum SearchEngines
    {
        All = 0,
        Bing = 1,
        Google = 2
    }

    // [ENUM] Sorting
    public enum Sorting
    {
        TermAscending = 0,
        TermDescending = 1,
        ResultAscending = 2,
        ResultDescending = 3
    }

    // [ENUM] ResultFormatting
    public enum ResultFormatting
    {
        Txt = 0,
        Html = 1
    }
}