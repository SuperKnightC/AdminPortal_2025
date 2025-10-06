using System.Data;
using Microsoft.Data.SqlClient;
using AdminPortal.Models;

namespace AdminPortal.Data
{
    public class PackageRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public PackageRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task InsertPackage(string imageID,string packageType, string name, decimal price, decimal point, int validDay, int dayPass, DateTime LastValidDate, string recordStatus, 
                                        DateTime createdDate, int createdUserID, DateTime modifiedDate, int modifiedUserID, string remarks )
        {
            using (var conn = _databaseHelper.GetConnection()) //use dbHelper for sqlConn
            {
                await conn.OpenAsync(); //open the connection

                var cmd = new SqlCommand("Insert into packages(ImageID,PackageType,Name,Price,Point,ValidDay,DayPass,LastValidDate," +
                                         "RecordStatus,CreatedDate,CreatedUserID,ModifiedDate,ModifiedUserID,Remark) " +
                                         "VALUES (@ImageID,@PackageType,@Name,@Price,@Point,@ValidDay,@DayPass,@LastValidDate,@RecordStatus,@CreatedDate," +
                                         "@CreatedUserID,@ModifiedDate,@ModifiedUserID,@Remark)", conn);//create queries

                cmd.Parameters.AddWithValue("ImageID", imageID);
                cmd.Parameters.AddWithValue("PackageType", packageType);
                cmd.Parameters.AddWithValue("Name", name);
                cmd.Parameters.AddWithValue("Price", price);
                cmd.Parameters.AddWithValue("Point", point);
                cmd.Parameters.AddWithValue("ValidDay",validDay);
                cmd.Parameters.AddWithValue("DayPass", dayPass);
                cmd.Parameters.AddWithValue("LastValidDate", LastValidDate);
                cmd.Parameters.AddWithValue("RecordStatus",recordStatus);
                cmd.Parameters.AddWithValue("CreatedDate",createdDate);
                cmd.Parameters.AddWithValue("CreatedUserID", createdUserID);
                cmd.Parameters.AddWithValue("ModifiedDate",modifiedDate);
                cmd.Parameters.AddWithValue("ModifiedUserID",modifiedUserID);
                cmd.Parameters.AddWithValue("Remark", remarks);



                await cmd.ExecuteNonQueryAsync();//insert

            }

        }


    }
}
