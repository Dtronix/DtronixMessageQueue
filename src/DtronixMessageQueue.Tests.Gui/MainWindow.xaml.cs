using System;
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
using DtronixMessageQueue.Tests.Gui.Tests.Connection;
using DtronixMessageQueue.Tests.Gui.Tests.MaxThroughput;

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

        public static readonly DependencyProperty IsTestRunningProperty = DependencyProperty.Register(
            "IsTestRunning", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public bool IsTestRunning
        {
            get { return (bool) GetValue(IsTestRunningProperty); }
            set { SetValue(IsTestRunningProperty, value); }
        }

        public static readonly DependencyProperty CanModifySettingsProperty = DependencyProperty.Register(
            "CanModifySettings", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public bool CanModifySettings
        {
            get { return (bool) GetValue(CanModifySettingsProperty); }
            set { SetValue(CanModifySettingsProperty, value); }
        }



        private Process _currentProcess;
        private Timer _processMemoryTimer;
        private Stopwatch _totalTransferStopwatch;
        private long _lastTotalSent = 0;
        private long _lastTotalReceived = 0;

        private string[] _startupArgs;

        private TestController _testController;

        public MainWindow(string[] args)
        {
            InitializeComponent();

            _startupArgs = args;

            _currentProcess = Process.GetCurrentProcess();

            _testController = new TestController(this);

            PerformanceTests = new ObservableCollection<PerformanceTest>()
            {
                new ConnectionPerformanceTest(_testController),
                new MaxThroughputPerformanceTest(_testController),
            };

            DataContext = this;

            CanModifySettings = true;
            IsTestRunning = false;


            SelectedPerformanceTest = PerformanceTests[0];

            _processMemoryTimer = new Timer(MemoryTimer);
            _processMemoryTimer.Change(100, 1000);
            _totalTransferStopwatch = Stopwatch.StartNew();

            IpAddress = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList.First(
                    f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .ToString();



        }


        private void MemoryTimer(object state)
        {
            var totalReceived = ConnectionPerformanceTestSession.TotalReceieved + MaxThroughputPerformanceTestSession.TotalReceieved;
            var totalSent = ConnectionPerformanceTestSession.TotalSent + MaxThroughputPerformanceTestSession.TotalSent;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    using (Process currentProcess = Process.GetCurrentProcess())
                        MemoryUsage = FormatSize(currentProcess.WorkingSet64);

                    TotalTransferred =
                        FormatSize(totalReceived + totalSent);

                    TransferDown =
                        FormatSize((double)(totalReceived - _lastTotalReceived) /
                                   _totalTransferStopwatch.ElapsedMilliseconds * 1000) + "ps";

                    TransferUp =
                        FormatSize((double)(totalSent - _lastTotalSent) /
                                   _totalTransferStopwatch.ElapsedMilliseconds * 1000) + "ps";

                    _lastTotalReceived = totalReceived;
                    _lastTotalSent = totalSent;

                    _totalTransferStopwatch.Restart();
                });
            }
            catch (Exception e)
            {
                // ignore
            }
            

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
            _testController.StopTest();
            IsTestRunning = false;
            CanModifySettings = !IsTestRunning;
        }


        private void StartAsServer(object sender, RoutedEventArgs e)
        {
            _testController.StartServer();
            IsTestRunning = true;
            CanModifySettings = !IsTestRunning;
        }

        public void StartAsClient(object sender, RoutedEventArgs e)
        {
            _testController.StartClient(IpAddress);
            IsTestRunning = true;
            CanModifySettings = !IsTestRunning;
        }

        private void NewClient(object sender, RoutedEventArgs e)
        {
            Process.Start("DtronixMessageQueue.Tests.Gui.exe", "client");
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _testController.StopTest();
            _testController.CloseConnectedClients();
        }

        private void TestChanged(object sender, SelectionChangedEventArgs e)
        {
            var control = SelectedPerformanceTest.Control;

            if (control == null)
                return;

            ConfigurationContainer.Content = control;

            _testController.SetTest(SelectedPerformanceTest);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {

            if (_startupArgs.Length == 0)
            {
                CurrentMode = "Setup";
                IsServer = true;
            }
            else if (_startupArgs[0] == "client")
            {
                CurrentMode = "ControllClient";
                CanModifySettings = false;
                StartAsClient(this, null);
                IsServer = false;
            }
        }

        private void Pause(object sender, RoutedEventArgs e)
        {
            _testController.PauseTest();
        }
    }
}
