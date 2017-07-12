using System.Windows;
using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    /// <summary>
    /// Interaction logic for ConnectionPerformanceTestControl.xaml
    /// </summary>
    public partial class ConnectionPerformanceTestControl : UserControl
    {

        public static readonly DependencyProperty BytesPerMessageProperty = DependencyProperty.Register(
            "BytesPerMessage", typeof(string), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(string)));

        public string BytesPerMessage
        {
            get { return (string) GetValue(BytesPerMessageProperty); }
            set { SetValue(BytesPerMessageProperty, value); }
        }

        public int ConfigBytesPerMessage => int.Parse(BytesPerMessage);

        public static readonly DependencyProperty MessagePeriodProperty = DependencyProperty.Register(
            "MessagePeriod", typeof(string), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(string)));

        public string MessagePeriod
        {
            get { return (string) GetValue(MessagePeriodProperty); }
            set { SetValue(MessagePeriodProperty, value); }
        }

        public int ConfigMessagePeriod => int.Parse(MessagePeriod);

        public ConnectionPerformanceTestControl()
        {
            InitializeComponent();
            DataContext = this;

            BytesPerMessage = "16381";
            MessagePeriod = "1000";


        }
    }
}
