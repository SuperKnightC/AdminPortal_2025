using System.Data;
using Microsoft.Data.SqlClient;
using be_general_support_api.Models; 

namespace be_general_support_api.Data 
{

    public class UserRepository 
    {
        //This repo handle user login and registration

        #region -- Constructor and DI, DB helper --
        private readonly DatabaseHelper _databaseHelper; //accept database helper via DI
         public UserRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }
        #endregion

        #region -- Get User By Email Method --
        //Used in Login to retrieve user details
        public async Task<AuthUser?> GetAuthUserByEmailAsync(string email)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                SELECT 
                    a.AccID, 
                    a.Email, 
                    a.Password, 
                    u.staff_name, 
                    u.department 
                FROM 
                    dbo.App_Account a
                JOIN 
                    dbo.useru u ON a.AccID = u.account_id
                WHERE 
                    a.Email = @email 
                    AND a.RecordStatus = 'Active' 
                    AND a.IsStaff = 'Y'", conn);

                cmd.Parameters.AddWithValue("@email", email);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new AuthUser
                        {
                            AccountId = (int)reader["AccID"],
                            Email = reader["Email"] is DBNull ? string.Empty : reader["Email"].ToString(),
                            PasswordHash = reader["Password"] is DBNull ? string.Empty : reader["Password"].ToString(),
                            Name = reader["staff_name"] is DBNull ? string.Empty : reader["staff_name"].ToString(),
                            Department = reader["department"] is DBNull ? string.Empty : reader["department"].ToString()
                        };
                    }
                }
            }
            return null;
        }
        #endregion
    }
}
    


