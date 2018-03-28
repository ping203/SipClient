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
    class SQLiteBase
    {
        private static int MAX_RECORDS = 25;
        private static string _CONN_STR_TO_SQLITE_DB = 
        	string.Concat("Data Source=",
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            Properties.Resources.CallerDataBaseFileName);

        /// <summary>
        /// Add record to dataase
        /// </summary>
        public static void AddRecordToDataBase(SipClient.Classes.CallRecord call)
        {
            string sql = string.Format("insert into calls(Phone, TimeStart, TimeEnd, isIncoming, isOutcoming, isRejected) values('{0}' ,'{1}' ,'{2}' ,{3}, {4}, {5})",
                call.Phone,
                call.TimeStart,
                call.TimeEnd,
                call.isIncoming ? 1 : 0,
                call.isOutcoming ? 1 : 0,
                call.isRejected ? 1 : 0);

            using (var connection = new SQLiteConnection(_CONN_STR_TO_SQLITE_DB))
            {
                connection.Open();
                // check connection state
                if (connection.State != ConnectionState.Open)
                    throw new Exception("Connection is not open!");

                // check overflow
                if (CheckBaseOverflow())
                    RemoveFirstRecord(connection);

                // add record to table
                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// True if base is Overflow
        /// </summary>
        private static bool CheckBaseOverflow()
        {
            int recordsCount = 0;

            var dt = GetDataTable("select count(Phone) from calls;");

            if (dt.Rows.Count == 0)
            {
                throw new Exception("Data can't load! Check database!");
            }
            recordsCount = Convert.IsDBNull(dt.Rows[0][0]) ? 0 : Convert.ToInt32(dt.Rows[0][0]);

            return (recordsCount >= MAX_RECORDS);
        }

        /// <summary>
        /// remove first record
        /// </summary
        private static void RemoveFirstRecord(SQLiteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                throw new Exception("Connection is not open!");
            // get first record
            var dt = GetDataTable("select * from calls order by TimeStart asc limit 1");
            if (dt != null && dt.Rows.Count > 0)
            {
                string value = dt.Rows[0]["TimeStart"].ToString();
                // remove him
                using (SQLiteCommand cmd = new SQLiteCommand(string.Format("delete from calls where TimeStart = '{0}';", value), connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void RemoveAllRecords()
        {
            using (var connection = new SQLiteConnection(_CONN_STR_TO_SQLITE_DB))
            {
                connection.Open();

                if (connection.State != ConnectionState.Open)
                    throw new Exception("Connection is not open!");

                using (SQLiteCommand cmd = new SQLiteCommand("delete from calls;",connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();

            using (var c = new SQLiteConnection(_CONN_STR_TO_SQLITE_DB))
            {
                c.Open();

                if (c.State != ConnectionState.Open)
                    throw new Exception("Connection is not open!");

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
    }
}
