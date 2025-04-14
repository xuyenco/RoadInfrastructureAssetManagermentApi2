using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class AssetCagetoriesService : IAssetCagetoriesService
    {
        private readonly string _connectionString;
        private static readonly string[] ValidGeometryTypes = { "point", "line", "polygon" };

        public AssetCagetoriesService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<AssetCagetoriesResponse>> GetAllAssetCagetories()
        {
            var assetCategories = new List<AssetCagetoriesResponse>();

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
                            var category = new AssetCagetoriesResponse
                            {
                                cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                cagetory_name = reader.GetString(reader.GetOrdinal("category_name")),
                                geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                attributes_schema = ParseJsonObject(reader.GetString("attributes_schema"), "attributes_schema"),
                                lifecycle_stages = ParseJsonArray(reader.GetString("lifecycle_stages"), "lifecycle_stages"),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ?
                                    null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                marker_url = reader.IsDBNull(reader.GetOrdinal("marker_url")) ? null : reader.GetString(reader.GetOrdinal("marker_url"))
                            };
                            assetCategories.Add(category);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve asset categories from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return assetCategories;
            }
        }

        public async Task<AssetCagetoriesResponse?> GetAssetCagetoriesByid(int id)
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
                                return new AssetCagetoriesResponse
                                {
                                    cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    cagetory_name = reader.GetString(reader.GetOrdinal("category_name")),
                                    geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                    attributes_schema = ParseJsonObject(reader.GetString("attributes_schema"), "attributes_schema"),
                                    lifecycle_stages = ParseJsonArray(reader.GetString("lifecycle_stages"), "lifecycle_stages"),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ?
                                        null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    marker_url = reader.IsDBNull(reader.GetOrdinal("marker_url")) ? null : reader.GetString(reader.GetOrdinal("marker_url"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve asset category with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<AssetCagetoriesResponse?> CreateAssetCagetories(AssetCagetoriesRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO asset_categories 
                (category_name, geometry_type, attributes_schema, lifecycle_stages,marker_url)
                VALUES (@name, @geomType, @attrsSchema::jsonb, @lifecycle::jsonb,@marker_url)
                RETURNING category_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@name", entity.cagetory_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attributes_schema.ToString());
                        cmd.Parameters.AddWithValue("@lifecycle", entity.lifecycle_stages.ToString());
                        cmd.Parameters.AddWithValue("@marker_url", entity.marker_url);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetAssetCagetoriesByid(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        throw new InvalidOperationException($"Category name '{entity.cagetory_name}' already exists.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        throw new InvalidOperationException("Invalid geometry type provided.", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to create asset category.", ex);
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
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE asset_categories SET
                    category_name = @name,
                    geometry_type = @geomType,
                    attributes_schema = @attrsSchema::jsonb,
                    lifecycle_stages = @lifecycle::jsonb,
                    marker_url = @marker_url
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
                        cmd.Parameters.AddWithValue("@marker_url", entity.marker_url);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetAssetCagetoriesByid(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        throw new InvalidOperationException($"Category name '{entity.cagetory_name}' already exists.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        throw new InvalidOperationException("Invalid geometry type provided.", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to update asset category with ID {id}.", ex);
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
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Cannot delete category with ID {id} because it is referenced by other records.", ex);
                    }
                    throw new InvalidOperationException($"Failed to delete asset category with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper methods
        private void ValidateRequest(AssetCagetoriesRequest entity)
        {
            if (string.IsNullOrWhiteSpace(entity.cagetory_name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }
            if (!ValidGeometryTypes.Contains(entity.geometry_type))
            {
                throw new ArgumentException($"Geometry type must be one of: {string.Join(", ", ValidGeometryTypes)}.");
            }
            try
            {
                entity.attributes_schema.ToString();
                entity.lifecycle_stages.ToString();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid JSON format for attributes_schema or lifecycle_stages.", ex);
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

        private JArray ParseJsonArray(string json, string fieldName)
        {
            try
            {
                return JArray.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON format for {fieldName}.", ex);
            }
        }
    }
}