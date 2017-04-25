using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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
using System.Windows.Threading;

namespace TouchenceSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private DispatcherTimer timerUpdateTouchSensorData { get; set; }
        private TouchSensorManager tsm { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            string parameterFilePath = ".\\Settings.xml";
            try
            {
                System.IO.FileStream fs = new System.IO.FileStream(parameterFilePath, System.IO.FileMode.Open, FileAccess.Read);
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(TouchSensorManager));
                tsm = (TouchSensorManager)serializer.Deserialize(fs);
                fs.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,
                "Error: check settings.xml",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

                Environment.Exit(1);
            }

            
            tsm.InitializeSensorReaders();

            string portInfo = "COM Port: ";
            foreach (var tmpTSR in tsm.sensorReaders.Select((v, i) => new { Value = v, Index = i }))
            {
                portInfo += tmpTSR.Value.COMPort + ", ";
            }
            label_ComPort.Content = portInfo;
            label_ServerPort.Content = "Server Port: " + tsm.serverPort.ToString();

                this.Loaded += new RoutedEventHandler( MainWindow_Loaded );
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timerUpdateTouchSensorData = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            timerUpdateTouchSensorData.Interval = TimeSpan.FromMilliseconds(20);
            timerUpdateTouchSensorData.Tick += new EventHandler(DispatcherTimer_Tick);
            timerUpdateTouchSensorData.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {

            string touchSensorDataString = "";
            //TODO: This process sometime causes exception
            try {

                foreach (var tmpTSR in tsm.sensorReaders.Select((v, i) => new { Value = v, Index = i }))
                {
                    foreach (var tmpTS in tmpTSR.Value.sensors.Select((v, i) => new { Value = v, Index = i }))
                    {
                        touchSensorDataString += "ID:" + tmpTS.Value.ID.ToString() + "\n";
                        foreach (var tmpTSH in tmpTS.Value.heightChanged.Select((v, i) => new { Value = v, Index = i }))
                        {
                            touchSensorDataString += (tmpTSH.Index + 1).ToString("D2") + ": " + tmpTSH.Value.ToString("F2") + ", ";
                            
                            if ((tmpTSH.Index + 1) % 8 == 0)
                            {
                                touchSensorDataString += "\n";
                            }
                        }
                        touchSensorDataString += "\n";
                    }
                }
                textBoxTouchSensorData.Text = touchSensorDataString;

            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tsm.Dispose();
        }

        private void buttonSetBackGroundData_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tmpTSR in tsm.sensorReaders.Select((v, i) => new { Value = v, Index = i }))
            {
                foreach (var tmpTS in tmpTSR.Value.sensors.Select((v, i) => new { Value = v, Index = i }))
                {
                    tmpTS.Value.SetBackGroundData();
                }
            }

        }
    }
}
