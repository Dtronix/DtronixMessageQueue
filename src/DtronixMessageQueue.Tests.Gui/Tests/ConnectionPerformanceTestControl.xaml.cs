using System;
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

namespace DtronixMessageQueue.Tests.Gui.Tests
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
