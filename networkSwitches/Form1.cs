using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace networkSwitches
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string telnetConfig = "telnet.config";
        int timeout = 500;

        private string sendTelnetCmd(NetworkStream stream, string command, char eol = (char)10, int buffSize = 4096, int timeout = 1000)
        {
            DateTime startTime = DateTime.Now;
            debug($"Set timeout to {timeout} ms");
            stream.ReadTimeout = timeout;
            debug($"Set response buffer to {buffSize} bytes");
            byte[] response = new byte[buffSize];
            if (command.Contains(textBox2.Text))
            {
                debug($"Sending command: {command.Replace(textBox2.Text, new string('*', textBox2.Text.Length))}");
            } else
            {
                debug($"Sending command: {command}");
            }
            Byte[] data = System.Text.Encoding.ASCII.GetBytes($"{command}{Environment.NewLine}");
            stream.Write(data, 0, data.Length);
            debug($"Send {data.Length} bytes");
            Thread.Sleep(timeout);

            while (true)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(response, 0, response.Length);
                    string respMsg = System.Text.Encoding.ASCII.GetString(response, 0, bytesRead);
                    if ((char)respMsg[bytesRead - 1] == eol)
                    {
                        debug($"Received {bytesRead} bytes");
                        debug($"RESPONSE: \r\n{respMsg}\r\n");
                        return respMsg;
                    } else
                    {
                        debug($"Waiting for EOL CHAR [{(int)eol}]");

                        if (startTime > DateTime.Now.AddMilliseconds(timeout))
                        {
                            debug($"EOL timeout reached [{timeout} seconds], continuing...");
                            return respMsg;
                        } 
                    }
                } else
                {
                    debug($"TCP STREAM CLOSED");
                    return "EOF";
                }
            }
        }

        private void debug(string text)
        {
            debugBox.AppendText($"{text}{Environment.NewLine}");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            debug("Form started");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            debug("Form loaded");
            textBox2.Focus();

            if (File.Exists(telnetConfig))
            {
                debug($"Loading telnet configuration: {telnetConfig}");
                string[] lines = File.ReadAllLines(telnetConfig);
                foreach (string line in lines)
                {
                    debug($"  Adding telnet destination: {line.Trim()}");
                    listBox1.Items.Add(line.Trim());
                }
            }
            else
            {
                debug($"Telnet configuration not found");
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!listBox1.Items.Contains(textBox1.Text) && textBox1.Text.Trim() != "")
                {
                    listBox1.Items.Add(textBox1.Text);
                    textBox1.Clear();
                    textBox1.Focus();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!listBox1.Items.Contains(textBox1.Text) && textBox1.Text.Trim() != "")
            {
                listBox1.Items.Add(textBox1.Text);
                textBox1.Clear();
            }

            textBox1.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = listBox1.Items.Count - 1; i >= 0; i--)
            {
                if (listBox1.SelectedIndices.Contains(i))
                {
                    listBox1.Items.RemoveAt(i);
                }
            }
        }

        private void listBox1_MouseHover(object sender, EventArgs e)
        {
            string tooltipText = $"Servers can be loaded by default from a list in the file '{telnetConfig}' stored in the same directory as the application.{Environment.NewLine}";
            tooltipText += $"{Environment.NewLine}Example:{Environment.NewLine}";
            tooltipText += $"192.168.4.20{Environment.NewLine}";
            tooltipText += $"10.1.3.37{Environment.NewLine}";
            tooltipText += $"switch42.domain.local{Environment.NewLine}";
            tooltipText += $"172.16.6.6";

            toolTip1.SetToolTip(listBox1, tooltipText);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Trim() == "")
            {
                debug($"No password");
                return;
            }

            debug($"Connecting to telnet devices");
            foreach (var item in listBox1.Items)
            {
                string dest = item.ToString();
                debug($"  Creating TCP stream for {dest} on port 23");
                TcpClient client = new TcpClient(dest, 23);
                NetworkStream stream = client.GetStream();
                string output;

                sendTelnetCmd(stream, textBox2.Text, '#');
                sendTelnetCmd(stream, "term len 0", '#');
                output = sendTelnetCmd(stream, "sho int status", '#', 20000, timeout);
                string[] macs = sendTelnetCmd(stream, "sho mac addr", '#', 20000, timeout).Split('\n');
                sendTelnetCmd(stream, "exit", '\n', 1, 1);

                Color vlan1color = Color.IndianRed;
                Color vlan10color = Color.LightGreen;
                Color vlan20color = Color.LightYellow;
                Color vlan30color = Color.MediumPurple;
                Color trunkcolor = Color.Pink;
                Color routedcolor = Color.Brown;
                Color color10mbps = Color.Black;
                Color color100mbps = Color.DarkGray;
                Color color1gbps = Color.CornflowerBlue;
                Color color10gbps = Color.Magenta;

                int portCount = 0;
                int rowCount = 0;
                int offset = 40;

                Form newForm = new Form
                {
                    Text = dest,
                    Height = 650,
                    Width = 1900,
                    StartPosition = FormStartPosition.WindowsDefaultLocation,
                    Icon = this.Icon
                };

                TextBox consoleTextBox = new TextBox();
                consoleTextBox.Location = new Point(offset, 300);
                consoleTextBox.Width = 1800;
                consoleTextBox.Height = 300;
                consoleTextBox.Multiline = true;
                consoleTextBox.ReadOnly = false;
                consoleTextBox.BackColor = this.ForeColor;
                consoleTextBox.ForeColor = this.BackColor;
                consoleTextBox.Font = new Font("Lucida Console", (float)8, textBox1.Font.Style);
                consoleTextBox.BorderStyle = BorderStyle.None;
                consoleTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                consoleTextBox.ScrollBars = ScrollBars.Vertical;

                TextBox infoTextBox = new TextBox();
                infoTextBox.Location = new Point(offset, 180);
                infoTextBox.Width = 1000;
                infoTextBox.Height = 60;
                infoTextBox.Multiline = true;
                infoTextBox.ReadOnly = true;
                infoTextBox.BackColor = this.BackColor;
                infoTextBox.Font = new Font("Lucida Console", (float) 8, textBox1.Font.Style);
                infoTextBox.BorderStyle = BorderStyle.None;

                TextBox infoTextBox2 = new TextBox();
                infoTextBox2.Location = new Point(offset + infoTextBox.Width + 10, infoTextBox.Location.Y);
                infoTextBox2.Width = 750;
                infoTextBox2.Height = infoTextBox.Height;
                infoTextBox2.Multiline = true;
                infoTextBox2.ReadOnly = true;
                infoTextBox2.BackColor = this.BackColor;
                infoTextBox2.Font = new Font("Lucida Console", (float)8, textBox1.Font.Style);
                infoTextBox2.BorderStyle = BorderStyle.None;
                infoTextBox2.ScrollBars = ScrollBars.Vertical;

                Label legendLabel = new Label();
                legendLabel.Text = "Legend:";
                legendLabel.Location = new Point(offset, 225);

                Button legend1 = new Button();
                legend1.Text = "Connected";
                legend1.Width = 70;
                legend1.Height = 40;
                legend1.FlatStyle = FlatStyle.Flat;
                legend1.Font = new Font(legend1.Font.FontFamily, (float) 6, FontStyle.Bold);
                legend1.Location = new Point(offset + 80 * 0, 250);

                Button legend2 = new Button();
                legend2.Text = "Not Connected";
                legend2.Width = 70;
                legend2.Height = 40;
                legend2.FlatStyle = FlatStyle.Standard;
                legend2.Location = new Point(offset + 80 * 1, 250);

                Button legend3 = new Button();
                legend3.Text = "VLAN 1";
                legend3.Width = 80;
                legend3.Height = 40;
                legend3.FlatStyle = FlatStyle.Flat;
                legend3.BackColor = vlan1color;
                legend3.Location = new Point(offset + 80 * 3, 250);

                Button legend4 = new Button();
                legend4.Text = "VLAN 10";
                legend4.Width = 80;
                legend4.Height = 40;
                legend4.FlatStyle = FlatStyle.Flat;
                legend4.BackColor = vlan10color;
                legend4.Location = new Point(offset + 80 * 4, 250);

                Button legend5 = new Button();
                legend5.Text = "VLAN 20";
                legend5.Width = 80;
                legend5.Height = 40;
                legend5.FlatStyle = FlatStyle.Flat;
                legend5.BackColor = vlan20color;
                legend5.Location = new Point(offset + 80 * 5, 250);

                Button legend6 = new Button();
                legend6.Text = "VLAN 30";
                legend6.Width = 80;
                legend6.Height = 40;
                legend6.FlatStyle = FlatStyle.Flat;
                legend6.BackColor = vlan30color;
                legend6.Location = new Point(offset + 80 * 6, 250);

                Button legend7 = new Button();
                legend7.Text = "TRUNK";
                legend7.Width = 80;
                legend7.Height = 40;
                legend7.FlatStyle = FlatStyle.Flat;
                legend7.BackColor = trunkcolor;
                legend7.Location = new Point(offset + 80 * 7, 250);

                Button legend8 = new Button();
                legend8.Text = "ROUTED";
                legend8.Width = 80;
                legend8.Height = 40;
                legend8.FlatStyle = FlatStyle.Flat;
                legend8.BackColor = routedcolor;
                legend8.Location = new Point(offset + 80 * 8, 250);

                Button legend9 = new Button();
                legend9.Text = "10 Mbps";
                legend9.Width = 80;
                legend9.Height = 40;
                legend9.FlatStyle = FlatStyle.Flat;
                legend9.FlatAppearance.BorderColor = color10mbps;
                legend9.FlatAppearance.BorderSize = 3;
                legend9.Location = new Point(offset + 80 * 10, 250);

                Button legend10 = new Button();
                legend10.Text = "100 Mbps";
                legend10.Width = 80;
                legend10.Height = 40;
                legend10.FlatStyle = FlatStyle.Flat;
                legend10.FlatAppearance.BorderColor = color100mbps;
                legend10.FlatAppearance.BorderSize = 3;
                legend10.Location = new Point(offset + 80 * 11, 250);

                Button legend11 = new Button();
                legend11.Text = "1 Gbps";
                legend11.Width = 80;
                legend11.Height = 40;
                legend11.FlatStyle = FlatStyle.Flat;
                legend11.FlatAppearance.BorderColor = color1gbps;
                legend11.FlatAppearance.BorderSize = 3;
                legend11.Location = new Point(offset + 80 * 12, 250);

                Button legend12 = new Button();
                legend12.Text = "10 Gbps";
                legend12.Width = 80;
                legend12.Height = 40;
                legend12.FlatStyle = FlatStyle.Flat;
                legend12.FlatAppearance.BorderColor = color10gbps;
                legend12.FlatAppearance.BorderSize = 3;
                legend12.Location = new Point(offset + 80 * 13, 250);


                foreach (string line in output.Split('\n'))
                {
                    if (Regex.Match(line.Trim(), @"^((Gi)|(Fa)|(Te))").Success)
                    {
                        string interfaceName = line.Trim().Substring(0, 10).Trim();
                        string description = line.Trim().Substring(10, 19).Trim();
                        string status = line.Trim().Substring(29, 13).Trim();
                        string vlan = line.Trim().Substring(42, 11).Trim();
                        string duplex = line.Trim().Substring(53, 7).Trim();
                        string speed = line.Trim().Substring(60, 7).Trim();
                        string mediaType = line.Trim().Substring(67).Trim();

                        Button newButton = new Button();
                        newButton.Text = interfaceName;
                        newButton.Width = 70;
                        newButton.Height = 30;
                        newButton.Font =  new Font(newButton.Font.Name, (float)7, newButton.Font.Style);

                        if (vlan == "1")
                        {
                            newButton.BackColor = vlan1color;
                        }
                        if (vlan == "10")
                        {
                            newButton.BackColor = vlan10color;
                        }
                        if (vlan == "20")
                        {
                            newButton.BackColor = vlan20color;
                        }
                        if (vlan == "30")
                        {
                            newButton.BackColor = vlan30color;
                        }
                        if (vlan == "trunk")
                        {
                            newButton.BackColor = trunkcolor;
                        }
                        if (vlan == "routed")
                        {
                            newButton.BackColor = routedcolor;
                        }
                        if (speed == "a-10")
                        {
                            newButton.FlatAppearance.BorderColor = color10mbps;
                            newButton.FlatAppearance.BorderSize = 3;
                        }
                        if (speed == "a-100")
                        {
                            newButton.FlatAppearance.BorderColor = color100mbps;
                            newButton.FlatAppearance.BorderSize = 3;
                        }
                        if (speed == "a-1000")
                        {
                            newButton.FlatAppearance.BorderColor = color1gbps;
                            newButton.FlatAppearance.BorderSize = 3;
                        }
                        if (speed == "10G")
                        {
                            newButton.FlatAppearance.BorderColor = color10gbps;
                            newButton.FlatAppearance.BorderSize = 3;
                        }
                        if (status == "connected")
                        {
                            newButton.FlatStyle = FlatStyle.Flat;
                            newButton.Font = new Font(newButton.Font.FontFamily, newButton.Font.Size, FontStyle.Bold);
                        } else
                        {
                            newButton.FlatStyle = FlatStyle.Standard;
                        }

                        if (portCount == 52)
                        {
                            rowCount++;
                            portCount = 0;
                        }

                        if (portCount % 2 == 0)
                        {
                            newButton.Location = new Point(offset + (portCount * 35), (offset / 2) + (rowCount * 70));
                        }
                        else
                        {
                            newButton.Location = new Point(offset + ((portCount - 1) * 35), (offset / 2) + ((rowCount * 70) + 35));
                        }

                        newButton.Click += (s, evnt) =>
                        {
                            infoTextBox.Text = $"Port      Name               Status       Vlan       Duplex  Speed Type{Environment.NewLine}{line.Trim()}";
                            infoTextBox2.Text = $"Vlan    Mac Address       Type        Ports{Environment.NewLine}";
                            foreach (string mac in macs)
                            {
                                if (Regex.Match(mac.Trim(), $"{interfaceName}$").Success)
                                {
                                    infoTextBox2.AppendText(mac + Environment.NewLine);
                                }
                            }
                            
                        };

                        newForm.Controls.Add(newButton);

                        portCount++;
                    }
                     
                }

                newForm.Load += (s, evnt) =>
                {
                    debug($"  Creating TCP stream for {dest} on port 23");
                    TcpClient tmpClient = new TcpClient(dest, 23);
                    NetworkStream tmpStream = tmpClient.GetStream();

                    string tmpOutput = sendTelnetCmd(tmpStream, textBox2.Text, '#'); // password

                    if (Regex.Match(tmpOutput, "EOF$").Success)
                    {
                        debug("+--------------------+");
                        debug("| Passowrd Incorrect |");
                        debug("+--------------------+");
                        newForm.Close();
                        return;
                    } 

                    string[] lines = tmpOutput.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string cmdPrompt = lines[lines.Length - 1];

                    debug($"Command Prompt: [{cmdPrompt}]");

                    consoleTextBox.AppendText(tmpOutput);
                    tmpOutput = sendTelnetCmd(tmpStream, $"term len 0", '#');
                    consoleTextBox.AppendText($"{tmpOutput} ");

                    consoleTextBox.KeyPress += (send, ent) =>
                    {                        
                        int cursorPos = consoleTextBox.SelectionStart;
                        int cursorLine = consoleTextBox.GetLineFromCharIndex(cursorPos);
                        int lineStart = consoleTextBox.GetFirstCharIndexFromLine(cursorLine);

                        if ((ent.KeyChar == '\r'))
                        {
                            //string consoleCmd = consoleTextBox.Lines[consoleTextBox.Lines.Length - 1].Substring(cmdPrompt.Length);
                            string consoleCmd = consoleTextBox.Text.Substring(consoleTextBox.Text.LastIndexOf('#') + 1);
                            debug($"CONSOLE CMD: {consoleCmd}");
                            string cmdOutput = sendTelnetCmd(tmpStream, consoleCmd, '#', 200000, timeout);

                            if (cmdOutput.Trim() == "EOF")
                            {
                                debug($"Closing Form");
                                newForm.Close();
                                return;
                            }

                            consoleTextBox.AppendText($"{cmdOutput} ");
                            SendKeys.Send("{BACKSPACE}");
                        }
                    };

                    consoleTextBox.Enter += (send, ent) =>
                    {
                        consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                        consoleTextBox.SelectionLength = 0;
                        consoleTextBox.Focus();
                    };

                    consoleTextBox.Click += (send, ent) =>
                    {
                        consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                        consoleTextBox.SelectionLength = 0;
                        consoleTextBox.Focus();
                    };

                    consoleTextBox.KeyDown += (send, ent) =>
                    {
                        if (ent.KeyCode == Keys.Up || ent.KeyCode == Keys.Down || ent.KeyCode == Keys.Left || ent.KeyCode == Keys.Right)
                        {
                            debug($"Arrow keys are not allowed");
                            ent.SuppressKeyPress = true;
                            consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                            consoleTextBox.SelectionLength = 0;
                            consoleTextBox.Focus();
                        }

                        if (ent.KeyCode == Keys.Back && consoleTextBox.Text[consoleTextBox.Text.Length - 1] == '#')
                        {
                            ent.SuppressKeyPress = true;
                            consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                            consoleTextBox.SelectionLength = 0;
                            consoleTextBox.Focus();
                        }
                    };
                };



                newForm.Controls.Add(legendLabel);
                newForm.Controls.Add(consoleTextBox);
                newForm.Controls.Add(infoTextBox);
                newForm.Controls.Add(infoTextBox2);
                newForm.Controls.Add(legend1);
                newForm.Controls.Add(legend2);
                newForm.Controls.Add(legend3);
                newForm.Controls.Add(legend4);
                newForm.Controls.Add(legend5);
                newForm.Controls.Add(legend6);
                newForm.Controls.Add(legend7);
                newForm.Controls.Add(legend8);
                newForm.Controls.Add(legend9);
                newForm.Controls.Add(legend10);
                newForm.Controls.Add(legend11);
                newForm.Controls.Add(legend12);

                newForm.Show();
            }

            debug($"Finsihed processing telnet devices");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            debug($"Reloading configuration");

            listBox1.Items.Clear();

            if (File.Exists(telnetConfig))
            {
                debug($"Loading telnet configuration: {telnetConfig}");
                string[] lines = File.ReadAllLines(telnetConfig);
                foreach (string line in lines)
                {
                    debug($"  Adding telnet destination: {line.Trim()}");
                    listBox1.Items.Add(line.Trim());
                }
            }
            else
            {
                debug($"Telnet configuration not found");
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button5_Click(sender, e);
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                button2_Click(sender, e);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            timeout = (int) numericUpDown1.Value;
        }
    }
}
