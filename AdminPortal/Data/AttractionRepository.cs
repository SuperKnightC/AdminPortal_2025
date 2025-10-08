// In /Data/AttractionRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using AdminPortal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminPortal.Data
{
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
}