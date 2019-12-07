using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BillingControlsTool
{
    public partial class Form1 : Form
    {
        struct WiFICredentials
        {
            public string ssid;
            public string password;

            public WiFICredentials(string ssid, string password)
            {
                this.ssid = ssid;
                this.password = password;
            }
        }

        struct OptionsExtension
        {
            public string api_key;
            public string to_bill;
            public int active_duration;

            public OptionsExtension(string api_key, string to_bill, int active_duration)
            {
                this.api_key = api_key;
                this.to_bill = to_bill;
                this.active_duration = active_duration;
            }
        }

        struct AppConfig
        {
            public WiFICredentials cred;
            public OptionsExtension opex;
        }

        public Form1()
        {
            InitializeComponent();

            foreach (Control c in tableLayoutPanel4.Controls)
                c.Enabled = false;
            foreach (Control c in tableLayoutPanel3.Controls)
                c.Enabled = false;

            if(File.Exists("config.json"))
            {
                var cfg = File.ReadAllText("config.json");

                var lcfg = JsonConvert.DeserializeObject<AppConfig>(cfg, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                textBox1.Text = lcfg.cred.ssid;
                textBox2.Text = lcfg.cred.password;

                textBox3.Text = lcfg.opex.active_duration > -1 ? (lcfg.opex.active_duration + "") : "";
                textBox4.Text = lcfg.opex.to_bill;
                textBox5.Text = lcfg.opex.api_key;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            foreach (Control c in Controls)
                c.Enabled = false;

            //read
            string sel = (string)comboBox1.SelectedItem;

            await Task.Run(async () =>
            {
                using (SerialPort com_connection = new SerialPort(sel, 115200, Parity.None, 8, StopBits.One))
                {
                    com_connection.NewLine = "\r\n";
                    com_connection.ReadTimeout = 2000;
                    com_connection.WriteTimeout = 2000;

                    com_connection.Open();

                    StreamWriter w = new StreamWriter(com_connection.BaseStream);
                    StreamReader r = new StreamReader(com_connection.BaseStream);
                    w.AutoFlush = true;

                    //read wifi credentials

                    w.Write('k'); //programming mode command

                    //programming mode key
                    w.Write('p');
                    w.Write('r');
                    w.Write('o');
                    w.Write('g');

                    if (r.Read() != 'z') //ack
                        return;

                    char[] buffer = new char[200];

                    w.Write('r'); //read credentials command

                    r.ReadBlock(buffer, 0, 200);

                    var dser = JsonConvert.DeserializeObject<WiFICredentials>(new string(buffer), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    Invoke((MethodInvoker)(() =>
                    {
                        textBox1.Text = dser.ssid;
                        textBox2.Text = dser.password;
                    }));

                    ////////////////////////////////////////////////////////////////////////////////

                    //read extended params

                    w.Write('k'); //programming mode command

                    //programming mode key
                    w.Write('p');
                    w.Write('r');
                    w.Write('o');
                    w.Write('g');

                    if (r.Read() != 'z') //ack
                        return;
                    
                    buffer = new char[200];

                    w.Write('b'); //read extended params

                    r.ReadBlock(buffer, 0, 400);

                    var dser2 = JsonConvert.DeserializeObject<OptionsExtension>(new string(buffer), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    Invoke((MethodInvoker)(() =>
                    {
                        textBox4.Text = dser2.to_bill;
                        textBox3.Text = dser2.active_duration + "";
                        textBox5.Text = dser2.api_key;
                    }));

                    //finished reading params

                    com_connection.Close();

                    Invoke((MethodInvoker)(() => {
                        foreach (Control c in Controls)
                            c.Enabled = true;
                    }));
                }

                Console.WriteLine("done reading");
            });
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //write

            foreach (Control c in Controls)
                c.Enabled = false;

            string sel = (string)comboBox1.SelectedItem;

            using (SerialPort com_connection = new SerialPort(sel, 115200, Parity.None, 8, StopBits.One))
            {
                com_connection.NewLine = "\r\n";
                com_connection.ReadTimeout = 2000;
                com_connection.WriteTimeout = 2000;

                com_connection.Open();

                StreamWriter w = new StreamWriter(com_connection.BaseStream);
                StreamReader r = new StreamReader(com_connection.BaseStream);
                w.AutoFlush = true;

                w.Write('k'); //programming mode command

                //programming mode key
                w.Write('p');
                w.Write('r');
                w.Write('o');
                w.Write('g');

                if (r.Read() != 'z') //ack
                    return;

                char[] buffer = new char[200];

                string wifi_cred = "{\"ssid\":\"" + textBox1.Text.Trim() + "\",\"password\":\"" + textBox2.Text.Trim() + "\"}";

                var chars = ASCIIEncoding.ASCII.GetBytes(wifi_cred);
                chars.CopyTo(buffer, 0);

                w.Write('w'); //write credentials command

                w.Write(buffer);

                await Task.Delay(100);

                if (r.Read() != 'z') //ack
                    return;

                await Task.Delay(100);

                /////////////////////////////////////////////////////////////////////

                w.Write('k'); //programming mode command

                //programming mode key
                w.Write('p');
                w.Write('r');
                w.Write('o');
                w.Write('g');

                if (r.Read() != 'z') //ack
                    return;

                buffer = new char[200];

                string ext_param = "{\"api_key\":\"" + textBox5.Text.Trim() + "\",\"to_bill\":\"" + textBox4.Text.Trim() + "\",\"active_duration\":\"" + textBox3.Text.Trim() + "\"}";

                var chars2 = ASCIIEncoding.ASCII.GetBytes(ext_param);
                chars2.CopyTo(buffer, 0);

                w.Write('s'); //write params command

                w.Write(buffer);

                r.Read();

                //finished writing

                com_connection.Close();

                Invoke((MethodInvoker)(() => {
                    foreach (Control c in Controls)
                        c.Enabled = true;
                }));
            }

            Console.WriteLine("done writing");
        }

        private void comboBox1_Validating(object sender, CancelEventArgs e)
        {

        }

        private void comboBox1_Enter(object sender, EventArgs e)
        {
            refresh_com_ports();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            //form shown

            refresh_com_ports();
        }

        private void refresh_com_ports()
        {
            var ports = SerialPort.GetPortNames();

            comboBox1.DataSource = ports;

            if (ports.Length > 0)
            {
                comboBox1.SelectedIndex = 0;
                foreach (Control c in tableLayoutPanel4.Controls)
                    c.Enabled = true;
            } 
            else
            {
                comboBox1.SelectedIndex = -1;
            }
                
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //form is closing. save the fields
            AppConfig cfg;

            int val;
            if (!Int32.TryParse(textBox3.Text, out val))
                val = -1;

            cfg.opex = new OptionsExtension(textBox5.Text, textBox4.Text, val);
            cfg.cred = new WiFICredentials(textBox1.Text, textBox2.Text);


            File.WriteAllText("config.json",JsonConvert.SerializeObject(cfg));
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex > -1)
            {
                button3.Enabled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex > -1)
                foreach (Control c in tableLayoutPanel3.Controls)
                    c.Enabled = true;
        }
    }


}
