using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sfsp;
using Sfsp.Messaging;

namespace TestSFSP
{
    enum test : byte
    {
        a = 1,
        b = 2
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            SfspScanner scanner = new SfspScanner(new SfspHostConfiguration("Boh"));

            scanner.StartScan(new TimeSpan(0,0,2));

            scanner.HostFound += (Object sender2, SfspHostFoundEventArgs e2) => {
                MessageBox.Show(e2.Host.Name + "\n" + e2.Host.Address.ToString());
            };
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SfspListener listener = new SfspListener(new SfspHostConfiguration(textBox2.Text));

            listener.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form2 frm2 = new Form2();
            frm2.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // MessageBox.Show(String.Join("\n", Environment.GetCommandLineArgs()));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath2("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
        }
    }
}
