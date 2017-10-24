﻿using System.Windows;

namespace DtronixMessageQueue.Tests.Gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            new MainWindow(e.Args).Show();
        }
    }
}
