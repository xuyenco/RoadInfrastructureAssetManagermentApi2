using Npgsql;
using BCrypt.Net;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Road_Infrastructure_Asset_Management.Jwt;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class UsersService : IUsersService
    {
        private readonly string _connectionString;
        private static readonly string[] ValidRoles = { "admin", "manager", "technician", "inspector" };

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

                try
                {
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
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url"))
                            };
                            users.Add(user);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve users from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
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
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve user with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<UsersResponse?> CreateUser(UsersRequest entity)
        {
            ValidateRequest(entity, isCreate: true);

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password_hash, workFactor: 12);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO users 
                (username, password_hash, full_name, email, role,image_url)
                VALUES (@username, @password_hash, @full_name, @email, @role, @image_url)
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
                        cmd.Parameters.AddWithValue("@image_url", entity.image_url);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetUserById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // Unique violation
                    {
                        if (ex.Message.Contains("username"))
                            throw new InvalidOperationException($"Username '{entity.username}' is already taken.", ex);
                        if (ex.Message.Contains("email"))
                            throw new InvalidOperationException($"Email '{entity.email}' is already in use.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        throw new InvalidOperationException($"Role must be one of: {string.Join(", ", ValidRoles)}.", ex);
                    }
                    throw new InvalidOperationException("Failed to create user.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<UsersResponse?> UpdateUser(int id, UsersRequest entity)
        {
            ValidateRequest(entity, isCreate: false);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE users SET
                    username = @username,
                    full_name = @full_name,
                    email = @email,
                    role = @role,
                    image_url = @image_url
                WHERE user_id = @id";

                // Nếu password_hash không null hoặc rỗng, cập nhật mật khẩu
                if (!string.IsNullOrWhiteSpace(entity.password_hash))
                {
                    sql = @"
                    UPDATE users SET
                        username = @username,
                        password_hash = @password_hash,
                        full_name = @full_name,
                        email = @email,
                        role = @role,
                        image_url = @image_url
                    WHERE user_id = @id";
                }
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@username", entity.username);
                        cmd.Parameters.AddWithValue("@full_name", entity.full_name);
                        cmd.Parameters.AddWithValue("@email", entity.email);
                        cmd.Parameters.AddWithValue("@role", entity.role);
                        cmd.Parameters.AddWithValue("@image_url", entity.image_url);

                        if (!string.IsNullOrWhiteSpace(entity.password_hash))
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password_hash, workFactor: 12);
                            cmd.Parameters.AddWithValue("@password_hash", hashedPassword);
                        }

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetUserById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // Unique violation
                    {
                        if (ex.Message.Contains("username"))
                            throw new InvalidOperationException($"Username '{entity.username}' is already taken.", ex);
                        if (ex.Message.Contains("email"))
                            throw new InvalidOperationException($"Email '{entity.email}' is already in use.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        throw new InvalidOperationException($"Role must be one of: {string.Join(", ", ValidRoles)}.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update user with ID {id}.", ex);
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
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Cannot delete user with ID {id} because it is referenced by other records (e.g., tasks or incidents).", ex);
                    }
                    throw new InvalidOperationException($"Failed to delete user with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<UsersResponse?> Login(LoginRequest user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                throw new ArgumentException("Username and password are required.");
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM users WHERE username = @username";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", user.Username);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));

                                if (BCrypt.Net.BCrypt.Verify(user.Password, storedHash))
                                {
                                    var userResponse = new UsersResponse
                                    {
                                        user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                        username = reader.GetString(reader.GetOrdinal("username")),
                                        full_name = reader.GetString(reader.GetOrdinal("full_name")),
                                        email = reader.GetString(reader.GetOrdinal("email")),
                                        role = reader.GetString(reader.GetOrdinal("role")),
                                        created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                    };

                                    // Tạo và lưu refresh token
                                    var (refreshToken, expiry) = JwtTokenHelper.GenerateRefreshToken();
                                    userResponse.refresh_token = refreshToken;
                                    userResponse.refresh_token_expiry = expiry;

                                    await UpdateRefreshToken(userResponse.user_id, refreshToken, expiry);
                                    return userResponse;
                                }
                            }
                            return null; // User not found or password mismatch
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to process login request.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        public async Task<UsersResponse?> RefreshToken(string refreshToken)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM users WHERE refresh_token = @refreshToken AND refresh_token_expiry > @now";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@refreshToken", refreshToken);
                        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var user = new UsersResponse
                                {
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    username = reader.GetString(reader.GetOrdinal("username")),
                                    full_name = reader.GetString(reader.GetOrdinal("full_name")),
                                    email = reader.GetString(reader.GetOrdinal("email")),
                                    role = reader.GetString(reader.GetOrdinal("role")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    refresh_token = reader.GetString(reader.GetOrdinal("refresh_token")),
                                    refresh_token_expiry = reader.GetDateTime(reader.GetOrdinal("refresh_token_expiry"))
                                };

                                // Tạo refresh token mới (tùy chọn)
                                var (newRefreshToken, expiry) = JwtTokenHelper.GenerateRefreshToken();
                                user.refresh_token = newRefreshToken;
                                user.refresh_token_expiry = expiry;
                                await UpdateRefreshToken(user.user_id, refreshToken, expiry);
                                return user;
                            }
                            return null; // Refresh token không hợp lệ hoặc hết hạn
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to process refresh token.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        // Phương thức phụ để cập nhật refresh token vào database
        private async Task UpdateRefreshToken(int userId, string refreshToken, DateTime expiry)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var updateSql = "UPDATE users SET refresh_token = @refreshToken, refresh_token_expiry = @expiry WHERE user_id = @userId";
                using (var cmd = new NpgsqlCommand(updateSql, connection))
                {
                    cmd.Parameters.AddWithValue("@refreshToken", refreshToken);
                    cmd.Parameters.AddWithValue("@expiry", expiry);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(UsersRequest entity, bool isCreate)
        {
            if (string.IsNullOrWhiteSpace(entity.username))
            {
                throw new ArgumentException("Username cannot be empty.");
            }
            if (isCreate && string.IsNullOrWhiteSpace(entity.password_hash))
            {
                throw new ArgumentException("Password cannot be empty when creating a user.");
            }
            if (string.IsNullOrWhiteSpace(entity.full_name))
            {
                throw new ArgumentException("Full name cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.email))
            {
                throw new ArgumentException("Email cannot be empty.");
            }
            if (!ValidRoles.Contains(entity.role))
            {
                throw new ArgumentException($"Role must be one of: {string.Join(", ", ValidRoles)}.");
            }
        }
    }
}