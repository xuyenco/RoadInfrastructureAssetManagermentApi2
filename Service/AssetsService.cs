using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Service
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
                var sql = @"SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, asset_name, asset_code, 
                           address, construction_year, operation_year, land_area, floor_area, 
                           original_value, remaining_value, asset_status, installation_unit, 
                           management_unit, custom_attributes, created_at 
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
                                geometry = ParseGeoJson(reader.GetString("geometry"), "geometry"),
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
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
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
                var sql = @"SELECT asset_id, category_id, ST_AsGeoJSON(geometry) as geometry, asset_name, asset_code, 
                           address, construction_year, operation_year, land_area, floor_area, 
                           original_value, remaining_value, asset_status, installation_unit, 
                           management_unit, custom_attributes, created_at 
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
                                return new AssetsResponse
                                {
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    geometry = ParseGeoJson(reader.GetString("geometry"), "geometry"),
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
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
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
                (category_id, asset_name, asset_code, address, geometry, construction_year, operation_year, 
                 land_area, floor_area, original_value, remaining_value, asset_status, installation_unit, 
                 management_unit, custom_attributes)
                VALUES (@category_id, @asset_name, @asset_code, @address, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), 
                        @construction_year, @operation_year, @land_area, @floor_area, @original_value, 
                        @remaining_value, @asset_status, @installation_unit, @management_unit, @custom_attributes::jsonb)
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
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetAssetById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Category ID {entity.category_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        throw new InvalidOperationException("Invalid asset status provided.", ex);
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
                    custom_attributes = @custom_attributes::jsonb
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
                        throw new InvalidOperationException($"Category ID {entity.category_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    else if (ex.SqlState == "23514") // CHECK constraint violation
                    {
                        throw new InvalidOperationException("Invalid asset status provided.", ex);
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
                        throw new InvalidOperationException($"Cannot delete asset with ID {id} because it is referenced by other records.", ex);
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
            if (entity.category_id <= 0)
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
            if (entity.custom_attributes == null)
            {
                throw new ArgumentException("Custom attributes cannot be null.");
            }
            try
            {
                entity.custom_attributes.ToString();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid JSON format for custom attributes.", ex);
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
    }
}