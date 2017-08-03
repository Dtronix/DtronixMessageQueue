using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests.Echo
{
    /// <summary>
    /// Interaction logic for ConnectionPerformanceTestControl.xaml
    /// </summary>
    public partial class EchoPerformanceTestControl : UserControl
    {
        private readonly EchoPerformanceTest _test;


        public static readonly DependencyProperty ConfigClientsProperty = DependencyProperty.Register(
            "ConfigClients", typeof(int), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigClients
        {
            get { return (int) GetValue(ConfigClientsProperty); }
            set { SetValue(ConfigClientsProperty, value); }
        }


        public static readonly DependencyProperty ConfigFrameSizeProperty = DependencyProperty.Register(
            "ConfigFrameSize", typeof(int), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigFrameSize {
            get { return (int) GetValue(ConfigFrameSizeProperty); }
            set { SetValue(ConfigFrameSizeProperty, value); }
        }

        public static readonly DependencyProperty TotalBytesProperty = DependencyProperty.Register(
            "TotalBytes", typeof(int), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(int)));

        public int TotalBytes {
            get { return (int) GetValue(TotalBytesProperty); }
            set { SetValue(TotalBytesProperty, value); }
        }

        public static readonly DependencyProperty TotalConnectionsProperty = DependencyProperty.Register(
            "TotalConnections", typeof(int), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(int)));

        private Timer _updateTimer;

        public int TotalConnections {
            get { return (int) GetValue(TotalConnectionsProperty); }
            set { SetValue(TotalConnectionsProperty, value); }
        }

        public static readonly DependencyProperty EchosPerSecondProperty = DependencyProperty.Register(
            "EchosPerSecond", typeof(int), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(int)));

        public int EchosPerSecond {
            get { return (int) GetValue(EchosPerSecondProperty); }
            set { SetValue(EchosPerSecondProperty, value); }
        }

        public static readonly DependencyProperty AverageResponseTimeProperty = DependencyProperty.Register(
            "AverageResponseTime", typeof(double), typeof(EchoPerformanceTestControl), new PropertyMetadata(default(double)));

        public double AverageResponseTime {
            get { return (double) GetValue(AverageResponseTimeProperty); }
            set { SetValue(AverageResponseTimeProperty, value); }
        }

        private Stopwatch sw = Stopwatch.StartNew();

        public EchoPerformanceTestControl(EchoPerformanceTest test)
        {
            _test = test;
            InitializeComponent();
            DataContext = this;
            ConfigClients = 100;
            ConfigFrameSize = 1024;

            _updateTimer = new Timer(Update);

            _updateTimer.Change(100, 100);

        }

        public void Update(object state)
        {
            Dispatcher.Invoke(() =>
            {
                TotalConnections = _test.TotalConnections;
                var eps = (int)(EchoPerformanceTestSession.MessageCount / (sw.ElapsedMilliseconds / 1000d));
                if (eps == 0)
                    return;

                EchosPerSecond = eps;
                AverageResponseTime = 1d / eps * 1000;
                EchoPerformanceTestSession.MessageCount = 0;
                sw.Restart();
            });
        }

        private void ConfigChanged(object sender, TextChangedEventArgs e)
        {

            
        }
    }
}
