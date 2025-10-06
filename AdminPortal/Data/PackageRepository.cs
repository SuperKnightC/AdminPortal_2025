using System.Data; //database tools
using Microsoft.Data.SqlClient; // like a bridge to sql server
using AdminPortal.Models; //call the model

namespace AdminPortal.Data //set the namespace for reference
{
    public class PackageRepository
    {
        private readonly DatabaseHelper _databaseHelper; //assign an object of dbhelper, readonly mean can be assign once only
        public PackageRepository(DatabaseHelper databaseHelper) //constructor to receive the dbhelper object via DI
        {
            _databaseHelper = databaseHelper;
        }

        // Change return type to Task<int>
        public async Task<int> InsertPackage(PackageViewModel package) //method to insert package, async for await
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync(); //open the connection, await mean wait for the task to complete, non-blocking

                int validDays = 0; //calculate valid days
                if (package.LastValidDate >= package.effectiveDate)
                {
                    validDays = (int)(package.LastValidDate - package.effectiveDate).TotalDays;
                }

                //prepare the sql command/queries
                var cmd = new SqlCommand(
                    "INSERT INTO Packages(Name, packageType, ValidDays, LastValidDate, remark) " +
                    "VALUES (@Name, @packageType, @ValidDays, @LastValidDate, @remark);" +
                    "SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@Name", package.Name);
                cmd.Parameters.AddWithValue("@packageType", package.packageType);
                cmd.Parameters.AddWithValue("@ValidDays", validDays);
                cmd.Parameters.AddWithValue("@LastValidDate", package.LastValidDate);
                cmd.Parameters.AddWithValue("@remark", (object)package.remark ?? DBNull.Value); 

                // Use ExecuteScalarAsync to get the returned ID
                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
    }
}
