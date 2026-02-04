using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Common.Interfaces
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
    }
}
