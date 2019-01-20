using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DCRSDataLayer
{
    public class DCRSData
    {
        private readonly string DCRSRoot;

        public DCRSData(string root)
        {
            DCRSRoot = root;
        }

        public List<RecordModel> GetRecords(DateTime dateStart, DateTime dateEnd)
        {
            List<RecordModel> Records = new List<RecordModel>();
            List<FileInfo> MDBFiles = GetMDBFiles(dateStart, dateEnd);
            foreach (FileInfo item in MDBFiles)
            {
                var dataTable = new DataTable();
                using (var conection = new OleDbConnection($"Provider=Microsoft.JET.OLEDB.4.0;data source={item.FullName}"))
                {
                    conection.Open();
                    var query = $"Select * From VoiceRecords";
                    var command = new OleDbCommand(query, conection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Records.Add(new RecordModel
                        {                   
                            RecordStart = DateTime.ParseExact($"{item.Name.Substring(0, 6)}{reader[6].ToString().Substring(5).Replace(".","")}", "yyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture),
                            RecordEnd = DateTime.ParseExact($"{item.Name.Substring(0, 6)}{reader[6].ToString().Substring(5).Replace(".", "")}", "yyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture).AddSeconds(int.Parse(reader[5].ToString())),
                            Duration = int.Parse(reader[5].ToString()),
                            AgentName = reader[18].ToString(),
                            RecordFile = new FileInfo($"{DCRSRoot}\\DCRS\\MESSAGE\\{int.Parse(reader[13].ToString()).ToString("D3")}\\{reader[6].ToString()}")
                        });
                    }                    
                }
            }
            return Records;

        }

        private List<FileInfo> GetMDBFiles(DateTime dateStart, DateTime dateEnd)
        {
            if ((dateEnd - dateStart).TotalDays > 3)
            {
                throw new Exception("Intervalo selecionado superior a 2 dias. Valor limitado para evitar erro de leitura no DCRS.");
            }
            List<FileInfo> MDBFiles = new List<FileInfo>();
            for (DateTime date = dateStart; date.Date <= dateEnd.Date; date = date.AddDays(1))
            {
                FileInfo MDBFileFullPath = new FileInfo($"{DCRSRoot}\\DCRS\\DATABASE\\{date.ToString("yyMMdd")}00.MDB");
                if (MDBFileFullPath.Exists)
                {
                    MDBFiles.Add(MDBFileFullPath);
                }
                else
                {
                    throw new Exception("Não há registro para uma ou mais datas do intervalo escolhido");
                }
            }
            return MDBFiles;
        }
    }
}
