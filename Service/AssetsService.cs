using Microsoft.Extensions.Logging; 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System.Data;
using System.Text;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class AssetsService : IAssetsService
    {
        private readonly string _connectionString;
        private readonly ILogger<AssetsService> _logger; 

        public AssetsService(string connectionString, ILogger<AssetsService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<(IEnumerable<AssetsResponse> Assets, int TotalCount)> GetAssetsPagination(int page, int pageSize, string searchTerm, int searchField)
        {
            var assets = new List<AssetsResponse>();
            int totalCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Build SQL query
                var sqlBuilder = new StringBuilder(@"SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, asset_name, asset_code, 
                                            address, construction_year, operation_year, land_area, floor_area, 
                                            original_value, remaining_value, asset_status, installation_unit, 
                                            management_unit, custom_attributes, created_at, image_url, image_name, image_public_id
                                            FROM assets");
                var countSql = "SELECT COUNT(*) FROM assets";
                var parameters = new List<NpgsqlParameter>();

                // Add search conditions if searchTerm is provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = $"%{searchTerm.ToLower()}%"; // Prepare for ILIKE
                    string condition = searchField switch
                    {
                        0 => "LOWER(asset_code) ILIKE @searchTerm", // Asset ID
                        1 => "LOWER(asset_name) ILIKE @searchTerm",      // Asset Name
                        2 => "LOWER(asset_code) ILIKE @searchTerm",      // Asset Code
                        3 => "LOWER(address) ILIKE @searchTerm",         // Address
                        4 => "LOWER(asset_status) ILIKE @searchTerm",    // Asset Status
                        5 => "TO_CHAR(created_at, 'DD/MM/YYYY HH24:MI') ILIKE @searchTerm", // Created At
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

                // Add pagination
                sqlBuilder.Append(" ORDER BY asset_id");
                sqlBuilder.Append(" OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
                parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
                parameters.Add(new NpgsqlParameter("@pageSize", pageSize));

                try
                {
                    // Get total count
                    using (var countCmd = new NpgsqlCommand(countSql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // Get paginated assets
                    using (var cmd = new NpgsqlCommand(sqlBuilder.ToString(), connection))
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var asset = new AssetsResponse
                                {
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    geometry = reader.IsDBNull(reader.GetOrdinal("geometry")) ? null : ParseGeoJson(reader.GetString("geometry"), "geometry"),
                                    asset_name = reader.IsDBNull(reader.GetOrdinal("asset_name")) ? null : reader.GetString(reader.GetOrdinal("asset_name")),
                                    asset_code = reader.IsDBNull(reader.GetOrdinal("asset_code")) ? null : reader.GetString(reader.GetOrdinal("asset_code")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    construction_year = reader.IsDBNull(reader.GetOrdinal("construction_year")) ? null : reader.GetDateTime(reader.GetOrdinal("construction_year")),
                                    operation_year = reader.IsDBNull(reader.GetOrdinal("operation_year")) ? null : reader.GetDateTime(reader.GetOrdinal("operation_year")),
                                    land_area = reader.IsDBNull(reader.GetOrdinal("land_area")) ? null : reader.GetDouble(reader.GetOrdinal("land_area")),
                                    floor_area = reader.IsDBNull(reader.GetOrdinal("floor_area")) ? null : reader.GetDouble(reader.GetOrdinal("floor_area")),
                                    original_value = reader.IsDBNull(reader.GetOrdinal("original_value")) ? null : reader.GetDouble(reader.GetOrdinal("original_value")),
                                    remaining_value = reader.IsDBNull(reader.GetOrdinal("remaining_value")) ? null : reader.GetDouble(reader.GetOrdinal("remaining_value")),
                                    asset_status = reader.IsDBNull(reader.GetOrdinal("asset_status")) ? null : reader.GetString(reader.GetOrdinal("asset_status")),
                                    installation_unit = reader.IsDBNull(reader.GetOrdinal("installation_unit")) ? null : reader.GetString(reader.GetOrdinal("installation_unit")),
                                    management_unit = reader.IsDBNull(reader.GetOrdinal("management_unit")) ? null : reader.GetString(reader.GetOrdinal("management_unit")),
                                    custom_attributes = reader.IsDBNull(reader.GetOrdinal("custom_attributes")) ? null : JObject.Parse(reader.GetString(reader.GetOrdinal("custom_attributes"))),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id"))
                                };
                                assets.Add(asset);
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} assets for page {Page} with total count {TotalCount}", assets.Count, page, totalCount);
                    return (assets, totalCount);
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve assets with pagination and search");
                    throw new InvalidOperationException("Failed to retrieve assets from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<AssetsResponse>> GetAllAssets()
        {
            var assets = new List<AssetsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, asset_name, asset_code, 
                           address, construction_year, operation_year, land_area, floor_area, 
                           original_value, remaining_value, asset_status, installation_unit, 
                           management_unit, custom_attributes, created_at, image_url, image_name, image_public_id
                           FROM assets";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var asset = new AssetsResponse
                            {
                                asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                geometry = reader.IsDBNull(reader.GetOrdinal("geometry")) ? null : ParseGeoJson(reader.GetString("geometry"), "geometry"),
                                asset_name = reader.IsDBNull(reader.GetOrdinal("asset_name")) ? null : reader.GetString(reader.GetOrdinal("asset_name")),
                                asset_code = reader.IsDBNull(reader.GetOrdinal("asset_code")) ? null : reader.GetString(reader.GetOrdinal("asset_code")),
                                address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                construction_year = reader.IsDBNull(reader.GetOrdinal("construction_year")) ? null : reader.GetDateTime(reader.GetOrdinal("construction_year")),
                                operation_year = reader.IsDBNull(reader.GetOrdinal("operation_year")) ? null : reader.GetDateTime(reader.GetOrdinal("operation_year")),
                                land_area = reader.IsDBNull(reader.GetOrdinal("land_area")) ? null : reader.GetDouble(reader.GetOrdinal("land_area")),
                                floor_area = reader.IsDBNull(reader.GetOrdinal("floor_area")) ? null : reader.GetDouble(reader.GetOrdinal("floor_area")),
                                original_value = reader.IsDBNull(reader.GetOrdinal("original_value")) ? null : reader.GetDouble(reader.GetOrdinal("original_value")),
                                remaining_value = reader.IsDBNull(reader.GetOrdinal("remaining_value")) ? null : reader.GetDouble(reader.GetOrdinal("remaining_value")),
                                asset_status = reader.IsDBNull(reader.GetOrdinal("asset_status")) ? null : reader.GetString(reader.GetOrdinal("asset_status")),
                                installation_unit = reader.IsDBNull(reader.GetOrdinal("installation_unit")) ? null : reader.GetString(reader.GetOrdinal("installation_unit")),
                                management_unit = reader.IsDBNull(reader.GetOrdinal("management_unit")) ? null : reader.GetString(reader.GetOrdinal("management_unit")),
                                custom_attributes = reader.IsDBNull(reader.GetOrdinal("custom_attributes")) ? null : JObject.Parse(reader.GetString(reader.GetOrdinal("custom_attributes"))),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id"))
                            };
                            assets.Add(asset);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} assets successfully", assets.Count); 
                    return assets;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve assets from database"); 
                    throw new InvalidOperationException("Failed to retrieve assets from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetsResponse?> GetAssetById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, asset_name, asset_code, 
                           address, construction_year, operation_year, land_area, floor_area, 
                           original_value, remaining_value, asset_status, installation_unit, 
                           management_unit, custom_attributes, created_at, image_url, image_name, image_public_id
                           FROM assets WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var asset = new AssetsResponse
                                {
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    geometry = reader.IsDBNull(reader.GetOrdinal("geometry")) ? null : ParseGeoJson(reader.GetString("geometry"), "geometry"),
                                    asset_name = reader.IsDBNull(reader.GetOrdinal("asset_name")) ? null : reader.GetString(reader.GetOrdinal("asset_name")),
                                    asset_code = reader.IsDBNull(reader.GetOrdinal("asset_code")) ? null : reader.GetString(reader.GetOrdinal("asset_code")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    construction_year = reader.IsDBNull(reader.GetOrdinal("construction_year")) ? null : reader.GetDateTime(reader.GetOrdinal("construction_year")),
                                    operation_year = reader.IsDBNull(reader.GetOrdinal("operation_year")) ? null : reader.GetDateTime(reader.GetOrdinal("operation_year")),
                                    land_area = reader.IsDBNull(reader.GetOrdinal("land_area")) ? null : reader.GetDouble(reader.GetOrdinal("land_area")),
                                    floor_area = reader.IsDBNull(reader.GetOrdinal("floor_area")) ? null : reader.GetDouble(reader.GetOrdinal("floor_area")),
                                    original_value = reader.IsDBNull(reader.GetOrdinal("original_value")) ? null : reader.GetDouble(reader.GetOrdinal("original_value")),
                                    remaining_value = reader.IsDBNull(reader.GetOrdinal("remaining_value")) ? null : reader.GetDouble(reader.GetOrdinal("remaining_value")),
                                    asset_status = reader.IsDBNull(reader.GetOrdinal("asset_status")) ? null : reader.GetString(reader.GetOrdinal("asset_status")),
                                    installation_unit = reader.IsDBNull(reader.GetOrdinal("installation_unit")) ? null : reader.GetString(reader.GetOrdinal("installation_unit")),
                                    management_unit = reader.IsDBNull(reader.GetOrdinal("management_unit")) ? null : reader.GetString(reader.GetOrdinal("management_unit")),
                                    custom_attributes = reader.IsDBNull(reader.GetOrdinal("custom_attributes")) ? null : JObject.Parse(reader.GetString(reader.GetOrdinal("custom_attributes"))),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id"))
                                };
                                _logger.LogInformation("Retrieved asset with ID {AssetId} successfully", id);
                                return asset;
                            }
                            _logger.LogWarning("Asset with ID {AssetId} not found", id);
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve asset with ID {AssetId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve asset with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetsResponse?> CreateAsset(AssetsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO assets 
                (category_id, asset_name, asset_code, address, geometry, construction_year, operation_year, 
                 land_area, floor_area, original_value, remaining_value, asset_status, installation_unit, 
                 management_unit, custom_attributes, image_url, image_name, image_public_id)
                VALUES (@category_id, @asset_name, @asset_code, @address, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), 
                        @construction_year, @operation_year, @land_area, @floor_area, @original_value, 
                        @remaining_value, @asset_status, @installation_unit, @management_unit, @custom_attributes::jsonb, @image_url, @image_name, @image_public_id)
                RETURNING asset_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@category_id", entity.category_id);
                        cmd.Parameters.AddWithValue("@asset_name", (object)entity.asset_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@asset_code", (object)entity.asset_code ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@construction_year", (object)entity.construction_year ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@operation_year", (object)entity.operation_year ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@land_area", (object)entity.land_area ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@floor_area", (object)entity.floor_area ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@original_value", (object)entity.original_value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@remaining_value", (object)entity.remaining_value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@asset_status", (object)entity.asset_status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@installation_unit", (object)entity.installation_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@management_unit", (object)entity.management_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@custom_attributes", entity.custom_attributes.ToString());
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created asset with ID {AssetId} successfully", newId); // Log thành công
                        return await GetAssetById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create asset: Invalid category ID {CategoryId}", entity.category_id); 
                        throw new InvalidOperationException($"Category ID {entity.category_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        _logger.LogError(ex, "Failed to create asset: Invalid GeoJSON format for geometry"); 
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        _logger.LogError(ex, "Failed to create asset: Invalid asset status");
                        throw new InvalidOperationException("Invalid asset status provided.", ex);
                    }
                    _logger.LogError(ex, "Failed to create asset");
                    throw new InvalidOperationException("Failed to create asset.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetsResponse?> UpdateAsset(int id, AssetsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE assets SET
                    category_id = @category_id,
                    asset_name = @asset_name,
                    asset_code = @asset_code,
                    address = @address,
                    geometry = ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405),
                    construction_year = @construction_year,
                    operation_year = @operation_year,
                    land_area = @land_area,
                    floor_area = @floor_area,
                    original_value = @original_value,
                    remaining_value = @remaining_value,
                    asset_status = @asset_status,
                    installation_unit = @installation_unit,
                    management_unit = @management_unit,
                    custom_attributes = @custom_attributes::jsonb,
                    image_url = @image_url,
                    image_name = @image_name,
                    image_public_id = @image_public_id
                WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@category_id", entity.category_id);
                        cmd.Parameters.AddWithValue("@asset_name", (object)entity.asset_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@asset_code", (object)entity.asset_code ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@construction_year", (object)entity.construction_year ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@operation_year", (object)entity.operation_year ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@land_area", (object)entity.land_area ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@floor_area", (object)entity.floor_area ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@original_value", (object)entity.original_value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@remaining_value", (object)entity.remaining_value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@asset_status", (object)entity.asset_status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@installation_unit", (object)entity.installation_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@management_unit", (object)entity.management_unit ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@custom_attributes", entity.custom_attributes.ToString());
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated asset with ID {AssetId} successfully", id); 
                            return await GetAssetById(id);
                        }
                        _logger.LogWarning("Asset with ID {AssetId} not found for update", id);
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update asset with ID {AssetId}: Invalid category ID {CategoryId}", id, entity.category_id); 
                        throw new InvalidOperationException($"Category ID {entity.category_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        _logger.LogError(ex, "Failed to update asset with ID {AssetId}: Invalid GeoJSON format for geometry");
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        _logger.LogError(ex, "Failed to update asset with ID {AssetId}: Invalid asset status");
                        throw new InvalidOperationException("Invalid asset status provided.", ex);
                    }
                    _logger.LogError(ex, "Failed to update asset with ID {AssetId}"); 
                    throw new InvalidOperationException($"Failed to update asset with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteAsset(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM assets WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted asset with ID {AssetId} successfully", id); // Log thành công
                            return true;
                        }
                        _logger.LogWarning("Asset with ID {AssetId} not found for deletion", id); // Log không tìm thấy
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete asset with ID {AssetId}: Referenced by other records"); // Log lỗi khóa ngoại
                        throw new InvalidOperationException($"Cannot delete asset with ID {id} because it is referenced by other records.", ex);
                    }
                    _logger.LogError(ex, "Failed to delete asset with ID {AssetId}"); // Log lỗi chung
                    throw new InvalidOperationException($"Failed to delete asset with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper methods
        private void ValidateRequest(AssetsRequest entity)
        {
            if (entity.category_id <= 0)
            {
                _logger.LogWarning("Validation failed: Category ID must be a positive integer"); // Log lỗi validation
                throw new ArgumentException("Category ID must be a positive integer.");
            }
            if (entity.geometry == null || string.IsNullOrEmpty(entity.geometry.type))
            {
                _logger.LogWarning("Validation failed: Geometry cannot be null or invalid"); // Log lỗi validation
                throw new ArgumentException("Geometry cannot be null or invalid.");
            }
            try
            {
                JsonConvert.SerializeObject(entity.geometry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid GeoJSON format for geometry"); // Log lỗi GeoJSON
                throw new ArgumentException("Invalid GeoJSON format for geometry.", ex);
            }
            if (entity.custom_attributes == null)
            {
                _logger.LogWarning("Validation failed: Custom attributes cannot be null"); // Log lỗi validation
                throw new ArgumentException("Custom attributes cannot be null.");
            }
            try
            {
                entity.custom_attributes.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid JSON format for custom attributes"); // Log lỗi JSON
                throw new ArgumentException("Invalid JSON format for custom attributes.", ex);
            }
        }

        private GeoJsonGeometry ParseGeoJson(string json, string fieldName)
        {
            try
            {
                var geometry = JsonConvert.DeserializeObject<GeoJsonGeometry>(json);
                _logger.LogInformation("Parsed GeoJSON for {FieldName} successfully", fieldName); // Log thành công
                return geometry;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse GeoJSON for {FieldName}", fieldName); // Log lỗi
                throw new InvalidOperationException($"Invalid GeoJSON format for {fieldName}.", ex);
            }
        }
    }
}