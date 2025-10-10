using System.Data;
using Microsoft.Data.SqlClient;
using AdminPortal.Models; //library and access

namespace AdminPortal.Data //declare namespace
{

    public class UserRepository //define this class
    {
        //This repo handle user login and registration

        #region -- Constructor and DI, DB helper --
        private readonly DatabaseHelper _databaseHelper; //accept database helper via DI
         public UserRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }
        #endregion

        #region -- Add User Method (not used) --
        public async Task AddUserAsync(string email,string password) //receive credential
        {
            using (var conn = _databaseHelper.GetConnection()) //use dbHelper for sqlConn
            {
                await conn.OpenAsync(); //open the connection

                var cmd = new SqlCommand("Insert into users(email,passwordHash) VALUES (@email,@password)", conn);//create queries
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);

                await cmd.ExecuteNonQueryAsync();//insert

            }
        }
        #endregion

        #region -- Get User By Email Method --
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using (var conn = _databaseHelper.GetConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand("SELECT id, email, passwordHash FROM users WHERE email = @email", conn);

                cmd.Parameters.AddWithValue("@email", email);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            PasswordHash = reader.GetString(2)
                        };
                    }
                }
            }

            return null;
        }
        #endregion
    }
}
    


