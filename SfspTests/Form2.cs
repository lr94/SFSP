using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace TestSFSP
{
    public partial class Form2 : Form
    {
        private Object guard = new Object();
        private Queue<String> queue = new Queue<string>();

        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t_consumer = new Thread(Consumer);
            t_consumer.Start();

            Thread t_producer = new Thread(Producer);
            t_producer.Start();
        }

        private void Producer()
        {
            int counter = 0;
            while(counter < 30)
            {
                lock(guard)
                {
                    Thread.Sleep(new Random().Next(200,2000));
                    queue.Enqueue(counter.ToString());
                    SetTitle(queue.Count.ToString());
                    counter++;
                    Monitor.Pulse(guard);
                }
            }
        }

        private void Consumer()
        {
            Queue<string> secondaryQueue = new Queue<string>();
            while(true)
            {
                lock(guard)
                {
                    Monitor.Wait(guard, 1000);
                    while (queue.Count > 0)
                    {
                        secondaryQueue.Enqueue(queue.Dequeue());
                        SetTitle(queue.Count.ToString());
                    }
                }

                while(secondaryQueue.Count > 0)
                {
                    string s = secondaryQueue.Dequeue();
                    Thread.Sleep(5000);
                    append(s);
                }
            }
        }

        delegate void StrMethod(string s);
        private void append(string s)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new StrMethod(append), new Object[] { s });
            }
            else
                textBox1.AppendText(s + "\n");
        }

        private void SetTitle(string s)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new StrMethod(SetTitle), new Object[] { s });
            }
            else
                this.Text = s;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread serverThread = new Thread(Server);
            serverThread.Start();
        }

        private void Server()
        {
            TcpListener server = new TcpListener(new IPEndPoint(IPAddress.Any, 6060));
            server.Start();

            bool cont = true;

            while (cont)
            {
                //TcpClient c = server.AcceptTcpClient();
                
                // TcpClient c = server.AcceptTcpClient();
                IAsyncResult res = server.BeginAcceptTcpClient(null, null);
                if(!res.AsyncWaitHandle.WaitOne(10000))
                {
                    System.Diagnostics.Debug.WriteLine("TIMEOUT");
                    break;
                }
                System.Diagnostics.Debug.WriteLine("ACCEPT");
                TcpClient c = server.EndAcceptTcpClient(res);
                System.Diagnostics.Debug.WriteLine("ACCEPTED");


                byte[] recv = new byte[1024];
                // c.GetStream().Read(recv, 0, 1024);
                // MessageBox.Show(System.Text.Encoding.UTF8.GetString(recv));
                byte[] data = System.Text.Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\n\nGU");
                c.GetStream().Write(data, 0, data.Length);
                
                c.Close();
            }

            server.Stop();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("foo");
                    throw new Exception("exception");   //error occurs here
                }
                finally   //will execute this as it is the first exception statement
                {
                    System.Diagnostics.Debug.WriteLine("foo's finally called");
                }
            }
            catch (Exception)  // then this
            {
                System.Diagnostics.Debug.WriteLine("Exception caught");
            }
        }
    }
}
