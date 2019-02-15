using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DCRSDataLayer;
using AudioFormatLib;
using System.Net.NetworkInformation;
using System.Threading;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using DCRSAdapter.Properties;

namespace DCRSAdapter
{
    public partial class Form1 : Form
    {
        private DirectoryInfo reportDir = new DirectoryInfo($"{Environment.CurrentDirectory}\\report");
 
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.dataGridView2.CellClick += DataGridView2_CellClick;
            //Directory.CreateDirectory(reportDir.FullName);
            //Directory.CreateDirectory($"{reportDir.FullName}\\audio");
        }

        private void DataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            FileInfo recordFile = new FileInfo(dataGridView2[3, e.RowIndex].Value.ToString());
            Vox2Wav.Decode(recordFile.FullName, $"{Path.GetTempPath()}\\DCRSaudio\\{recordFile.Name}.wav", true);
            axWindowsMediaPlayer1.URL = $"{Path.GetTempPath()}\\DCRSaudio\\{recordFile.Name}.wav";
            axWindowsMediaPlayer1.Ctlcontrols.play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            maskedTextBox1.Text = DateTime.Now.AddHours(-1).ToString("HHmm");
            maskedTextBox2.Text = DateTime.Now.ToString("HHmm");
            comboBox1.SelectedIndex = 0;
            toolStripProgressBar1.Visible = false;
            lblProgress.Visible = false;

            if (File.Exists($"{Environment.CurrentDirectory}\\config.cfg"))
            {
                txtDCRSPath.Text = File.ReadAllText($"{Environment.CurrentDirectory}\\config.cfg");
            }
            CancellationToken cancelPing = new CancellationToken();
            Task result = PeriodicPingDCRSAsync(5000, cancelPing);
        }

        private void CreateJsonTimelineData(List<RecordModel> records)
        {
            var groupsData = records.Select(s => s.AgentName).Distinct().OrderByDescending(o => o).Select(s => new
            {
                id = s,
                content = s
            });
            var itemsData = records.Select(s => new
            {
                id = s.GetHashCode(),
                content = $"<audio controls><source src='audio/{s.RecordFile.Name}.wav' type='audio/wav'><audio>",
                start = s.RecordStart,
                end = s.RecordEnd,
                group = s.AgentName
            });
            var titlesData = (from DataGridViewRow row in dataGridView1.Rows
                              select new
                              {
                                  id = "custombar" + row.Cells[1].GetHashCode(),
                                  dateTime = row.Cells[0].Value,
                                  title = row.Cells[1].Value
                              });            
            var serializer = new JavaScriptSerializer();
            string HTML = File.ReadAllText($"{folderBrowserDialog1.SelectedPath}\\ReportDCRS.html");
            HTML = $"{HTML.Substring(0, HTML.IndexOf("GROUPS_START") + 13)}var groups = new vis.DataSet({serializer.Serialize(groupsData)});{HTML.Substring(HTML.IndexOf("\n//GROUPS_END"))}";

            HTML = $"{HTML.Substring(0, HTML.IndexOf("ITEMS_START") + 12)}var items = new vis.DataSet({serializer.Serialize(itemsData)});{HTML.Substring(HTML.IndexOf("\n//ITEMS_END"))}";

            HTML = $"{HTML.Substring(0, HTML.IndexOf("TITLES_START") + 13)}var titles = {serializer.Serialize(titlesData)};{HTML.Substring(HTML.IndexOf("\n//TITLES_END"))}";

            File.WriteAllText($"{folderBrowserDialog1.SelectedPath}\\ReportDCRS.html", HTML);
        }

        private void ConvertRecordsToWav(List<RecordModel> records)
        {
             foreach (var item in records)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        toolStripProgressBar1.Value += 1;
                        lblProgress.Text = $"Gravação {toolStripProgressBar1.Value} de {records.Count()}";
                    }));
                }
                Vox2Wav.Decode(item.RecordFile.FullName, $"{folderBrowserDialog1.SelectedPath}\\audio\\{item.RecordFile.Name}.wav", true);
            }
        }

        private async Task PeriodicPingDCRSAsync(int interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                Ping ping = new Ping();      
                try
                {
                    lblPingStatus.Image = Resources.conn_pcs_on_off.ToBitmap();
                    lblPingStatus.Text = "";
                    await Task.Delay(1000, cancellationToken);
                    PingReply reply = ping.Send(txtDCRSPath.Text.Replace("\\",""));
                    if (reply.Status == IPStatus.Success)
                    {
                        lblPingStatus.Image = Resources.conn_pcs_on_on.ToBitmap();
                        lblPingStatus.Text = $" ({reply.RoundtripTime} ms)";
                    }
                    ping.Dispose();
                }
                catch (Exception)
                {
                    lblPingStatus.Image = Resources.conn_pcs_no_network.ToBitmap();
                    lblPingStatus.Text = "";
                    ping.Dispose();
                }        
                await Task.Delay(interval, cancellationToken);
            }
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Insira novo endereço para DCRS", "Editar", "", -1, -1);
            if (input.Trim() != "")
            {
                txtDCRSPath.Text = input;
                File.WriteAllText($"{Environment.CurrentDirectory}\\config.cfg", input);
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result != DialogResult.OK) throw new Exception("Selecione uma pasta destino para criar a visualização");
                new Microsoft.VisualBasic.Devices.Computer().FileSystem.CopyDirectory(reportDir.FullName, folderBrowserDialog1.SelectedPath);

                if (dateTimePicker1.Text == "" || dateTimePicker2.Text == "" || maskedTextBox1.Text == "" || maskedTextBox2.Text == "" || comboBox1.SelectedIndex == -1) throw new Exception("Selecione o intervalo de interesse e origem");

                DCRSData DCRSData = new DCRSData(txtDCRSPath.Text);
                DateTime dateStart = DateTime.Parse($"{dateTimePicker1.Text} {maskedTextBox1.Text}");
                DateTime dateEnd = DateTime.Parse($"{dateTimePicker2.Text} {maskedTextBox2.Text}");
                
                if ((dateEnd - dateStart).TotalDays > 2)
                {
                    throw new Exception("Intervalo de dias deve ser inferior a 2 dias");
                }
                List<RecordModel> Records = DCRSData.GetRecords(dateStart,dateEnd);
                List<RecordModel> filteredRecords = new List<RecordModel>();

                if (comboBox1.SelectedIndex == 0) //completo
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd).ToList();
                }
                else if (comboBox1.SelectedIndex == 1) //turno
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd && w.AgentName.Contains("MESA")).ToList();
                }
                else //comercial
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd && !w.AgentName.Contains("MESA")).ToList();
                }
                toolStripProgressBar1.Visible = true;
                lblProgress.Visible = true;
                toolStripProgressBar1.Maximum = filteredRecords.Count();
                toolStripProgressBar1.Value = 0;
                foreach (Control control in this.Controls)
                {
                    control.Enabled = false;
                }
                await Task.Run(() =>
                {                    
                    CreateJsonTimelineData(filteredRecords);
                    ConvertRecordsToWav(filteredRecords);
                });
                foreach (Control control in this.Controls)
                {
                    control.Enabled = true;
                }
                toolStripProgressBar1.Visible = false;
                lblProgress.Visible = false;
                Process.Start("chrome", string.Format("\"{0}\"", $"{folderBrowserDialog1.SelectedPath.ToString()}\\ReportDCRS.html"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                foreach (Control control in this.Controls)
                {
                    control.Enabled = true;
                }
                toolStripProgressBar1.Visible = false;
                lblProgress.Visible = false;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add(DateTime.Parse($"{dateTimePicker3.Text} {maskedTextBox3.Text}"), txtTitle.Text);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                if (dateTimePicker1.Text == "" || dateTimePicker2.Text == "" || maskedTextBox1.Text == "" || maskedTextBox2.Text == "" || comboBox1.SelectedIndex == -1) throw new Exception("Selecione o intervalo de interesse e origem");

                DCRSData DCRSData = new DCRSData(txtDCRSPath.Text);
                DateTime dateStart = DateTime.Parse($"{dateTimePicker1.Text} {maskedTextBox1.Text}");
                DateTime dateEnd = DateTime.Parse($"{dateTimePicker2.Text} {maskedTextBox2.Text}");

                List<RecordModel> Records = DCRSData.GetRecords(dateStart, dateEnd);
                List<RecordModel> filteredRecords = new List<RecordModel>();

                if (comboBox1.SelectedIndex == 0) //completo
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd).ToList();
                }
                else if (comboBox1.SelectedIndex == 1) //turno
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd && w.AgentName.Contains("MESA")).ToList();
                }
                else //comercial
                {
                    filteredRecords = Records.Where(w => w.RecordStart > dateStart && w.RecordEnd < dateEnd && !w.AgentName.Contains("MESA")).ToList();
                }
                dataGridView2.DataSource = filteredRecords.Select(s => new
                {
                    s.RecordStart,
                    s.AgentName,
                    s.Duration,
                    s.RecordFile
                }).ToList();
                dataGridView2.Columns[0].HeaderText = "Início";
                dataGridView2.Columns[1].HeaderText = "Origem";
                dataGridView2.Columns[2].HeaderText = "Duração (s)";
                dataGridView2.Columns[3].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
