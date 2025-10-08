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
        public async Task<int> InsertPackage(PackageViewModel package)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                int validDays = 0;
                if (package.LastValidDate >= package.effectiveDate)
                {
                    validDays = (int)(package.LastValidDate - package.effectiveDate).TotalDays + 1;
                }

                // Updated SQL command with all new columns
                var cmd = new SqlCommand(
                    @"INSERT INTO Packages(
                PackageNo, Name, PackageType, Price, Point, ValidDays, DaysPass,
                LastValidDate, Link, RecordStatus, CreatedDate, CreatedUserID,
                ModifiedDate, ModifiedUserID, GroupEntityID, TerminalGroupID,
                ProductID, ImageID, Remark
                ) VALUES (
                    @PackageNo, @Name, @PackageType, @Price, @Point, @ValidDays, @DaysPass,
                    @LastValidDate, @Link, @RecordStatus, GETDATE(), @CreatedUserID,
                    GETDATE(), @ModifiedUserID, @GroupEntityID, @TerminalGroupID,
                    @ProductID, @ImageID, @Remark
                );
                SELECT SCOPE_IDENTITY();", conn);

                // == Parameters from the ViewModel ==
                cmd.Parameters.AddWithValue("@Name", package.Name);
                cmd.Parameters.AddWithValue("@PackageType", package.packageType);
                cmd.Parameters.AddWithValue("@Price", package.Price);
                cmd.Parameters.AddWithValue("@Point", package.Point);
                cmd.Parameters.AddWithValue("@LastValidDate", package.LastValidDate);
                cmd.Parameters.AddWithValue("@Remark", (object)package.remark ?? DBNull.Value); // Note: Your INSERT doesn't have @remark, you may want to add it.

                // == Calculated Value ==
                cmd.Parameters.AddWithValue("@ValidDays", validDays);

                // == Hardcoded Values as Requested ==
                cmd.Parameters.AddWithValue("@PackageNo", DBNull.Value); // Set to NULL
                cmd.Parameters.AddWithValue("@GroupEntityID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@TerminalGroupID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@ProductID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@ImageID", "30"); // Fixed value
                cmd.Parameters.AddWithValue("@DaysPass", 7); // Fixed value (example)
                cmd.Parameters.AddWithValue("@Link", "#"); // Fixed value
                cmd.Parameters.AddWithValue("@RecordStatus", "Pending"); // Default status
                cmd.Parameters.AddWithValue("@CreatedUserID", 1); // Fixed user ID
                cmd.Parameters.AddWithValue("@ModifiedUserID", 1); // Fixed user ID

                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
    }
}
