using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToXSharp
{
    public class LoadGlobalSettings
    {
        public string CookieJar { get; set; }

        public LoadGlobalSettings()
        {
            CookieJar = string.Empty;
        }
    }
}
