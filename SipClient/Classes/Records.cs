using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;

namespace SipClient.Classes
{
    class Records
    {
        private string sqliteConString = @"Data Source=.\calls.db";

        private void AddRecordToTable(SipClient.Classes.CallRecord call)
        {
            string sql = string.Format("insert into calls(Phone, Time, Type) values('{0}' ,'{1}' , {2})", call.Phone, call.Time, call.Type);

            using (var c = new SQLiteConnection(sqliteConString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private DataTable GetDataTable(string sql)
        {
            try
            {
                DataTable dt = new DataTable();
                using (var c = new SQLiteConnection(sqliteConString))
                {
                    c.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            dt.Load(rdr);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
