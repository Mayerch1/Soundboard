﻿using System;
using System.Collections.Generic;
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

namespace DiscordBot.UI.Tutorial
{
    /// <summary>
    /// Interaction logic for Slide_Bot_0.xaml
    /// </summary>
    public partial class Slide_Bot_0 : UserControl
    {
        public Slide_Bot_0()
        {
            InitializeComponent();
        }
        private void btn_OpenPortal_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discordapp.com/login?redirect_to=%2Fdevelopers%2Fapplications%2Fme");
        }
    }
}
