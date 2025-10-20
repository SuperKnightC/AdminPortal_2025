using Microsoft.Data.SqlClient;
using System.Data;
namespace AdminPortal.Data //declare the namespace for this file
{
    #region -- DB connection
    public class DatabaseHelper
    {
        private readonly string _connectionString; //declare a read only file loaded from appsetting.json

        public DatabaseHelper(string connectionString) //asp.net framework read the default conn and pass it to constructor
        {
            _connectionString = connectionString; // assign the the framework string received to private field ready to use
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString); //repository that need access to db will call this method
            }
    }
    #endregion
    public static class SqlDataReaderExtensions 
    {
        public static bool HasColumn(this SqlDataReader dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}