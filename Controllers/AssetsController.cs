using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
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
        private readonly  IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;

        public AssetsController(IAssetsService Service, IConfiguration configuration, Cloudinary cloudinary)
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAssets()
        {
            try
            {
                var assets = await _Service.GetAllAssets();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetsById(int id)
        {
            try
            {
                var asset = await _Service.GetAssetById(id);
                if (asset == null)
                {
                    return NotFound("Asset does not exist");
                }
                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssets([FromForm] AssetImageUploadRequest request)
        {
            Console.WriteLine("Data input: " + request.custom_attributes);
            Console.WriteLine("Geometry received: " + request.geometry);

            // Parse custom_attributes
            JObject customAttributes;
            try
            {
                customAttributes = JObject.Parse(request.custom_attributes);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Custom attributes parse error: {ex.Message}");
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            Console.WriteLine("Parsed custom attributes: " + customAttributes);

            // Parse geometry
            GeoJsonGeometry geometry = new GeoJsonGeometry();
            try
            {
                if (string.IsNullOrEmpty(request.geometry))
                {
                    return BadRequest("Hình học là bắt buộc.");
                }

                // Clean the geometry string to remove extra quotes or escape sequences
                string cleanedGeometry = request.geometry;
                if (cleanedGeometry.StartsWith("\"") && cleanedGeometry.EndsWith("\""))
                {
                    cleanedGeometry = cleanedGeometry[1..^1];
                    cleanedGeometry = cleanedGeometry.Replace("\\\"", "\"").Replace("\\u0022", "\"");
                }

                Console.WriteLine("Cleaned geometry: " + cleanedGeometry);

                // Parse the cleaned JSON into a JsonDocument
                using var jsonDoc = JsonDocument.Parse(cleanedGeometry);
                var root = jsonDoc.RootElement;

                // Extract type
                geometry.type = root.GetProperty("type").GetString() ?? string.Empty;

                // Extract coordinates based on geometry type
                if (string.IsNullOrEmpty(geometry.type))
                {
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
                    return BadRequest("GeoJSON thiếu coordinates hoặc định dạng không hợp lệ.");
                }

                Console.WriteLine($"Parsed geometry: type={geometry.type}, coordinates={System.Text.Json.JsonSerializer.Serialize(geometry.coordinates)}");
            }
            catch (System.Text.Json.JsonException ex)
            {
                Console.WriteLine($"Geometry parse error: {ex.Message}");
                return BadRequest($"Định dạng GeoJSON không hợp lệ: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected geometry error: {ex.Message}");
                return BadRequest($"Lỗi xử lý GeoJSON: {ex.Message}");
            }

            // Validate image
            if (request.image == null || request.image.Length == 0)
            {
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
                    return StatusCode((int)uploadImageResult.StatusCode, uploadImageResult.Error.Message);
                }
                var imageUrl = uploadImageResult.SecureUrl.ToString();

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
                };

                var category = await _Service.CreateAsset(finalRequest);
                if (category == null)
                {
                    return BadRequest("Failed to create asset category.");
                }
                return CreatedAtAction(nameof(GetAssetsById), new { id = category.category_id }, category);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateAssets(int id, [FromForm] AssetImageUploadRequest request)
        {
            // Parse attributes_schema
            JObject customAttributes;
            try
            {
                customAttributes = JObject.Parse(request.custom_attributes);
            }
            catch (JsonReaderException)
            {
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            if (request.geometry == null || request.geometry.Length == 0)
            {
                return BadRequest("Vị trí địa lý không thể rỗng");
            }

            // Parse geometry
            GeoJsonGeometry geometry = new GeoJsonGeometry();
            try
            {
                if (string.IsNullOrEmpty(request.geometry))
                {
                    return BadRequest("Hình học là bắt buộc.");
                }

                // Clean the geometry string to remove extra quotes or escape sequences
                string cleanedGeometry = request.geometry;
                if (cleanedGeometry.StartsWith("\"") && cleanedGeometry.EndsWith("\""))
                {
                    cleanedGeometry = cleanedGeometry[1..^1];
                    cleanedGeometry = cleanedGeometry.Replace("\\\"", "\"").Replace("\\u0022", "\"");
                }

                Console.WriteLine("Cleaned geometry: " + cleanedGeometry);

                // Parse the cleaned JSON into a JsonDocument
                using var jsonDoc = JsonDocument.Parse(cleanedGeometry);
                var root = jsonDoc.RootElement;

                // Extract type
                geometry.type = root.GetProperty("type").GetString() ?? string.Empty;

                // Extract coordinates based on geometry type
                if (string.IsNullOrEmpty(geometry.type))
                {
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
                    return BadRequest("GeoJSON thiếu coordinates hoặc định dạng không hợp lệ.");
                }

                Console.WriteLine($"Parsed geometry: type={geometry.type}, coordinates={System.Text.Json.JsonSerializer.Serialize(geometry.coordinates)}");
            }
            catch (System.Text.Json.JsonException ex)
            {
                Console.WriteLine($"Geometry parse error: {ex.Message}");
                return BadRequest($"Định dạng GeoJSON không hợp lệ: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected geometry error: {ex.Message}");
                return BadRequest($"Lỗi xử lý GeoJSON: {ex.Message}");
            }

            Console.WriteLine(customAttributes);
            try
            {
                // Lấy danh mục hiện tại để kiểm tra image cũ
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    return NotFound("Asset category does not exist");
                }

                string imageUrl = existingAsset.image_url; // Giữ URL cũ nếu không có ảnh mới

                // Nếu có file image mới, xử lý xóa ảnh cũ và tải ảnh mới
                if (request.image != null && request.image.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(existingAsset.image_url))
                    {
                        var publicId = Path.GetFileNameWithoutExtension(new Uri(existingAsset.image_url).AbsolutePath);
                        var deletionParams = new DeletionParams(publicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            Console.WriteLine($"Failed to delete old image: {deletionResult.Error?.Message}");
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
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    imageUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                }

                // Nếu không có file marker mới, giữ nguyên marker_url cũ

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
                };

                var updatedCategory = await _Service.UpdateAsset(id, finalRequest);
                if (updatedCategory == null)
                {
                    return BadRequest("Failed to update asset category.");
                }
                return Ok(updatedCategory);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAssets(int id)
        {
            try
            {
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    return NotFound("Asset does not exist");
                }

                var result = await _Service.DeleteAsset(id);
                if (!result)
                {
                    return BadRequest("Failed to delete asset.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    return Conflict(ex.Message);
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}