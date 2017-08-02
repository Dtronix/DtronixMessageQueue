using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests.MaxThroughput
{
    /// <summary>
    /// Interaction logic for ConnectionPerformanceTestControl.xaml
    /// </summary>
    public partial class MaxThroughputPerformanceTestControl : UserControl
    {
        private readonly MaxThroughputPerformanceTest _test;


        public static readonly DependencyProperty ConfigClientsProperty = DependencyProperty.Register(
            "ConfigClients", typeof(int), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigClients
        {
            get { return (int) GetValue(ConfigClientsProperty); }
            set { SetValue(ConfigClientsProperty, value); }
        }

        public static readonly DependencyProperty ConfigFramesProperty = DependencyProperty.Register(
            "ConfigFrames", typeof(int), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigFrames {
            get { return (int) GetValue(ConfigFramesProperty); }
            set { SetValue(ConfigFramesProperty, value); }
        }


        public static readonly DependencyProperty ConfigFrameSizeProperty = DependencyProperty.Register(
            "ConfigFrameSize", typeof(int), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigFrameSize {
            get { return (int) GetValue(ConfigFrameSizeProperty); }
            set { SetValue(ConfigFrameSizeProperty, value); }
        }

        public static readonly DependencyProperty TotalBytesProperty = DependencyProperty.Register(
            "TotalBytes", typeof(int), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(int)));

        public int TotalBytes {
            get { return (int) GetValue(TotalBytesProperty); }
            set { SetValue(TotalBytesProperty, value); }
        }

        public static readonly DependencyProperty TotalConnectionsProperty = DependencyProperty.Register(
            "TotalConnections", typeof(int), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(int)));

        private Timer _updateTimer;

        public int TotalConnections {
            get { return (int) GetValue(TotalConnectionsProperty); }
            set { SetValue(TotalConnectionsProperty, value); }
        }

        public MaxThroughputPerformanceTestControl(MaxThroughputPerformanceTest test)
        {
            _test = test;
            InitializeComponent();
            DataContext = this;
            ConfigClients = 1;
            ConfigFrameSize = 16000;
            ConfigFrames = 10;

            _updateTimer = new Timer(Update);

            _updateTimer.Change(500, 500);
        }

        public void Update(object state)
        {
            Dispatcher.Invoke(() =>
            {
                TotalConnections = _test.TotalConnections;
            });
        }

        private void ConfigChanged(object sender, TextChangedEventArgs e)
        {

            TotalBytes = ConfigFrames * ConfigFrameSize;
        }
    }
}
