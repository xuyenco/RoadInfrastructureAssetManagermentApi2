using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Road_Infrastructure_Asset_Management.Model.ImageUpload;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using System.Text.Json;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetsService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        private readonly ILogger<AssetsController> _logger; 

        public AssetsController(IAssetsService Service, IConfiguration configuration, Cloudinary cloudinary, ILogger<AssetsController> logger) 
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAssets()
        {
            try
            {
                _logger.LogInformation("Received request to get all assets"); 
                var assets = await _Service.GetAllAssets();
                _logger.LogInformation("Returned {Count} assets", assets.Count()); 
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all assets"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetsById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get asset with ID {AssetId}", id); 
                var asset = await _Service.GetAssetById(id);
                if (asset == null)
                {
                    _logger.LogWarning("Asset with ID {AssetId} not found", id); 
                    return NotFound("Asset does not exist");
                }
                _logger.LogInformation("Returned asset with ID {AssetId}", id);
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get asset with ID {AssetId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssets([FromForm] AssetImageUploadRequest request)
        {
            _logger.LogInformation("Received request to create asset with data input: {CustomAttributes}, geometry: {Geometry}", request.custom_attributes, request.geometry); 

            // Parse custom_attributes
            JObject customAttributes;
            try
            {
                customAttributes = JObject.Parse(request.custom_attributes);
                _logger.LogInformation("Parsed custom attributes: {CustomAttributes}", customAttributes); 
            }
            catch (JsonReaderException ex)
            {
                _logger.LogWarning(ex, "Custom attributes parse error: {Message}", ex.Message); 
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            // Parse geometry
            GeoJsonGeometry geometry = new GeoJsonGeometry();
            try
            {
                if (string.IsNullOrEmpty(request.geometry))
                {
                    _logger.LogWarning("Validation failed: Geometry is required"); 
                    return BadRequest("Hình học là bắt buộc.");
                }

                // Clean the geometry string to remove extra quotes or escape sequences
                string cleanedGeometry = request.geometry;
                if (cleanedGeometry.StartsWith("\"") && cleanedGeometry.EndsWith("\""))
                {
                    cleanedGeometry = cleanedGeometry[1..^1];
                    cleanedGeometry = cleanedGeometry.Replace("\\\"", "\"").Replace("\\u0022", "\"");
                }

                _logger.LogInformation("Cleaned geometry: {CleanedGeometry}", cleanedGeometry); 

                // Parse the cleaned JSON into a JsonDocument
                using var jsonDoc = JsonDocument.Parse(cleanedGeometry);
                var root = jsonDoc.RootElement;

                // Extract type
                geometry.type = root.GetProperty("type").GetString() ?? string.Empty;

                // Extract coordinates based on geometry type
                if (string.IsNullOrEmpty(geometry.type))
                {
                    _logger.LogWarning("Validation failed: GeoJSON missing type"); 
                    return BadRequest("GeoJSON thiếu type.");
                }

                var coordinatesElement = root.GetProperty("coordinates");
                geometry.coordinates = geometry.type.ToLower() switch
                {
                    "point" => coordinatesElement.Deserialize<double[]>(), // [x, y]
                    "linestring" => coordinatesElement.Deserialize<double[][]>(), // [[x1, y1], [x2, y2], ...]
                    "polygon" => coordinatesElement.Deserialize<double[][][]>(), // [[[x1, y1], [x2, y2], ...]]
                    _ => throw new System.Text.Json.JsonException($"Loại hình học không được hỗ trợ: {geometry.type}")
                };

                if (geometry.coordinates == null)
                {
                    _logger.LogWarning("Validation failed: GeoJSON missing coordinates or invalid format"); 
                    return BadRequest("GeoJSON thiếu coordinates hoặc định dạng không hợp lệ.");
                }

                _logger.LogInformation("Parsed geometry: type={Type}, coordinates={Coordinates}", geometry.type, System.Text.Json.JsonSerializer.Serialize(geometry.coordinates)); 
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Geometry parse error: {Message}", ex.Message); 
                return BadRequest($"Định dạng GeoJSON không hợp lệ: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected geometry error: {Message}", ex.Message); 
                return BadRequest($"Lỗi xử lý GeoJSON: {ex.Message}");
            }

            // Validate image
            if (request.image == null || request.image.Length == 0)
            {
                _logger.LogWarning("Validation failed: Invalid image file"); 
                return BadRequest("Image file không hợp lệ");
            }

            try
            {
                // Upload image to Cloudinary
                var uploadImageParams = new ImageUploadParams
                {
                    File = new FileDescription(request.image.FileName, request.image.OpenReadStream()),
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = true 
                };
                var uploadImageResult = await _Cloudinary.UploadAsync(uploadImageParams);
                if (uploadImageResult.Error != null)
                {
                    _logger.LogError("Failed to upload image for asset creation: {Error}", uploadImageResult.Error.Message); 
                    return StatusCode((int)uploadImageResult.StatusCode, uploadImageResult.Error.Message);
                }
                var imageUrl = uploadImageResult.SecureUrl.ToString();
                var imageName = uploadImageResult.OriginalFilename;
                var imagePublicId = uploadImageResult.PublicId;
                _logger.LogInformation("Uploaded image for asset creation: PublicId {PublicId}", imagePublicId); 

                // Create final request
                var finalRequest = new AssetsRequest
                {
                    category_id = request.category_id,
                    asset_name = request.asset_name,
                    asset_code = request.asset_code,
                    address = request.address,
                    geometry = geometry,
                    construction_year = request.construction_year,
                    operation_year = request.operation_year,
                    land_area = request.land_area,
                    floor_area = request.floor_area,
                    original_value = request.original_value,
                    remaining_value = request.remaining_value,
                    asset_status = request.asset_status,
                    installation_unit = request.installation_unit,
                    management_unit = request.management_unit,
                    custom_attributes = customAttributes,
                    image_url = imageUrl,
                    image_name = imageName,
                    image_public_id = imagePublicId,
                };

                var asset = await _Service.CreateAsset(finalRequest);
                if (asset == null)
                {
                    _logger.LogError("Failed to create asset"); 
                    return BadRequest("Failed to create asset category.");
                }
                _logger.LogInformation("Created asset with ID {AssetId} successfully", asset.asset_id); 
                return CreatedAtAction(nameof(GetAssetsById), new { id = asset.category_id }, asset);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating asset: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create asset: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating asset");
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateAssets(int id, [FromForm] AssetImageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to update asset with ID {AssetId}, custom attributes: {CustomAttributes}", id, request.custom_attributes); 

                // Parse attributes_schema
                JObject customAttributes;
                try
                {
                    customAttributes = JObject.Parse(request.custom_attributes);
                    _logger.LogInformation("Parsed custom attributes: {CustomAttributes}", customAttributes); 
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON format for attributes_schema: {Message}", ex.Message); 
                    return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
                }

                if (request.geometry == null || request.geometry.Length == 0)
                {
                    _logger.LogWarning("Validation failed: Geometry cannot be empty for asset ID {AssetId}", id); 
                    return BadRequest("Vị trí địa lý không thể rỗng");
                }

                // Parse geometry
                GeoJsonGeometry geometry = new GeoJsonGeometry();
                try
                {
                    if (string.IsNullOrEmpty(request.geometry))
                    {
                        _logger.LogWarning("Validation failed: Geometry is required for asset ID {AssetId}", id);
                        return BadRequest("Hình học là bắt buộc.");
                    }

                    // Clean the geometry string to remove extra quotes or escape sequences
                    string cleanedGeometry = request.geometry;
                    if (cleanedGeometry.StartsWith("\"") && cleanedGeometry.EndsWith("\""))
                    {
                        cleanedGeometry = cleanedGeometry[1..^1];
                        cleanedGeometry = cleanedGeometry.Replace("\\\"", "\"").Replace("\\u0022", "\"");
                    }

                    _logger.LogInformation("Cleaned geometry for asset ID {AssetId}: {CleanedGeometry}", id, cleanedGeometry); 

                    // Parse the cleaned JSON into a JsonDocument
                    using var jsonDoc = JsonDocument.Parse(cleanedGeometry);
                    var root = jsonDoc.RootElement;

                    // Extract type
                    geometry.type = root.GetProperty("type").GetString() ?? string.Empty;

                    // Extract coordinates based on geometry type
                    if (string.IsNullOrEmpty(geometry.type))
                    {
                        _logger.LogWarning("Validation failed: GeoJSON missing type for asset ID {AssetId}", id);
                        return BadRequest("GeoJSON thiếu type.");
                    }

                    var coordinatesElement = root.GetProperty("coordinates");
                    geometry.coordinates = geometry.type.ToLower() switch
                    {
                        "point" => coordinatesElement.Deserialize<double[]>(), // [x, y]
                        "linestring" => coordinatesElement.Deserialize<double[][]>(), // [[x1, y1], [x2, y2], ...]
                        "polygon" => coordinatesElement.Deserialize<double[][][]>(), // [[[x1, y1], [x2, y2], ...]]
                        _ => throw new System.Text.Json.JsonException($"Loại hình học không được hỗ trợ: {geometry.type}")
                    };

                    if (geometry.coordinates == null)
                    {
                        _logger.LogWarning("Validation failed: GeoJSON missing coordinates or invalid format for asset ID {AssetId}", id); 
                        return BadRequest("GeoJSON thiếu coordinates hoặc định dạng không hợp lệ.");
                    }

                    _logger.LogInformation("Parsed geometry for asset ID {AssetId}: type={Type}, coordinates={Coordinates}", id, geometry.type, System.Text.Json.JsonSerializer.Serialize(geometry.coordinates)); 
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Geometry parse error for asset ID {AssetId}: {Message}", id, ex.Message); 
                    return BadRequest($"Định dạng GeoJSON không hợp lệ: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected geometry error for asset ID {AssetId}: {Message}", id, ex.Message);
                    return BadRequest($"Lỗi xử lý GeoJSON: {ex.Message}");
                }

                // Lấy danh mục hiện tại để kiểm tra image cũ
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    _logger.LogWarning("Asset with ID {AssetId} not found for update", id); 
                    return NotFound("Asset category does not exist");
                }

                string imageUrl = existingAsset.image_url;
                string imageName = existingAsset.image_name;
                string imagePublicId = existingAsset.image_public_id; 

                
                if (request.image != null && request.image.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(existingAsset.image_url))
                    {
                        var deletionParams = new DeletionParams(imagePublicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete old image for asset with ID {AssetId}: {Error}", id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old image for asset with ID {AssetId}: PublicId {PublicId}", id, imagePublicId);
                        }
                    }

                    // Tải ảnh mới lên Cloudinary
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(request.image.FileName, request.image.OpenReadStream()),
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = true 
                    };
                    var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Failed to upload new image for asset with ID {AssetId}: {Error}", id, uploadResult.Error.Message); 
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    imageUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                    imageName = uploadResult.OriginalFilename;
                    imagePublicId = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded new image for asset with ID {AssetId}: PublicId {PublicId}", id, imagePublicId); 
                }

                // Tạo request để cập nhật
                var finalRequest = new AssetsRequest
                {
                    category_id = request.category_id,
                    asset_name = request.asset_name,
                    asset_code = request.asset_code,
                    address = request.address,
                    geometry = geometry,
                    construction_year = request.construction_year,
                    operation_year = request.operation_year,
                    land_area = request.land_area,
                    floor_area = request.floor_area,
                    original_value = request.original_value,
                    remaining_value = request.remaining_value,
                    asset_status = request.asset_status,
                    installation_unit = request.installation_unit,
                    management_unit = request.management_unit,
                    custom_attributes = customAttributes,
                    image_url = imageUrl,
                    image_name = imageName,
                    image_public_id = imagePublicId,
                };

                var updatedAsset = await _Service.UpdateAsset(id, finalRequest);
                if (updatedAsset == null)
                {
                    _logger.LogError("Failed to update asset with ID {AssetId}", id); 
                    return BadRequest("Failed to update asset category.");
                }
                _logger.LogInformation("Updated asset with ID {AssetId} successfully", id);
                return Ok(updatedAsset);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating asset with ID {AssetId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update asset with ID {AssetId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating asset with ID {AssetId}", id);
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAssets(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete asset with ID {AssetId}", id); 
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    _logger.LogWarning("Asset with ID {AssetId} not found for deletion", id); 
                    return NotFound("Asset does not exist");
                }

                var imagePublicId = existingAsset.image_public_id;

                if (imagePublicId != null)
                {
                    var deletionParams = new DeletionParams(imagePublicId);
                    var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        _logger.LogError("Failed to delete image for asset with ID {AssetId}: {Error}", id, deletionResult.Error?.Message); 
                    }
                    else
                    {
                        _logger.LogInformation("Deleted image for asset with ID {AssetId}: PublicId {PublicId}", id, imagePublicId); 
                    }
                }

                var result = await _Service.DeleteAsset(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete asset with ID {AssetId}", id); 
                    return BadRequest("Failed to delete asset.");
                }
                _logger.LogInformation("Deleted asset with ID {AssetId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete asset with ID {AssetId}: {Message}", id, ex.Message); 
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete asset with ID {AssetId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting asset with ID {AssetId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}