using System.Data;
using Microsoft.Data.SqlClient;
using AdminPortal.Models;

namespace AdminPortal.Data
{
    public class PackageItemRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        public PackageItemRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        // Pass the entire PackageItem object
        public async Task InsertPackageItem(PackageItem item)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    @"INSERT INTO PackageItem(PackageID, ItemName, ItemType, ItemPrice, ItemPoint, AgeCategory, EntryQty,RecordStatus) 
              VALUES (@PackageID, @ItemName, @ItemType, @Price, @Point, @AgeCategory, @EntryQty, @RecordStatus)", conn);

                cmd.Parameters.AddWithValue("RecordStatus", "Pending");
                cmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                cmd.Parameters.AddWithValue("@ItemType", item.itemType);
                cmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                cmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);

                // Correctly handle NULL values for Price and Point
                cmd.Parameters.AddWithValue("@Price", (object)item.Price ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Point", (object)item.Point ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}