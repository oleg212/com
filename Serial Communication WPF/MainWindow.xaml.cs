using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using Renci.SshNet;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace Serial_Communication_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region variables
        //Richtextbox
        FlowDocument mcFlowDoc = new FlowDocument();
        Paragraph para = new Paragraph();
        //Serial 
        SerialPort serial = new SerialPort();
        string recieved_data;
        
        #endregion


        public MainWindow()
        {
            InitializeComponent();
            InitializeComponent();
            //overwite to ensure state
            Connect_btn.Content = "Connect";
        }
        SshClient client;
        
        private void Connect_Comms(object sender, RoutedEventArgs e)
        {
            if (Connect_btn.Content == "Connect")
            {
                client = new SshClient(ipbox.Text, usernamebox.Text, passwordbox.Text);
                //Sets up serial port
                try { 
                serial.PortName = Comm_Port_Names.Text;
                serial.BaudRate = Convert.ToInt32(Baud_Rates.Text);
                serial.Handshake = System.IO.Ports.Handshake.None;
                serial.Parity = Parity.None;
                serial.DataBits = 8;
                serial.StopBits = StopBits.One;
                serial.ReadTimeout = 200;
                serial.WriteTimeout = 50;
                serial.Open();
                    
                Connect_btn.Content = "Disconnect";
                client.Connect();
                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);
                }
                catch { }
                
                
            }
            else
            {
                try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
                {
                    client.Disconnect();
                    serial.Close();
                    Connect_btn.Content = "Connect";
                }
                catch
                {
                }
            }
        }

        #region Recieving
        
        private delegate void UpdateUiTextDelegate(string text);
        private void Recieve(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            recieved_data = serial.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), recieved_data);
        }

        int timer=0;
        string normal = "";
        string getvalue(string parameter,string input)
        {   int index1 = input.IndexOf(parameter);
            int index2 = input.IndexOf(' ',index1+ parameter.Length);
            if (index2 == -1) { index2 = index1 + 8; }
            string value= input.Substring(index1+ parameter.Length, index2-index1- parameter.Length-1);
            Trace.WriteLine(value);
            return value;
        }
        private void WriteData(string text)
        {   if (text== null)
            {
                return;
            }
            string final = "";
            int indexOfSubstring = text.IndexOf("$GPGGA");
            int indexOftab = text.IndexOf('\n');
            if (indexOftab > -1)
            {
                normal += text.Substring(0, indexOftab);
            }
            else
            {
                normal += text;
            }


            SshCommand command = client.CreateCommand("cat /tmp/4ginfo_tmp");
            string result = command.Execute();
            string result1 = result;

            if (result.Contains("Signal Quality"))
            {
                string[] mdata = result.Split('\n',',');
                result = mdata[3].Split(':')[2]+mdata[4]+mdata[5]+mdata[6]+", ";
                

           


                if (indexOftab > -1)
            {
                if (normal.IndexOf("GPGGA") != -1)
                {

                    normal = normal.Substring(normal.IndexOf("GPGGA"));
                    string[] gpsdata = normal.Split(',');
                    string output = " LAT: " + gpsdata[2] + " LATDIR: " + gpsdata[3] + " LON: " + gpsdata[4] + " LONDIR: " + gpsdata[5] + '\n';
                    string timeb = gpsdata[1];
                    string timec = timeb.Substring(0, 2) + ':' + timeb.Substring(2, 2) + ':' + timeb.Substring(4, 2);

                    var time = TimeSpan.Parse(timec);
                    double latitude = double.Parse(gpsdata[2], System.Globalization.CultureInfo.InvariantCulture);
                    if (gpsdata[3].Equals('S'))
                    {
                        latitude *= -1;
                    }

                    double longitude = double.Parse(gpsdata[4], System.Globalization.CultureInfo.InvariantCulture);
                    if (gpsdata[5].Equals('W'))
                    {
                        longitude *= -1;
                    }

                    if (result.Contains("ERROR") == false)
                    {
                        para.Inlines.Add(time.ToString() + output + result + "\n");
                        string[] results = result.Split();
                        string rsrp = "'" + results[3] + "'";
                        string rsrq = "'" + results[6] + "'";
                        string sinr = "'" + results[9] + "'";
                        string rssi = "'" + results[12] + "'";
                        

                        output=output.Replace('\n',' ');
                            if (rssi == "'''")
                            {
                                return;
                            }
                        string[] gpsinfo = output.Split(' ');

                        string lat= "'" + gpsinfo[2] + "'";
                        string latdir= "'" + gpsinfo[4] + "'";
                        string lon= "'" + gpsinfo[6] + "'";
                        string londir= "'" + gpsinfo[8] + "'";
                        if (rsrp != "N/A") {
                            
                            string sql = "INSERT INTO `test`.`signal` ( `lat`, `latdir`, `lon`, `londir`, `rsrp`, `rsrq`, `sinr`, `rssi`) VALUES ("+lat+", "+latdir + ", " + lon + ", " + londir + ", " + rsrp + ", " + rsrq + ", " + sinr + ", " + rssi+");";
                            MySqlCommand commandsql = new MySqlCommand(sql, conn);
                            Trace.WriteLine(sql);
                            MySqlDataReader reader = commandsql.ExecuteReader();

                            while (reader.Read())
                            {
                                Trace.WriteLine(reader[0].ToString() + " " + reader[1].ToString());
                            }
                            reader.Close();
                            Thread.Sleep(500);

                        }
                    }
                    final = time.ToString() + output + result + "\n";

                }

                if (result.Contains("ERROR") == false)
                {
                    normal = text.Substring(indexOftab);
                        
                                                                   
                }
            }
            
                mcFlowDoc.Blocks.Add(para);
                Commdata.Document = mcFlowDoc;

            }
        }


        #endregion


        #region Sending        

        private void Send_Data(object sender, RoutedEventArgs e)
        {
            SerialCmdSend(SerialData.Text);
            SerialData.Text = "";
        }
        public void SerialCmdSend(string data)
        {
            if (serial.IsOpen)
            {
                try
                {
                    // Send the binary data out the port
                    byte[] hexstring = Encoding.ASCII.GetBytes(data);
                    //There is a intermitant problem that I came across
                    //If I write more than one byte in succesion without a 
                    //delay the PIC i'm communicating with will Crash
                    //I expect this id due to PC timing issues ad they are
                    //not directley connected to the COM port the solution
                    //Is a ver small 1 millisecound delay between chracters
                    foreach (byte hexval in hexstring)
                    {
                        byte[] _hexval = new byte[] { hexval }; // need to convert byte to byte[] to write
                        serial.Write(_hexval, 0, 1);
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    para.Inlines.Add("Failed to SEND" + data + "\n" + ex + "\n");
                    mcFlowDoc.Blocks.Add(para);
                    Commdata.Document = mcFlowDoc;
                }
            }
            else
            {
            }
        }


        #endregion

        private void Comm_Port_Names_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {   

                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), recieved_data);
                if (client == null) { return; }
                SshCommand command = client.CreateCommand("cat /tmp/4ginfo_tmp");
                string result = command.Execute();
                if (result.Contains("Signal Quality"))
                {
                    string[] mdata = result.Split('=');
                    string[] mdata2 = mdata[0].Split(':');
                    result = mdata2[6] + "= " + mdata[1] + "= " + mdata[2] + "= " + mdata[3] + "= " + (mdata[4].Split(':', ' '))[0];


                    Dispatcher.Invoke(() => para.Inlines.Add(result));
                    Dispatcher.Invoke(() => normal += result);



                    Task.Delay(1000).Wait();


                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), recieved_data + "\n" + result);
                    Dispatcher.Invoke(() => mcFlowDoc.Blocks.Add(para));
                    Dispatcher.Invoke(() => Commdata.Document = mcFlowDoc);
                }
            
        }
        string[] port;

        MySqlConnection conn=null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            port = SerialPort.GetPortNames();
            Comm_Port_Names.ItemsSource = port;
            string connectionString = "server=localhost;user=root;database = test;password=Oracle2275As.;";
            conn = new MySqlConnection(connectionString);
            conn.Open();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            conn.Close();
        }
    }
}
