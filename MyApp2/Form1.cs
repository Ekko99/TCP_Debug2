using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MyApp2
{
    public partial class Form1 : Form
    {
        XMyTcpServer tcpServer;
        Socket Socket_Client;
        bool IsListen = false;
        Color colorAlive = Color.DeepSkyBlue;
        string lastClient;
        string lastServer;
        public Form1()
        {
            InitializeComponent();
            string HostName = Dns.GetHostName();
            IPAddress[] iPAddresses = Dns.GetHostAddresses(HostName);
            for(int i = 0; i < iPAddresses.Length; i++)
            {
                if (iPAddresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    comboBox1.Items.Add(iPAddresses[i].ToString());
                }
            }
            textBox2.Text = "5000";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (btnListen.Text)
            {
                case "侦听":
                    try
                    {
                        tcpServer = new XMyTcpServer();
                        tcpServer.Socket_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPAddress ip = IPAddress.Parse(textBox1.Text);
                        IPEndPoint ipPort = new IPEndPoint(ip, int.Parse(textBox2.Text));
                        //tcpServer.Socket_Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);//设置端口可复用，否则端口close后一段时间不能再绑定
                        tcpServer.Socket_Server.Bind(ipPort);
                        tcpServer.Socket_Server.Listen(20);
                        richTextBox3.AppendText($"侦听成功：{ipPort.ToString()}\n");
                        richTextBox1.ScrollToCaret();
                        IsListen = true;
                        btnListen.Text = "停止";
                        btnListen.ForeColor = colorAlive;
                        //接受客户端的连接
                        Thread thReceiveConnect = new Thread(delegate () { ReceiveConnect(tcpServer.Socket_Server); });
                        thReceiveConnect.IsBackground = true;
                        thReceiveConnect.Start();

                    }
                    catch (Exception ex)
                    {
                        richTextBox3.AppendText($"{ex.Message}\n");
                        richTextBox3.ScrollToCaret();
                    }
                    break;
                case "停止":
                    IsListen = false;
                    btnListen.Text = "侦听";
                    btnListen.ForeColor = Color.White;
                    tcpServer.Socket_Server.Close();
                    break;
            }
            

        }
        private void ReceiveConnect(object o)
        {
            Socket Socket_server = o as Socket;
            try
            {
                while (IsListen)
                {
                    Socket connectClient=null;
                    try
                    {
                        connectClient = tcpServer.Socket_Server.Accept();//Accept会阻塞
                    }
                    catch
                    {
                        this.Invoke(new EventHandler(delegate {
                            richTextBox3.AppendText("已停止侦听\n");
                            richTextBox3.ScrollToCaret();
                            checkedListBox1.Items.Clear();
                        }));
                    }
                    if (connectClient != null)
                    {
                        tcpServer.ClientList.Add(connectClient.RemoteEndPoint.ToString(), connectClient);
                        this.Invoke(new EventHandler(delegate {
                            richTextBox3.AppendText($"新的客户端:{connectClient.RemoteEndPoint.ToString()}\n");
                            richTextBox3.ScrollToCaret();
                            checkedListBox1.Items.Add(connectClient.RemoteEndPoint.ToString());
                        }));
                        //给新的客户端创建socket进行通信
                        Thread thReceiveMsg = new Thread(ReceiveMsg);
                        thReceiveMsg.IsBackground = true;
                        thReceiveMsg.Start(connectClient);
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new EventHandler(delegate
                {
                    richTextBox3.AppendText($"异常：{ex.Message}\n");
                }));
            }
            finally
            {
                //if (Socket_server != null)
                //{
                //    Socket_server.Close();
                //}
                Socket_server.Close();
            }
        }
        private void ReceiveMsg(object o)
        {
            Socket socketForClient = o as Socket;
            try
            {
                while (true)
                {
                    if (!IsListen)
                    {
                        socketForClient.Close();
                        return;
                    }
                    byte[] msgByte = new byte[1024 * 1024];
                    int length = socketForClient.Receive(msgByte);//Receive会阻塞
                    if (length > 0)
                    {
                        if (socketForClient.RemoteEndPoint.ToString() != lastClient)
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                richTextBox2.AppendText($"【{socketForClient.RemoteEndPoint.ToString()}---{DateTime.Now.ToString()}】\n");
                            }));
                            lastClient = socketForClient.RemoteEndPoint.ToString();
                        }
                        string msgStr=string.Empty;
                        if (cbReceiveHexS.Checked)
                        {
                            for (int i = 0; i < length; i++)
                            {
                                msgStr += (msgByte[i].ToString("X2") + " ");
                            }
                        }
                        else
                        {
                            msgStr = Encoding.UTF8.GetString(msgByte, 0, length);
                        }
                        this.Invoke(new EventHandler(delegate
                        {
                            richTextBox2.AppendText($"{msgStr}");
                            richTextBox2.ScrollToCaret();
                        }));
                    }
                    Thread.Sleep(1);
                }
            }
            catch(Exception ex)
            {
                this.Invoke(new EventHandler(delegate {
                    richTextBox3.AppendText($"异常：{ex.Message}\n" );
                    richTextBox3.AppendText($"客户端：{socketForClient.RemoteEndPoint.ToString()}断开连接！\n");
                    richTextBox3.ScrollToCaret();
                    checkedListBox1.Items.Remove(socketForClient.RemoteEndPoint.ToString());

                }));
                tcpServer.ClientList.Remove(socketForClient.RemoteEndPoint.ToString());
                socketForClient.Close();

            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Text = comboBox1.SelectedItem.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            byte[] sendByte;
            if (cbSendHexS.Checked)
            {
                sendByte = strToHexByte(richTextBox1.Text);
            }
            else
            {
                sendByte = Encoding.UTF8.GetBytes(richTextBox1.Text);
            }
            foreach(var clientIP in checkedListBox1.CheckedItems)
            {
                tcpServer.ClientList[clientIP.ToString()].Send(sendByte);
            }
        }




        
        /// <summary>
        /// 客户端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                switch (btnConnect.Text)
                {
                    case "连接":
                        //btnConnect.Enabled = false;
                        btnConnect.Text = "...";
                        btnConnect.ForeColor = Color.Yellow;
                        Thread thConnect = new Thread(delegate() 
                        {
                            try
                            {
                                Socket_Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                IPAddress ip_client = IPAddress.Parse(textBox3.Text);
                                IPEndPoint ipPort = new IPEndPoint(ip_client, int.Parse(textBox4.Text));
                                Socket_Client.Connect(ipPort);
                                this.Invoke(new EventHandler(delegate
                                {
                                    lbLocalHost.Text = Socket_Client.LocalEndPoint.ToString();
                                    richTextBox4.AppendText("连接成功\n");
                                    richTextBox4.ScrollToCaret();
                                    btnConnect.Text = "断开";
                                    btnConnect.Enabled = true;
                                    btnConnect.ForeColor = colorAlive;
                                }));
                                Thread thReciveMsg_C = new Thread(delegate () { ReceiveMsg_C(Socket_Client); });
                                thReciveMsg_C.IsBackground = true;
                                thReciveMsg_C.Start();
                            }
                            catch(Exception ex)
                            {
                                this.Invoke(new EventHandler(delegate
                                {
                                    richTextBox4.AppendText($"{ex.Message}\n");
                                    richTextBox4.ScrollToCaret();
                                    btnConnect.Enabled = true;
                                    btnConnect.Text = "连接";
                                    btnConnect.ForeColor = Color.White;
                                }));
                            }
                        });
                        thConnect.IsBackground = true;
                        thConnect.Start();
                        break;
                    case "断开":
                        Socket_Client.Close();
                        btnConnect.Text = "连接";
                        btnConnect.ForeColor = Color.White;
                        break;

                }
            }
            catch(Exception ex)
            {
                richTextBox4.AppendText($"{ex.Message}\n");
                richTextBox4.ScrollToCaret();
            }
        }
        private void ReceiveMsg_C(Socket socket_C)
        {
            //Socket socket_C = o as Socket;
            try
            {
                while (true)
                {
                    if (socket_C.Connected == false) { return; }
                    byte[] RecByte = new byte[1024 * 1024];
                    int length = socket_C.Receive(RecByte);
                    if (length > 0)
                    {
                        string msgStr = string.Empty;
                        if (cbReceiveHexC.Checked)
                        {
                            for(int i = 0; i < length; i++)
                            {
                                msgStr += (RecByte[i].ToString("X2")+" ");
                            }
                            msgStr += "\n";
                        }
                        else
                        {
                            msgStr = $"{Encoding.UTF8.GetString(RecByte, 0, length)}";

                        }
                        this.Invoke(new EventHandler(delegate
                        {
                            //richTextBox6.AppendText($"【{socket_C.RemoteEndPoint.ToString()},{DateTime.Now.ToString()}】\n");
                            richTextBox6.AppendText(msgStr);
                            richTextBox6.ScrollToCaret();
                        }));
                    }
                }
            }
            catch(Exception ex)
            {
                this.Invoke(new EventHandler(delegate {
                    richTextBox4.AppendText($"异常：{ex.Message}\n");
                    richTextBox4.ScrollToCaret();
                    socket_C.Close();
                    btnConnect.Text = "连接";
                    btnConnect.ForeColor = Color.White;

                }));

            }
            Thread.Sleep(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] sendByte;
                if (cbSendHexC.Checked)
                {
                    sendByte = strToHexByte(richTextBox5.Text);
                }
                else
                {
                    sendByte = Encoding.UTF8.GetBytes(richTextBox5.Text);
                }
                Socket_Client.Send(sendByte);
            }
            catch(Exception ex)
            {
                Socket_Client.Close();
                this.Invoke(new EventHandler(delegate {
                    richTextBox4.AppendText($"异常：{ex.Message}\n");
                    richTextBox4.ScrollToCaret();
                    btnConnect.Text = "连接";
                    btnConnect.ForeColor = Color.White;
                }));
            }
        }





        /// <summary>
        /// 转换
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] strToHexByte(string hexString)
        {
            hexString=hexString.Replace(" ", "");
            hexString = hexString.Replace(",", "");
            if (hexString.Length % 2 != 0)
            {
                hexString += " ";
            }
            byte[] returnBytes = new byte[hexString.Length / 2];
            for(int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }
    }
}
