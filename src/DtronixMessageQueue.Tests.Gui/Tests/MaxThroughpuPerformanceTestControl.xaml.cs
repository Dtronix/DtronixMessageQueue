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
    public partial class MaxThroughputPerformanceTestControl : UserControl
    {




        public static readonly DependencyProperty FramesProperty = DependencyProperty.Register(
            "Frames", typeof(string), typeof(MaxThroughputPerformanceTestControl), new PropertyMetadata(default(string)));

        public string Frames
        {
            get { return (string) GetValue(FramesProperty); }
            set { SetValue(FramesProperty, value); }
        }

        public int ConfigFrames => int.Parse(Frames);


        public static readonly DependencyProperty FrameSizeProperty = DependencyProperty.Register(
            "FrameSize", typeof(string), typeof(MaxThroughputPerformanceTestControl),
            new PropertyMetadata(default(string)));

        public string FrameSize
        {
            get { return (string) GetValue(FrameSizeProperty); }
            set { SetValue(FrameSizeProperty, value); }
        }

        public int ConfigFrameSize => int.Parse(FrameSize);

        public static readonly DependencyProperty TotalBytesProperty = DependencyProperty.Register(
            "TotalBytes", typeof(string), typeof(MaxThroughputPerformanceTestControl),
            new PropertyMetadata(default(string)));

        public string TotalBytes
        {
            get { return (string) GetValue(TotalBytesProperty); }
            set { SetValue(TotalBytesProperty, value); }
        }

        public MaxThroughputPerformanceTestControl()
        {
            InitializeComponent();
            DataContext = this;

            FrameSize = "16000";
            Frames = "10";
        }

        private void ConfigChanged(object sender, TextChangedEventArgs e)
        {

            TotalBytes = (ConfigFrames * ConfigFrameSize).ToString();
        }
    }
}
