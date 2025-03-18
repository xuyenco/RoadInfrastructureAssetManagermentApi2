using Npgsql;
using BCrypt.Net;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class UsersService : IUsersService
    {
        private readonly string _connectionString;

        public UsersService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<UsersResponse>> GetAllUsers()
        {
            var users = new List<UsersResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM users";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var user = new UsersResponse
                        {
                            user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                            username = reader.GetString(reader.GetOrdinal("username")),
                            full_name = reader.GetString(reader.GetOrdinal("full_name")),
                            email = reader.GetString(reader.GetOrdinal("email")),
                            role = reader.GetString(reader.GetOrdinal("role")),
                            created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                        };
                        users.Add(user);
                    }
                }
                await _connection.CloseAsync();
                return users;
            }
        }
        public async Task<UsersResponse?> GetUserById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM users WHERE user_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UsersResponse
                                {
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    username = reader.GetString(reader.GetOrdinal("username")),
                                    full_name = reader.GetString(reader.GetOrdinal("full_name")),
                                    email = reader.GetString(reader.GetOrdinal("email")),
                                    role = reader.GetString(reader.GetOrdinal("role")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<UsersResponse?> CreateUser(UsersRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password_hash, workFactor: 12);
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO users 
                (username, password_hash, full_name, email, role)
                VALUES (@username, @password_hash, @full_name, @email, @role)
                RETURNING user_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@username", entity.username);
                        cmd.Parameters.AddWithValue("@password_hash", hashedPassword);
                        cmd.Parameters.AddWithValue("@full_name", entity.full_name);
                        cmd.Parameters.AddWithValue("@email", entity.email);
                        cmd.Parameters.AddWithValue("@role", entity.role);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetUserById(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<UsersResponse?> UpdateUser(int id, UsersRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password_hash, workFactor: 12);
                await _connection.OpenAsync();
                var sql = @"
                UPDATE users SET
                    username  = @username,
                    password_hash = @password_hash,
                    full_name = @full_name,
                    email = @email,
                    role = @role
                WHERE user_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@username", entity.username);
                        cmd.Parameters.AddWithValue("@password_hash", hashedPassword);
                        cmd.Parameters.AddWithValue("@full_name", entity.full_name);
                        cmd.Parameters.AddWithValue("@email", entity.email);
                        cmd.Parameters.AddWithValue("@role", entity.role);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetUserById(newId);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetUserById(id);
                        }
                        return null;
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<bool> DeleteUser(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM users WHERE user_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<UsersResponse?> Login(LoginRequest user)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var findUserSql = "SELECT * FROM users WHERE username = @username";

                    using (var cmd = new NpgsqlCommand(findUserSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", user.Username);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));

                                if (BCrypt.Net.BCrypt.Verify(user.Password, storedHash))
                                {
                                    return new UsersResponse
                                    {
                                        user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                        username = reader.GetString(reader.GetOrdinal("username")),
                                        full_name = reader.GetString(reader.GetOrdinal("full_name")),
                                        email = reader.GetString(reader.GetOrdinal("email")),
                                        role = reader.GetString(reader.GetOrdinal("role")),
                                        created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                    };
                                }
                            }
                            return null; // User not found or password mismatch
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 4. Add proper error handling/logging
                    Console.WriteLine($"Login error: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
