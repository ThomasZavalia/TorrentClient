using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Ports
{
    public interface IProgressNotifier
    {
        Task ReportProgressAsync(int completed, int total, double percentage);
        Task ReportLogAsync(string message); 
    }
}
