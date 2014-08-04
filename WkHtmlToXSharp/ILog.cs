using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToXSharp
{
    public interface ILog
    {
        void Warn(string message);

        void Info(string message);

        void Error(string message, Exception ex);

        void Debug(string message);
    }
}
