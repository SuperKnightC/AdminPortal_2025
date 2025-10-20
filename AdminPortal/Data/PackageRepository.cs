using System.Data; //database tools
using Microsoft.Data.SqlClient; // like a bridge to sql server
using AdminPortal.Models; //call the model

namespace AdminPortal.Data //set the namespace for reference
{
    public class PackageRepository
    {
        // This repo handles all database operations related to Package

        #region-- Database Helper --
        private readonly DatabaseHelper _databaseHelper; //assign an object of dbhelper, readonly mean can be assign once only
        public PackageRepository(DatabaseHelper databaseHelper) //constructor to receive the dbhelper object via DI
        {
            _databaseHelper = databaseHelper;
        }
        #endregion

        #region -- Package Insert --
        public async Task<int> InsertPackage(PackageViewModel package, int createdUserId)
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
                cmd.Parameters.AddWithValue("@Remark", (object)package.remark ?? DBNull.Value);

                // == Calculated Value ==
                cmd.Parameters.AddWithValue("@ValidDays", validDays);

                // == Hardcoded Values as Requested ==
                cmd.Parameters.AddWithValue("@PackageNo", DBNull.Value); // Set to NULL
                cmd.Parameters.AddWithValue("@GroupEntityID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@TerminalGroupID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@ProductID", 1); // Fixed value
                cmd.Parameters.AddWithValue("@ImageID", (object)package.ImageID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DaysPass", 7); // Fixed value for now
                cmd.Parameters.AddWithValue("@Link", "#"); // Fixed value
                cmd.Parameters.AddWithValue("@RecordStatus", "Pending"); // Default status
                cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                cmd.Parameters.AddWithValue("@ModifiedUserID", createdUserId);  // Fixed user ID

                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
        #endregion

        #region-- Get Package By ID For Details --
        public async Task<Package?> GetPackageByIdAsync(int id)
        {
            Package package = null;
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT * FROM Packages WHERE PackageID = @PackageID", conn);
                cmd.Parameters.AddWithValue("@PackageID", id);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        package = MapPackageFromReader(reader);
                    }
                }
            }
            return package;
        }

        #endregion

        #region-- Get All Packages For Dashboard --
        public async Task<List<Package>> GetAllAsync(string statusFilter = null)
        {
            var packages = new List<Package>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                // 1. Start building the query string and create the command object
                string sql = @"
                SELECT
                    p.*,
                    u_created.staff_name AS CreatedByName,
                    u_modified.staff_name AS ModifiedByName
                FROM Packages p
                LEFT JOIN useru u_created ON p.CreatedUserID = u_created.UserID
                LEFT JOIN useru u_modified ON p.ModifiedUserID = u_modified.UserID";
                var cmd = new SqlCommand();

                // 2. If a filter is provided, add the WHERE clause and the parameter
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Show All")
                {
                    sql += " WHERE RecordStatus = @Status";
                    cmd.Parameters.AddWithValue("@Status", statusFilter);
                }

                // 3. Now, assign the final SQL and connection to the command
                cmd.CommandText = sql;
                cmd.Connection = conn;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Using the helper method is critical to prevent crashes from NULL data
                        packages.Add(MapPackageFromReader(reader));
                    }
                }
            }
            return packages;
        }
        #endregion

        #region-- Reject Package --
        public async Task RejectPackageAsync(int packageId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE Packages SET RecordStatus = 'Rejected' WHERE PackageID = @PackageID", conn);
                cmd.Parameters.AddWithValue("@PackageID", packageId);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        #endregion

        #region-- Approve Package --
        public async Task ApprovePackageAsync(int packageId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Read the main package data
                        var selectPackageCmd = new SqlCommand("SELECT * FROM Packages WHERE PackageID = @PackageID", conn, transaction);
                        selectPackageCmd.Parameters.AddWithValue("@PackageID", packageId);

                        Package packageToCopy = null;
                        using (var reader = await selectPackageCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // == MAPPING 1: Complete package mapping ==
                                packageToCopy = new Package
                                {
                                    PackageID = (int)reader["PackageID"],
                                    PackageNo = reader["PackageNo"]?.ToString(),
                                    Name = reader["Name"].ToString(),
                                    PackageType = reader["PackageType"].ToString(),
                                    Price = (decimal)reader["Price"],
                                    Point = (int)reader["Point"],
                                    ValidDays = (int)reader["ValidDays"],
                                    DaysPass = (int)reader["DaysPass"],
                                    LastValidDate = (DateTime)reader["LastValidDate"],
                                    Link = reader["Link"]?.ToString(),
                                    Status = "Approved", // Set status to Approved for the new table
                                    CreatedDate = (DateTime)reader["CreatedDate"],
                                    CreatedUserID = (int)reader["CreatedUserID"],
                                    ModifiedDate = (DateTime)reader["ModifiedDate"],
                                    ModifiedUserID = (int)reader["ModifiedUserID"],
                                    GroupEntityID = (int)reader["GroupEntityID"],
                                    TerminalGroupID = (int)reader["TerminalGroupID"],
                                    ProductID = (long)reader["ProductID"],
                                    ImageID = reader["ImageID"]?.ToString(),
                                    Remark = reader["Remark"]?.ToString()
                                };
                            }
                        }
                        if (packageToCopy == null) throw new Exception("Package not found.");

                        // 2. Insert the package data into the new App_PackageAO table
                        var insertPackageCmd = new SqlCommand(
                            @"INSERT INTO App_PackageAO (PackageNo, Name, PackageType, Price, Point, ValidDays, DaysPass, LastValidDate, Link, RecordStatus, CreatedDate, CreatedUserID, ModifiedDate, ModifiedUserID, GroupEntityID, TerminalGroupID, ProductID, ImageID, Remark) 
                      VALUES (@PackageNo, @Name, @PackageType, @Price, @Point, @ValidDays, @DaysPass, @LastValidDate, @Link, @RecordStatus, @CreatedDate, @CreatedUserID, @ModifiedDate, @ModifiedUserID, @GroupEntityID, @TerminalGroupID, @ProductID, @ImageID, @Remark)", conn, transaction);

                        // == MAPPING 2: Add all parameters for the package insert ==
                        insertPackageCmd.Parameters.AddWithValue("@PackageNo", (object)packageToCopy.PackageNo ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@Name", packageToCopy.Name);
                        insertPackageCmd.Parameters.AddWithValue("@PackageType", packageToCopy.PackageType);
                        insertPackageCmd.Parameters.AddWithValue("@Price", packageToCopy.Price);
                        insertPackageCmd.Parameters.AddWithValue("@Point", packageToCopy.Point);
                        insertPackageCmd.Parameters.AddWithValue("@ValidDays", packageToCopy.ValidDays);
                        insertPackageCmd.Parameters.AddWithValue("@DaysPass", packageToCopy.DaysPass);
                        insertPackageCmd.Parameters.AddWithValue("@LastValidDate", packageToCopy.LastValidDate);
                        insertPackageCmd.Parameters.AddWithValue("@Link", (object)packageToCopy.Link ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@RecordStatus", packageToCopy.Status);
                        insertPackageCmd.Parameters.AddWithValue("@CreatedDate", packageToCopy.CreatedDate);
                        insertPackageCmd.Parameters.AddWithValue("@CreatedUserID", packageToCopy.CreatedUserID);
                        insertPackageCmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now); // Set new modified date
                        insertPackageCmd.Parameters.AddWithValue("@ModifiedUserID", packageToCopy.ModifiedUserID);
                        insertPackageCmd.Parameters.AddWithValue("@GroupEntityID", packageToCopy.GroupEntityID);
                        insertPackageCmd.Parameters.AddWithValue("@TerminalGroupID", packageToCopy.TerminalGroupID);
                        insertPackageCmd.Parameters.AddWithValue("@ProductID", packageToCopy.ProductID);
                        insertPackageCmd.Parameters.AddWithValue("@ImageID", (object)packageToCopy.ImageID ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@Remark", (object)packageToCopy.Remark ?? DBNull.Value);
                        await insertPackageCmd.ExecuteNonQueryAsync();

                        // 3. Read all associated package items
                        var selectItemsCmd = new SqlCommand("SELECT * FROM PackageItem WHERE PackageID = @PackageID", conn, transaction);
                        selectItemsCmd.Parameters.AddWithValue("@PackageID", packageId);

                        var itemsToCopy = new List<PackageItem>();
                        using (var reader = await selectItemsCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // == MAPPING 3: Complete item mapping ==
                                itemsToCopy.Add(new PackageItem
                                {
                                    PackageID = (int)reader["PackageID"],
                                    ItemName = reader["ItemName"].ToString(),
                                    itemType = reader["itemType"].ToString(),
                                    Price = reader["ItemPrice"] as decimal?,
                                    Point = reader["ItemPoint"] as int?,
                                    AgeCategory = reader["AgeCategory"].ToString(),
                                    EntryQty = (int)reader["EntryQty"]
                                });
                            }
                        }

                        // 4. Insert each item into the new App_PackageItemAO table
                        foreach (var item in itemsToCopy)
                        {
                            var insertItemCmd = new SqlCommand(
                                @"INSERT INTO App_PackageItemAO (PackageID, PackageQty, ItemName, ItemType, ItemPrice, ItemPoint, AgeCategory, EntryQty) 
                          VALUES (@PackageID,@PackageQty, @ItemName, @itemType, @ItemPrice, @ItemPoint, @AgeCategory, @EntryQty)", conn, transaction);

                            // == MAPPING 4: Add all parameters for the item insert ==
                            insertItemCmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                            insertItemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                            insertItemCmd.Parameters.AddWithValue("@itemType", item.itemType);
                            insertItemCmd.Parameters.AddWithValue("@ItemPrice", (object)item.Price ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@ItemPoint", (object)item.Point ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                            insertItemCmd.Parameters.AddWithValue("@PackageQty", item.EntryQty);
                            insertItemCmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);
                            await insertItemCmd.ExecuteNonQueryAsync();
                        }

                        // 5. Update the original package's status to "Approved"
                        var updateStatusCmd = new SqlCommand("UPDATE Packages SET RecordStatus = 'Approved' WHERE PackageID = @PackageID", conn, transaction);
                        updateStatusCmd.Parameters.AddWithValue("@PackageID", packageId);
                        await updateStatusCmd.ExecuteNonQueryAsync();

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        #endregion

        #region -- Map Package From Reader Helper Method --
        private Package MapPackageFromReader(SqlDataReader reader)
        {
            var package = new Package
            {
                PackageID = (int)reader["PackageID"],
                Name = reader["Name"].ToString(),
                PackageType = reader["PackageType"].ToString(),
                Status = reader["RecordStatus"].ToString(),
                Link = reader["Link"].ToString(),
                PackageNo = reader["PackageNo"]?.ToString(),

                // Safely handle all nullable DateTime types
                LastValidDate = reader["LastValidDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["LastValidDate"],
                CreatedDate = reader["CreatedDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["CreatedDate"],
                ModifiedDate = reader["ModifiedDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["ModifiedDate"],

                // Safely handle all nullable numeric and string types
                Price = reader["Price"] is DBNull ? 0 : (decimal)reader["Price"],
                Point = reader["Point"] is DBNull ? 0 : (int)reader["Point"],
                ValidDays = reader["ValidDays"] is DBNull ? 0 : (int)reader["ValidDays"],
                DaysPass = reader["DaysPass"] is DBNull ? 0 : (int)reader["DaysPass"],
                CreatedUserID = reader["CreatedUserID"] is DBNull ? 0 : (int)reader["CreatedUserID"],
                ModifiedUserID = reader["ModifiedUserID"] is DBNull ? 0 : (int)reader["ModifiedUserID"],
                GroupEntityID = reader["GroupEntityID"] is DBNull ? 0 : (int)reader["GroupEntityID"],
                TerminalGroupID = reader["TerminalGroupID"] is DBNull ? 0 : (int)reader["TerminalGroupID"],
                ProductID = reader["ProductID"] is DBNull ? 0 : (long)reader["ProductID"],
                ImageID = reader["ImageID"] is DBNull ? null : reader["ImageID"].ToString()


            };

            // --- MAP THE NEW JOINED NAMES ---
            // Check if the joined columns exist before trying to read them
            if (reader.HasColumn("CreatedByName"))
            {
                package.CreatedByName = reader["CreatedByName"] is DBNull ? "N/A" : reader["CreatedByName"].ToString();
            }
            if (reader.HasColumn("ModifiedByName"))
            {
                package.ModifiedByName = reader["ModifiedByName"] is DBNull ? "N/A" : reader["ModifiedByName"].ToString();
            }

            return package;
        }
        #endregion

        #region -- Update package status with UserID --
        public async Task<bool> UpdatePackageStatusAsync(int packageId, string newStatus, int modifiedUserId)
        {
            using (var connection = _databaseHelper.GetConnection())
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "UPDATE Packages SET RecordStatus = @Status, ModifiedUserID = @ModifiedUserID, ModifiedDate = GETDATE() WHERE PackageID = @PackageID",
                    connection);
                command.Parameters.AddWithValue("@Status", newStatus);
                command.Parameters.AddWithValue("@ModifiedUserID", modifiedUserId); // Use the passed-in user ID
                command.Parameters.AddWithValue("@PackageID", packageId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
        #endregion
    }

}
