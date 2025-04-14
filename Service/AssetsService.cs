using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System.Data;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class AssetsService : IAssetsService
    {
        private readonly string _connectionString;

        public AssetsService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<AssetsResponse>> GetAllAssets()
        {
            var assets = new List<AssetsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at FROM assets";

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
                                cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                geometry = ParseGeoJson(reader.GetString("geometry"), "geometry"),
                                attributes = ParseJsonObject(reader.GetString("attributes"), "attributes"),
                                lifecycle_stage = reader.GetString(reader.GetOrdinal("lifecycle_stage")),
                                installation_date = reader.IsDBNull(reader.GetOrdinal("installation_date")) ? null : reader.GetDateTime(reader.GetOrdinal("installation_date")),
                                expected_lifetime = reader.GetInt32(reader.GetOrdinal("expected_lifetime")),
                                condition = reader.GetString(reader.GetOrdinal("condition")),
                                last_inspection_date = reader.IsDBNull(reader.GetOrdinal("last_inspection_date")) ? null : reader.GetDateTime(reader.GetOrdinal("last_inspection_date")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updated_at = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                            };
                            assets.Add(asset);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve assets from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return assets;
            }
        }

        public async Task<AssetsResponse?> GetAssetById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at FROM assets WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new AssetsResponse
                                {
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    geometry = ParseGeoJson(reader.GetString("geometry"), "geometry"),
                                    attributes = ParseJsonObject(reader.GetString("attributes"), "attributes"),
                                    lifecycle_stage = reader.GetString(reader.GetOrdinal("lifecycle_stage")),
                                    installation_date = reader.IsDBNull(reader.GetOrdinal("installation_date")) ? null : reader.GetDateTime(reader.GetOrdinal("installation_date")),
                                    expected_lifetime = reader.GetInt32(reader.GetOrdinal("expected_lifetime")),
                                    condition = reader.GetString(reader.GetOrdinal("condition")),
                                    last_inspection_date = reader.IsDBNull(reader.GetOrdinal("last_inspection_date")) ? null : reader.GetDateTime(reader.GetOrdinal("last_inspection_date")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    updated_at = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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
                (category_id, geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at)
                VALUES (@category_id,ST_SetSRID(ST_GeomFromGeoJSON(@geometry),3405), @attributes::jsonb, @lifecycle_stage, @installation_date, @expected_lifetime, @condition, @last_inspection_date, @created_at, @updated_at)
                RETURNING asset_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@category_id", entity.cagetory_id);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@attributes", entity.attributes.ToString());
                        cmd.Parameters.AddWithValue("@lifecycle_stage", entity.lifecycle_stage);
                        cmd.Parameters.AddWithValue("@installation_date", entity.installation_date ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@expected_lifetime", entity.expected_lifetime);
                        cmd.Parameters.AddWithValue("@condition", entity.condition);
                        cmd.Parameters.AddWithValue("@last_inspection_date", entity.last_inspection_date ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetAssetById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to create asset.", ex);
                    }
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
                    geometry = ST_SetSRID(ST_GeomFromGeoJSON(@geometry),3405),
                    attributes = @attributes::jsonb,
                    lifecycle_stage = @lifecycle_stage,
                    installation_date = @installation_date,
                    expected_lifetime = @expected_lifetime,
                    condition = @condition,
                    last_inspection_date = @last_inspection_date,
                    updated_at = @updated_at
                WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@category_id", entity.cagetory_id);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@attributes", entity.attributes.ToString());
                        cmd.Parameters.AddWithValue("@lifecycle_stage", entity.lifecycle_stage);
                        cmd.Parameters.AddWithValue("@installation_date", entity.installation_date ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@expected_lifetime", entity.expected_lifetime);
                        cmd.Parameters.AddWithValue("@condition", entity.condition);
                        cmd.Parameters.AddWithValue("@last_inspection_date", entity.last_inspection_date ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetAssetById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to update asset with ID {id}.", ex);
                    }
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
                        return affectedRows > 0;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Cannot delete asset with ID {id} because it is referenced by other records (e.g., tasks or incidents).", ex);
                    }
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
            if (entity.cagetory_id <= 0)
            {
                throw new ArgumentException("Category ID must be a positive integer.");
            }
            if (entity.geometry == null || string.IsNullOrEmpty(entity.geometry.type))
            {
                throw new ArgumentException("Geometry cannot be null or invalid.");
            }
            try
            {
                JsonConvert.SerializeObject(entity.geometry);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid GeoJSON format for geometry.", ex);
            }
            if (string.IsNullOrWhiteSpace(entity.lifecycle_stage))
            {
                throw new ArgumentException("Lifecycle stage cannot be empty.");
            }
            if (entity.expected_lifetime < 0)
            {
                throw new ArgumentException("Expected lifetime cannot be negative.");
            }
            if (string.IsNullOrWhiteSpace(entity.condition))
            {
                throw new ArgumentException("Condition cannot be empty.");
            }
            try
            {
                entity.attributes.ToString();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid JSON format for attributes.", ex);
            }
        }

        private GeoJsonGeometry ParseGeoJson(string json, string fieldName)
        {
            try
            {
                return JsonConvert.DeserializeObject<GeoJsonGeometry>(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid GeoJSON format for {fieldName}.", ex);
            }
        }

        private JObject ParseJsonObject(string json, string fieldName)
        {
            try
            {
                return JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON format for {fieldName}.", ex);
            }
        }
    }
}