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

namespace DCRSAdapter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DCRSData DCRSData = new DCRSData(Environment.CurrentDirectory);
            bool teste = PingDCRS("Desktop-8a96f40");
            DirectoryInfo reportDir = new DirectoryInfo($"{Environment.CurrentDirectory}\\report");
            if (!reportDir.Exists)
            {
                Directory.CreateDirectory(reportDir.FullName);
            }
            try
            {
                List<RecordModel> Records = DCRSData.GetRecords(new DateTime(2019, 1, 16), new DateTime(2019, 1, 18));
                foreach (var item in Records)
                {
                    Vox2Wav.Decode(item.RecordFile.FullName, $"{reportDir}\\{item.RecordFile.Name}.wav",true);                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool PingDCRS(string DCRSName)
        {
            bool avaliable = false;
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send(DCRSName);
                avaliable = reply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return avaliable;
            }
            return avaliable;         
        }
    }
}
