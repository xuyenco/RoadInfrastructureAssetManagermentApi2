using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.ImageUpload;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AssetCagetoriesController : ControllerBase
    {
        private readonly IAssetCategoriesService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        private readonly ILogger<AssetCagetoriesController> _logger;

        public AssetCagetoriesController(IAssetCategoriesService Service, Cloudinary cloudinary, IConfiguration configuration, ILogger<AssetCagetoriesController> logger)
        {
            _Service = Service;
            _Cloudinary = cloudinary;
            _Configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult> GetAllAssetCagetories()
        {
            try
            {
                _logger.LogInformation("Received request to get all asset categories");
                var cagetories = await _Service.GetAllAssetCategories();
                _logger.LogInformation("Returned {Count} asset categories", cagetories.Count());
                return Ok(cagetories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all asset categories");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        //[Authorize]
        public async Task<ActionResult> GetAssetCagetoriesById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get asset category with ID {CategoryId}", id);
                var category = await _Service.GetAssetCategoriesById(id);
                if (category == null)
                {
                    _logger.LogWarning("Asset category with ID {CategoryId} not found", id);
                    return NotFound("Asset category does not exist");
                }
                _logger.LogInformation("Returned asset category with ID {CategoryId}", id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get asset category with ID {CategoryId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        //[Authorize(Roles = "admin")]
        public async Task<ActionResult> CreateAssetCagetories([FromForm] AssetCagetoryImageUploadRequest request)
        {
            _logger.LogInformation("Received request to create asset category with data input: {AttributeSchema}", request.attribute_schema);

            JObject attributesSchema;
            try
            {
                attributesSchema = JObject.Parse(request.attribute_schema);
                _logger.LogInformation("Parsed attribute schema: {AttributeSchema}", attributesSchema);
            }
            catch (JsonReaderException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON format for attribute_schema: {Message}", ex.Message);
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            string iconUrl = null;
            string iconPublicId = null;
            string sampleImageUrl = null;
            string sampleImageName = null;
            string sampleImagePublicId = null;

            try
            {
                // Upload Icon if provided
                if (request.icon != null && request.icon.Length > 0)
                {
                    var uploadIconParams = new ImageUploadParams
                    {
                        File = new FileDescription(request.icon.FileName, request.icon.OpenReadStream()),
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = true
                    };
                    var uploadIconResult = await _Cloudinary.UploadAsync(uploadIconParams);
                    if (uploadIconResult.Error != null)
                    {
                        _logger.LogError("Failed to upload icon for asset category creation: {Error}", uploadIconResult.Error.Message);
                        return StatusCode((int)uploadIconResult.StatusCode, uploadIconResult.Error.Message);
                    }
                    iconUrl = uploadIconResult.SecureUrl.ToString();
                    iconPublicId = uploadIconResult.PublicId;
                    _logger.LogInformation("Uploaded icon for asset category creation: PublicId {PublicId}", iconPublicId);
                }
                else
                {
                    _logger.LogInformation("No icon file provided for asset category creation");
                }

                // Upload Sample Image if provided
                if (request.sample_image != null && request.sample_image.Length > 0)
                {
                    var uploadSampleImageParams = new ImageUploadParams
                    {
                        File = new FileDescription(request.sample_image.FileName, request.sample_image.OpenReadStream()),
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = true
                    };
                    var uploadSampleImageResult = await _Cloudinary.UploadAsync(uploadSampleImageParams);
                    if (uploadSampleImageResult.Error != null)
                    {
                        _logger.LogError("Failed to upload sample image for asset category creation: {Error}", uploadSampleImageResult.Error.Message);
                        return StatusCode((int)uploadSampleImageResult.StatusCode, uploadSampleImageResult.Error.Message);
                    }
                    sampleImageUrl = uploadSampleImageResult.SecureUrl.ToString();
                    sampleImageName = uploadSampleImageResult.OriginalFilename;
                    sampleImagePublicId = uploadSampleImageResult.PublicId;
                    _logger.LogInformation("Uploaded sample image for asset category creation: PublicId {PublicId}", sampleImagePublicId);
                }
                else
                {
                    _logger.LogInformation("No sample image file provided for asset category creation");
                }

                var finalRequest = new AssetCategoriesRequest
                {
                    category_name = request.category_name,
                    attribute_schema = attributesSchema,
                    geometry_type = request.geometry_type,
                    sample_image = sampleImageUrl,
                    sample_image_name = sampleImageName,
                    sample_image_public_id = sampleImagePublicId,
                    icon_url = iconUrl,
                    icon_public_id = iconPublicId
                };

                var category = await _Service.CreateAssetCategories(finalRequest);
                if (category == null)
                {
                    _logger.LogError("Failed to create asset category");
                    return BadRequest("Failed to create asset category.");
                }
                _logger.LogInformation("Created asset category with ID {CategoryId} successfully", category.category_id);
                return CreatedAtAction(nameof(GetAssetCagetoriesById), new { id = category.category_id }, category);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating asset category: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create asset category: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating asset category");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<ActionResult> UpdateAssetCagetories(int id, [FromForm] AssetCagetoryImageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to update asset category with ID {CategoryId}, attribute schema: {AttributeSchema}", id, request.attribute_schema);

                // Parse attributes_schema
                JObject attributesSchema;
                try
                {
                    attributesSchema = JObject.Parse(request.attribute_schema);
                    _logger.LogInformation("Parsed attribute schema: {AttributeSchema}", attributesSchema);
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON format for attribute_schema: {Message}", ex.Message);
                    return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
                }

                // Lấy danh mục hiện tại để kiểm tra marker_url cũ
                var existingCategory = await _Service.GetAssetCategoriesById(id);
                if (existingCategory == null)
                {
                    _logger.LogWarning("Asset category with ID {CategoryId} not found for update", id);
                    return NotFound("Asset category does not exist");
                }

                string sampleImageUrl = existingCategory.sample_image; // Giữ URL cũ nếu không có ảnh mới
                string sampleImageName = existingCategory.sample_image_name;
                string sampleImagePublicId = existingCategory.sample_image_public_id;
                string iconUrl = existingCategory.icon_url;
                string iconPublicId = existingCategory.icon_public_id; // Thống nhất camelCase

                // Nếu có file icon mới, xử lý xóa ảnh cũ và tải ảnh mới
                if (request.icon != null && request.icon.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(existingCategory.icon_url))
                    {
                        var deletionParams = new DeletionParams(iconPublicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete old icon for asset category with ID {CategoryId}: {Error}", id, deletionResult.Error?.Message);
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old icon for asset category with ID {CategoryId}: PublicId {PublicId}", id, iconPublicId);
                        }
                    }

                    // Tải ảnh mới lên Cloudinary
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(request.icon.FileName, request.icon.OpenReadStream()),
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = true,
                    };
                    var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Failed to upload new icon for asset category with ID {CategoryId}: {Error}", id, uploadResult.Error.Message);
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    iconUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                    iconPublicId = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded new icon for asset category with ID {CategoryId}: PublicId {PublicId}", id, iconPublicId);
                }

                // Nếu có file sample_image mới, xử lý xóa ảnh cũ và tải ảnh mới
                if (request.sample_image != null && request.sample_image.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(existingCategory.sample_image))
                    {
                        var deletionParams = new DeletionParams(sampleImagePublicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete old sample image for asset category with ID {CategoryId}: {Error}", id, deletionResult.Error?.Message);
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old sample image for asset category with ID {CategoryId}: PublicId {PublicId}", id, sampleImagePublicId);
                        }
                    }

                    // Tải ảnh mới lên Cloudinary
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(request.sample_image.FileName, request.sample_image.OpenReadStream()),
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = true
                    };
                    var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Failed to upload new sample image for asset category with ID {CategoryId}: {Error}", id, uploadResult.Error.Message);
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    sampleImageUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                    sampleImagePublicId = uploadResult.PublicId;
                    sampleImageName = uploadResult.OriginalFilename;
                    _logger.LogInformation("Uploaded new sample image for asset category with ID {CategoryId}: PublicId {PublicId}", id, sampleImagePublicId);
                }

                // Tạo request để cập nhật
                var finalRequest = new AssetCategoriesRequest
                {
                    category_name = request.category_name,
                    attribute_schema = attributesSchema,
                    geometry_type = request.geometry_type,
                    sample_image = sampleImageUrl,
                    sample_image_public_id = sampleImagePublicId,
                    sample_image_name = sampleImageName,
                    icon_url = iconUrl,
                    icon_public_id = iconPublicId,
                };

                var updatedCategory = await _Service.UpdateAssetCategories(id, finalRequest);
                if (updatedCategory == null)
                {
                    _logger.LogError("Failed to update asset category with ID {CategoryId}", id);
                    return BadRequest("Failed to update asset category.");
                }
                _logger.LogInformation("Updated asset category with ID {CategoryId} successfully", id);
                return Ok(updatedCategory);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating asset category with ID {CategoryId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update asset category with ID {CategoryId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating asset category with ID {CategoryId}", id);
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteAssetCagetories(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete asset category with ID {CategoryId}", id);
                var existingCategory = await _Service.GetAssetCategoriesById(id);
                if (existingCategory == null)
                {
                    _logger.LogWarning("Asset category with ID {CategoryId} not found for deletion", id);
                    return NotFound("Asset category does not exist");
                }

                string sampleImagePublicId = existingCategory.sample_image_public_id;
                string iconPublicId = existingCategory.icon_public_id;

                if (sampleImagePublicId != null)
                {
                    var deletionParams = new DeletionParams(sampleImagePublicId);
                    var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        _logger.LogError("Failed to delete sample image for asset category with ID {CategoryId}: {Error}", id, deletionResult.Error?.Message);
                    }
                    else
                    {
                        _logger.LogInformation("Deleted sample image for asset category with ID {CategoryId}: PublicId {PublicId}", id, sampleImagePublicId);
                    }
                }

                if (iconPublicId != null)
                {
                    var deletionParams = new DeletionParams(iconPublicId);
                    var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        _logger.LogError("Failed to delete icon for asset category with ID {CategoryId}: {Error}", id, deletionResult.Error?.Message);
                    }
                    else
                    {
                        _logger.LogInformation("Deleted icon for asset category with ID {CategoryId}: PublicId {PublicId}", id, iconPublicId);
                    }
                }

                var result = await _Service.DeleteAssetCategories(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete asset category with ID {CategoryId}", id);
                    return BadRequest("Failed to delete asset category.");
                }
                _logger.LogInformation("Deleted asset category with ID {CategoryId} successfully", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete asset category with ID {CategoryId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete asset category with ID {CategoryId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting asset category with ID {CategoryId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}