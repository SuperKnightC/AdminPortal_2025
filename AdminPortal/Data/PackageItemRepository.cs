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

        public async Task InsertPackageItem(PackageItem item)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    @"INSERT INTO PackageItem(PackageID, ItemName, itemType, Price, Point, AgeCategory, EntryQty) 
                      VALUES (@PackageID, @ItemName, @itemType, @Price, @Point, @AgeCategory, @EntryQty)", conn);

                cmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                cmd.Parameters.AddWithValue("@itemType", item.itemType);
                cmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                cmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);
                cmd.Parameters.AddWithValue("@Price", (object)item.Price ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Point", (object)item.Point ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
    #endregion

    #region--Age Category Fetch--
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

    #region--Attraction Fetch--
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
    }
    #endregion
}