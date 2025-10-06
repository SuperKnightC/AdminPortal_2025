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
                    "INSERT INTO PackageItem(PackageID, ItemName, ItemType, ItemPrice, AgeCategory, EntryQty) " +
                    "VALUES (@PackageID, @ItemName, @ItemType, @ItemPrice, @AgeCategory, @EntryQty)", conn);

                cmd.Parameters.AddWithValue("@PackageID", item.PackageID);
                cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                cmd.Parameters.AddWithValue("@ItemType", item.itemType);
                cmd.Parameters.AddWithValue("@ItemPrice", item.ItemPrice);
                cmd.Parameters.AddWithValue("@AgeCategory", item.AgeCategory);
                cmd.Parameters.AddWithValue("@EntryQty", item.EntryQty);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}