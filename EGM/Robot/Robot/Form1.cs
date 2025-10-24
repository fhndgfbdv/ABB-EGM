using abb.egm;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.IOSystemDomain;
using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TigRotX_ABB;
using static ForceControlOf_EGM.Common;

namespace Robot
{
    public partial class Form1 : Form
    {
        //定义实时显示
        private System.Windows.Forms.Timer DisplayTimer;
        //机器人网络扫描器NetworkScanner类实例化对象scanner
        private NetworkScanner scanner = new NetworkScanner();

        //机器人控制器Controller类实例化对象controller1
        private Controller controller1 = null;

        //机器人控制器RapidTask类实例化对象为数组tasks
        private ABB.Robotics.Controllers.RapidDomain.Task[] tasks = null;
        private bool IsControlLogin;

        //以太网变量UDP
        private UdpClient UDPClient;
        private Thread UDPThread;
        private bool ExitThread = false;
        private bool IsUDPConnectRobot = false;
        private uint _seqNumber = 0;
        private static ABBRobotPos ReceiveRobotInfor = new ABBRobotPos();
        //纠偏距离
        private CartesianCorrection CurCorrection;
        private Thread CorrectThread;
        private bool EGMCorrectStart = false;
        private Num  modeChoose;
        public Form1()
        {
            InitializeComponent();
            //实时显示
            this.DisplayTimer = new System.Windows.Forms.Timer();
            this.DisplayTimer.Interval = 50;
            this.DisplayTimer.Tick += DisplayTimer_Tick;
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (IsControlLogin == true)
            {
                leb_Robot.Value = true;
            }
            else
            {
                leb_Robot.Value = false;
            }
            //如果定时事件被激活，就执行里面的代码
            if (DisplayTimer.Enabled == true && IsControlLogin == true)
            {
                label_actual.Text = controller1.MotionSystem.SpeedRatio.ToString();
                hScrollBar_speed.Value = controller1.MotionSystem.SpeedRatio;
                RobotAxisAngel();
                RobotAxisTorque();
                RobotAxisSpeed();
                RobotWorldPosition();
                RobotEGMReceive();
            }
        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            scanner.Scan();
            listView1.Items.Clear();

            ControllerInfoCollection controls = scanner.Controllers;
            foreach (ControllerInfo info in controls)
            {
                ListViewItem item = new ListViewItem(info.SystemName);
                item.SubItems.Add(info.IPAddress.ToString());
                item.Tag = info;
                listView1.Items.Add(item);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewItem item1 = this.listView1.SelectedItems[0];
            if (item1.Tag != null)
            {
                ControllerInfo controllerInfo1 = (ControllerInfo)item1.Tag;
                if (controllerInfo1.Availability == Availability.Available)
                {
                    if (this.controller1 != null)
                    {
                        this.controller1.Logoff();
                        this.controller1.Dispose();
                        this.controller1 = null;
                    }

                    controller1 = Controller.Connect(controllerInfo1, ConnectionType.Standalone);
                    this.controller1.Logon(UserInfo.DefaultUser);
                    subscribe_value();
                    IsControlLogin = true;
                    DisplayTimer.Enabled = true;
                    MessageBox.Show("成功登录： " + controllerInfo1.SystemName);
                }
                else
                {
                    MessageBox.Show("控制器连接失败！");
                }
            }

        }
        private void subscribe_value()
        {
            Signal rsignal;
            rsignal = controller1.IOSystem.GetSignal("ao_speed");
            rsignal.Changed += new EventHandler<SignalChangedEventArgs>(io_StateChanged);
        }
        private void io_StateChanged(object sender, SignalChangedEventArgs e)
        {
            //显示速度实时变化
            this.Invoke(new EventHandler(DisplaySpeed), sender, e);
        }
        private void DisplaySpeed(object sender, System.EventArgs e)
        {
            Signal s = (Signal)sender;
            label_CurSpeed.Text = Math.Round(s.Value * 1000).ToString();
            //速度输出模拟量单位 米/秒，转化为mm/s
        }
        private void btn_Start_Click(object sender, EventArgs e)
        {
            try
            {
                //请求当前控制器Rapid的权限
                using (Mastership m = Mastership.Request(controller1.Rapid))
                {
                    //启动控制器的Rapid程序
                    controller1.Rapid.Start();
                }
            }
            catch (Exception ex)
            {
                //显示出错信息
                MessageBox.Show(ex.ToString());

            }

        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            try
            {
                //请求当前控制器Rapid的权限
                using (Mastership m = Mastership.Request(controller1.Rapid))
                {
                    //停止控制器的Rapid程序
                    controller1.Rapid.Stop(StopMode.Immediate);

                }
            }
            catch (Exception ex)
            {
                //显示出错信息
                MessageBox.Show(ex.ToString());

            }
        }

        private void btn_MotorOn_Click(object sender, EventArgs e)
        {
            try
            {
                //判断控制器是否自动模式
                if (controller1.OperatingMode == ControllerOperatingMode.Auto)
                {
                    //上电操作
                    controller1.State = ControllerState.MotorsOn;
                    MessageBox.Show("上电成功");
                }
                //不是自动模式的处理
                else
                {
                    MessageBox.Show("请切换到自动模式");
                }
            }
            //当发生上电异常时的处理
            catch (System.Exception ex)
            {
                MessageBox.Show("异常处理：" + ex.Message);
            }
        }

        private void btn_MotorOff_Click(object sender, EventArgs e)
        {
            try
            {
                //判断控制器是否自动模式
                if (controller1.OperatingMode == ControllerOperatingMode.Auto)
                {
                    //下电操作
                    controller1.State = ControllerState.MotorsOff;
                    MessageBox.Show("下电完成");
                }
                //当发生下电异常时的处理
                else
                {
                    MessageBox.Show("请切换到自动模式");
                }
            }
            //当发生下电异常时的处理
            catch (System.Exception ex)
            {
                MessageBox.Show("异常处理：" + ex.Message);
            }
        }

        private void btn_PPtoMAIN_Click(object sender, EventArgs e)
        {
            try
            {
                //判断控制器是否自动模式
                if (controller1.OperatingMode == ControllerOperatingMode.Auto)
                {
                    //将控制器里的Rapid任务集合提取到tasks
                    tasks = controller1.Rapid.GetTasks();
                    //请求当前控制器Rapid的权限
                    using (Mastership m = Mastership.Request(controller1.Rapid))
                    {
                        //将控制器的第一个机器人运动任务的指针复位
                        tasks[0].ResetProgramPointer();
                        MessageBox.Show("程序指针已复位");
                    }
                }
                else
                {
                    MessageBox.Show("请切换到自动模式");
                }
            }
            //没有获得控制权时的异常处理
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("权限被其它客户端占有" + ex.Message);
            }
            //当发生指针复位异常时的处理
            catch (System.Exception ex)
            {
                MessageBox.Show("异常处理：" + ex.Message);
            }
        }



        private void hScrollBar_speed_Scroll(object sender, ScrollEventArgs e)
        {
            //显示滚动框的值
            label_setspeed.Text = hScrollBar_speed.Value.ToString() + "%";
            if (IsControlLogin == true)
            {
                controller1.MotionSystem.SpeedRatio = hScrollBar_speed.Value;

                //    using (Mastership m = Mastership.Request(controller1))
                //    {
                //        //弹出对话框，确认速率
                //        DialogResult DR = MessageBox.Show("确认修改为" + label_setspeed.Text + "吗？", "CONFIRM", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                //        //点击确认的话，就将速率写入控制器
                //        if (DR == DialogResult.OK)
                //        {
                //            controller1.MotionSystem.SpeedRatio = hScrollBar_speed.Value;

                //        }
                //    }
            }
        }




        //用于获取轴角度的方法
        private void RobotAxisAngel()
        {
            //获取当前机器人轴1-6的角度
            JointTarget JointActual = controller1.MotionSystem.ActiveMechanicalUnit.GetPosition();
            labelAxis1.Text = "Axis1: " + JointActual.RobAx.Rax_1.ToString(format: "#0.00");
            labelAxis2.Text = "Axis2: " + JointActual.RobAx.Rax_2.ToString(format: "#0.00");
            labelAxis3.Text = "Axis3: " + JointActual.RobAx.Rax_3.ToString(format: "#0.00");
            labelAxis4.Text = "Axis4: " + JointActual.RobAx.Rax_4.ToString(format: "#0.00");
            labelAxis5.Text = "Axis5: " + JointActual.RobAx.Rax_5.ToString(format: "#0.00");
            labelAxis6.Text = "Axis6: " + JointActual.RobAx.Rax_6.ToString(format: "#0.00");

        }


        //用于获取大地坐标的方法
        private void RobotWorldPosition()
        {
            //声明变量用于暂存欧拉角数据
            double RX;
            double RY;
            double RZ;

            //获取机器人当前大地坐标数据
            RobTarget RobActual = controller1.MotionSystem.ActiveMechanicalUnit.GetPosition(CoordinateSystemType.World);
            labelX.Text = "X : " + RobActual.Trans.X.ToString(format: "#0.00");
            labelY.Text = "Y : " + RobActual.Trans.Y.ToString(format: "#0.00");
            labelZ.Text = "Z : " + RobActual.Trans.Z.ToString(format: "#0.00");
            //将描述姿态的四元数用方法转换为欧拉角表示
            RobActual.Rot.ToEulerAngles(out RX, out RY, out RZ);
            labelRX.Text = "EX: " + RX.ToString(format: "#0.00");
            labelRY.Text = "EY: " + RY.ToString(format: "#0.00");
            labelRZ.Text = "EZ: " + RZ.ToString(format: "#0.00");

        }

        private void RobotAxisSpeed()
        {
            RapidData rd = controller1.Rapid.GetRapidData("pc", "module1", "axis_speed_arr");
            if (rd.IsArray)
            {
                ArrayData ad = (ArrayData)rd.Value;
                float t1 = Convert.ToSingle(ad[1].ToString());
                label_A1Speed.Text = t1.ToString(format: "#0.000");
                float t2 = Convert.ToSingle(ad[1].ToString());
                label_A2Speed.Text = t1.ToString(format: "#0.000");
                float t3 = Convert.ToSingle(ad[1].ToString());
                label_A3Speed.Text = t1.ToString(format: "#0.000");
                float t4 = Convert.ToSingle(ad[1].ToString());
                label_A4Speed.Text = t1.ToString(format: "#0.000");
                float t5 = Convert.ToSingle(ad[1].ToString());
                label_A5Speed.Text = t1.ToString(format: "#0.000");
                float t6 = Convert.ToSingle(ad[1].ToString());
                label_A6Speed.Text = t1.ToString(format: "#0.000");
            }
        }

        private void RobotAxisTorque()
        {
            RapidData rd = controller1.Rapid.GetRapidData("pc", "module1", "axis_tor_arr");
            if (rd.IsArray)
            {
                ArrayData ad = (ArrayData)rd.Value;
                float t1 = Convert.ToSingle(ad[1].ToString());
                label_A1Torque.Text = t1.ToString(format: "#0.000");
                float t2 = Convert.ToSingle(ad[1].ToString());
                label_A2Torque.Text = t1.ToString(format: "#0.000");
                float t3 = Convert.ToSingle(ad[1].ToString());
                label_A3Torque.Text = t1.ToString(format: "#0.000");
                float t4 = Convert.ToSingle(ad[1].ToString());
                label_A4Torque.Text = t1.ToString(format: "#0.000");
                float t5 = Convert.ToSingle(ad[1].ToString());
                label_A5Torque.Text = t1.ToString(format: "#0.000");
                float t6 = Convert.ToSingle(ad[1].ToString());
                label_A6Torque.Text = t1.ToString(format: "#0.000");
            }
        }

        private void RobotEGMReceive()
        {
            if (IsUDPConnectRobot)
            {
                leb_EGM.Value = true;
                txt_RobotX.Text = ReceiveRobotInfor.X.ToString(format: "#0.00000");
                txt_RobotY.Text = ReceiveRobotInfor.Y.ToString(format: "#0.00000");
                txt_RobotZ.Text = ReceiveRobotInfor.Z.ToString(format: "#0.00000");
                txt_RobotRx.Text = ReceiveRobotInfor.Rx.ToString(format: "#0.00000");
                txt_RobotRy.Text = ReceiveRobotInfor.Ry.ToString(format: "#0.00000");
                txt_RobotRz.Text = ReceiveRobotInfor.Rz.ToString(format: "#0.00000");
                txt_RobotA1.Text = ReceiveRobotInfor.A1.ToString(format: "#0.00000");
                txt_RobotA2.Text = ReceiveRobotInfor.A2.ToString(format: "#0.00000");
                txt_RobotA3.Text = ReceiveRobotInfor.A3.ToString(format: "#0.00000");
                txt_RobotA4.Text = ReceiveRobotInfor.A4.ToString(format: "#0.00000");
                txt_RobotA5.Text = ReceiveRobotInfor.A5.ToString(format: "#0.00000");
                txt_RobotA6.Text = ReceiveRobotInfor.A6.ToString(format: "#0.00000");
            }
            else
            {
                leb_EGM.Value = false;
            }

        }


        private void ShowMsg(string str)
        {
            if (MSG_BOX.InvokeRequired)
            {
                MSG_BOX.BeginInvoke(new Action<string>(ShowMsg), str);
            }
            else
            {
                MSG_BOX.AppendText(DateTime.Now + "  " + str + " \r\n");
            }
        }

        private void btn_EGM_Connect_Click(object sender, EventArgs e)
        {
            UDPThread = new Thread(new ThreadStart(UDPCheckListening));
            UDPThread.Priority = ThreadPriority.Highest;
            UDPThread.IsBackground = true;
            UDPThread.Start();
            ExitThread = false;
        }

        private void btn_EGM_DisConnect_Click(object sender, EventArgs e)
        {
            if (IsUDPConnectRobot)
            {
                UDPClient.Close();
                UDPThread.Abort();
            }
            ExitThread = true;
            IsUDPConnectRobot = false;
            ShowMsg("EGM 断开连接成功");
        }

        private void UDPCheckListening()
        {
            int Port = Convert.ToInt32(this.txt_PortOfRobot.Text);
            // create an udp client and listen on any address and the port _ipPortNumber
            try
            {
                UDPClient = new UdpClient(Port);
            }
            catch (SocketException)
            {
                ShowMsg("UDPClient 正在连接，其端口号： " + Port);
                return;
            }

            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, Port);
            ShowMsg("EGM 开始监听 " + Port);
            IsUDPConnectRobot = true;
            while (ExitThread == false)
            {
                // get the message from robot
                try
                {
                    var data = UDPClient.Receive(ref remoteEP);
                    if (data != null)
                    {

                        // de-serialize inbound message from robot
                        EgmRobot robot = EgmRobot.CreateBuilder().MergeFrom(data).Build();

                        // display inbound message
                        DisplayInboundMessage(robot);

                        // create a new outbound sensor message
                        EgmSensor.Builder sensor = EgmSensor.CreateBuilder();
                        CreateSensorMessage(sensor);
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            EgmSensor sensorMessage = sensor.Build();
                            sensorMessage.WriteTo(memoryStream);
                            Console.WriteLine(sensorMessage.ToString());
                            //send the udp message to the robot
                            int bytesSent = UDPClient.Send(memoryStream.ToArray(), (int)memoryStream.Length, remoteEP);
                            if (bytesSent < 0)
                            {
                                Console.WriteLine("Error send to robot");
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    return;
                }
            }
        }


        private void DisplayInboundMessage(EgmRobot robot)
        {
            double RX;
            double RY;
            double RZ;

            if (robot.HasHeader && robot.Header.HasSeqno && robot.Header.HasTm)
            {
                Console.WriteLine("Seq={0} tm={1}  pos= {2}", robot.Header.Seqno.ToString(), robot.Header.Tm.ToString(), robot.FeedBack.Cartesian.Pos);
                RobTarget TemRobActual = new RobTarget();
                TemRobActual.Trans.X = (float)robot.FeedBack.Cartesian.Pos.X;
                TemRobActual.Trans.Y = (float)robot.FeedBack.Cartesian.Pos.Y;
                TemRobActual.Trans.Z = (float)robot.FeedBack.Cartesian.Pos.Z;
                TemRobActual.Rot.Q1 = robot.FeedBack.Cartesian.Orient.U0;
                TemRobActual.Rot.Q2 = robot.FeedBack.Cartesian.Orient.U1;
                TemRobActual.Rot.Q3 = robot.FeedBack.Cartesian.Orient.U2;
                TemRobActual.Rot.Q4 = robot.FeedBack.Cartesian.Orient.U3;
                ReceiveRobotInfor.X = robot.FeedBack.Cartesian.Pos.X;
                ReceiveRobotInfor.Y = robot.FeedBack.Cartesian.Pos.Y;
                ReceiveRobotInfor.Z = robot.FeedBack.Cartesian.Pos.Z;
                ReceiveRobotInfor.Q1 = robot.FeedBack.Cartesian.Orient.U0;
                ReceiveRobotInfor.Q2 = robot.FeedBack.Cartesian.Orient.U1;
                ReceiveRobotInfor.Q3 = robot.FeedBack.Cartesian.Orient.U2;
                ReceiveRobotInfor.Q4 = robot.FeedBack.Cartesian.Orient.U3;
                TemRobActual.Rot.ToEulerAngles(out RX, out RY, out RZ);
                ReceiveRobotInfor.Rx = RX;
                ReceiveRobotInfor.Ry = RY;
                ReceiveRobotInfor.Rz = RZ;
                ReceiveRobotInfor.A1 = robot.FeedBack.Joints.JointsList[0];
                ReceiveRobotInfor.A2 = robot.FeedBack.Joints.JointsList[1];
                ReceiveRobotInfor.A3 = robot.FeedBack.Joints.JointsList[2];
                ReceiveRobotInfor.A4 = robot.FeedBack.Joints.JointsList[3];
                ReceiveRobotInfor.A5 = robot.FeedBack.Joints.JointsList[4];
                ReceiveRobotInfor.A6 = robot.FeedBack.Joints.JointsList[5];
            }
            else
            {
                Console.WriteLine("No header in robot message");
            }
        }


        private void CreateSensorMessage(EgmSensor.Builder sensor)
        {
            // create a header
            EgmHeader.Builder hdr = new EgmHeader.Builder();
            hdr.SetSeqno(_seqNumber++).SetTm((uint)DateTime.Now.Ticks).SetMtype(EgmHeader.Types.MessageType.MSGTYPE_CORRECTION);

            sensor.SetHeader(hdr);

            // create some sensor data
            EgmPlanned.Builder planned = new EgmPlanned.Builder();
            EgmPose.Builder pos = new EgmPose.Builder();
            EgmQuaternion.Builder pq = new EgmQuaternion.Builder();
            EgmCartesian.Builder pc = new EgmCartesian.Builder();
            EgmJoints.Builder joint = new EgmJoints.Builder();
            //"笛卡尔模式"
            if (btn_EgmMode.Text == "笛卡尔模式" && EGMCorrectStart == true )
            {
                pc.SetX(ReceiveRobotInfor.X + CurCorrection.X)
                  .SetY(ReceiveRobotInfor.Y + CurCorrection.Y)
                  .SetZ(ReceiveRobotInfor.Z + CurCorrection.Z);
                double Rx = ReceiveRobotInfor.Rx + CurCorrection.Rx;
                double Ry = ReceiveRobotInfor.Ry + CurCorrection.Ry;
                double Rz = ReceiveRobotInfor.Rz + CurCorrection.Rz;
                EulerAngles TempEulerAngles = new EulerAngles(Rx, Ry, Rz);
                Quaternion TempTargetQuaternion = TempEulerAngles.ToQuaternion();
                double q1 = TempTargetQuaternion.W;
                double q2 = TempTargetQuaternion.X;
                double q3 = TempTargetQuaternion.Y;
                double q4 = TempTargetQuaternion.Z;

                pq.SetU0(q1)
                    .SetU1(q2)
                    .SetU2(q3)
                    .SetU3(q4);

                //pq.SetU0(1)
                //   .SetU1(0)
                //   .SetU2(0)
                //   .SetU3(0);

                pos.SetPos(pc).SetOrient(pq);

                planned.SetCartesian(pos);  // bind pos object to planned
                sensor.SetPlanned(planned); // bind planned to sensor object
            }

            //"关节模式"
            if (btn_EgmMode.Text == "关节模式" && EGMCorrectStart == true )
            {

                double j1 = Math.Round(ReceiveRobotInfor.A1 + CurCorrection.A1, 3);
                double j2 = Math.Round(ReceiveRobotInfor.A2 + CurCorrection.A2, 3);
                double j3 = Math.Round(ReceiveRobotInfor.A3 + CurCorrection.A3, 3);
                double j4 = Math.Round(ReceiveRobotInfor.A4 + CurCorrection.A4, 3);
                double j5 = Math.Round(ReceiveRobotInfor.A5 + CurCorrection.A5, 3);
                double j6 = Math.Round(ReceiveRobotInfor.A6 + CurCorrection.A6, 3);
                for (int i = 0; i < 6; i++)
                {
                    joint.AddJoints(i);
                }
                joint.SetJoints(0, j1)
                     .SetJoints(1, j2)
                     .SetJoints(2, j3)
                     .SetJoints(3, j4)
                     .SetJoints(4, j5)
                     .SetJoints(5, j6);
                planned.SetJoints(joint);
                sensor.SetPlanned(planned);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.controller1 != null)
            {
                //对controller1进行登出、清空
                this.controller1.Logoff();
                this.controller1.Dispose();
                this.controller1 = null;
            }
            IsControlLogin = false;
            IsUDPConnectRobot = false;
        }

        private void btn_CorrectStart_Click(object sender, EventArgs e)
        {
            //this.SendTimer.Enabled = true;
            if (IsUDPConnectRobot)
            {
                CorrectThread = new Thread(new ThreadStart(CorretControl));
                CorrectThread.IsBackground = true;
                CorrectThread.Start();
                EGMCorrectStart = true;
                ShowMsg("EGM纠偏开始");
            }
            else
            {
                ShowMsg("力控纠偏失败，机器人或力控尚未链接");
            }
        }
        private void btn_CorrectStop_Click(object sender, EventArgs e)
        {
            CurCorrection.X = 0;
            CurCorrection.Y = 0;
            CurCorrection.Z = 0;
            CurCorrection.Rx = 0;
            CurCorrection.Ry = 0;
            CurCorrection.Rz = 0;
            CurCorrection.A1 = 0;
            CurCorrection.A2 = 0;
            CurCorrection.A3 = 0;
            CurCorrection.A4 = 0;
            CurCorrection.A5 = 0;
            CurCorrection.A6 = 0;

            txt_CurCorrectionX.Text = "0";
            txt_CurCorrectionY.Text = "0";
            txt_CurCorrectionZ.Text = "0";
            txt_CurCorrectionRx.Text = "0";
            txt_CurCorrectionRy.Text = "0";
            txt_CurCorrectionRz.Text = "0";
            txt_CurCorrectionA1.Text = "0";
            txt_CurCorrectionA2.Text = "0";
            txt_CurCorrectionA3.Text = "0";
            txt_CurCorrectionA4.Text = "0";
            txt_CurCorrectionA5.Text = "0";
            txt_CurCorrectionA6.Text = "0";
            CorrectThread.Abort();
            EGMCorrectStart = false;
            ShowMsg("力控纠偏关闭");
        }

        private void CorretControl()
        {
            while (true)
            {
                if (double.TryParse(txt_CurCorrectionX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number1))
                {
                    //CurCorrection.X = Convert.ToDouble(txt_CurCorrectionX.Text);
                    CurCorrection.X = number1;
                }
                if (double.TryParse(txt_CurCorrectionY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number2))
                {
                    //CurCorrection.Y = Convert.ToDouble(txt_CurCorrectionY.Text);
                    CurCorrection.Y = number2;
                }
                if (double.TryParse(txt_CurCorrectionZ.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number3))
                {
                    //CurCorrection.Z = Convert.ToDouble(txt_CurCorrectionZ.Text);
                    CurCorrection.Z = number3;
                }
                if (double.TryParse(txt_CurCorrectionRx.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number4))
                {
                    //CurCorrection.Rx = Convert.ToDouble(txt_CurCorrectionRx.Text);
                    CurCorrection.Rx = number4;
                }
                if (double.TryParse(txt_CurCorrectionRy.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number5))
                {
                    //CurCorrection.Ry = Convert.ToDouble(txt_CurCorrectionRy.Text);
                    CurCorrection.Ry = number5;
                }
                if (double.TryParse(txt_CurCorrectionRz.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number6))
                {
                    //CurCorrection.Rz = Convert.ToDouble(txt_CurCorrectionRz.Text);
                    CurCorrection.Rz = number6;
                }
                if (double.TryParse(txt_CurCorrectionA1.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number7))
                {
                    //CurCorrection.A1 = Convert.ToDouble(txt_CurCorrectionA1.Text);
                    CurCorrection.A1 = number7;
                }
                if (double.TryParse(txt_CurCorrectionA2.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number8))
                {
                    //CurCorrection.A2 = Convert.ToDouble(txt_CurCorrectionA2.Text);
                    CurCorrection.A2 = number8;
                }
                if (double.TryParse(txt_CurCorrectionA3.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number9))
                {
                    //CurCorrection.A3 = Convert.ToDouble(txt_CurCorrectionA3.Text);
                    CurCorrection.A3 = number9;
                }
                if (double.TryParse(txt_CurCorrectionA4.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number10))
                {
                    //CurCorrection.A4 = Convert.ToDouble(txt_CurCorrectionA4.Text);
                    CurCorrection.A4 = number10;
                }
                if (double.TryParse(txt_CurCorrectionA5.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number11))
                {
                    //CurCorrection.A5 = Convert.ToDouble(txt_CurCorrectionA5.Text);
                    CurCorrection.A5 = number11;
                }
                if (double.TryParse(txt_CurCorrectionA6.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number12))
                {
                    //CurCorrection.A6 = Convert.ToDouble(txt_CurCorrectionA6.Text);
                    CurCorrection.A6 = number12;
                }
                Thread.Sleep(200); // 转换为毫秒
            }
        }

        private void btn_EgmMode_Click(object sender, EventArgs e)
        {
            try
            {
                using (Mastership.Request(controller1.Rapid))
                {
                    RapidData rd = controller1.Rapid.GetRapidData("T_ROB1", "EGM_test", "ModeChoose");
                    modeChoose = (Num)rd.Value;
                    if (btn_EgmMode.Text == "笛卡尔模式")
                    {
                        modeChoose.Value = 2;
                        btn_EgmMode.Text = "关节模式";
                    }
                    else
                    {
                        modeChoose.Value = 1;
                        btn_EgmMode.Text = "笛卡尔模式";
                    }
                    rd.Value = modeChoose;
                }
            }
            catch (Exception)
            {

                throw;
            }


        }

        private void btn_Logoff_Click(object sender, EventArgs e)
        {
            if (this.controller1 != null)
            {
                IsControlLogin = false;
                //对controller1进行登出、清空
                this.controller1.Logoff();
                this.controller1.Dispose();
                this.controller1 = null;

            }
        }


    }
}
