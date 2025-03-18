using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System.Data;
using System.Data.Common;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class AssetCagetoriesService : IAssetCagetoriesService
    {
        private readonly string _connectionString;

        public AssetCagetoriesService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<AssetCagetoriesResponse>> GetAllAssetCagetories()
        {
            var asset_cagetories = new List<AssetCagetoriesResponse>();

            using(var _connection = new NpgsqlConnection(_connectionString)) {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM asset_categories";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var category = new AssetCagetoriesResponse
                        {
                            cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                            cagetory_name = reader.GetString(reader.GetOrdinal("category_name")),
                            geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                            attributes_schema = JObject.Parse(reader.GetString("attributes_schema")),
                            lifecycle_stages = JArray.Parse(reader.GetString("lifecycle_stages")),
                            created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ?
                                (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                        };
                        asset_cagetories.Add(category);
                    }
                }
                await _connection.CloseAsync();
                return asset_cagetories;
            }
        }

        public async Task<AssetCagetoriesResponse?> GetAssetCagetoriesByid(int id)
        {
            using(var _connection = new NpgsqlConnection(_connectionString)) {
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
                                return new AssetCagetoriesResponse
                                {
                                    cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    cagetory_name = reader.GetString(reader.GetOrdinal("category_name")),
                                    geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                    attributes_schema = JObject.Parse(reader.GetString("attributes_schema")),
                                    lifecycle_stages = JArray.Parse(reader.GetString("lifecycle_stages")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ?
                                        (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_at"))
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

        public async Task<AssetCagetoriesResponse?> CreateAssetCagetories(AssetCagetoriesRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO asset_categories 
                (category_name, geometry_type, attributes_schema, lifecycle_stages)
                VALUES (@name, @geomType, @attrsSchema::jsonb, @lifecycle::jsonb)
                RETURNING category_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@name", entity.cagetory_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attributes_schema.ToString());
                        cmd.Parameters.AddWithValue("@lifecycle", entity.lifecycle_stages.ToString());
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetAssetCagetoriesByid(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }

            }
        }
        public async Task<AssetCagetoriesResponse?> UpdateAssetCagetories(int id, AssetCagetoriesRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE asset_categories SET
                    category_name = @name,
                    geometry_type = @geomType,
                    attributes_schema = @attrsSchema::jsonb,
                    lifecycle_stages = @lifecycle::jsonb,
                    created_at = @createdAt
                WHERE category_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", entity.cagetory_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attributes_schema.ToString());
                        cmd.Parameters.AddWithValue("@lifecycle", entity.lifecycle_stages.ToString());

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetAssetCagetoriesByid(id);
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
        public async Task<bool> DeleteAssetCagetories(int id)
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