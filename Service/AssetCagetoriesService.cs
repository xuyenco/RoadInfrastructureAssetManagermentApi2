using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class AssetCategoriesService : IAssetCategoriesService
    {
        private readonly string _connectionString;
        private static readonly string[] ValidGeometryTypes = { "point", "linestring", "polygon" };

        public AssetCategoriesService(string connectionString)
        {
            _connectionString = connectionString;
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
                                attribute_schema = reader.IsDBNull(reader.GetOrdinal("attribute_schema"))
                                    ? null
                                    : JObject.Parse(reader.GetString(reader.GetOrdinal("attribute_schema"))),
                                sample_image = reader.IsDBNull(reader.GetOrdinal("sample_image"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("sample_image")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at"))
                                    ? null
                                    : reader.GetDateTime(reader.GetOrdinal("created_at"))
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
                                return new AssetCategoriesResponse
                                {
                                    category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                    geometry_type = reader.GetString(reader.GetOrdinal("geometry_type")),
                                    attribute_schema = reader.IsDBNull(reader.GetOrdinal("attribute_schema"))
                                        ? null
                                        : JObject.Parse(reader.GetString(reader.GetOrdinal("attribute_schema"))),
                                    sample_image = reader.IsDBNull(reader.GetOrdinal("sample_image"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("sample_image")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at"))
                                        ? null
                                        : reader.GetDateTime(reader.GetOrdinal("created_at"))
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

        public async Task<AssetCategoriesResponse?> CreateAssetCategories(AssetCategoriesRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO asset_categories 
                (category_name, geometry_type, attribute_schema, sample_image)
                VALUES (@name, @geomType, @attrsSchema::jsonb, @sampleImage)
                RETURNING category_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@name", entity.category_name);
                        cmd.Parameters.AddWithValue("@geomType", entity.geometry_type);
                        cmd.Parameters.AddWithValue("@attrsSchema", entity.attribute_schema.ToString());
                        cmd.Parameters.AddWithValue("@sampleImage", (object)entity.sample_image ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetAssetCategoriesById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        throw new InvalidOperationException($"Category name '{entity.category_name}' already exists.", ex);
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
                    sample_image = @sampleImage
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

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetAssetCategoriesById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23505") // UNIQUE constraint violation
                    {
                        throw new InvalidOperationException($"Category name '{entity.category_name}' already exists.", ex);
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
        private void ValidateRequest(AssetCategoriesRequest entity)
        {
            if (string.IsNullOrWhiteSpace(entity.category_name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }
            if (!ValidGeometryTypes.Contains(entity.geometry_type))
            {
                throw new ArgumentException($"Geometry type must be one of: {string.Join(", ", ValidGeometryTypes)}.");
            }
            try
            {
                entity.attribute_schema.ToString();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid JSON format for attribute_schema.", ex);
            }
        }
    }
}