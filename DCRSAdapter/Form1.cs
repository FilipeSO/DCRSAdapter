using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DCRSDataLayer;

namespace DCRSAdapter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DCRSData DCRSData = new DCRSData(Environment.CurrentDirectory);
            try
            {
                List<RecordModel> Records = DCRSData.GetRecords(new DateTime(2019, 1, 16), new DateTime(2019, 1, 18));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
