using System.Data;
using Microsoft.Data.SqlClient;
using be_general_support_api.Models;

namespace be_general_support_api.Data
{
    // This Repo Handle Package
    public class PackageRepository
    {
        #region-- Database Helper --
        private readonly DatabaseHelper _databaseHelper;
        public PackageRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }
        #endregion

        #region -- Package Insert --
        // Inserts a new package into the database
        // Returns the newly created PackageID
        // Used in the package creation process
        // Calculates ValidDays based on effective and last valid dates
        // Sets default values for certain fields
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

                cmd.Parameters.AddWithValue("@Name", package.Name);
                cmd.Parameters.AddWithValue("@PackageType", package.packageType);
                cmd.Parameters.AddWithValue("@Price", package.Price);
                cmd.Parameters.AddWithValue("@Point", package.Point);
                cmd.Parameters.AddWithValue("@LastValidDate", package.LastValidDate);
                cmd.Parameters.AddWithValue("@Remark", (object)package.remark ?? DBNull.Value);
                
                cmd.Parameters.AddWithValue("@ValidDays", validDays);
                cmd.Parameters.AddWithValue("@PackageNo", DBNull.Value);
                cmd.Parameters.AddWithValue("@GroupEntityID", 1);
                cmd.Parameters.AddWithValue("@TerminalGroupID", 1);
                cmd.Parameters.AddWithValue("@ProductID", 1);
                cmd.Parameters.AddWithValue("@ImageID", (object)package.ImageID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DaysPass", 7);
                cmd.Parameters.AddWithValue("@Link", "#");
                cmd.Parameters.AddWithValue("@RecordStatus", "Pending");
                cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                cmd.Parameters.AddWithValue("@ModifiedUserID", createdUserId);

                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
        #endregion

        #region-- Get Package By ID For Details --
        // Retrieves a package by its ID, including creator and modifier names
        // Used in the package details view
        // Returns null if not found
        public async Task<Package?> GetPackageByIdAsync(int id)
        {
            Package package = null;
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT
                        p.*,
                        u_created.staff_name AS CreatedByName,
                        u_modified.staff_name AS ModifiedByName,
                        acc_created.FirstName AS CreatedByFirstName,
                        acc_modified.FirstName AS ModifiedByFirstName
                    FROM Packages p
                    LEFT JOIN useru u_created ON p.CreatedUserID = u_created.account_id
                    LEFT JOIN useru u_modified ON p.ModifiedUserID = u_modified.account_id
                    LEFT JOIN App_Account acc_created ON p.CreatedUserID = acc_created.AccID
                    LEFT JOIN App_Account acc_modified ON p.ModifiedUserID = acc_modified.AccID
                    WHERE p.PackageID = @PackageID", conn);

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
        // Retrieves all packages with optional status filtering
        // Supports "Active", "Show All", and other status filters
        // Uses UNION ALL to combine results from live and staging tables as needed
        private const string PackageQueryFragment = @"
            SELECT
                p.PackageID, 
                p.PackageNo COLLATE DATABASE_DEFAULT AS PackageNo, 
                p.Name COLLATE DATABASE_DEFAULT AS Name, 
                p.PackageType COLLATE DATABASE_DEFAULT AS PackageType, 
                p.Price, 
                p.Point, 
                p.ValidDays, 
                p.DaysPass, 
                p.LastValidDate, 
                p.Link COLLATE DATABASE_DEFAULT AS Link, 
                p.RecordStatus COLLATE DATABASE_DEFAULT AS RecordStatus, 
                p.CreatedDate, 
                p.CreatedUserID, 
                p.ModifiedDate, 
                p.ModifiedUserID, 
                p.GroupEntityID, 
                p.TerminalGroupID, 
                p.ProductID, 
                p.ImageID COLLATE DATABASE_DEFAULT AS ImageID, 
                p.Remark COLLATE DATABASE_DEFAULT AS Remark,
                u_created.staff_name COLLATE DATABASE_DEFAULT AS CreatedByName,
                u_modified.staff_name COLLATE DATABASE_DEFAULT AS ModifiedByName,
                acc_created.FirstName COLLATE DATABASE_DEFAULT AS CreatedByFirstName,
                acc_modified.FirstName COLLATE DATABASE_DEFAULT AS ModifiedByFirstName
            FROM {0} p
            LEFT JOIN useru u_created ON p.CreatedUserID = u_created.account_id
            LEFT JOIN useru u_modified ON p.ModifiedUserID = u_modified.account_id
            LEFT JOIN App_Account acc_created ON p.CreatedUserID = acc_created.AccID
            LEFT JOIN App_Account acc_modified ON p.ModifiedUserID = acc_modified.AccID
        ";

        public async Task<List<Package>> GetAllAsync(string statusFilter = null)
        {
            var packages = new List<Package>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                string sql = "";
                var cmd = new SqlCommand();

                if (statusFilter == "Active")
                {
                    // 1. ACTIVE filter: Only show Approved packages from the LIVE (AO) table
                    sql = string.Format(PackageQueryFragment, "App_PackageAO") + " WHERE p.RecordStatus = 'Approved'";
                }
                else if (statusFilter == "Show All")
                {
                    // 2. SHOW ALL filter: Show Approved from LIVE (AO) + all NON-Approved from STAGING (Packages)
                    string queryAO = string.Format(PackageQueryFragment, "App_PackageAO") + " WHERE p.RecordStatus = 'Approved'";
                    string queryPackages = string.Format(PackageQueryFragment, "Packages") + " WHERE p.RecordStatus != 'Approved'";
                    sql = $"{queryAO} UNION ALL {queryPackages}";
                }
                else
                {
                    // 3. OTHER filters (Pending, Draft, etc.): Only show from STAGING (Packages)
                    sql = string.Format(PackageQueryFragment, "Packages") + " WHERE p.RecordStatus = @Status";
                    cmd.Parameters.AddWithValue("@Status", statusFilter);
                }

                cmd.CommandText = sql;
                cmd.Connection = conn;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        packages.Add(MapPackageFromReader(reader));
                    }
                }
            }
            return packages;
        }
        #endregion

        #region-- Reject Package --
        // Rejects a package by updating its status to "Rejected"
        // and setting the ModifiedUserID to the user who performed the rejection
        public async Task RejectPackageAsync(int packageId, int rejectedByUserId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    @"UPDATE Packages 
                      SET RecordStatus = 'Rejected', 
                          ModifiedUserID = @ModifiedUserID, 
                          ModifiedDate = GETDATE() 
                      WHERE PackageID = @PackageID", conn);
                cmd.Parameters.AddWithValue("@PackageID", packageId);
                cmd.Parameters.AddWithValue("@ModifiedUserID", rejectedByUserId);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        #endregion

        #region-- Approve Package --
        // Approves a package by updating its status and copying data to App_PackageAO and App_PackageItemAO eg the live tables
        public async Task ApprovePackageAsync(int packageId, int approvedByUserId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Update the package status and ModifiedUserID in Packages table
                        var updatePackageCmd = new SqlCommand(
                            @"UPDATE Packages 
                              SET RecordStatus = 'Approved', 
                                  ModifiedUserID = @ModifiedUserID, 
                                  ModifiedDate = GETDATE() 
                              WHERE PackageID = @PackageID", conn, transaction);
                        updatePackageCmd.Parameters.AddWithValue("@PackageID", packageId);
                        updatePackageCmd.Parameters.AddWithValue("@ModifiedUserID", approvedByUserId);
                        await updatePackageCmd.ExecuteNonQueryAsync();

                        // 2. Read the package data
                        var selectPackageCmd = new SqlCommand("SELECT * FROM Packages WHERE PackageID = @PackageID", conn, transaction);
                        selectPackageCmd.Parameters.AddWithValue("@PackageID", packageId);

                        Package packageToCopy = null;
                        using (var reader = await selectPackageCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
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
                                    Status = "Approved",
                                    CreatedDate = (DateTime)reader["CreatedDate"],
                                    CreatedUserID = (int)reader["CreatedUserID"],
                                    ModifiedDate = (DateTime)reader["ModifiedDate"],
                                    ModifiedUserID = (int)reader["ModifiedUserID"],
                                    GroupEntityID = (int)reader["GroupEntityID"],
                                    TerminalGroupID = (int)reader["TerminalGroupID"],
                                    ProductID = (long)reader["ProductID"],
                                    ImageID = reader["ImageID"]?.ToString(),
                                    Remark = reader["Remark"]?.ToString(),
                                    
                                };
                            }
                        }
                        if (packageToCopy == null) throw new Exception("Package not found.");

                        // 3. Insert into App_PackageAO
                        var insertPackageCmd = new SqlCommand(
                            @"INSERT INTO App_PackageAO (PackageNo, Name, PackageType, Price, Point, ValidDays, DaysPass, LastValidDate, Link, RecordStatus, CreatedDate, CreatedUserID, ModifiedDate, ModifiedUserID, GroupEntityID, TerminalGroupID, ProductID, ImageID, Remark ) 
                              VALUES (@PackageNo, @Name, @PackageType, @Price, @Point, @ValidDays, @DaysPass, @LastValidDate, @Link, @RecordStatus, @CreatedDate, @CreatedUserID, @ModifiedDate, @ModifiedUserID, @GroupEntityID, @TerminalGroupID, @ProductID, @ImageID, @Remark )", conn, transaction);

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
                        insertPackageCmd.Parameters.AddWithValue("@ModifiedDate", packageToCopy.ModifiedDate);
                        insertPackageCmd.Parameters.AddWithValue("@ModifiedUserID", packageToCopy.ModifiedUserID);
                        insertPackageCmd.Parameters.AddWithValue("@GroupEntityID", packageToCopy.GroupEntityID);
                        insertPackageCmd.Parameters.AddWithValue("@TerminalGroupID", packageToCopy.TerminalGroupID);
                        insertPackageCmd.Parameters.AddWithValue("@ProductID", packageToCopy.ProductID);
                        insertPackageCmd.Parameters.AddWithValue("@ImageID", (object)packageToCopy.ImageID ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@Remark", (object)packageToCopy.Remark ?? DBNull.Value);
                      
                        await insertPackageCmd.ExecuteNonQueryAsync();

                        // 4. Read package items
                        var selectItemsCmd = new SqlCommand("SELECT * FROM PackageItem WHERE PackageID = @PackageID", conn, transaction);
                        selectItemsCmd.Parameters.AddWithValue("@PackageID", packageId);

                        var itemsToCopy = new List<PackageItem>();
                        using (var reader = await selectItemsCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                itemsToCopy.Add(new PackageItem
                                {
                                    PackageID = (int)reader["PackageID"],
                                    ItemName = reader["ItemName"].ToString(),
                                    itemType = reader["itemType"].ToString(),
                                    Price = reader["ItemPrice"] as decimal?,
                                    Point = reader["ItemPoint"] as int?,
                                    AgeCategory = reader["AgeCategory"].ToString(),
                                    EntryQty = (int)reader["EntryQty"],
                                    Nationality = reader.HasColumn("Nationality") && reader["Nationality"] is not DBNull ? reader["Nationality"].ToString() : null
                                });
                            }
                        }

                        // 5. Insert items into App_PackageItemAO
                        foreach (var item in itemsToCopy)
                        {
                            var insertItemCmd = new SqlCommand(
                                @"INSERT INTO App_PackageItemAO (PackageID, PackageQty, ItemName, ItemType, ItemPrice, ItemPoint, AgeCategory, EntryQty, Nationality) 
                                  VALUES (@PackageID, @PackageQty, @ItemName, @itemType, @ItemPrice, @ItemPoint, @AgeCategory, @EntryQty, @Nationality)", conn, transaction);

                            insertItemCmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                            insertItemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                            insertItemCmd.Parameters.AddWithValue("@itemType", item.itemType);
                            insertItemCmd.Parameters.AddWithValue("@ItemPrice", (object)item.Price ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@ItemPoint", (object)item.Point ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                            insertItemCmd.Parameters.AddWithValue("@PackageQty", item.EntryQty);
                            insertItemCmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);
                            insertItemCmd.Parameters.AddWithValue("@Nationality", (object)item.Nationality ?? DBNull.Value);
                            await insertItemCmd.ExecuteNonQueryAsync();
                        }

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

        #region -- Duplicate Package --
        // Duplicates a package and its items, setting the new package to "Draft" status
        public async Task<int> DuplicatePackageAsync(int originalPackageId, int newUserId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Read the original package
                        var selectPackageCmd = new SqlCommand("SELECT * FROM Packages WHERE PackageID = @PackageID", conn, transaction);
                        selectPackageCmd.Parameters.AddWithValue("@PackageID", originalPackageId);

                        Package packageToCopy = null;
                        using (var reader = await selectPackageCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                packageToCopy = new Package
                                {
                                    Name = reader["Name"].ToString() + " (Copy)", // Add (Copy) suffix
                                    PackageType = reader["PackageType"].ToString(),
                                    Price = (decimal)reader["Price"],
                                    Point = (int)reader["Point"],
                                    ValidDays = (int)reader["ValidDays"],
                                    DaysPass = (int)reader["DaysPass"],
                                    LastValidDate = (DateTime)reader["LastValidDate"],
                                    Link = reader["Link"]?.ToString(),
                                    GroupEntityID = (int)reader["GroupEntityID"],
                                    TerminalGroupID = (int)reader["TerminalGroupID"],
                                    ProductID = (long)reader["ProductID"],
                                    ImageID = reader["ImageID"]?.ToString(),
                                    Remark = reader["Remark"]?.ToString()
                                    // We ignore Status, Created/Modified fields as they will be new
                                };
                            }
                        }
                        if (packageToCopy == null)
                        {
                            throw new Exception("Original package not found.");
                        }

                        // 2. Read original package items
                        var selectItemsCmd = new SqlCommand("SELECT * FROM PackageItem WHERE PackageID = @PackageID", conn, transaction);
                        selectItemsCmd.Parameters.AddWithValue("@PackageID", originalPackageId);

                        var itemsToCopy = new List<PackageItem>();
                        using (var reader = await selectItemsCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                itemsToCopy.Add(new PackageItem
                                {
                                    ItemName = reader["ItemName"].ToString(),
                                    itemType = reader["itemType"].ToString(),
                                    Price = reader["ItemPrice"] as decimal?,
                                    Point = reader["ItemPoint"] as int?,
                                    AgeCategory = reader["AgeCategory"].ToString(),
                                    EntryQty = (int)reader["EntryQty"],
                                    Nationality = reader.HasColumn("Nationality") && reader["Nationality"] is not DBNull ? reader["Nationality"].ToString() : null
                                    // We ignore CreatedUserID as it will be new
                                });
                            }
                        }

                        // 3. Insert the new package as "Draft"
                        var insertPackageCmd = new SqlCommand(
                            @"INSERT INTO Packages(
                                Name, PackageType, Price, Point, ValidDays, DaysPass,
                                LastValidDate, Link, RecordStatus, CreatedDate, CreatedUserID,
                                ModifiedDate, ModifiedUserID, GroupEntityID, TerminalGroupID,
                                ProductID, ImageID, Remark
                            ) VALUES (
                                @Name, @PackageType, @Price, @Point, @ValidDays, @DaysPass,
                                @LastValidDate, @Link, 'Draft', GETDATE(), @CreatedUserID,
                                GETDATE(), @ModifiedUserID, @GroupEntityID, @TerminalGroupID,
                                @ProductID, @ImageID, @Remark
                            );
                            SELECT SCOPE_IDENTITY();", conn, transaction);

                        insertPackageCmd.Parameters.AddWithValue("@Name", packageToCopy.Name);
                        insertPackageCmd.Parameters.AddWithValue("@PackageType", packageToCopy.PackageType);
                        insertPackageCmd.Parameters.AddWithValue("@Price", packageToCopy.Price);
                        insertPackageCmd.Parameters.AddWithValue("@Point", packageToCopy.Point);
                        insertPackageCmd.Parameters.AddWithValue("@ValidDays", packageToCopy.ValidDays);
                        insertPackageCmd.Parameters.AddWithValue("@DaysPass", packageToCopy.DaysPass);
                        insertPackageCmd.Parameters.AddWithValue("@LastValidDate", packageToCopy.LastValidDate);
                        insertPackageCmd.Parameters.AddWithValue("@Link", (object)packageToCopy.Link ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@CreatedUserID", newUserId);
                        insertPackageCmd.Parameters.AddWithValue("@ModifiedUserID", newUserId);
                        insertPackageCmd.Parameters.AddWithValue("@GroupEntityID", packageToCopy.GroupEntityID);
                        insertPackageCmd.Parameters.AddWithValue("@TerminalGroupID", packageToCopy.TerminalGroupID);
                        insertPackageCmd.Parameters.AddWithValue("@ProductID", packageToCopy.ProductID);
                        insertPackageCmd.Parameters.AddWithValue("@ImageID", (object)packageToCopy.ImageID ?? DBNull.Value);
                        insertPackageCmd.Parameters.AddWithValue("@Remark", (object)packageToCopy.Remark ?? DBNull.Value);

                        int newPackageId = Convert.ToInt32(await insertPackageCmd.ExecuteScalarAsync());

                        // 4. Insert the new package items
                        foreach (var item in itemsToCopy)
                        {
                            var insertItemCmd = new SqlCommand(
                                @"INSERT INTO PackageItem (PackageID, ItemName, itemType, ItemPrice, ItemPoint, AgeCategory, EntryQty, CreatedUserID, Nationality)
                                  VALUES (@PackageID, @ItemName, @itemType, @ItemPrice, @ItemPoint, @AgeCategory, @EntryQty, @CreatedUserID, @Nationality)", conn, transaction);

                            insertItemCmd.Parameters.AddWithValue("@PackageID", newPackageId); // Use new ID
                            insertItemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                            insertItemCmd.Parameters.AddWithValue("@itemType", item.itemType);
                            insertItemCmd.Parameters.AddWithValue("@ItemPrice", (object)item.Price ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@ItemPoint", (object)item.Point ?? DBNull.Value);
                            insertItemCmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                            insertItemCmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);
                            insertItemCmd.Parameters.AddWithValue("@CreatedUserID", newUserId); // Use new user ID
                            insertItemCmd.Parameters.AddWithValue("@Nationality", (object)item.Nationality ?? DBNull.Value);

                            await insertItemCmd.ExecuteNonQueryAsync();
                        }

                        // 5. Commit
                        transaction.Commit();
                        return newPackageId;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw; // Re-throw the exception so the controller can catch it
                    }
                }
            }
        }
        #endregion

        #region -- Map Package From Reader Helper Method --
        // Helper method to map SqlDataReader to Package object
        // Used in GetPackageByIdAsync and GetAllAsync
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
                LastValidDate = reader["LastValidDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["LastValidDate"],
                CreatedDate = reader["CreatedDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["CreatedDate"],
                ModifiedDate = reader["ModifiedDate"] is DBNull ? DateTime.MinValue : (DateTime)reader["ModifiedDate"],
                Price = reader["Price"] is DBNull ? 0 : (decimal)reader["Price"],
                Point = reader["Point"] is DBNull ? 0 : (int)reader["Point"],
                ValidDays = reader["ValidDays"] is DBNull ? 0 : (int)reader["ValidDays"],
                DaysPass = reader["DaysPass"] is DBNull ? 0 : (int)reader["DaysPass"],
                CreatedUserID = reader["CreatedUserID"] is DBNull ? 0 : (int)reader["CreatedUserID"],
                ModifiedUserID = reader["ModifiedUserID"] is DBNull ? 0 : (int)reader["ModifiedUserID"],
                GroupEntityID = reader["GroupEntityID"] is DBNull ? 0 : (int)reader["GroupEntityID"],
                TerminalGroupID = reader["TerminalGroupID"] is DBNull ? 0 : (int)reader["TerminalGroupID"],
                ProductID = reader["ProductID"] is DBNull ? 0 : (long)reader["ProductID"],
                ImageID = reader["ImageID"] is DBNull ? null : reader["ImageID"].ToString(),
                Remark = reader["Remark"] is DBNull ? null : reader["Remark"].ToString(),
                
            };

            if (reader.HasColumn("CreatedByName"))
            {
                package.CreatedByName = reader["CreatedByName"] is DBNull ? "N/A" : reader["CreatedByName"].ToString();
            }
            if (reader.HasColumn("ModifiedByName"))
            {
                package.ModifiedByName = reader["ModifiedByName"] is DBNull ? "N/A" : reader["ModifiedByName"].ToString();
            }
            if (reader.HasColumn("CreatedByFirstName"))
            {
                package.CreatedByFirstName = reader["CreatedByFirstName"] is DBNull ? "N/A" : reader["CreatedByFirstName"].ToString();
            }
            if (reader.HasColumn("ModifiedByFirstName"))
            {
                package.ModifiedByFirstName = reader["ModifiedByFirstName"] is DBNull ? "N/A" : reader["ModifiedByFirstName"].ToString();
            }

            return package;
        }
        #endregion

        #region -- Update package status with UserID --
        // Used to update status along with ModifiedUserID
        // Used for simple status updates that do not require full approve/reject logic eg. "Draft" to "Pending"
        public async Task<bool> UpdatePackageStatusAsync(int packageId, string newStatus, int modifiedUserId)
        {
            using (var connection = _databaseHelper.GetConnection())
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "UPDATE Packages SET RecordStatus = @Status, ModifiedUserID = @ModifiedUserID, ModifiedDate = GETDATE() WHERE PackageID = @PackageID",
                    connection);
                command.Parameters.AddWithValue("@Status", newStatus);
                command.Parameters.AddWithValue("@ModifiedUserID", modifiedUserId);
                command.Parameters.AddWithValue("@PackageID", packageId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
        #endregion
    }
}