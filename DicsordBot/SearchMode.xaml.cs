﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DicsordBot
{
#pragma warning disable CS1591

    /// <summary>
    /// Interaction logic for SearchMode.xaml
    /// </summary>
    public partial class SearchMode : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<Data.FileData> filteredFiles;
        public ObservableCollection<Data.FileData> FilteredFiles { get { return filteredFiles; } set { filteredFiles = value; OnPropertyChanged("FilteredFiles"); } }
        public ObservableCollection<Data.Playlist> Playlists { get { return Handle.Data.Playlists; } set { Handle.Data.Playlists = value; OnPropertyChanged("Playlists"); } }

        public delegate void ListItemPlayHandler(uint tag, bool isPriority);

        public ListItemPlayHandler ListItemPlay;

        public SearchMode()
        {
            //make deep copy
            FilteredFiles = new ObservableCollection<Data.FileData>(Handle.Data.Files);

            InitializeComponent();
            this.DataContext = this;
        }

        private bool checkContainLowerCase(Data.FileData file, string filter)
        {
            string filterLow = filter.ToLower();

            if (file.Name.ToLower().Contains(filterLow) || file.Author.ToLower().Contains(filterLow)
                || file.Album.ToLower().Contains(filterLow) || file.Genre.ToLower().Contains(filterLow))
            {
                return true;
            }
            else
                return false;
        }

        private void filterListBox(string filter)
        {
            //clear list and apply filter

            if (!string.IsNullOrEmpty(filter))
            {
                FilteredFiles.Clear();

                foreach (var file in Handle.Data.Files)
                {
                    //add all files matching
                    if (checkContainLowerCase(file, filter))
                        FilteredFiles.Add(file);
                }
            }
            else
            {
                //reset filter if empty
                //make deep copy
                FilteredFiles = new ObservableCollection<Data.FileData>(Handle.Data.Files);
            }
        }

        #region event

        private void box_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterListBox(((TextBox)sender).Text);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion event

        private void stack_list_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            uint tag = (uint)((FrameworkElement)sender).Tag;

            ListItemPlay(tag, true);
        }

        private void menu_openContext_Click(object sender, RoutedEventArgs e)
        {
            //that's ugly, but it gets the 'grandParent' to open the context
            var listElement = sender as FrameworkElement;
            if (listElement != null)
            {
                var parent = listElement.Parent as FrameworkElement;
                if (parent != null)
                {
                    var grandParent = parent.Parent as FrameworkElement;
                    if (grandParent != null)
                        grandParent.ContextMenu.IsOpen = true;
                }
            }
        }

        private void menu_addToQueue_Clicked(object sender, RoutedEventArgs e)
        {
            uint tag = (uint)((FrameworkElement)sender).Tag;
            ListItemPlay(tag, false);
        }

        private void menu_createAndAddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var location = this.PointToScreen(new Point(0, 0));
            var dialog = new PlaylistAddDialog(location.X, location.Y, this.ActualWidth, this.ActualHeight);

            //create new playlist from dialog result
            var result = dialog.ShowDialog();
            if (result == true)
            {
                Handle.Data.Playlists.Add(new Data.Playlist(dialog.PlaylistName));
            }

            uint tag = (uint)((FrameworkElement)sender).Tag;

            //search for title with tag
            foreach (var title in Handle.Data.Files)
            {
                if (title.Id == tag)
                    Handle.Data.Playlists[Handle.Data.Playlists.Count - 1].Tracks.Add(title);
            }
        }

        private void context_AddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            //This should crash
            //uint tag = (uint)((FrameworkElement)sender).Tag;

            //ListItemPlay(tag, false);
        }
    }

#pragma warning restore CS1591
}