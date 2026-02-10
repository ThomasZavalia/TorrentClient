using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Managers
{
    public class ProgressEventArgs : EventArgs
    {
        public int CompletedPieces { get; init; }
        public int TotalPieces { get; init; }
        public double Percentage { get; init; }
    }
}
