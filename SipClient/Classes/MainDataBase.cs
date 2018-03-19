using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows;

namespace SipClient.Classes
{
    public class MainDataBase
    {
        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Properties.Resources.ConnectionStringCentralDB))
                {
                    conn.Open();

                    if (conn.State != ConnectionState.Open)
                        throw new Exception("Connection not resolve!");

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            dt.Load(dr);
                        }
                    };
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
            }

            return dt;
        }
    }
}
