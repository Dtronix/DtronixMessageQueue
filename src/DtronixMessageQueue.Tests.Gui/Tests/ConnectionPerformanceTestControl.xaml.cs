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

        public static readonly DependencyProperty MessagePeroidProperty = DependencyProperty.Register(
            "MessagePeroid", typeof(string), typeof(ConnectionPerformanceTestControl), new PropertyMetadata(default(string)));

        public string MessagePeroid
        {
            get { return (string) GetValue(MessagePeroidProperty); }
            set { SetValue(MessagePeroidProperty, value); }
        }

        public int ConfigMessagePeriod => int.Parse(MessagePeroid);

        public ConnectionPerformanceTestControl()
        {
            InitializeComponent();
            DataContext = this;

            BytesPerMessage = "16381";
            MessagePeroid = "1000";


        }
    }
}
