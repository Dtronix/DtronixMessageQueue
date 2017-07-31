using System.Windows;
using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    /// <summary>
    /// Interaction logic for ConnectionPerformanceTestControl.xaml
    /// </summary>
    public partial class ConnectionPerformanceTestControl : UserControl
    {

        public static readonly DependencyProperty ConfigClientsProperty = DependencyProperty.Register(
            "ConfigClients", typeof(int), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigClients {
            get { return (int) GetValue(ConfigClientsProperty); }
            set { SetValue(ConfigClientsProperty, value); }
        }

        public static readonly DependencyProperty ConfigBytesPerMessageProperty = DependencyProperty.Register(
            "ConfigBytesPerMessage", typeof(int), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigBytesPerMessage
        {
            get { return (int) GetValue(ConfigBytesPerMessageProperty); }
            set { SetValue(ConfigBytesPerMessageProperty, value); }
        }

        public static readonly DependencyProperty ConfigMessagePeriodProperty = DependencyProperty.Register(
            "ConfigMessagePeriod", typeof(int), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(int)));

        public int ConfigMessagePeriod {
            get { return (int) GetValue(ConfigMessagePeriodProperty); }
            set { SetValue(ConfigMessagePeriodProperty, value); }
        }

        public ConnectionPerformanceTestControl()
        {
            InitializeComponent();
            DataContext = this;

            ConfigBytesPerMessage = 16381;
            ConfigClients = 100;
            ConfigMessagePeriod = 1000;
        }
    }
}
