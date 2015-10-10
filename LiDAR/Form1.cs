using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace LiDAR
{
    public partial class Basic : Form
    {
        private delegate void DataReceivedEventHandler(); //委托声明
        //private Boolean bComOpen;//标志串口是否打开
        private Boolean bDataReceive;//标志是否在接收数据
        private String send_cmd = "";//数据发送命令字符串
        int interval_val, start_val, end_val;//间隔，开始点，结束点
        private String strReceiveData;//接收数据的字符串
        private String stringCharacters=";Truantboy";
        private int[] dataFinal = new int[1090];//解析的数据
        private int[] dataInFile = new int[1090];//要保存的数组缓存
        private int save_state = 0;
        Thread saveInFile;//用于写文件的线程
        Thread da;//用于数据处理，单开线程试图解决程序死机
        Display display;//子窗口
        private int test_connect = 0;
        private string portnumber = "";
        private int data_test = 0;

        private int z_s = 2; //自动或者手动程序，1为自动，2为手动
        private int blue_red = 2;//红蓝场程序，红场为1，蓝场为2

        public Basic()
        {
            InitializeComponent();
            //display = new Display();
        }
        //窗口初始化函数
        private void Basic_Load(object sender, EventArgs e)
        {
            LoadSerialPortConfiguration();
            cmbPortName.SelectedIndex = 0;
            cmbBaudRate.SelectedIndex = 6;
            cmbDataBits.SelectedIndex = 4;
            cmbStopBits.SelectedIndex = 1;
            cmbParity.SelectedIndex = 0;
            Interval.Text = "1";
            startStep.Text = "0";
            endStep.Text = "270";
            receiveStart.Enabled = false;
            bDataReceive = false;
            saveInFile = new Thread(new ParameterizedThreadStart(writeFile));
            da = new Thread(new ParameterizedThreadStart(dataAnalysis));
            readconf();
            quickstart();
        }

        private void LoadSerialPortConfiguration()
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                cmbPortName.Items.Add(s);
            }

            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                cmbParity.Items.Add(s);
            }

            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                cmbStopBits.Items.Add(s);
            }
        }
        //串口接收数据事件
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            DataReceivedEventHandler DataReceive = new DataReceivedEventHandler(this.TextDataReceive);
            DataReceive();
        }
        //以文本格式显示
        private void TextDataReceive()
        {
            test_connect = 0;
            try
            {
                strReceiveData = serialPort1.ReadExisting();
            }
            catch (Exception e)
            {
                serialPort1.Close();
                Thread.Sleep(500);
                open_serialPort();
                return;
            }
            if (da.IsAlive)
            {
                da.Abort();
            }
            da = new Thread(new ParameterizedThreadStart(dataAnalysis));
            da.Start();
            //int ok = dataAnalysis();
            int ok = 1;
            /*//测试用
            Random ra = new Random();
            for (int i = 0; i < 1080;i++ )
            {
                dataFinal[i] = ra.Next(15000);
            }
            if (displaydata.Checked == true)
            {
                display.reDrawData();
            }
            //测试完毕*/
            if (displaydata.Checked == true)
            {
                if (ok == 1)
                {
                    try
                    {
                        display.reDrawData();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                
            }
            
            teststring.Invoke(new EventHandler(delegate
            {
                //teststring.AppendText(ok.ToString()+"]]]"+strReceiveData);
                
                if (data_test == 0)
                {
                    teststring.Text = ok.ToString() + "]]0";//+ strReceiveData;
                    data_test = 1;
                }
                else
                {
                    teststring.Text = ok.ToString() + "]]1";//+ strReceiveData;
                    data_test = 0;
                }
                //teststring.ScrollToCaret();
            }));
             
        }
        //数据解析函数,若所有数据校验和正确并返回且状态是00或者99，返回true，否则返回false
        private void dataAnalysis(object obj)
        {
            Boolean ok = true;
            int strlen = stringCharacters.Length;
            if (send_cmd.StartsWith("QT")) return;// -1;
            String tp_cmd = send_cmd.Substring(0, 15) + send_cmd.Substring(15, strlen) + "\n";
            int first = strReceiveData.IndexOf(tp_cmd);
            if (first < 0) { return ; } //-2
            ok = checksum(first + 15 + strlen, first + 19 + strlen);
            if (ok == false) { return ; }//-3
            String status = strReceiveData.Substring(first + 16 + strlen, 2);
            if (status == "00") { return ; }//2
            else if (status != "99") { return ; }//-4
            ok = checksum(first + 19 + strlen, first + 25 + strlen);
            if (ok == false) { return ; }//-5
            int point = first + 26 + strlen;
            byte[] dataInAscii = new byte[3250];
            int datapoint=0;
            while (true)
            {
                int i = point;
                while (strReceiveData[i] != '\n')
                {
                    dataInAscii[datapoint] = Convert.ToByte(strReceiveData[i]);
                    datapoint++;
                    i++;
                }
                datapoint--;
                dataInAscii[datapoint] = 0;
                ok = checksum(point - 1, i);
                point = i + 1;
                if (ok == false) { return ; }//-6
                if (strReceiveData[i + 1] == '\n')
                {
                    break;
                }
            }
            if (datapoint % 3 != 0)//数据个数不为3的倍数，数据出错
            {
                return ;//-7
            }
            if (datapoint / 3 != (end_val-start_val + 1))//数据个数出错
            {
                return ;//-8
            }
            int a,b,c;
            dataFinal = new int[1090];
            for (int i = 0; i <= datapoint; i = i + 3)
            {
                a = dataInAscii[i]-48;
                b = dataInAscii[i + 1]-48;
                c = dataInAscii[i + 2]-48;
                dataFinal[i / 3+start_val] = ((a << 12) + (b << 6) + c);
            }
            if (save_state == 1)
            {
                Array.Copy(dataFinal,dataInFile,1090);
                if (saveInFile.IsAlive)
                {
                    saveInFile.Join();
                }
                saveInFile = new Thread(new ParameterizedThreadStart(writeFile));
                saveInFile.Start(dataInFile);
                save_state = 0;
            }
            
            //以下代码为数组倒置
            for (int dao = 0; dao < 540; dao++)
            {
                int tp_a = dataFinal[dao];
                dataFinal[dao] = dataFinal[1079 - dao];
                dataFinal[1079 - dao] = tp_a;
            }
            //倒置结束
            
            return ;//1
        }
        //校验和检查(start和end为开头与结束两个'\n'的位置)
        private Boolean checksum(int start,int end)
        {
            int sum = 0;
            try
            {
                sum = Convert.ToInt32(strReceiveData[end - 1]);
            }
            catch (Exception ex) { return false; }
            int i=0,tp_sum=0;
            for (i = start+1; i < end - 1; i++)
            {
                tp_sum = tp_sum + Convert.ToInt32(strReceiveData[i]);
            }
            tp_sum = tp_sum & 63;//63 二进制后六位为1，其余为0
            tp_sum = tp_sum + 48;//48 十六进制为30H
            if (tp_sum == sum)
            {
                return true;
            }
            return false;
        }

        private void btnOpenSerialPort_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                try
                {
                    portnumber = cmbPortName.Text;
                    open_serialPort();
                    cmbPortName.Enabled = false;
                    cmbBaudRate.Enabled = false;
                    cmbDataBits.Enabled = false;
                    cmbStopBits.Enabled = false;
                    cmbParity.Enabled = false;
                    receiveStart.Enabled = true;
                    send_cmd = "QT\n";
                    serialPort1.Write(send_cmd);

                    btnOpenSerialPort.Text = "断开连接";

                }
                catch (System.IO.IOException) { } //可以进一步扩展
            }
            else
            {
                serialPort1.Close();
                btnOpenSerialPort.Text = "连接";
                cmbPortName.Enabled = true;
                cmbBaudRate.Enabled = true;
                cmbDataBits.Enabled = true;
                cmbStopBits.Enabled = true;
                cmbParity.Enabled = true;
                receiveStart.Text = "接收";
                bDataReceive = false;
                receiveStart.Enabled = false;

                //关闭串口时要做的处理
                //tmrAutoSend.Enabled = false;
            }
        }

        private void open_serialPort()
        {
            //int ok = 0;
            serialPort1.PortName = portnumber;
            serialPort1.BaudRate = 9600;
            try
            {
                //设置serialPort1的串口属性
                //serialPort1.BaudRate = int.Parse(cmbBaudRate.Text);
                //serialPort1.DataBits = int.Parse(cmbDataBits.Text);
                //serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);
                //serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text);
                //serialPort1.WriteBufferSize = 4096;
                //打开串口
                serialPort1.Open();
                //ok = 1;
            }
            catch (Exception exc)
            {
                Thread.Sleep(100);
                open_serialPort();
                //ok = 0;
            }
        }

        private void tmrAutoSend_Tick(object sender, EventArgs e)
        {
            
        }

        private void recevieStart_Click(object sender, EventArgs e)
        {
            if (bDataReceive == false)
            {
                Boolean isok = checkText();
                if (isok)
                {
                    build_cmd();
                    serialPort1.Write(send_cmd);
                }
                bDataReceive = true;
                receiveStart.Text = "停止接收";
                tmrAutoDetect.Start();
            }
            else
            {
                send_cmd = "QT" + stringCharacters + "\n";
                int out_flag = 0;
                while (true)
                {
                    try
                    {
                        serialPort1.Write(send_cmd);
                        out_flag = 1;
                    }
                    catch (Exception ex)
                    {
                        if (serialPort1.IsOpen)
                        {
                            serialPort1.Close();
                        }
                        Thread.Sleep(500);
                        open_serialPort();
                    }
                    if (out_flag == 1)
                    {
                        break;
                    }
                }
                bDataReceive = false;
                receiveStart.Text = "接收";
                tmrAutoDetect.Stop();
            }
        }
        //文本框逻辑检查
        private Boolean checkText()
        {
            Boolean isok = true;
            if (int.TryParse(Interval.Text, out interval_val) == false)
            {
                interval_val = 1;
                Interval.Text = "1";
                isok = false;
            }
            if (interval_val > 9 || interval_val < 0)
            {
                interval_val = 1;
                Interval.Text = "1";
                isok = false;
            }
            if (int.TryParse(startStep.Text, out start_val) == false)
            {
                start_val = 0;
                startStep.Text = "0";
                isok = false;
            }
            if (start_val > 269 || start_val < 0)
            {
                start_val = 0;
                startStep.Text = "0";
                isok = false;
            }
            if (int.TryParse(endStep.Text, out end_val) == false)
            {
                end_val = 270;
                endStep.Text = "270";
                isok = false;
            }
            if (end_val > 270 || end_val < 1)
            {
                end_val = 270;
                endStep.Text = "270";
                isok = false;
            }
            if (start_val >= end_val)
            {
                start_val = 0;
                startStep.Text = "0";
                end_val = 270;
                endStep.Text = "270";
                isok = false;
            }
            start_val = start_val * 4;
            end_val = end_val * 4;
            return isok;
        }
        //命令合成
        private void build_cmd()
        {
            send_cmd = "MD" + (start_val + 10000).ToString().Substring(1) +
                (end_val + 10000).ToString().Substring(1) + "01" + interval_val.ToString() +
                "00" + stringCharacters+"\n";
        }
        //文本框改变重组命令
        private void TextChanged(object sender, EventArgs e)
        {
            if (bDataReceive == true)
            {
                Boolean isok = checkText();
                if (isok)
                {
                    send_cmd = "QT"+stringCharacters+"\n";
                    serialPort1.Write(send_cmd);
                    build_cmd();
                    serialPort1.Write(send_cmd);
                }
            }
        }

        private void displaydata_CheckedChanged(object sender, EventArgs e)
        {
            if (displaydata.Checked == true)
            {
                display = new Display(this);
                display.Show();
                //test_set_data();//测试用
            }
            else
            {
                display.Dispose();
            }
            
        }
        //测试用
        private void test_set_data()
        {
            StreamReader sr = new StreamReader("test.txt");
            String line;
            //int i=1;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                string[] s = line.Split(' ');
                //dataFinal[1080-i] = Int32.Parse(s[1]);
                dataFinal[i] = Int32.Parse(s[1]);
                i++;
            }
            display.reDrawData();
        }
        //线程函数，主要用于写文件
        static void writeFile(object obj)
        {
            int[] dataInFile = (int[])obj;
            String name = DateTime.Now.ToString("yyyy-MM-dd");
            name = name + "_" + DateTime.Now.ToString("hh_mm_ss")+".txt";
            FileStream fs = new FileStream(name, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            for (int i = 0; i < 1080; i++)
            {
                sw.WriteLine(i.ToString() + ":" + dataInFile[i].ToString());
            }
                
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }

        //对外接口，用于子窗口设置复选框值
        public void setDisplayCheckBox()
        {
            this.displaydata.Checked = false;
        }
        //对外接口，用于子窗口获取雷达数据
        public int[] getData()
        {
            return this.dataFinal;
        }

        private void dataSave_Click(object sender, EventArgs e)
        {
            /*测试数据
            for (int i = 0; i < 1090; i++)
            {
                dataInFile[i] = i * 10;
            }*/
            save_state = 1;
        }

        private void Basic_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen == true)
            {
                send_cmd = "QT" + stringCharacters + "\n";
                serialPort1.Write(send_cmd);
                serialPort1.Close();
            }
            this.Dispose();
            if(display != null)
                display.Dispose();
            Application.Exit();
            
        }

        private void tmrAutoDetect_Tick(object sender, EventArgs e)
        {
            test_connect++;
            if (test_connect > 5)
            {
                try
                {
                    serialPort1.Write(send_cmd);
                }
                catch (Exception ex)
                {
                    serialPort1.Close();
                    open_serialPort();
                    if (send_cmd.Length != 0)
                    {
                        serialPort1.Write(send_cmd);
                    }
                }
            }
        }
        private void readconf()
        {
            StreamReader locreader = new StreamReader("config.txt");
            string sLine = "";
            string[] conf = new string[10];
            sLine = locreader.ReadLine(); //只读取第一行场地与机器人类型配置
            conf = sLine.Split(' ');
            if (conf[1] == "蓝") blue_red = 2; else blue_red = 1;
            if (conf[3] == "自动") z_s = 1; else z_s = 2;
            locreader.Close();
        }
        public int get_z_or_s()
        {
            return this.z_s;
        }
        public int get_blue_or_red()
        {
            return this.blue_red;
        }
        private void quickstart()
        {
            
            cmbPortName.Enabled = false;
            cmbBaudRate.Enabled = false;
            cmbDataBits.Enabled = false;
            cmbStopBits.Enabled = false;
            cmbParity.Enabled = false;
            receiveStart.Enabled = true;
            btnOpenSerialPort.Text = "断开连接";
            if (z_s == 1) portnumber = "com21"; else portnumber = "com5";
            serialPort1.PortName = portnumber;
            serialPort1.BaudRate = 9600;
            send_cmd = "QT\n";
            try
            {
                serialPort1.Open();
                serialPort1.Write(send_cmd);
                recevieStart_Click(null, EventArgs.Empty);
                displaydata.Checked = true;
            }
            catch (Exception ex)
            {
                cmbPortName.Enabled = true;
                cmbBaudRate.Enabled = true;
                cmbDataBits.Enabled = true;
                cmbStopBits.Enabled = true;
                cmbParity.Enabled = true;
                receiveStart.Text = "接收";
                btnOpenSerialPort.Text = "连接";
                bDataReceive = false;
                receiveStart.Enabled = false;
            }
        }
    }
}
