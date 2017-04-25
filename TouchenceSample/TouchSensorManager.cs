using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TouchenceSample
{
    [System.Xml.Serialization.XmlRoot("touchsensormanager")]
    public class TouchSensorManager
    {
        [System.Xml.Serialization.XmlElement("touchsensorreader")]
        public List<TouchSensorReader> sensorReaders;
        [System.Xml.Serialization.XmlElement("serverport")]
        public int serverPort { get; set; }

        public bool runSendDataThread { get; set; }
 
        private int totalSensorNum { get; set; }

        private object lockObjectConnection { get; set; }

        private Thread sendTouchDataThread { get; set; }

        private List<StateObject> activeConnections { get; set; }


        public TouchSensorManager()
        {

        }
        public void InitializeSensorReaders()
        {
            lockObjectConnection = new object();
            activeConnections = new List<StateObject>();
            totalSensorNum = 0;
            foreach (TouchSensorReader tsr in sensorReaders)
            {
                tsr.InitializeSerialPort();
                totalSensorNum += tsr.sensors.Count;
            }
            runSendDataThread = true;
            StartListening();
            this.sendTouchDataThread = new Thread(new ThreadStart(SendTouchSensorDataThread));
            this.sendTouchDataThread.Start();

        }
        public void Dispose()
        {

            runSendDataThread = false;
        }

        
        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public bool sendDataFlag = true;
        }


        public void StartListening()
        {
            IPAddress ipAddress = IPAddress.Parse(GetIPAddress("localhost"));
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            activeConnections.Add(state);
            Console.WriteLine("there is {0} connections", activeConnections.Count);
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            try {
                int bytesRead = handler.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    content = state.sb.ToString();
                    if (content.IndexOf("\n") > -1 || content.IndexOf("<EOF>") > -1)
                    {
                        string getString = content.Replace("\r", "").Replace("\n", "");
                        if (getString == "Start")
                        {
                            Console.WriteLine("Start sending data");
                            state.sendDataFlag = true;
                        }
                        else if (getString == "Stop")
                        {
                            Console.WriteLine("Stop sending data");
                            state.sendDataFlag = false;
                        }
                        state.sb.Length = 0; ;
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                    }
                }
                if (bytesRead == 0)
                {
                    lock(lockObjectConnection)
                    {
                        Console.WriteLine("Disconnected?");
                        activeConnections.Remove(state);
                    }
                }
            }catch(Exception e)
            {
                lock (lockObjectConnection)
                {
                    Console.WriteLine(e.Message);
                    activeConnections.Remove(state);
                }
            }

        }

        private void Send(Socket handler, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendTouchSensorDataThread()
        {
            while (runSendDataThread == true)
            {
                string tmpWriteMessage = "";
                string tmpWriteMessage1 = "";

                tmpWriteMessage = CommonFunctions.GetUnixTimeWithMillisecond(DateTime.Now).ToString() + ",";
                foreach (var tmpTSR in sensorReaders.Select((v, i) => new { Value = v, Index = i }))
                {
                    foreach (var tmpTS in tmpTSR.Value.sensors.Select((v, i) => new { Value = v, Index = i }))
                    {
                        tmpWriteMessage1 += tmpTS.Value.ID.ToString() + "," + tmpTS.Value.GetTouchLevel().ToString();
                    }
                }
                tmpWriteMessage += totalSensorNum.ToString() + "," + tmpWriteMessage1 + "\r\n";

                try
                {
                    lock (lockObjectConnection)
                    {
                        foreach (StateObject each in activeConnections)
                        {
                            if (each.sendDataFlag == true)
                            {
                                Send(each.workSocket, tmpWriteMessage);
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Thread.Sleep(16);
            }
        }


        private string GetIPAddress(string hostname)
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(hostname);

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    //System.Diagnostics.Debug.WriteLine("LocalIPadress: " + ip);
                    return ip.ToString();
                }
            }
            return string.Empty;
        }
        

 
    }

    public class TouchSensorReader
    {
        [System.Xml.Serialization.XmlElement("touchsensors")]
        public List<TouchSensor> sensors;
        [System.Xml.Serialization.XmlElement("comport")]
        public string COMPort { get; set; }
        [System.Xml.Serialization.XmlElement("sensortype")]
        public string sensorType { get; set; }
        [System.Xml.Serialization.XmlElement("frequency")]
        public int frequency { get; set; }
        private System.IO.Ports.SerialPort serialPort = null;

        private object lockObjectSerial = new object();

        private string recievedSerialData = "";

        private double[] sensorData;


        public TouchSensorReader()
        {

        }

        private TouchSensor FindTouchSensor(string tmpID)
        {
            foreach (TouchSensor ts in sensors)
            {
                if (ts.ID == tmpID)
                {
                    return ts;
                }
            }
            return null;
        }

        public void InitializeSerialPort()
        {
            sensorData = new double[16];

            foreach (TouchSensor ts in sensors)
            {
                ts.Initialize();
            }

            serialPort = new SerialPort(COMPort, 115200, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;

            serialPort.NewLine = Environment.NewLine;
            serialPort.Open();
            string tmpCommand = "";
            string tmpReadData = "";
            serialPort.ReadTimeout = 10000;
            serialPort.WriteTimeout = 10000;
            lock (lockObjectSerial)
            {
                tmpCommand = "r\r\n";
                serialPort.Write(tmpCommand);
                tmpReadData = serialPort.ReadLine();
                Console.WriteLine("Received:" + tmpReadData + "\r\n");
                if (tmpReadData != "U:")
                {
                    return;
                }

                tmpCommand = sensors.Count.ToString("00") + "\r\n";
                serialPort.Write(tmpCommand);
                tmpReadData = serialPort.ReadLine();
                Console.WriteLine("Received:" + tmpReadData + "\r\n");
                while (tmpReadData != sensors.Count.ToString("00"))
                {
                    tmpReadData = serialPort.ReadLine();
                    Console.WriteLine("Received:" + tmpReadData + "\r\n");
                }

                foreach(TouchSensor ts in sensors)
                {
                    tmpReadData = serialPort.ReadLine();
                    Console.WriteLine("Received:" + tmpReadData + "\r\n");
                    if (tmpReadData != "I:")
                    {
                        return;
                    }
                    tmpCommand = ts.ID + "\r\n";
                    serialPort.Write(tmpCommand);
                    tmpReadData = serialPort.ReadLine();
                    Console.WriteLine("Received:" + tmpReadData + "\r\n");
                    if (tmpReadData != ts.ID)
                    {
                        return;
                    }
                }

                tmpCommand = "@0201" + frequency.ToString("X2") + "\r\n";
                serialPort.Write(tmpCommand);
                tmpReadData = serialPort.ReadLine();
                Console.WriteLine("Received:" + tmpReadData + "\r\n");
                if (tmpReadData != frequency.ToString())
                {
                    return;
                }
                tmpCommand = "l\r\n";
                serialPort.Write(tmpCommand);
                serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialPortDataReceived);
            }

        }

        private void SerialPortDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (serialPort.IsOpen == false)
            {
                return;
            }

            try
            {
                string data = serialPort.ReadExisting();
                if (data != "")
                {
                    lock (lockObjectSerial)
                    {
                        recievedSerialData += data;
                        string[] delimiter = { "\r\n" };
                        while (recievedSerialData.Contains("\r\n"))
                        {
                            string[] tmpRecieved = recievedSerialData.Split(delimiter, 2, StringSplitOptions.None);
                            if (tmpRecieved[0].Length == 68)
                            {
                                string tmpSensorID = tmpRecieved[0].Substring(0, 2);
                                string tmpBinaryCount = tmpRecieved[0].Substring(2, 2);
                                TouchSensor tmpTS = FindTouchSensor(tmpSensorID);
                                if(tmpTS != null)
                                {
                                    tmpTS.CalculateSensorData(tmpRecieved[0]);
                                }

                            }
                            recievedSerialData = tmpRecieved[1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }
    public class TouchSensor
    {
        [System.Xml.Serialization.XmlElement("id")]
        public string ID { get; set; }
        [System.Xml.Serialization.XmlElement("calibrationfilename")]
        public string calibrationFileName { get; set; }
        [System.Xml.Serialization.XmlElement("backgroundfilename")]
        public string backgroundFileName { get; set; }

        private object lockObjectDataSet = new object();

        private List<double> calibrationSensorData;
        private List<int> backgroundSensorData;
        private String lastRawTouchSensorData;
        public List<double> heightChanged { get; set; }

        public TouchSensor()
        {

        }

        public void SetCalibirationData()
        {

        }

        public void SetBackGroundData()
        {
            if(lastRawTouchSensorData == "")
            {
                return;
            }
            for (int i = 0; i < 16; i++)
            {
                int tmpP = int.Parse(lastRawTouchSensorData.Substring((i + 1) * 4, 4), System.Globalization.NumberStyles.HexNumber);
                backgroundSensorData[i] = tmpP;
            }
            using (StreamWriter sr = new StreamWriter(backgroundFileName, false, Encoding.GetEncoding("Shift_JIS")))
            {
                foreach(int bg in backgroundSensorData)
                {
                    sr.WriteLine(bg.ToString("X4"));
                }
            }
        }
        public int GetTouchLevel()
        {
            int ret = 0;
            try {
                lock(lockObjectDataSet)
                {
                    foreach (double hc in heightChanged)
                    {
                        int tmpRet = 0;
                        if (hc > 10.0)
                        {
                            tmpRet = 3;
                        }
                        else if (hc > 5.0)
                        {
                            tmpRet = 2;
                        }
                        else if (hc > 2.0)
                        {
                            tmpRet = 1;
                        }
                        if (tmpRet > ret)
                        {
                            ret = tmpRet;
                            if (ret == 3)
                            {
                                break;
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return ret;
        }

        public void CalculateSensorData(string dataString)
        {
            lastRawTouchSensorData = dataString;
            lock (lockObjectDataSet)
            {
                for (int i = 0; i < 16; i++)
                {
                    int tmpP = int.Parse(dataString.Substring((i + 1) * 4, 4), System.Globalization.NumberStyles.HexNumber);
                    int tmpPBase = backgroundSensorData[i];
                    heightChanged[i] = calibrationSensorData[i * 4 + 0] * Math.Pow((double)(tmpP - tmpPBase), 4) +
                                        calibrationSensorData[i * 4 + 1] * Math.Pow((double)(tmpP - tmpPBase), 3) +
                                        calibrationSensorData[i * 4 + 2] * Math.Pow((double)(tmpP - tmpPBase), 2) +
                                        calibrationSensorData[i * 4 + 3] * Math.Pow((double)(tmpP - tmpPBase), 1);

                }
            }
        }
        public void Initialize()
        {
            calibrationSensorData = new List<double>();
            backgroundSensorData = new List<int>();

            heightChanged = new List<double>();
            while(heightChanged.Count<16)
            {
                heightChanged.Add(0);
            }

            string tmpCalibrationData = "";
            using (StreamReader sr = new StreamReader(calibrationFileName, Encoding.GetEncoding("Shift_JIS")))
            {
                tmpCalibrationData = sr.ReadToEnd();
            }
            string[] tmpCalibrationData1 = tmpCalibrationData.Replace("\r\n", "\n").Split('\n');
            foreach (string tmpCalibrationData2 in tmpCalibrationData1)
            {
                if(tmpCalibrationData2 == "")
                {
                    continue;
                }
                string[] tmpCalibrationData3 = tmpCalibrationData2.Split('\t');
                foreach (string tmpCalibrationData4 in tmpCalibrationData3)
                {
                    calibrationSensorData.Add(double.Parse(tmpCalibrationData4));

                }
            }
            string tmpBackgroundData = "";
            using (StreamReader sr = new StreamReader(backgroundFileName, Encoding.GetEncoding("Shift_JIS")))
            {
                tmpBackgroundData = sr.ReadToEnd();
            }
            string[] tmpBackgroundData1 = tmpBackgroundData.Replace("\r\n", "\n").Split('\n');
            foreach (string tmpBackgroundData2 in tmpBackgroundData1)
            {
                if (tmpBackgroundData2 == "")
                {
                    continue;
                }
                backgroundSensorData.Add(int.Parse(tmpBackgroundData2, System.Globalization.NumberStyles.HexNumber));
            }
        }

    }
}
