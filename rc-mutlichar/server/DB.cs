using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using MySql.Data.MySqlClient;

namespace Retro_Multichar_sv
{
    public class DB : BaseScript
    {
        static MySqlConnection conn;
        static DB()
        {
            Debug.WriteLine("Trying to connect to the DB");
            try
            {
                conn = new MySqlConnection("conneectionstring");
                conn.Open();
                Debug.WriteLine("Connection to DB established");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }            
        }
        public static List<object[]> Retrieve(string query)
        {
            Console.WriteLine(query);
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                List<object[]> data = new List<object[]>();

                while (rdr.Read())
                {
                    object[] rowData = new object[rdr.FieldCount];
                    rdr.GetValues(rowData);
                    data.Add(rowData);
                }
                rdr.Close();
                return data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public static void Insert(string query)
        {
            Console.WriteLine(query);
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public static bool CheckIfExist(string query)
        {
            Console.WriteLine(query);
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.HasRows)
                {
                    rdr.Close();
                    return true;
                }
                rdr.Close();
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
