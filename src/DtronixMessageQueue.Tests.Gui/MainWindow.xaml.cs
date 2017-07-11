﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using DtronixMessageQueue.Tests.Gui.Tests;

namespace DtronixMessageQueue.Tests.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static readonly DependencyProperty CurrentModeProperty = DependencyProperty.Register(
            "CurrentMode", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string CurrentMode {
            get { return (string) GetValue(CurrentModeProperty); }
            set { SetValue(CurrentModeProperty, value); }
        }

        public static readonly DependencyProperty IsServerProperty = DependencyProperty.Register(
            "IsServer", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty MemoryUsageProperty = DependencyProperty.Register(
            "MemoryUsage", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string MemoryUsage {
            get { return (string) GetValue(MemoryUsageProperty); }
            set { SetValue(MemoryUsageProperty, value); }
        }

        public bool IsServer
        {
            get { return (bool) GetValue(IsServerProperty); }
            set { SetValue(IsServerProperty, value); }
        }


        public static readonly DependencyProperty PerformanceTestsProperty = DependencyProperty.Register(
            "PerformanceTests", typeof(ObservableCollection<PerformanceTest>), typeof(MainWindow), new PropertyMetadata(default(ObservableCollection<PerformanceTest>)));

        public ObservableCollection<PerformanceTest> PerformanceTests
        {
            get { return (ObservableCollection<PerformanceTest>) GetValue(PerformanceTestsProperty); }
            set { SetValue(PerformanceTestsProperty, value); }
        }

        public static readonly DependencyProperty SelectedPerformanceTestProperty = DependencyProperty.Register(
            "SelectedPerformanceTest", typeof(PerformanceTest), typeof(MainWindow), new PropertyMetadata(default(PerformanceTest)));

        public PerformanceTest SelectedPerformanceTest {
            get { return (PerformanceTest) GetValue(SelectedPerformanceTestProperty); }
            set { SetValue(SelectedPerformanceTestProperty, value); }
        }

        public static readonly DependencyProperty ClientProcessesProperty = DependencyProperty.Register(
            "ClientProcesses", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string ClientProcesses {
            get { return (string) GetValue(ClientProcessesProperty); }
            set { SetValue(ClientProcessesProperty, value); }
        }

        public static readonly DependencyProperty ClientConnectionsProperty = DependencyProperty.Register(
            "ClientConnections", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string ClientConnections {
            get { return (string) GetValue(ClientConnectionsProperty); }
            set { SetValue(ClientConnectionsProperty, value); }
        }

        public static readonly DependencyProperty TotalTransferredProperty = DependencyProperty.Register(
            "TotalTransferred", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string TotalTransferred
        {
            get { return (string) GetValue(TotalTransferredProperty); }
            set { SetValue(TotalTransferredProperty, value); }
        }


        public static readonly DependencyProperty TransferDownProperty = DependencyProperty.Register(
            "TransferDown", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string TransferDown
        {
            get { return (string) GetValue(TransferDownProperty); }
            set { SetValue(TransferDownProperty, value); }
        }

        public static readonly DependencyProperty TransferUpProperty = DependencyProperty.Register(
            "TransferUp", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string TransferUp
        {
            get { return (string) GetValue(TransferUpProperty); }
            set { SetValue(TransferUpProperty, value); }
        }

        public static readonly DependencyProperty IpAddressProperty = DependencyProperty.Register(
            "IpAddress", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string IpAddress
        {
            get { return (string) GetValue(IpAddressProperty); }
            set { SetValue(IpAddressProperty, value); }
        }



        private Process _currentProcess;
        private Timer _processMemoryTimer;
        private Stopwatch _totalTransferStopwatch;
        private long _lastTotalSent = 0;
        private long _lastTotalReceived = 0;

        public MainWindow(string[] args)
        {
            InitializeComponent();

            _currentProcess = Process.GetCurrentProcess();

            PerformanceTests = new ObservableCollection<PerformanceTest>();

            DataContext = this;

            PerformanceTests.Add(new ConnectionPerformanceTest(this));

            SelectedPerformanceTest = PerformanceTests[0];

            if (args.Length == 0)
            {
                CurrentMode = "Setup";
                IsServer = true;
            }
            else if (args[0] == "client")
            {
                CurrentMode = "Client";
                IsServer = false;
            }

            _processMemoryTimer = new Timer(MemoryTimer);
            _processMemoryTimer.Change(100, 1000);
            _totalTransferStopwatch = Stopwatch.StartNew();

            ClientConnections = "100";

            IpAddress = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList.First(
                    f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .ToString();



        }


        private void MemoryTimer(object state)
        {
            Dispatcher.Invoke(() =>
            {
                using (Process currentProcess = Process.GetCurrentProcess())
                    MemoryUsage = FormatSize(currentProcess.WorkingSet64);

                TotalTransferred =
                    FormatSize(ConnectionPerformanceTestSession.TotalReceieved + ConnectionPerformanceTestSession.TotalSent);

                TransferDown =
                    FormatSize((double) (ConnectionPerformanceTestSession.TotalReceieved - _lastTotalReceived) /
                               _totalTransferStopwatch.ElapsedMilliseconds) + "ps";

                TransferUp =
                    FormatSize((double)(ConnectionPerformanceTestSession.TotalSent - _lastTotalSent) /
                               _totalTransferStopwatch.ElapsedMilliseconds) + "ps";

                _lastTotalReceived = ConnectionPerformanceTestSession.TotalReceieved;
                _lastTotalSent = ConnectionPerformanceTestSession.TotalSent;

                _totalTransferStopwatch.Restart();
            });

        }

        private string FormatSize(double length)
        {
            double size = length;

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

 
            return $"{size:0.0} {sizes[order]}";
     
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            SelectedPerformanceTest.StopTest();
        }


        private void StartAsServer(object sender, RoutedEventArgs e)
        {
            SelectedPerformanceTest.StartServer(int.Parse(ClientConnections));
        }

        private void StartAsClient(object sender, RoutedEventArgs e)
        {
            SelectedPerformanceTest.StartClient(IpAddress);
        }

        private void NewClient(object sender, RoutedEventArgs e)
        {
            Process.Start("DtronixMessageQueue.Tests.Gui.exe", "client");
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SelectedPerformanceTest.StopTest();
        }
    }
}
