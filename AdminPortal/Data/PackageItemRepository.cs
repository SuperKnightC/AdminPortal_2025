using AdminPortal.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AdminPortal.Data
{
    //This Repo Handle Package Item 

    #region-- PackageItem Insert--
    // Main Class: Handles Package Items
    public class PackageItemRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public PackageItemRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task InsertPackageItem(PackageItem item, int createdUserId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    @"INSERT INTO PackageItem (PackageID, ItemName, itemType, ItemPrice, ItemPoint, AgeCategory, EntryQty, CreatedUserID, Nationality)
              VALUES (@PackageID, @ItemName, @itemType, @ItemPrice, @ItemPoint, @AgeCategory, @EntryQty, @CreatedUserID, @Nationality)", conn);

                // FIXED: Add ALL parameters
                cmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                cmd.Parameters.AddWithValue("@ItemName", item.ItemName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@itemType", item.itemType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemPrice", item.Price.HasValue ? (object)item.Price.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemPoint", item.Point.HasValue ? (object)item.Point.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);
                cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                cmd.Parameters.AddWithValue("@Nationality", (object)item.Nationality ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        public async Task<List<PackageItem>> GetItemsByPackageIdAsync(int packageId)
        {
            var items = new List<PackageItem>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT * FROM PackageItem WHERE PackageID = @PackageID", conn);
                cmd.Parameters.AddWithValue("@PackageID", packageId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PackageItem
                        {
                            ItemName = reader["ItemName"].ToString(),
                            Price = reader["ItemPrice"] as decimal?,
                            Point = reader["ItemPoint"] as int?,
                            AgeCategory = reader["AgeCategory"].ToString(),
                            EntryQty = (int)reader["EntryQty"],
                            Nationality = reader.HasColumn("Nationality") && reader["Nationality"] is not DBNull ? reader["Nationality"].ToString() : null
                        });
                    }
                }
            }
            return items;

        }
        public async Task<List<PackageItem>> GetApprovedItemsByPackageIdAsync(int packageId)
        {
            var items = new List<PackageItem>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                // Query the AO table
                var cmd = new SqlCommand("SELECT * FROM App_PackageItemAO WHERE PackageID = @PackageID", conn);
                cmd.Parameters.AddWithValue("@PackageID", packageId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PackageItem
                        {
                            ItemName = reader["ItemName"].ToString(),
                            Price = reader["ItemPrice"] as decimal?,
                            Point = reader["ItemPoint"] as int?,
                            AgeCategory = reader["AgeCategory"].ToString(),
                            // Use EntryQty (or PackageQty if that's the correct column name in App_PackageItemAO)
                            EntryQty = (int)reader["EntryQty"],
                            Nationality = reader.HasColumn("Nationality") && reader["Nationality"] is not DBNull ? reader["Nationality"].ToString() : null
                        });
                    }
                }
            }
            return items;

        }



    }
    #endregion

    #region-- Age Category Fetch --
    // Consolidated Class: Handles Age Categories
    public class AgeCategoryRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public AgeCategoryRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<List<AgeCategory>> GetAllAsync()
        {
            var categories = new List<AgeCategory>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT AgeCode, AgeCategory AS CategoryName FROM dbo.ICTR_AgeCategory WHERE RecordStatus = 'Active' ORDER BY AgeCode", conn);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new AgeCategory
                        {
                            AgeCode = reader["AgeCode"].ToString(),
                            CategoryName = reader["CategoryName"].ToString()
                        });
                    }
                }
            }
            return categories;
        }
    }
    #endregion

    #region-- Attraction Fetch --
    // Consolidated Class: Handles Attractions
    public class AttractionRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public AttractionRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<List<Attraction>> GetAllActiveAsync()
        {
            var attractions = new List<Attraction>();
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT Name FROM Attraction WHERE RecordStatus = 'Active' ORDER BY Name", conn);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        attractions.Add(new Attraction
                        {
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
            return attractions;
        }
    }
    #endregion

    #region-- Package Image Insert --
    // Consolidated Class: Handles Package Images
    public class PackageImageRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public PackageImageRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<int> InsertAsync(string imageUrl)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "INSERT INTO packageImages (ImageURL) VALUES (@ImageURL); SELECT SCOPE_IDENTITY();",
                    conn);
                cmd.Parameters.AddWithValue("@ImageURL", imageUrl);

                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
        public async Task<string> GetUrlByIdAsync(int imageId)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT ImageURL FROM packageImages WHERE ImageID = @ImageID", conn);
                cmd.Parameters.AddWithValue("@ImageID", imageId);

                // ExecuteScalarAsync is efficient for getting a single value
                object result = await cmd.ExecuteScalarAsync();

                // Return the URL string, or null if no image was found
                return result?.ToString();
            }
        }

       
    }
    #endregion

}