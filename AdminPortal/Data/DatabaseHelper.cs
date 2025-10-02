using Microsoft.Data.SqlClient;
using System.Data;
namespace AdminPortal.Data //declare the namespace for this file
{
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
}