using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Application.Ports
{
    public interface ITorrentParser
    {
        TorrentInfo Parse(string filePath);
    }
}
