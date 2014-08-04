using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToXSharp
{
    public static class Logger
    {
        public static ILog Log { get; set; }

        internal static void Warn(string message)
        {
            if (Log != null)
                Log.Warn(message);
        }

        internal static void WarnFormat(string format, params object[] args)
        {
            if (Log != null)
                Log.Warn(string.Format(format, args));
        }

        internal static void InfoFormat(string format, params object[] args)
        {
            if (Log != null)
                Log.Info(string.Format(format, args));
        }

        internal static void Error(string message, Exception ex)
        {
            if (Log != null)
                Log.Error(message, ex);
        }

        internal static void Debug(string message)
        {
            if (Log != null)
                Log.Debug(message);
        }

        internal static void DebugFormat(string format, params object[] args)
        {
            if (Log != null)
                Log.Debug(string.Format(format, args));
        }
    }
}
