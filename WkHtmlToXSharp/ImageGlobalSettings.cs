using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToXSharp
{
    public class ImageGlobalSettings
    {
        //public CropSettings Crop { get { return _crop; } } Does not work. 
        public LoadGlobalSettings LoadGlobal { get; private set; }
        public LoadSettings LoadPage { get; private set; }
        //public WebSettings Web { get { return _webSettings; } } Does not work.

        /// <summary>
        /// True to ignore error messages, otherwise false.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// True when outputing PNG or SVG, will make the white background transparent, otherwise false.
        /// </summary>
        public bool Transparent { get; set; }

        /// <summary>
        /// True to use gaphics, otherwise false.
        /// </summary>
        public bool UseGraphics { get; set; }

        /// <summary>
        /// The URL or path of the input file, if "-" stdin is used. E.g. "http://google.com"
        /// </summary>
        public string In { get; set; }

        /// <summary>
        /// The path of the output file, if "-" stdout is used, if empty the content is stored to a internalBuffer.
        /// </summary>
        public string Out { get; set; }

        /// <summary>
        /// The output format to use, must be either "", "jpg", "png", "bmp" or "svg".
        /// </summary>
        public string Fmt { get; set; }

        /// <summary>
        /// The height of the screen used to render is pixels, e.g "1024".
        /// </summary>
        public int ScreenHeight { get; set; }

        /// <summary>
        /// The width of the screen used to render is pixels, e.g "1024".
        /// </summary>
        public int ScreenWidth { get; set; }

        /// <summary>
        /// The compression factor to use when outputting a JPEG image. E.g. "94".
        /// http://en.wikipedia.org/wiki/JPEG
        /// </summary>
        public int Quality { get; set; }

        ///public bool SmartWidth { get; set; } Does not work.

        public ImageGlobalSettings()
        {
            LoadGlobal = new LoadGlobalSettings();
            LoadPage = new LoadSettings();

            In = string.Empty;
            Out = string.Empty;
            Fmt = string.Empty;

            Quality = 94;

            ScreenHeight = 0;
            ScreenWidth = 1024;
        }
    }
}
