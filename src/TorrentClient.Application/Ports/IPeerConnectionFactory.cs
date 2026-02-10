using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Ports
{
    public interface IPeerConnectionFactory
    {
        IPeerConnection Create();
    }
}
