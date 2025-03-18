using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
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
                var sql = "SELECT asset_id, category_id, ST_AsGeoJSON(ST_Transform(geometry, 4326)) as geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at FROM assets";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var asset = new AssetsResponse
                        {
                            asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                            cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                            geometry = reader.IsDBNull(reader.GetOrdinal("geometry"))
                                ? new GeoJsonGeometry()
                                : JsonConvert.DeserializeObject<GeoJsonGeometry>(reader.GetString(reader.GetOrdinal("geometry"))),
                            attributes = JObject.Parse(reader.GetString(reader.GetOrdinal("attributes"))),
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
                await _connection.CloseAsync();
                return assets;
            }
        }

        public async Task<AssetsResponse?> GetAssetById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT asset_id, category_id, ST_AsGeoJSON(ST_Transform(geometry, 4326)) as geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at FROM assets WHERE asset_id = @id";

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
                                    geometry = reader.IsDBNull(reader.GetOrdinal("geometry"))
                                        ? new GeoJsonGeometry()
                                        : JsonConvert.DeserializeObject<GeoJsonGeometry>(reader.GetString(reader.GetOrdinal("geometry"))),
                                    attributes = JObject.Parse(reader.GetString("attributes")),
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetsResponse?> CreateAsset(AssetsRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO assets 
                (category_id, geometry, attributes, lifecycle_stage, installation_date, expected_lifetime, condition, last_inspection_date, created_at, updated_at)
                VALUES (@category_id, ST_Transform(ST_GeomFromGeoJSON(@geometry), 3405), @attributes, @lifecycle_stage, @installation_date, @expected_lifetime, @condition, @last_inspection_date, @created_at, @updated_at)
                RETURNING asset_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@category_id", entity.cagetory_id);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.Add("@attributes", NpgsqlDbType.Jsonb).Value = entity.attributes.ToString(); cmd.Parameters.AddWithValue("@lifecycle_stage", entity.lifecycle_stage);
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetsResponse?> UpdateAsset(int id, AssetsRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE assets SET
                    category_id = @category_id,
                    geometry = ST_Transform(ST_GeomFromGeoJSON(@geometry), 3405),
                    attributes = @attributes,
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
    }
}