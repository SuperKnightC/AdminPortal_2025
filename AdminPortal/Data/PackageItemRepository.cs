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

        public async Task InsertPackage(int packageID, int packageQty, int terminalID, string ageCategory, char nationality,string itemType, int entryQty, string itemName, decimal itemPrice,
                                        string itemPoint, string taxCode, string isEntry, string recordStatus,DateTime createdDate, int createdUserID, DateTime modifiedDate, int modifiedUserID)
        {
            using (var conn = _databaseHelper.GetConnection()) //use dbHelper for sqlConn
            {
                await conn.OpenAsync(); //open the connection

                var cmd = new SqlCommand("Insert into packages(PackageID, PackageQty,TerminalID,AgeCategory,Nationality,ItemType,EntryQty,ItemName,ItemPrice" +
                                         "ItemPoint,TaxCode,IsEntry,RecordStatus,CreatedDate,CreatedUserID,ModifiedDate,ModifiedUserID) " +
                                         "VALUES (@PackageID, @PackageQty,@TerminalID,@AgeCategory,@Nationality,@ItemType,@EntryQty,@ItemName,@ItemPrice" +
                                         "@ItemPoint,@TaxCode,@IsEntry,@RecordStatus,@CreatedDate,@CreatedUserID,@ModifiedDate,@ModifiedUserID)", conn);//create queries

                cmd.Parameters.AddWithValue("packageID", packageID);
                cmd.Parameters.AddWithValue("packageQty", packageQty);
                cmd.Parameters.AddWithValue("TerminalID", terminalID);
                cmd.Parameters.AddWithValue("AgeCategory",ageCategory);
                cmd.Parameters.AddWithValue("Nationality", nationality);
                cmd.Parameters.AddWithValue("ItemType", itemType);
                cmd.Parameters.AddWithValue("EntryQty", entryQty);
                cmd.Parameters.AddWithValue("ItemName", itemName);
                cmd.Parameters.AddWithValue("ItemPrice", itemPrice);
                cmd.Parameters.AddWithValue("ItemPoint", itemPoint);
                cmd.Parameters.AddWithValue("TaxCode", taxCode);
                cmd.Parameters.AddWithValue("IsEntry", isEntry);
                cmd.Parameters.AddWithValue("RecordStatus", recordStatus);
                cmd.Parameters.AddWithValue("CreatedDate", createdDate);
                cmd.Parameters.AddWithValue("CreatedUserID", createdUserID);
                cmd.Parameters.AddWithValue("ModifiedDate", modifiedDate);
                cmd.Parameters.AddWithValue("ModifiedUserID", modifiedUserID);


                await cmd.ExecuteNonQueryAsync();//insert

            }

        }

    }
}
