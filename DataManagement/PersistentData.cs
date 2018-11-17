﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace DataManagement
{
    /// <summary>
    /// stores all data which are permanentaly preserved
    /// </summary>
    [Serializable()]
    public class PersistentData : INotifyPropertyChanged
    {
        #region consts


        /// <summary>
        /// complete url to git Repository
        /// </summary>
        public const string gitCompleteUrl = "https://github.com/Mayerch1/TheDiscordSoundboard/";


        /// <summary>
        /// this is the Name of the Github repository
        /// </summary>
        public const string gitRepo = "TheDiscordSoundboard";
        /// <summary>
        /// this is the account name of the repository owner
        /// </summary>
        public const string gitAuthor = "mayerch1";

  

        /// <summary>
        /// default folder to create, e.g. in Appdata
        /// </summary>
        public const string defaultFolderName = "TheDicsordSoundboard (TDS)";

        /// <summary>
        /// folder to cache images in
        /// </summary>
        public const string imageCacheFolder = "Images";

        /// <summary>
        /// folder to cache videos in
        /// </summary>
        public const string videoCacheFolder = "Videos";

        /// <summary>
        /// version of this build, refers to the github release number
        /// </summary>
        public const string version = "2.2.0";

     
        

        #endregion consts

        #region persistend fields

        private ObservableCollection<ButtonData> btnList = new ObservableCollection<ButtonData>();
        private ObservableCollection<string> mediaSources = new ObservableCollection<string>();
        private ObservableCollection<FileData> playListIndex = new ObservableCollection<FileData>();
        private ObservableCollection<Hotkey> hotkeyList = new ObservableCollection<Hotkey>();
        private ObservableCollection<string> supportedFormats = new ObservableCollection<string> { "mp3", "wav", "asf", "wma", "wmv", "sami", "smi", "3g2", "3gp", "3pg2", "3pgg", "aac", "adts", "m4a", "m4v", "mov", "mp4" };

        private bool isFirstStart = true;
        private bool isEulaAccepted = false;

        private string settingsPath;
        private int highestButtonToSave = -1;
        private ulong clientId;
        private string clientName;
        private ulong channelId = 0;
        private string clientAvatar;
        private string token = null;
        private int selectedServerIndex = 0;
        private bool alwaysCacheVideo = true;


        private bool isDarkTheme = false;
        private Swatch primarySwatch = null;
        private Swatch secondarySwatch = null;


        private int minVisibleButtons = 35;
        private int maxHistoryLen = 50;
        private int maxVideoHistoryLen = 25;

        private float volume = 0.5f;
        private int volumeCap = 30;

        #endregion persistend fields

        #region persistend properties

        /// <summary>
        /// IsFirstStart property
        /// </summary>
        /// <value>
        /// if true, introduction guides will be loaded
        /// </value>
        public bool IsFirstStart { get => isFirstStart;
            set { isFirstStart = value; OnPropertyChanged("IsFirstStart"); } }


        /// <summary>
        /// IsEulaAccepted
        /// </summary>
        /// <value> if true, the user acknowledged the legal consequences of using stream function</value>
        public bool IsEulaAccepted
        {
            get => isEulaAccepted;
            set
            {
                isEulaAccepted = value;
                OnPropertyChanged("IsEulaAccepted");
            }
        }

        /// <summary>
        ///  SettingsPath property, automatically saved
        /// </summary>
        public string SettingsPath { get => settingsPath;
            set { settingsPath = value;  OnPropertyChanged("SettingsPath"); } }

        /// <summary>
        ///  HihgestButtonToSave property
        /// </summary>
        /// <value>
        /// all buttons above this number are empty
        /// </value>
        public int HighestButtonToSave { get => highestButtonToSave;
            set { highestButtonToSave = value; OnPropertyChanged("HighestButtonToSave"); } }

        /// <summary>
        ///  ClientId property
        /// </summary>
        public ulong ClientId { get => clientId;
            set { clientId = value; OnPropertyChanged("ClientId"); } }

        /// <summary>
        /// ChannelId property
        /// </summary>
        /// <value>
        /// target channel to join, set to 0 to join to owners channel
        /// </value>
        public ulong ChannelId { get => channelId;
            set { channelId = value; OnPropertyChanged("ChannelId"); } }

        /// <summary>
        ///  ClientAvatar property
        /// </summary>
        /// <value>
        /// url to image avatar image
        /// </value>
        public string ClientAvatar { get => clientAvatar;
            set { clientAvatar = value; OnPropertyChanged("ClientAvatar"); } }

        /// <summary>
        /// IsDarkTheme property
        /// </summary>
        public bool IsDarkTheme { get => isDarkTheme;
            set { isDarkTheme = value; new PaletteHelper().SetLightDark(value);  OnPropertyChanged("IsDarkTheme"); } }


        /// <summary>
        /// this is the main color scheme
        /// </summary>
        [XmlIgnore]
        public Swatch PrimarySwatch
        {
            get => primarySwatch;
            set
            {
                if (value != null)
                {
                    primarySwatch = value;
                    new PaletteHelper().ReplacePrimaryColor(value);
                    OnPropertyChanged("PrimarySwatch");
                }
            }
        }

        /// <summary>
        /// this is the secondary color scheme
        /// </summary>
        [XmlIgnore]
        public Swatch SecondarySwatch
        {
            get => secondarySwatch;
            set
            {
                if (value != null)
                {
                    secondarySwatch = value;
                    new PaletteHelper().ReplaceAccentColor(value);
                    OnPropertyChanged("SecondarySwatch");
                }
            }
        }


        /// <summary>
        /// string of primary swatch for saving
        /// </summary>
        public string PrimarySwatchString
        {
            get
            {
                return DateTime.Now.ToString();
                //if (PrimarySwatch != null)
                //{
                //    return PrimarySwatch.Name;
                //}

                //return null;
            }
            

        }
    
        /// <summary>
        /// string of primary swatch for saving
        /// </summary>
        public string SecondarySwatchString
        {
            get
            {
                return "test123";
                //if (SecondarySwatch != null)
                //{
                //    return SecondarySwatch.Name;
                //}

                //return null;
            }          
        }

        //raise ClientNameChanged to check for client names, if old Token was not able to do so
        /// <summary>
        /// Token property
        /// </summary>
        public string Token { get => token;
            set { token = value; OnPropertyChanged("Token"); if (ClientNameChanged != null) ClientNameChanged(value); } }

        /// <summary>
        /// SelectedServerIndex
        /// </summary>
        /// <value>
        /// the index of the server, which channles are displayed in the channel selector
        /// </value>
        public int SelectedServerIndex { get => selectedServerIndex;
            set { selectedServerIndex = value; OnPropertyChanged("SelectedServerIndex"); } }

        /// <summary>
        /// Volume property
        /// </summary>
        /// <value>
        /// stores volume from 0.0 to 1.0
        /// </value>
        public float Volume { get => volume;
            set { volume = value; OnPropertyChanged("Volume"); } }

        /// <summary>
        /// VolumeCap property
        /// </summary>
        /// <value>
        /// limits the volume from 0 to 100 percent
        /// </value>
        public int VolumeCap { get => volumeCap;
            set { volumeCap = value; OnPropertyChanged("VolumeCap"); } }

        /// <summary>
        /// ClientName property
        /// </summary>
        /// <value>
        /// discord username in form of 'Name#1234'
        /// </value>
        public string ClientName { get => clientName;
            set { clientName = value; OnPropertyChanged("ClientName"); if (ClientNameChanged != null) ClientNameChanged(value); } }

      
        /// <summary>
        /// Do not use Videostream, instead cache each video
        /// </summary>
        public bool AlwaysCacheVideo
        {
            get => alwaysCacheVideo;
            set { alwaysCacheVideo = value; OnPropertyChanged("AlwaysCacheVideo"); } }

   
        /// <summary>
        /// minVisibleButtons Property
        /// </summary>
        /// <remarks>
        /// the count of buttons which are shown, even if there are less buttons used
        /// </remarks>
        public int MinVisibleButtons
        {
            get => minVisibleButtons;
            set
            {
                minVisibleButtons = value;
                OnPropertyChanged("MinVisibleButtons");
            }
        }

        /// <summary>
        /// Max entries in file history
        /// </summary>
        public int MaxHistoryLen
        {
            get => maxHistoryLen;
            set
            {
                maxHistoryLen = value;
                OnPropertyChanged("MaxHistoryLen");
            }
        }

        /// <summary>
        /// Max entries in video history
        /// </summary>
        public int MaxVideoHistoryLen
        {
            get => maxVideoHistoryLen;
            set
            {
                maxVideoHistoryLen = value;
                OnPropertyChanged("MaxVideoHistoryLen");
            }
        }


        /// <summary>
        /// MediaSources property
        /// </summary>
        /// <value>
        /// list of all locations to monitor for files
        /// </value>
        public ObservableCollection<string> MediaSources { get => mediaSources;
            set { mediaSources = value; OnPropertyChanged("MediaSources"); } }



        /// <summary>
        /// a list with all supported formats (only file ending, without .)
        /// </summary>
        public ObservableCollection<string> SupportedFormats
        {
            get => supportedFormats;
            set
            {
                supportedFormats = value;
                OnPropertyChanged("SupportedFormats");
            }
        }


        /// <summary>
        ///name and directory of each playlist, used for loading the files
        /// </summary>
        public ObservableCollection<FileData> PlayListIndex { get => playListIndex;
            set { playListIndex = value; OnPropertyChanged("PlaylistFiles"); } }

        /// <summary>
        /// List of all registered hotkeys
        /// </summary>
        public ObservableCollection<Hotkey> HotkeyList { get => hotkeyList;
            set { hotkeyList = value; OnPropertyChanged("HotkeyList"); } }

        /// <summary>
        /// BtnList property
        /// </summary>
        /// <value>
        /// list of all button-data elements
        /// </value>
        public ObservableCollection<ButtonData> BtnList { get => btnList;
            set { btnList = value; OnPropertyChanged("BtnList"); } }

        #endregion persistend properties

        #region events

        /// <summary>
        /// ClientNameHandler delegate
        /// </summary>
        /// <param name="newName">new owner Name</param>
        public delegate void ClientNameHandler(string newName);

        /// <summary>
        /// ClientNameHandler event, if client name has changed
        /// </summary>
        public event ClientNameHandler ClientNameChanged;

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