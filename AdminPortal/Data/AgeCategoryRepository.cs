// In /Data/AgeCategoryRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using AdminPortal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminPortal.Data
{
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
                // IMPORTANT: Replace 'YourAgeCategoryTableName' with your actual table name
                var cmd = new SqlCommand("SELECT AgeCode, AgeCategory AS CategoryName FROM ICTR_AgeCategory WHERE RecordStatus = 'Active' ORDER BY AgeCode", conn);

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
}