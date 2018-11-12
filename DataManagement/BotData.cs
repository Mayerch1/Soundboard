﻿using System.IO;

namespace DataManagement
{
#pragma warning disable CS1591

    /// <summary>
    /// Contains data for streaming
    /// </summary>
    public class BotData
    {
        public BotData()
        {
        }

        public BotData(string name, string filePath="")
        {
            this.name = name;
            this.filePath = filePath;
        }
     
        public BotData(ButtonData btn)
        {
            name = btn.Name;
            filePath = btn.File;
            isEarrape = btn.IsEarrape;
            isLoop = btn.IsLoop;
            id = btn.ID;
            stream = null;
        }

        public string name = "";
        public string filePath = null;
        public Stream stream = null;
        public bool isEarrape = false;
        public bool isLoop = false;
        public int id = -1;
    }

#pragma warning restore CS1591
}