using Microsoft.Extensions.Logging; // Thêm namespace cho logging
using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class AssetCategoriesService : IAssetCategoriesService
    {
        private readonly string _connectionString;
        private readonly ILogger<AssetCategoriesService> _logger; 
        private static readonly string[] ValidGeometryTypes = { "point", "linestring", "polygon" };

        public AssetCategoriesService(string connectionString, ILogger<AssetCategoriesService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<AssetCategoriesResponse>> GetAllAssetCategories()
        {
            var assetCategories = new List<AssetCategoriesResponse>();

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM asset_categories";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var category = new AssetCategoriesResponse
                            {
                                category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                attribute_schema = reader.IsDBNull(reader.GetOrdinal("attribute_schema")) ? null : JObject.Parse(reader.GetString(reader.GetOrdinal("attribute_schema"))),
                                sample_image = reader.IsDBNull(reader.GetOrdinal("sample_image")) ? null : reader.GetString(reader.GetOrdinal("sample_image")),
                                sample_image_name = reader.IsDBNull(reader.GetOrdinal("sample_image_name")) ? null : reader.GetString(reader.GetOrdinal("sample_image_name")),
                                sample_image_public_id = reader.IsDBNull(reader.GetOrdinal("sample_image_public_id")) ? null : reader.GetString(reader.GetOrdinal("sample_image_public_id")),
                                icon_url = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString(reader.GetOrdinal("icon_url")),
                                icon_public_id = reader.IsDBNull(reader.GetOrdinal("icon_public_id")) ? null : reader.GetString(reader.GetOrdinal("icon_public_id")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            assetCategories.Add(category);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} asset categories successfully", assetCategories.Count); 
                    return assetCategories;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve asset categories from database"); 
                    throw new InvalidOperationException("Failed to retrieve asset categories from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetCategoriesResponse?> GetAssetCategoriesById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM asset_categories WHERE category_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var category = new AssetCategoriesResponse
                                {
                                    category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                    geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                    attribute_schema = reader.IsDBNull(reader.GetOrdinal("attribute_schema")) ? null : JObject.Parse(reader.GetString(reader.GetOrdinal("attribute_schema"))),
                                    sample_image = reader.IsDBNull(reader.GetOrdinal("sample_image")) ? null : reader.GetString(reader.GetOrdinal("sample_image")),
                                    sample_image_name = reader.IsDBNull(reader.GetOrdinal("sample_image_name")) ? null : reader.GetString(reader.GetOrdinal("sample_image_name")),
                                    sample_image_public_id = reader.IsDBNull(reader.GetOrdinal("sample_image_public_id")) ? null : reader.GetString(reader.GetOrdinal("sample_image_public_id")),
                                    icon_url = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString(reader.GetOrdinal("icon_url")),
                                    icon_public_id = reader.IsDBNull(reader.GetOrdinal("icon_public_id")) ? null : reader.GetString(reader.GetOrdinal("icon_public_id")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved asset category with ID {CategoryId} successfully", id);
                                return category;
                            }
                            _logger.LogWarning("Asset category with ID {CategoryId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve asset category with ID {CategoryId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve asset category with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetCategoriesResponse?> CreateAssetCategories(AssetCategoriesRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO asset_categories 
                (category_name, geometry_type, attribute_schema, sample_image, sample_image_name, sample_image_public_id, icon_url, icon_public_id)
                VALUES (@name, @geomType, @attrsSchema::jsonb, @sampleImage, @sample_image_name, @sample_image_public_id, @icon_url, @icon_public_id)
                RETURNING category_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@name", entity.category_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attribute_schema.ToString());
                        cmd.Parameters.AddWithValue("@sampleImage", (object)entity.sample_image ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sample_image_name", (object)entity.sample_image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sample_image_public_id", (object)entity.sample_image_public_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@icon_url", (object)entity.icon_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@icon_public_id", (object)entity.icon_public_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created asset category with ID {CategoryId} successfully", newId); 
                        return await GetAssetCategoriesById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        _logger.LogError(ex, "Failed to create asset category: Category name '{CategoryName}' already exists", entity.category_name); 
                        throw new InvalidOperationException($"Category name '{entity.category_name}' already exists.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        _logger.LogError(ex, "Failed to create asset category: Invalid geometry type"); 
                        throw new InvalidOperationException("Invalid geometry type provided.", ex);
                    }
                    _logger.LogError(ex, "Failed to create asset category");
                    throw new InvalidOperationException("Failed to create asset category.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetCategoriesResponse?> UpdateAssetCategories(int id, AssetCategoriesRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE asset_categories SET
                    category_name = @name,
                    geometry_type = @geomType,
                    attribute_schema = @attrsSchema::jsonb,
                    sample_image = @sampleImage,
                    sample_image_name = @sample_image_name,
                    sample_image_public_id = @sample_image_public_id,
                    icon_url = @icon_url,
                    icon_public_id = @icon_public_id
                WHERE category_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", entity.category_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attribute_schema.ToString());
                        cmd.Parameters.AddWithValue("@sampleImage", (object)entity.sample_image ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sample_image_name", (object)entity.sample_image_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sample_image_public_id", (object)entity.sample_image_public_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@icon_url", (object)entity.icon_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@icon_public_id", (object)entity.icon_public_id ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated asset category with ID {CategoryId} successfully", id); 
                            return await GetAssetCategoriesById(id);
                        }
                        _logger.LogWarning("Asset category with ID {CategoryId} not found for update", id); 
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        _logger.LogError(ex, "Failed to update asset category with ID {CategoryId}: Category name '{CategoryName}' already exists", id, entity.category_name); 
                        throw new InvalidOperationException($"Category name '{entity.category_name}' already exists.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        _logger.LogError(ex, "Failed to update asset category with ID {CategoryId}: Invalid geometry type", id); 
                        throw new InvalidOperationException("Invalid geometry type provided.", ex);
                    }
                    _logger.LogError(ex, "Failed to update asset category with ID {CategoryId}", id);
                    throw new InvalidOperationException($"Failed to update asset category with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteAssetCategories(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM asset_categories WHERE category_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted asset category with ID {CategoryId} successfully", id); 
                            return true;
                        }
                        _logger.LogWarning("Asset category with ID {CategoryId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete asset category with ID {CategoryId}: Referenced by other records", id);
                        throw new InvalidOperationException($"Cannot delete category with ID {id} because it is referenced by other records.", ex);
                    }
                    _logger.LogError(ex, "Failed to delete asset category with ID {CategoryId}", id); 
                    throw new InvalidOperationException($"Failed to delete asset category with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper methods
        private void ValidateRequest(AssetCategoriesRequest entity)
        {
            if (string.IsNullOrWhiteSpace(entity.category_name))
            {
                _logger.LogWarning("Validation failed: Category name cannot be empty"); 
                throw new ArgumentException("Category name cannot be empty.");
            }
            if (!ValidGeometryTypes.Contains(entity.geometry_type))
            {
                _logger.LogWarning("Validation failed: Geometry type must be one of: {ValidTypes}", string.Join(", ", ValidGeometryTypes)); 
                throw new ArgumentException($"Geometry type must be one of: {string.Join(", ", ValidGeometryTypes)}.");
            }
            try
            {
                entity.attribute_schema.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid JSON format for attribute_schema"); 
                throw new ArgumentException("Invalid JSON format for attribute_schema.", ex);
            }
        }
    }
}