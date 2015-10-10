using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Management;
using System.Diagnostics;

namespace LiDAR
{
    public partial class Display : Form
    {
        Basic basic = null;
        private delegate void DataReceivedEventHandler(); //委托声明
        private double pie = 3.1415926536;
        private int r;//半径
        int centerx, centery;//圆心
        private int maxval=15000;
        private double[,] trust_circle_ye = new double[50,3];//可信叶子信息
        int trust_nums_ye = 0;//可信叶子个数
        private double[,] trust_circle_huan = new double[50, 3];//可信圆环信息
        int trust_nums_huan = 0;//可信圆环个数
        int is_update_trust = 0;//更新标志
        private double [] distance_hh = new double[25]; //环环间距
        private double [] distance_yy = new double[66]; //叶叶间距
        private double [] distance_hy = new double[84]; //环叶间距
        private double[,] absolute_circle = new double[10,2]; //环的绝对位置
        private double[,] absolute_distance = new double[10, 10];//环间的距离
        private double[,] absolute_hudu = new double[10, 10]; //环环间线段的全局坐标弧度
        //private double cir_x, cir_y, cir_du;

        private byte[] sendByte = new byte[6] { 0xaa, 0xf1, 0, 0, 0, 0 };      //指令数组
        private byte[] sendByte2 = new byte[10] { 0xaa, 0xf2, 0, 0, 0, 0, 0, 0, 0, 0 };    //数据数组
        private byte[] sendByte3 = new byte[2] { 0, 0 };                      //中间数组1
        private byte[] sendByte4 = new byte[2] { 0, 0 };                      //中间数组2
        private byte[] sendByte5 = new byte[2] { 0, 0 };                      //校验数组1
        private byte[] sendByte6 = new byte[2] { 0, 0 };                      //校验数组2
        private byte[] sendByte7 = new byte[2] { 0, 0 };                      //角度校验数组
        Int16  Check_6;
        private int z_step = 1;
        private int s_step = 1;
        private int draw_data_flag = 0;
        private int draw_num = 0;
        Thread receive_s_step;
        private int z_or_s = 2; //自动或者手动程序，1为自动，2为手动
        private int blue_or_red = 2;//红蓝场程序，红场为1，蓝场为2
        private double[, ,] z_step_loc = new double[2, 30, 2]; //第一维，0为红场，1为蓝场，第二维步骤，第三维为x,y
        private double[, ,] s_step_loc = new double[2, 30, 2];
        
        private int reset_location_flag = 0;
        private double[,] reset_data = new double[100, 2];
        private int reset_num = 0;

        private UdpClient sendudp;
        private UdpClient receiveudp;
        private UdpClient heartudp;//心跳udp

        private string configstr = "";
        
        //Thread testheartmsg;
        Process pro = Process.GetCurrentProcess();

        public Display()
        {   
            InitializeComponent();
        }
        //重写的构造函数
        public Display(Basic basicform)
        {
            InitializeComponent();
            this.basic = basicform;
            this.set_basic_data();
            LoadSerialPortConfiguration();
            cmbPortName.SelectedIndex = 0;

            this.z_or_s = this.basic.get_z_or_s();
            this.blue_or_red = this.basic.get_blue_or_red();
        }
        private void LoadSerialPortConfiguration()
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                cmbPortName.Items.Add(s);
            }
        }
        private void set_basic_data()
        {
            absolute_circle[0, 0] = -2345;
            absolute_circle[0, 1] = 7200;
            absolute_circle[1, 0] = -1300;
            absolute_circle[1, 1] = 4620;
            absolute_circle[2, 0] = -880;
            absolute_circle[2, 1] = 5750;
            absolute_circle[3, 0] = -815;
            absolute_circle[3, 1] = 7050;
            absolute_circle[4, 0] = 1000;
            absolute_circle[4, 1] = 4350;
            absolute_circle[5, 0] = 2500;
            absolute_circle[5, 1] = 6200;
            absolute_circle[6, 0] = 2850;
            absolute_circle[6, 1] = 5300;
            for (int i = 0; i < 6; i++)
            {
                for (int j = i + 1; j < 7; j++)
                {
                    absolute_distance[i, j] = Math.Sqrt(Math.Pow(absolute_circle[i, 0] - absolute_circle[j, 0], 2.0) + Math.Pow(absolute_circle[i, 1] - absolute_circle[j, 1], 2.0));
                }
            }
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (i == j) continue;
                    int tp_a = i < j ? i : j;
                    int tp_b = i > j ? i : j;
                    double tp = absolute_circle[j, 1] - absolute_circle[i, 1];
                    int sign = tp > 0 ? 1 : -1;
                    absolute_hudu[i, j] = sign * Math.Acos((absolute_circle[j, 0] - absolute_circle[i, 0]) * 1.0 / absolute_distance[tp_a, tp_b]);
                }
            }
        }

        private void Display_Paint(object sender, PaintEventArgs e)
        {
            int x = this.Size.Width;
            int y = this.Size.Height;
            if (x < y)
            {
                r = (x - 82 ) / 2;
            }
            else
            {
                r = (y - 48) / 2;
            }
            centerx = r + 110;
            centery = r + 10;//圆心
            drawBackground();
        }
        //绘制背景
        private void drawBackground()
        {
            Graphics g = this.CreateGraphics();
            Point center = new Point(centerx, centery);
            Pen bluePen = new Pen(Color.Blue, 0.1f);
            Pen greenPen = new Pen(Color.Green, 0.1f);
            g.DrawEllipse(bluePen, 110, 10, 2 * r, 2 * r);//画椭圆的方法，x坐标、y坐标、宽、高，如果是100，则半径为50
            int tpr = (int)Math.Round(r * Math.Cos(pie / 4));
            g.DrawLine(bluePen, center, new Point(centerx + tpr, centery + tpr));
            g.DrawLine(bluePen, center, new Point(centerx - tpr, centery + tpr));
            g.Dispose();
        }
        //数据绘制
        private void drawData()
        {
            int[] dataFinal = basic.getData();
            Graphics g = this.CreateGraphics();
            Pen greenPen = new Pen(Color.Green, 0.1f);
            Point p;
            Point[] pts = new Point[2160];
            int pts_p = 0;
            for (int i = 180; i < 1260; i++)//180为45（度）*4，1260为 315（度）*4
            {
                double tpi = (i * 1.0) / 4.0;
                int len = dataFinal[i - 180];
                if (len > maxval) { len = maxval; }
                double tpr = len * r * 1.0 / (maxval * 1.0);
                p = getCoordinate(centerx, centery, tpr, tpi);
                pts[pts_p] = new Point(centerx, centery);
                pts[pts_p + 1] = new Point(p.X, p.Y);
                pts_p = pts_p + 2;
            }
            g.DrawLines(greenPen, pts);
            g.Dispose();
        }
        //坐标变换
        private Point getCoordinate(int x, int y, double len, double degree)//最下方为0度位置，逆时针转
        {
            double du = degree / 360.0 * 2 * pie;
            double tpy = Math.Cos(du) * len;
            double tpx = Math.Sin(du) * len;
            Point p = new Point((int)Math.Round(x + tpx), (int)Math.Round(y + tpy));
            return p;
        }

        private void Display_SizeChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        public void reDrawData()
        {
            if (draw_data_flag == 1)
            {
                this.Invalidate();
                drawData();
                draw_num++;
                if (draw_num > 20)
                {
                    draw_num = 0;
                    draw_data_flag = 0;
                    drawimg.Text = "开始画图";
                }
            }
            calc_Circle_Location();
            heartmsg(pro.Id);
        }

        private void Display_FormClosing(object sender, FormClosingEventArgs e)
        {
            basic.setDisplayCheckBox();
            receiveudp.Close();
            if (serialPort1.IsOpen)
                serialPort1.Close();
        }

        private void maxSet_Click(object sender, EventArgs e)
        {
            if (int.TryParse(maxDistance.Text, out maxval) == false)
            {
                maxval = 15;
                maxDistance.Text = "15";
            }
            if (maxval > 30 || maxval < 1)
            {
                maxval = 15;
                maxDistance.Text = "15";
            }
            maxval = maxval * 1000;
            this.reDrawData();//测试
        }

        private void Display_Load(object sender, EventArgs e)
        {
            maxDistance.Text = "15";
            text_step.Invoke(new EventHandler(delegate { text_step.Text = z_step.ToString(); }));
            IPAddress localIp = IPAddress.Parse("127.0.0.1");
            IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65510"));
            receiveudp = new UdpClient(localIpEndPoint);
            receive_s_step = new Thread(new ParameterizedThreadStart(receiveSStep));
            receive_s_step.Start();
            readLocation();
            if (blue_or_red == 1) bluered.Text = "红场"; else bluered.Text = "蓝场";
            if (z_or_s == 1)
            {
                quickstart();
            }
            //testheartmsg = new Thread(new ParameterizedThreadStart(heart));
            //testheartmsg.Start();
        }

        //定位计算，计算圆环位置
        private void calc_Circle_Location()
        {
            int i = 0;
            int[] dataFinal = basic.getData();
            double[] x = new double[1080];
            double[] y = new double[1080];
            for (i = 0; i < 1080; i++)
            {
                double hudu = ((i + 1) * 1.0 / 4 - 45) * pie / 180;
                x[i] = dataFinal[i] * Math.Cos(hudu);
                y[i] = dataFinal[i] * Math.Sin(hudu);
            }
            int flag = 0;
            i = 0;
            int start = 0, over = 0, low = 0, tp = 0;
            int p_nums_ye = 0;
            int p_nums_huan = 0;
            int p_nums_all = 0;
            double[,] final_circle_ye = new double[50,4];//检测出的叶子结果
            double[,] final_circle_huan = new double[50, 4];//检测出的圆环结果
            double[,] final_circle_all = new double[50, 4];//检测出的叶子结果
            while (i < 1078)
            {
                int a = Math.Abs(dataFinal[i + 1] - dataFinal[i]);
                tp = dataFinal[i];
                if (a > 50)
                {
                    if (flag == 0)
                    {
                        start = i;
                        flag = 1;
                    }
                    else
                    {
                        over = i;
                        flag = 0;
                        low = dataFinal[start + 2];
                        for (int j = start + 2; j <= over - 2; j++)
                        {
                            if (dataFinal[j] < low)
                            {
                                low = dataFinal[j];
                            }
                        }
                        double cir_x = 0, cir_y = 0, cir_du = 0, cir_dis = 0;
                        int isok = detect(start, over, low, x, y,out cir_x,out cir_y,out cir_du,out cir_dis);
                        if (isok != -1)
                        {
                            final_circle_all[p_nums_all, 0] = cir_x;
                            final_circle_all[p_nums_all, 1] = cir_y;
                            final_circle_all[p_nums_all, 2] = cir_du;
                            final_circle_all[p_nums_all, 3] = cir_dis;
                            p_nums_all++;
                        }
                        if (isok == 0)
                        {
                            final_circle_ye[p_nums_ye, 0] = cir_x;
                            final_circle_ye[p_nums_ye, 1] = cir_y;
                            final_circle_ye[p_nums_ye, 2] = cir_du;
                            final_circle_ye[p_nums_ye, 3] = cir_dis;
                            if (draw_data_flag == 1)
                            {
                                draw_circle(cir_x, cir_y, 0);
                            }
                            p_nums_ye++;
                        }
                        else if(isok == 1)
                        {
                            final_circle_huan[p_nums_huan, 0] = cir_x;
                            final_circle_huan[p_nums_huan, 1] = cir_y;
                            final_circle_huan[p_nums_huan, 2] = cir_du;
                            final_circle_huan[p_nums_huan, 3] = cir_dis;
                            if (draw_data_flag == 1)
                            {
                                draw_circle(cir_x, cir_y, 1);
                            }
                            p_nums_huan++;
                        }
                        i--;
                    }
                }
                i++;
            }
            if (p_nums_huan > 100) //设为100跳过该段程序
            {
                string send_data = p_nums_huan.ToString();
                send_data = send_data + " ";
                for (int huan_num = 0; huan_num < p_nums_huan; huan_num++)
                {
                    double dis = Math.Sqrt(Math.Pow(final_circle_huan[huan_num, 0], 2) + Math.Pow(final_circle_huan[huan_num, 1], 2));
                    send_data = send_data + dis.ToString() + " ";
                    send_data = send_data + final_circle_huan[huan_num, 2].ToString() + " ";
                }
                send_data = send_data + "over!";
                IPAddress localIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65520"));
                sendudp = new UdpClient(localIpEndPoint);
                string message = send_data;
                byte[] sendbytes = Encoding.Unicode.GetBytes(message);
                IPAddress remoteIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIp, int.Parse("65500"));
                sendudp.Send(sendbytes, sendbytes.Length, remoteIpEndPoint);
                sendudp.Close();
                search_circle_location(450, 750, 1.5707, p_nums_huan, final_circle_huan);
            }
            //新的检测程序
            //detect_z0(p_nums_ye,final_circle_ye,-210,417,1200,3);
            //detect_s0(p_nums_all, final_circle_all, -185, 310, 500);
            //detect_z1(p_nums_all, final_circle_all, 185, 390, 500);
            //detect_z0(p_nums_ye, final_circle_ye, -210, 417, 1200, 3);
            //detect_z1(p_nums_all, final_circle_all, 185, 390, 500);
            if (reset_location_flag == 1)
            {
                double xx, yy;
                int okornot = detect_reset(p_nums_huan, final_circle_huan, out xx, out yy);
                if (okornot == 1)
                {
                    reset_data[reset_num, 0] = xx;
                    reset_data[reset_num, 1] = yy;
                    reset_num++;
                }
                if (reset_num == 50)
                {
                    double xxx = 0, yyy = 0;
                    for (int tpi = 0; tpi < reset_num; tpi++)
                    {
                        xxx = xxx + reset_data[tpi, 0];
                        yyy = yyy + reset_data[tpi, 1];
                    }
                    int step_str = Int32.Parse(resetstep.Text);
                    step_str = step_str - 2;
                    text_x.Text = Math.Round(xxx * 1.0 / reset_num).ToString();
                    text_y.Text = Math.Round(yyy * 1.0 / reset_num).ToString();
                    if (z_or_s == 1)
                    {
                        z_step_loc[blue_or_red - 1, step_str, 0] = Math.Round(xxx * 1.0 / reset_num);
                        z_step_loc[blue_or_red - 1, step_str, 1] = Math.Round(yyy * 1.0 / reset_num);
                    }
                    else
                    {
                        s_step_loc[blue_or_red - 1, step_str, 0] = Math.Round(xxx * 1.0 / reset_num);
                        s_step_loc[blue_or_red - 1, step_str, 1] = Math.Round(yyy * 1.0 / reset_num);
                    }
                    writeconfigfile();
                    reset_num = 0;
                    resetdata.Text = "开始校准";
                    resetdata.Enabled = true;
                    reset_location_flag = 0;
                }
            }
            if (blue_or_red == 1)
            {
                if (z_or_s == 1)
                {
                    if (z_step == 1)
                    {
                        detect_z0(p_nums_ye, final_circle_ye, -210, 417, 1200, 3);
                    }
                    else
                    {
                        detect_z1(p_nums_huan, final_circle_huan, z_step_loc[0, z_step - 2, 0], z_step_loc[0, z_step - 2, 1], 1000);
                    }
                    /*
                    else if (z_step == 2)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, 185, 390, 1000);
                    }
                    else if (z_step == 3)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -211, 390, 1000);
                    }
                    else if (z_step == 4)//取叶子
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -511, 613, 1000);
                    }
                    else if (z_step == 5)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -206, 401, 1000);
                    }
                    else if (z_step == 6)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -454, 778, 1000);
                    }*/
                }
                else if (z_or_s == 2)
                {
                    //detect_sstep(p_nums_huan, final_circle_huan);

                    if (s_step == 1)
                    {
                        detect_s0(p_nums_all, final_circle_all, 11, 275, 1500);
                    }
                    else
                    {
                        detect_s1(p_nums_huan, final_circle_huan, s_step_loc[0, s_step-2, 0], s_step_loc[0, s_step-2, 1], 3000);
                    }
                    /*
                    else if (s_step == 2)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, -510, 675, 1000);
                    }
                    else if (s_step == 3)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 16, 645, 1000);
                    }
                    else if (s_step == 4)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 27, 1049, 1500);
                    }
                    else if (s_step == 5)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, -1815, 645, 2000);
                    }
                    else if (s_step == 6)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 197, 1149, 1500);
                    }*/
                }
            }
            else if (blue_or_red == 2)
            {
                if (z_or_s == 1)
                {
                    if (z_step == 1)
                    {
                        detect_z0_blue(p_nums_ye, final_circle_ye, 184, 407, 1200, 3);
                    }
                    else
                    {
                        detect_z1(p_nums_huan, final_circle_huan, z_step_loc[1,z_step-2,0], z_step_loc[1,z_step-2,1], 3000);
                    }
                    /*
                    else if (z_step == 2)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -220, 406, 1000);
                    }
                    else if (z_step == 3)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, 171, 395, 1000);
                    }
                    else if (z_step == 4)//取叶子
                    {
                        detect_z1(p_nums_huan, final_circle_huan, 353, 835, 1000);
                    }
                    else if (z_step == 5)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, -206, 401, 1000);
                    }
                    else if (z_step == 6)
                    {
                        detect_z1(p_nums_huan, final_circle_huan, 785, 1418, 2000);
                    }
                    */
                }
                else if (z_or_s == 2)
                {
                    //detect_sstep_blue(p_nums_huan, final_circle_huan);

                    if (s_step == 1)
                    {
                        detect_s0_blue(p_nums_all, final_circle_all, 11, 275, 1500);
                    }
                    else
                    {
                        detect_s1(p_nums_huan, final_circle_huan, s_step_loc[1,s_step-2,0], s_step_loc[1,s_step-2,1], 3000);
                    }
                    /*
                    else if (s_step == 2)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 549, 683, 1000);
                    }
                    else if (s_step == 3)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 23, 665, 1000);
                    }
                    else if (s_step == 4)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 22, 1057, 1500);
                    }
                    else if (s_step == 5)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, 425, 1090, 2000);
                    }
                    else if (s_step == 6)
                    {
                        detect_s1(p_nums_huan, final_circle_huan, -171, 1079, 1500);
                    }
                    */
                }
            }
            
            //新的程序结束
            //my_method(p_nums_huan, final_circle_huan);
            if (is_update_trust == 1)
            {
                int tp_i;
                for (tp_i = 0; tp_i < p_nums_huan; tp_i++)
                {
                    trust_circle_huan[tp_i, 0] = final_circle_huan[tp_i, 0];
                    trust_circle_huan[tp_i, 1] = final_circle_huan[tp_i, 1];
                }
                trust_nums_huan = p_nums_huan;
                for (tp_i = 0; tp_i < p_nums_ye; tp_i++)
                {
                    trust_circle_ye[tp_i, 0] = final_circle_ye[tp_i, 0];
                    trust_circle_ye[tp_i, 1] = final_circle_ye[tp_i, 1];
                }
                trust_nums_ye = p_nums_ye;
                is_update_trust = 0;
            }
        }
        private int detect(int start, int over, int low, double[] x, double[] y, out double cir_x,out double cir_y,out double cir_du,out double length)
        {
            int huan_min_r = 180, huan_max_r = 220;//半径可信阈值设定，叶子105-145，圆环180-220
            int ye_min_r = 105, ye_max_r = 145;
            int err = 20;//判断是否为圆的半径误差
            double du = (over - start)*1.0 / 4.0 * pie / 180.0 / 2.0;
            double r = low * Math.Sin(du) / (1 - Math.Sin(du));
            int type = 0; //返回值，设定0为叶子，1为圆环，-1为未检测成功
            cir_x = cir_y = cir_du = -1;
            length = -1;
            if (r > ye_min_r && r < ye_max_r)
            {
                type = 0;
            }
            else if (r > huan_min_r && r < huan_max_r)
            {
                type = 1;
            }
            else if (r > ye_max_r && r < huan_min_r)
            {
                type = 2;
            }
            else
            {
                return -1;
            }
            length = low + r;
            int center = (over + start) / 2;
            double hudu = ((center + 1)*1.0 / (4*1.0) - 45.0) * pie / 180.0;
            double m = length * Math.Cos(hudu);
            double n = length * Math.Sin(hudu);
            int num = 0;
            for (int i = start; i < over; i++)
            {
                if (Math.Abs(Math.Sqrt(Math.Pow((x[i] - m), 2.0) + Math.Pow((y[i] - n) , 2.0)) - r) < err)
                {
                    num++;
                }
            }
            if (num * 1.0 / (over - start + 1) * 1.0 > 0.80)
            {
                cir_x = m;
                cir_y = n;
                cir_du = hudu;
                return type;
            }
            return -1;
        }
        private int draw_circle(double x,double y,int type)//将检测出的圆画出，type，0为叶子，1为圆环
        {
            double length = Math.Sqrt(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));
            if (length > maxval) { return -1; }
            double tp_cir = r * 1.0 / (maxval * 1.0);//图缩小比例
            double draw_x = centerx + x * tp_cir;
            double draw_y = centery - y * tp_cir;
            Graphics g = this.CreateGraphics();
            int leaf_r = (int)(125 * tp_cir);
            int cir_r = (int)(200 * tp_cir);
            if (type == 0)
            {
                Brush deepbluebush = new SolidBrush(Color.DeepSkyBlue);
                draw_x = draw_x - leaf_r;
                draw_y = draw_y - leaf_r;
                g.FillEllipse(deepbluebush, (int)draw_x, (int)draw_y, 2 * leaf_r, 2 * leaf_r);
            }
            else
            {
                Brush redbush = new SolidBrush(Color.Red);
                Pen redPen = new Pen(Color.Red, 0.1f);
                draw_x = draw_x - cir_r;
                draw_y = draw_y - cir_r;
                g.FillEllipse(redbush, (int)draw_x, (int)draw_y, 2 * cir_r, 2 * cir_r);
            }
            return 0;
        }

        private void search_circle_location(double x, double y, double abso_du, int p_nums_huan, double[,] final_circle_huan)
        {
            int i=0;
            int[] ans = new int[10];
            int ans_flag = 0;
            double dian_du = 0;
            double final_x, final_y;
            for (i = 0; i < p_nums_huan; i++)
            {
                double length = Math.Sqrt(Math.Pow(final_circle_huan[i, 0], 2.0) + Math.Pow(final_circle_huan[i, 1], 2.0));
                dian_du = abso_du + final_circle_huan[i, 2] - pie/2;//感觉应该是二分之派
                final_x = x + length * Math.Cos(dian_du);
                final_y = y + length * Math.Sin(dian_du);
                double dis = 0;
                double min = 10000;
                int flag = 0;
                for (int j = 0; j < 7; j++)
                {
                    dis = Math.Sqrt(Math.Pow(final_x-absolute_circle[j,0], 2.0) + Math.Pow(final_y-absolute_circle[j,1], 2.0));
                    if (dis < min)
                    {
                        min = dis;
                        flag = j;
                    }
                }
                ans[ans_flag] = flag;
                ans_flag++;
            }
            int a = 0;
            a++;
        }
        private void my_method(int huan_nums,double[,] final_circle)
        {
            //finalx.Invoke(new EventHandler(delegate { finalx.Text = "0"; }));
            //finaly.Invoke(new EventHandler(delegate { finaly.Text = "0"; }));
            if (huan_nums < 3) return;
            double[] dis = new double[2];
            dis[0] = Math.Sqrt(Math.Pow(final_circle[0, 0] - final_circle[1, 0], 2.0) + Math.Pow(final_circle[0, 1] - final_circle[1, 1], 2.0));
            dis[1] = Math.Sqrt(Math.Pow(final_circle[1, 0] - final_circle[2, 0], 2.0) + Math.Pow(final_circle[1, 1] - final_circle[2, 1], 2.0));
            double[] diff = new double[2];
            diff[0] = diff[1] = 1000;
            //int dis1 = 0, dis2 = 0;
            int[,] dis1 = new int[10,2];
            int[,] dis2 = new int[10,2];
            int [] tp_flag = new int[2];
            for (int m = 0; m < 2; m++)
            {
                for (int i = 0; i < 6; i++)
                {
                    for (int j = i + 1; j < 7; j++)
                    {
                        double tp = Math.Abs(absolute_distance[i, j] - dis[m]);
                        if (tp < diff[m])
                        {
                            if (tp < 50)
                            {
                                diff[m] = tp;
                                dis1[tp_flag[m], m] = i;
                                dis2[tp_flag[m], m] = j;
                                tp_flag[m]++;
                            }
                        }
                    }
                }
            }
            if (diff[0] == 1000 || diff[1] == 1000) return;
            double[, ,] tp_result = new double[10, 40, 2];
            for (int m = 0; m < 2; m++)
            {
                int i = 0;
                for (i = 0; i < tp_flag[m]; i++)//发现0-2 环距离与 4-6环距离非常接近，必须将所有结果包含
                {
                    if (diff[m] < 50)
                    {
                        double tp_du = Math.Acos((Math.Pow(absolute_distance[dis1[i,m], dis2[i,m]], 2.0) +
                            Math.Pow(final_circle[1 + m, 3], 2.0) - Math.Pow(final_circle[0 + m, 3], 2.0)) /
                            (2.0 * absolute_distance[dis1[i, m], dis2[i, m]] * final_circle[1 + m, 3]));
                        tp_result[m, 0 + i * 4, 0] = final_circle[1 + m, 3] * Math.Cos(absolute_hudu[dis1[i, m], dis2[i, m]] + tp_du) + absolute_circle[dis1[i, m], 0];
                        tp_result[m, 0 + i * 4, 1] = final_circle[1 + m, 3] * Math.Sin(absolute_hudu[dis1[i, m], dis2[i, m]] + tp_du) + absolute_circle[dis1[i, m], 1];
                        tp_result[m, 1 + i * 4, 0] = final_circle[1 + m, 3] * Math.Cos(absolute_hudu[dis1[i, m], dis2[i, m]] - tp_du) + absolute_circle[dis1[i, m], 0];
                        tp_result[m, 1 + i * 4, 1] = final_circle[1 + m, 3] * Math.Sin(absolute_hudu[dis1[i, m], dis2[i, m]] - tp_du) + absolute_circle[dis1[i, m], 1];
                        tp_result[m, 2 + i * 4, 0] = final_circle[1 + m, 3] * Math.Cos(absolute_hudu[dis2[i, m], dis1[i, m]] + tp_du) + absolute_circle[dis2[i, m], 0];
                        tp_result[m, 2 + i * 4, 1] = final_circle[1 + m, 3] * Math.Sin(absolute_hudu[dis2[i, m], dis1[i, m]] + tp_du) + absolute_circle[dis2[i, m], 1];
                        tp_result[m, 3 + i * 4, 0] = final_circle[1 + m, 3] * Math.Cos(absolute_hudu[dis2[i, m], dis1[i, m]] - tp_du) + absolute_circle[dis2[i, m], 0];
                        tp_result[m, 3 + i * 4, 1] = final_circle[1 + m, 3] * Math.Sin(absolute_hudu[dis2[i, m], dis1[i, m]] - tp_du) + absolute_circle[dis2[i, m], 1];
                    }
                }
            }
            double len = 0;
            double tp_len = 10000;
            int p = 0, q = 0;
            for (int i = 0; i < tp_flag[0] * 4; i++)
            {
                for (int j = 0; j < 0 + tp_flag[1] * 4; j++)
                {
                    len = Math.Sqrt(Math.Pow(tp_result[0, i, 0] - tp_result[1, j, 0], 2.0) + Math.Pow(tp_result[0, i, 1] - tp_result[1, j,1], 2.0));
                    if (len < tp_len)
                    {
                        p = i;
                        q = j;
                        tp_len = len;
                    }
                }
            }
            double final_x = (tp_result[0, p, 0] + tp_result[1, q, 0]) / 2.0;
            double final_y = (tp_result[0, p, 1] + tp_result[1, q, 1]) / 2.0;
            //finalx.Invoke(new EventHandler(delegate { finalx.Text = final_x.ToString(); }));
            //finaly.Invoke(new EventHandler(delegate { finaly.Text = final_y.ToString(); }));
        }
        private int detect_z0(int huan_nums, double[,] final_circle,double vx,double vy,double dis,int num)
        {
            if (huan_nums < num) return -1;
            double[] x = new double[num];
            double[] y = new double[num];
            int flag = 0;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 3] < dis)
                {
                    x[flag] = final_circle[i, 0];
                    y[flag] = final_circle[i, 1];
                    flag++;
                }
                if (flag == num) break;
            }
            if (flag != num) return -1;
            double value_y = (y[0] + y[1] + y[2]) / 3;
            double value_x = x[2];
            double tp_x = vx - value_x;
            double tp_y = value_y - vy;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < 500 && Math.Abs(send_y) < 500 && serialPort1.IsOpen)
            {
                sender2(send_x, send_y, 0);
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
            if (send_x < 10 && send_y < 10)
            {
                return 1;
            }
            return 0;
        }
        private int detect_z0_blue(int huan_nums, double[,] final_circle, double vx, double vy, double dis, int num)
        {
            if (huan_nums < num) return -1;
            double[] x = new double[num];
            double[] y = new double[num];
            int flag = 0;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 3] < dis)
                {
                    x[flag] = final_circle[i, 0];
                    y[flag] = final_circle[i, 1];
                    flag++;
                }
                if (flag == num) break;
            }
            if (flag != num) return -1;
            double value_y = (y[0] + y[1] + y[2]) / 3;
            double value_x = x[0];
            double tp_x = vx - value_x;
            double tp_y = value_y - vy;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < 500 && Math.Abs(send_y) < 500 && serialPort1.IsOpen)
            {
                sender2(send_x, send_y, 0);
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
            if (send_x < 10 && send_y < 10)
            {
                return 1;
            }
            return 0;
        }
        private int detect_z1(int huan_nums, double[,] final_circle, double vx, double vy, double dis)
        {
            if (huan_nums < 1) return -1;
            double x = 0, y = 0;
            int flag = 0;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 3] < dis)
                {
                    x = final_circle[i, 0];
                    y = final_circle[i, 1];
                    dis = final_circle[i, 3];
                    flag = 1;
                }
            }
            if(flag == 0) return -1;
            double tp_x = vx - x;
            double tp_y = y - vy;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < 1000 && Math.Abs(send_y) < 1000 && serialPort1.IsOpen)
            {
                sender2(send_x, send_y, 0);
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
            if (send_x < 10 && send_y < 10)
            {
                return 1;
            }
            return 0;
        }
        private void detect_s0(int huan_nums, double[,] final_circle, double vx, double vy, double dis)
        {
            if (huan_nums < 1) return;
            double x=0,y=0;
            double tp = -10000;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 0] > tp)
                {
                    x = final_circle[i, 0];
                    y = final_circle[i, 1];
                    tp = final_circle[i,0];
                }
            }
            double tp_x = vx - x;
            double tp_y = y - vy;
            if (tp_y < -10) tp_y = tp_y * 5;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < dis && Math.Abs(send_y) < dis)
            {
                string send_data = send_x.ToString() + " " + send_y.ToString();
                IPAddress localIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65520"));
                sendudp = new UdpClient(localIpEndPoint);
                string message = send_data;
                byte[] sendbytes = Encoding.Unicode.GetBytes(message);
                IPAddress remoteIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIp, int.Parse("65500"));
                sendudp.Send(sendbytes, sendbytes.Length, remoteIpEndPoint);
                sendudp.Close();
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
        }
        private void detect_s0_blue(int huan_nums, double[,] final_circle, double vx, double vy, double dis)
        {
            if (huan_nums < 1) return;
            double x = 0, y = 0;
            double tp = 10000;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 0] < tp)
                {
                    x = final_circle[i, 0];
                    y = final_circle[i, 1];
                    tp = final_circle[i, 0];
                }
            }
            double tp_x = vx - x;
            double tp_y = y - vy;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < dis && Math.Abs(send_y) < dis)
            {
                string send_data = send_x.ToString() + " " + send_y.ToString();
                IPAddress localIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65520"));
                sendudp = new UdpClient(localIpEndPoint);
                string message = send_data;
                byte[] sendbytes = Encoding.Unicode.GetBytes(message);
                IPAddress remoteIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIp, int.Parse("65500"));
                sendudp.Send(sendbytes, sendbytes.Length, remoteIpEndPoint);
                sendudp.Close();
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
        }
        private int detect_s1(int huan_nums, double[,] final_circle, double vx, double vy, double dis)
        {
            if (huan_nums < 1) return -1;
            double x = 0, y = 0;
            int flag = 0;
            for (int i = 0; i < huan_nums; i++)
            {
                if (s_step != 5)
                {
                    if (Math.Abs(final_circle[i, 0]) < dis)
                    {
                        x = final_circle[i, 0];
                        y = final_circle[i, 1];
                        dis = final_circle[i, 0];
                        flag = 1;
                    }
                }
                else
                {
                    if (Math.Abs(final_circle[i, 3]) < dis)
                    {
                        x = final_circle[i, 0];
                        y = final_circle[i, 1];
                        dis = final_circle[i, 3];
                        flag = 1;
                    }
                }
            }
            if (flag == 0) return -1;
            double tp_x = vx - x;
            double tp_y = y - vy;
            short send_x = Convert.ToInt16(tp_x);
            short send_y = Convert.ToInt16(tp_y);
            if (Math.Abs(send_x) < 500 && Math.Abs(send_y) < 500)
            {
                string send_data = send_x.ToString() + " " + send_y.ToString();
                IPAddress localIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65520"));
                sendudp = new UdpClient(localIpEndPoint);
                string message = send_data;
                byte[] sendbytes = Encoding.Unicode.GetBytes(message);
                IPAddress remoteIp = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIp, int.Parse("65500"));
                sendudp.Send(sendbytes, sendbytes.Length, remoteIpEndPoint);
                sendudp.Close();
            }
            finalx.Invoke(new EventHandler(delegate { finalx.Text = tp_x.ToString(); }));
            finaly.Invoke(new EventHandler(delegate { finaly.Text = tp_y.ToString(); }));
            if (send_x < 10 && send_y < 10)
            {
                return 1;
            }
            return 0;
        }
        private int detect_reset(int huan_nums, double[,] final_circle, out double reset_x,out double reset_y)
        {
            reset_x = -1;
            reset_y = -1;
            if (huan_nums < 1) return -1;
            double x = 0, y = 0;
            int flag = 0;
            double tp = 10000;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 3] < tp)
                {
                    x = final_circle[i, 0];
                    y = final_circle[i, 1];
                    tp = final_circle[i, 3];
                    flag = 1;       
                }
            }
            if (flag == 0) return -1;
            reset_x = x;
            reset_y = y;
            return 1;
        }

        private void sender2(Int16 data1, Int16 data2, Int16 data3)     //发送坐标
        {
            sendByte3 = BitConverter.GetBytes(data1);       //x坐标转换
            sendByte2[2] = (byte)(sendByte3[0]);
            sendByte2[3] = (byte)(sendByte3[1]);

            sendByte4 = BitConverter.GetBytes(data2);       //y坐标转换
            sendByte2[4] = (byte)(sendByte4[0]);
            sendByte2[5] = (byte)(sendByte4[1]);

            sendByte7 = BitConverter.GetBytes(data3);       //y坐标转换
            sendByte2[6] = (byte)(sendByte7[0]);
            sendByte2[7] = (byte)(sendByte7[1]);

            Check_6 = (Int16)(sendByte2[0] + sendByte2[1] + sendByte2[2] + sendByte2[3] + sendByte2[4] + sendByte2[5] + sendByte2[6] + sendByte2[7]);
            sendByte6 = BitConverter.GetBytes(Check_6);
            sendByte2[8] = (byte)(sendByte6[0]);
            sendByte2[9] = (byte)(sendByte6[1]);


            try
            {
                serialPort1.Write(sendByte2, 0, 10);
            }
            catch (Exception err)
            {
                //MessageBox.Show(err.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                try
                {
                    int ok = open_serialPort();
                    if (ok == 0) return;

                    cmbPortName.Enabled = false;

                    button1.Text = "断开连接";

                }
                catch (System.IO.IOException) { } //可以进一步扩展
            }
            else
            {
                serialPort1.Close();
                button1.Text = "连接";
                cmbPortName.Enabled = true;

                //关闭串口时要做的处理
                //tmrAutoSend.Enabled = false;
            }
        }
        private int open_serialPort()
        {
            int ok = 0;
            try
            {
                //设置serialPort1的串口属性
                serialPort1.PortName = cmbPortName.Text;
                serialPort1.WriteBufferSize = 4096;
                serialPort1.BaudRate = 115200;
                //打开串口
                serialPort1.Open();
                ok = 1;
            }
            catch (Exception exc)
            {
                ok = 0;
            }
            return ok;
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            DataReceivedEventHandler DataReceive = new DataReceivedEventHandler(this.TextDataReceive);
            DataReceive();
        }
        private void TextDataReceive()
        {
            if (z_or_s != 1) z_or_s = 1;
            String strReceiveData = serialPort1.ReadExisting();
            int tp_z_step = strReceiveData[0];
            if (tp_z_step != z_step)
            {
                z_step = tp_z_step;
            }
            text_step.Invoke(new EventHandler(delegate { text_step.Text = z_step.ToString(); }));
        }

        private void drawimg_Click(object sender, EventArgs e)
        {
            if (draw_data_flag == 0)
            {
                draw_data_flag = 1;
                drawimg.Text = "停止画图";
            }
            else
            {
                draw_data_flag = 0;
                drawimg.Text = "开始画图";
            }

        }
        private void receiveSStep(object obj)
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // 关闭receiveUdpClient时此时会产生异常
                    byte[] receiveBytes = receiveudp.Receive(ref remoteIpEndPoint);
                    string message = Encoding.Unicode.GetString(receiveBytes);
                    int tpstep = Int32.Parse(message);
                    if (tpstep != s_step) s_step = tpstep;
                    if (z_or_s != 2) z_or_s = 2;
                }
                catch
                {
                    continue;
                }
            }
        }
        private int detect_sstep(int huan_nums, double[,] final_circle)
        {
            int flag1 = 0, flag2 = 0, flag3 = 0, flag4 = 0, flag5 = 0, flag6 = 0, flag7 = 0, flag8 = 0, flag9 = 0;
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 3] < 1800 && final_circle[i, 3] > 1200)
                {
                    flag1 = 1;
                    continue;
                }
                if (final_circle[i, 0] < -300 && final_circle[i, 0] > -800)
                {
                    flag2 = 1;
                    continue;
                }
                if (final_circle[i, 0] < -1515 && final_circle[i, 0] > -2115 && final_circle[i, 1] < 945 && final_circle[i, 1] > 345)
                {
                    flag6 = 1;
                    continue;
                }
            }
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 0] < 300 && final_circle[i, 0] > -300 && final_circle[i, 1] > 445 && final_circle[i, 1] < 845)
                {
                    flag3 = 1;
                }
            }
            for (int i = 0; i < huan_nums; i++)
            {
                if (final_circle[i, 0] < 400 && final_circle[i, 0] > -400&&final_circle[i,1]>700&&final_circle[i,1]<1300)
                {
                    flag4 = 1;
                }
                if (final_circle[i, 0] > 400)
                {
                    flag5 = 1;
                }
                if (final_circle[i, 0] < -2109 && final_circle[i, 0] > -2709 && final_circle[i, 1] > 1946 && final_circle[i, 1] < 2546)
                {
                    flag7 = 1;
                }
                if (final_circle[i, 0] < 105 && final_circle[i, 0] > -505 && final_circle[i, 1] > 1933 && final_circle[i, 1] < 2533)
                {
                    flag8 = 1;
                }
                if (final_circle[i, 3] < 3444 && final_circle[i, 3] > 2844)
                {
                    flag9 = 1;
                }
            }
            if (flag6 == 1) s_step = 5;
            if (flag4 == 1 && flag5 == 1) s_step = 4;
            if (flag1 == 1 && flag2 == 1 && flag5 == 0)
            {
                s_step = 2;
            }
            if (flag2 == 0 && flag1 == 0 && flag3 == 1 && flag5 == 0)
            {
                s_step = 3;
            }
            if (huan_nums == 0)
            {
                s_step = 1;
            }
            //if ((flag4 == 1 && flag8 == 1)) s_step = 6;//(flag4 == 1 && flag7 == 1) ||
            text_step.Invoke(new EventHandler(delegate { text_step.Text = s_step.ToString(); }));
            return 0;
        }
        private void readLocation()
        {
            StreamReader locreader = new StreamReader("config.txt");
            string sLine = "";
            sLine = locreader.ReadLine(); //略过第一行场地与机器人类型配置
            configstr = sLine;
            sLine = locreader.ReadLine();//略过第二行数据说明
            string [] tpline = new string[20];
            int i = 0;
            while (sLine != null)
            {
                sLine = locreader.ReadLine();
                if (sLine != null)
                {
                    tpline = sLine.Split('\t');
                    z_step_loc[0, i, 0] = Double.Parse(tpline[1]);
                    z_step_loc[0, i, 1] = Double.Parse(tpline[2]);
                    s_step_loc[0, i, 0] = Double.Parse(tpline[3]);
                    s_step_loc[0, i, 1] = Double.Parse(tpline[4]);
                    z_step_loc[1, i, 0] = Double.Parse(tpline[5]);
                    z_step_loc[1, i, 1] = Double.Parse(tpline[6]);
                    s_step_loc[1, i, 0] = Double.Parse(tpline[7]);
                    s_step_loc[1, i, 1] = Double.Parse(tpline[8]);
                    i++;

                }
            }
            locreader.Close();
        }
        private void bluered_Click(object sender, EventArgs e)
        {
            if (blue_or_red == 2)
            {
                bluered.Text = "红场";
                blue_or_red = 1;
            }
            else
            {
                bluered.Text = "蓝场";
                blue_or_red = 2;
            }
        }

        private void resetdata_Click(object sender, EventArgs e)
        {
            if (resetstep.Text == "")
                return;
            int step_str = Int32.Parse(resetstep.Text);
            if (step_str < 2) return;
            text_x.Text = "wait";
            text_y.Text = "wait";
            reset_location_flag = 1;
            resetdata.Text = "计算中";
            resetdata.Enabled = false;
        }
        private void writeconfigfile()
        {
            StreamWriter locwriter = new StreamWriter("config.txt");
            string writeline = "";
            locwriter.WriteLine(configstr);
            locwriter.WriteLine("步骤	红自动x	红自动y	红手动x	红手动y	蓝自动x	蓝自动y	蓝手动x	蓝手动y");
            for (int i = 0; i < 19; i++)
            {
                writeline = (i + 2).ToString() + '\t' +
                    z_step_loc[0, i, 0].ToString() + '\t' + z_step_loc[0, i, 1].ToString() + '\t' +
                    s_step_loc[0, i, 0].ToString() + '\t' + s_step_loc[0, i, 1].ToString() + '\t' +
                    z_step_loc[1, i, 0].ToString() + '\t' + z_step_loc[1, i, 1].ToString() + '\t' +
                    s_step_loc[1, i, 0].ToString() + '\t' + s_step_loc[1, i, 1].ToString();
                locwriter.WriteLine(writeline);
            }
            locwriter.Close();
        }
        private void quickstart()
        {
            try
            {
                //设置serialPort1的串口属性
                serialPort1.PortName = "com23";
                serialPort1.WriteBufferSize = 4096;
                serialPort1.BaudRate = 115200;
                //打开串口
                serialPort1.Open();
                cmbPortName.Enabled = false;
                for (int i = 0; i < cmbPortName.Items.Count; i++)
                {
                    if (cmbPortName.Items.IndexOf(i).ToString() == "COM23")
                    {
                        cmbPortName.SelectedIndex = i;
                    }
                }
                button1.Text = "断开连接";
            }
            catch (Exception exc)
            {
                
            }
        }
        private void heartmsg(int id)
        {
            string send_data = "msg"+ id.ToString();
            IPAddress localIp = IPAddress.Parse("127.0.0.1");
            IPEndPoint localIpEndPoint = new IPEndPoint(localIp, int.Parse("65399"));
            sendudp = new UdpClient(localIpEndPoint);
            string message = send_data;
            byte[] sendbytes = Encoding.Unicode.GetBytes(message);
            IPAddress remoteIp = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIp, int.Parse("65400"));
            sendudp.Send(sendbytes, sendbytes.Length, remoteIpEndPoint);
            sendudp.Close();
        }
        private void heart(object obj)
        {
            Process pro = Process.GetCurrentProcess();
            int i = 0;
            while (true)
            {
                i++;
                Thread.Sleep(100);
                if (i == 5) break;
                heartmsg(pro.Id);
            }
        }
    }
}