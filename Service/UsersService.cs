using Microsoft.Extensions.Logging; 
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Jwt;
using System.Text;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class UsersService : IUsersService
    {

        private readonly string _connectionString;
        private static readonly string[] ValidRoles = { "admin", "manager", "technician", "inspector", "supervisor" };
        private readonly ILogger<UsersService> _logger; 

        public UsersService(string connectionString, ILogger<UsersService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
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
                                full_name = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
                                email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                role = reader.GetString(reader.GetOrdinal("role")),
                                department_company_unit = reader.IsDBNull(reader.GetOrdinal("department_company_unit")) ? null : reader.GetString(reader.GetOrdinal("department_company_unit")),
                                image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                refresh_token = reader.IsDBNull(reader.GetOrdinal("refresh_token")) ? null : reader.GetString(reader.GetOrdinal("refresh_token")),
                                refresh_token_expiry = reader.IsDBNull(reader.GetOrdinal("refresh_token_expiry")) ? null : reader.GetDateTime(reader.GetOrdinal("refresh_token_expiry"))
                            };
                            users.Add(user);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} users successfully", users.Count); 
                    return users;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve users from database");
                    throw new InvalidOperationException("Failed to retrieve users from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<(IEnumerable<UsersResponse> Users, int TotalCount)> GetUsersPagination(int page, int pageSize, string searchTerm, int searchField)
        {
            var users = new List<UsersResponse>();
            int totalCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Xây dựng câu truy vấn SQL
                var sqlBuilder = new StringBuilder("SELECT * FROM users");
                var countSql = "SELECT COUNT(*) FROM users";
                var parameters = new List<NpgsqlParameter>();

                // Thêm điều kiện tìm kiếm nếu có
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = $"%{searchTerm.ToLower()}%"; // Chuẩn bị cho LIKE
                    string condition = searchField switch
                    {
                        0 => "CAST(user_id AS TEXT) ILIKE @searchTerm", // Mã người dùng
                        1 => "LOWER(username) ILIKE @searchTerm",       // Tên đăng nhập
                        2 => "LOWER(full_name) ILIKE @searchTerm",     // Họ và tên
                        3 => "LOWER(email) ILIKE @searchTerm",         // Email
                        4 => "LOWER(role) ILIKE @searchTerm",          // Vai trò
                        5 => "TO_CHAR(created_at, 'HH24:MI DD/MM/YYYY') ILIKE @searchTerm", // Ngày tạo
                        _ => null
                    };

                    if (condition != null)
                    {
                        sqlBuilder.Append(" WHERE ");
                        sqlBuilder.Append(condition);
                        countSql += $" WHERE {condition}";
                        parameters.Add(new NpgsqlParameter("@searchTerm", searchTerm));
                    }
                }

                // Thêm phân trang
                sqlBuilder.Append(" ORDER BY user_id");
                sqlBuilder.Append(" OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
                parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
                parameters.Add(new NpgsqlParameter("@pageSize", pageSize));

                try
                {
                    // Đếm tổng số bản ghi
                    using (var countCmd = new NpgsqlCommand(countSql, connection))
                    {
                        // Tạo bản sao tham số cho countCmd
                        foreach (var param in parameters)
                        {
                            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // Lấy danh sách người dùng
                    using (var cmd = new NpgsqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Tạo bản sao tham số cho cmd
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new UsersResponse
                                {
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    username = reader.GetString(reader.GetOrdinal("username")),
                                    full_name = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
                                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                    role = reader.GetString(reader.GetOrdinal("role")),
                                    department_company_unit = reader.IsDBNull(reader.GetOrdinal("department_company_unit")) ? null : reader.GetString(reader.GetOrdinal("department_company_unit")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    refresh_token = reader.IsDBNull(reader.GetOrdinal("refresh_token")) ? null : reader.GetString(reader.GetOrdinal("refresh_token")),
                                    refresh_token_expiry = reader.IsDBNull(reader.GetOrdinal("refresh_token_expiry")) ? null : reader.GetDateTime(reader.GetOrdinal("refresh_token_expiry"))
                                };
                                users.Add(user);
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} users for page {Page} with total count {TotalCount}", users.Count, page, totalCount);
                    return (users, totalCount);
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve users with pagination and search");
                    throw new InvalidOperationException("Failed to retrieve users from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
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
                                var user = new UsersResponse
                                {
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    username = reader.GetString(reader.GetOrdinal("username")),
                                    full_name = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
                                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                    role = reader.GetString(reader.GetOrdinal("role")),
                                    department_company_unit = reader.IsDBNull(reader.GetOrdinal("department_company_unit")) ? null : reader.GetString(reader.GetOrdinal("department_company_unit")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    refresh_token = reader.IsDBNull(reader.GetOrdinal("refresh_token")) ? null : reader.GetString(reader.GetOrdinal("refresh_token")),
                                    refresh_token_expiry = reader.IsDBNull(reader.GetOrdinal("refresh_token_expiry")) ? null : reader.GetDateTime(reader.GetOrdinal("refresh_token_expiry"))
                                };
                                _logger.LogInformation("Retrieved user with ID {UserId} successfully", id); 
                                return user;
                            }
                            _logger.LogWarning("User with ID {UserId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve user with ID {UserId}", id); 
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

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password, workFactor: 12);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO users 
                (username, password, full_name, email, role, department_company_unit, image_url, image_name, image_public_id)
                VALUES (@username, @password, @full_name, @email, @role, @department_company_unit, @image_url, @image_name, @image_public_id)
                RETURNING user_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@username", entity.username);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.Parameters.AddWithValue("@full_name", (object)entity.full_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", (object)entity.email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@role", entity.role);
                        cmd.Parameters.AddWithValue("@department_company_unit", (object)entity.department_company_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created user with ID {UserId} successfully", newId); 
                        return await GetUserById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // Unique violation
                    {
                        if (ex.Message.Contains("username"))
                        {
                            _logger.LogError(ex, "Failed to create user: Username {Username} is already taken", entity.username); 
                            throw new InvalidOperationException($"Username '{entity.username}' is already taken.", ex);
                        }
                        if (ex.Message.Contains("email"))
                        {
                            _logger.LogError(ex, "Failed to create user: Email {Email} is already in use", entity.email); 
                            throw new InvalidOperationException($"Email '{entity.email}' is already in use.", ex);
                        }
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        _logger.LogError(ex, "Failed to create user: Invalid role {Role}", entity.role); 
                        throw new InvalidOperationException($"Role must be one of: {string.Join(", ", ValidRoles)}.", ex);
                    }
                    _logger.LogError(ex, "Failed to create user"); 
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
                    department_company_unit = @department_company_unit,
                    image_url = @image_url,
                    image_name = @image_name,
                    image_public_id = @image_public_id
                WHERE user_id = @id";

                if (!string.IsNullOrWhiteSpace(entity.password))
                {
                    sql = @"
                    UPDATE users SET
                        username = @username,
                        password = @password,
                        full_name = @full_name,
                        email = @email,
                        role = @role,
                        department_company_unit = @department_company_unit,
                        image_url = @image_url,
                        image_name = @image_name,
                        image_public_id = @image_public_id
                    WHERE user_id = @id";
                }

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@username", entity.username);
                        cmd.Parameters.AddWithValue("@full_name", (object)entity.full_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", (object)entity.email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@role", entity.role);
                        cmd.Parameters.AddWithValue("@department_company_unit", (object)entity.department_company_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value);

                        if (!string.IsNullOrWhiteSpace(entity.password))
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(entity.password, workFactor: 12);
                            cmd.Parameters.AddWithValue("@password", hashedPassword);
                        }

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated user with ID {UserId} successfully", id); 
                            return await GetUserById(id);
                        }
                        _logger.LogWarning("User with ID {UserId} not found for update", id); 
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // Unique violation
                    {
                        if (ex.Message.Contains("username"))
                        {
                            _logger.LogError(ex, "Failed to update user with ID {UserId}: Username {Username} is already taken", id, entity.username); 
                            throw new InvalidOperationException($"Username '{entity.username}' is already taken.", ex);
                        }
                        if (ex.Message.Contains("email"))
                        {
                            _logger.LogError(ex, "Failed to update user with ID {UserId}: Email {Email} is already in use", id, entity.email); 
                            throw new InvalidOperationException($"Email '{entity.email}' is already in use.", ex);
                        }
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        _logger.LogError(ex, "Failed to update user with ID {UserId}: Invalid role {Role}", id, entity.role); 
                        throw new InvalidOperationException($"Role must be one of: {string.Join(", ", ValidRoles)}.", ex);
                    }
                    _logger.LogError(ex, "Failed to update user with ID {UserId}", id); 
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
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted user with ID {UserId} successfully", id);
                            return true;
                        }
                        _logger.LogWarning("User with ID {UserId} not found for deletion", id); 
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete user with ID {UserId}: User is referenced by other records", id); 
                        throw new InvalidOperationException($"Cannot delete user with ID {id} because it is referenced by other records (e.g., tasks).", ex);
                    }
                    _logger.LogError(ex, "Failed to delete user with ID {UserId}", id); 
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
                _logger.LogWarning("Login failed: Username or password is empty"); 
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
                                var storedHash = reader.GetString(reader.GetOrdinal("password"));

                                if (BCrypt.Net.BCrypt.Verify(user.Password, storedHash))
                                {
                                    var userResponse = new UsersResponse
                                    {
                                        user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                        username = reader.GetString(reader.GetOrdinal("username")),
                                        full_name = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
                                        email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                        role = reader.GetString(reader.GetOrdinal("role")),
                                        department_company_unit = reader.IsDBNull(reader.GetOrdinal("department_company_unit")) ? null : reader.GetString(reader.GetOrdinal("department_company_unit")),
                                        created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                    };

                                    // Tạo và lưu refresh token
                                    var (refreshToken, expiry) = JwtTokenHelper.GenerateRefreshToken();
                                    userResponse.refresh_token = refreshToken;
                                    userResponse.refresh_token_expiry = expiry;

                                    await UpdateRefreshToken(userResponse.user_id, refreshToken, expiry);
                                    _logger.LogInformation("User with username {Username} logged in successfully", user.Username); 
                                    return userResponse;
                                }
                            }
                            _logger.LogWarning("Login failed: Invalid username or password for username {Username}", user.Username); 
                            return null; // User not found or password mismatch
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to process login request for username {Username}", user.Username); // Log lỗi
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
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Refresh token failed: Token is empty");
                throw new ArgumentException("Refresh token is required.");
            }

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
                                    full_name = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
                                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                    role = reader.GetString(reader.GetOrdinal("role")),
                                    department_company_unit = reader.IsDBNull(reader.GetOrdinal("department_company_unit")) ? null : reader.GetString(reader.GetOrdinal("department_company_unit")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    refresh_token = reader.GetString(reader.GetOrdinal("refresh_token")),
                                    refresh_token_expiry = reader.GetDateTime(reader.GetOrdinal("refresh_token_expiry"))
                                };

                                // Tạo refresh token mới
                                var (newRefreshToken, expiry) = JwtTokenHelper.GenerateRefreshToken();
                                user.refresh_token = newRefreshToken;
                                user.refresh_token_expiry = expiry;

                                await UpdateRefreshToken(user.user_id, newRefreshToken, expiry);
                                _logger.LogInformation("Refreshed token for user with ID {UserId} successfully", user.user_id);
                                return user;
                            }
                            _logger.LogWarning("Refresh token failed: Invalid or expired token");
                            return null; // Refresh token không hợp lệ hoặc hết hạn
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to process refresh token"); // Log lỗi
                    throw new InvalidOperationException("Failed to process refresh token.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task UpdateRefreshToken(int userId, string refreshToken, DateTime expiry)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "UPDATE users SET refresh_token = @refreshToken, refresh_token_expiry = @expiry WHERE user_id = @userId";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@refreshToken", refreshToken);
                        cmd.Parameters.AddWithValue("@expiry", expiry);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        await cmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Updated refresh token for user with ID {UserId} successfully", userId); 
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to update refresh token for user with ID {UserId}", userId); 
                    throw new InvalidOperationException($"Failed to update refresh token for user ID {userId}.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(UsersRequest entity, bool isCreate)
        {
            if (string.IsNullOrWhiteSpace(entity.username))
            {
                _logger.LogWarning("Validation failed: Username cannot be empty"); 
                throw new ArgumentException("Username cannot be empty.");
            }
            if (isCreate && string.IsNullOrWhiteSpace(entity.password))
            {
                _logger.LogWarning("Validation failed: Password cannot be empty when creating a user"); 
                throw new ArgumentException("Password cannot be empty when creating a user.");
            }
            if (string.IsNullOrWhiteSpace(entity.role))
            {
                _logger.LogWarning("Validation failed: Role cannot be empty"); 
                throw new ArgumentException("Role cannot be empty.");
            }
            if (!ValidRoles.Contains(entity.role))
            {
                _logger.LogWarning("Validation failed: Invalid role {Role}", entity.role); 
                throw new ArgumentException($"Role must be one of: {string.Join(", ", ValidRoles)}.");
            }
        }
    }
}