﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicsordBot.Data
{
    /// <summary>
    /// represents a playlist
    /// </summary>
    [Serializable()]
    public class Playlist : INotifyPropertyChanged
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public Playlist()
        {
            id = sId++;
        }

        /// <summary>
        /// constructor setting the name property
        /// </summary>
        /// <param name="_name"></param>
        public Playlist(string _name)
        {
            id = sId++;
            Name = _name;
        }

        #region fileds

        private static uint sId = 0;

        private ObservableCollection<FileData> tracks = new ObservableCollection<FileData>();
        private string name;
        private readonly uint id;

        #endregion fileds

        #region properties

        /// <summary>
        /// Property for tracks of playlist
        /// </summary>
        public ObservableCollection<FileData> Tracks { get { return tracks; } set { tracks = value; OnPropertyChanged("Tracks"); } }

        /// <summary>
        /// Name property
        /// </summary>
        public string Name { get { return name; } set { name = value; OnPropertyChanged("Name"); } }

        /// <summary>
        /// unique id
        /// </summary>
        public uint Id { get { return Id; } }

        #endregion properties

        #region events

        /// <summary>
        /// PropertyChanged Event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// propertychanged method, notifies the actual handler
        /// </summary>
        /// <param name="info"></param>
        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(null, new PropertyChangedEventArgs(info));
            }
        }

        #endregion events
    }
}