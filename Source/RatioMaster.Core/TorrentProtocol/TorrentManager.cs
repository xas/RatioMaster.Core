﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TorrentManager
    {
        public TorrentInfo Info { get; private set; }

        public TorrentManager()
        {
            Info = new TorrentInfo();
        }
    }
}