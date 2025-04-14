﻿using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.ImageUpload;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;


namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AssetCagetoriesController : ControllerBase
    {
        private readonly IAssetCagetoriesService _Service;
        private readonly Cloudinary _Cloudinary;

        public AssetCagetoriesController(IAssetCagetoriesService Service, Cloudinary cloudinary)
        {
            _Service = Service;
            _Cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAssetCagetories()
        {
            try
            {
                var cagetories = await _Service.GetAllAssetCagetories();
                return Ok(cagetories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetCagetoriesById(int id)
        {
            try
            {
                var category = await _Service.GetAssetCagetoriesByid(id);
                if (category == null)
                {
                    return NotFound("Asset category does not exist");
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssetCagetories([FromForm] AssetCagetoryImageModel request)
        {
            Console.WriteLine("Data input: " + request.attributes_schema);

            JObject attributesSchema;
            try
            {
                attributesSchema = JObject.Parse(request.attributes_schema);
            }
            catch (JsonReaderException)
            {
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            Console.WriteLine(attributesSchema);

            JArray lifecycleStages;
            try
            {
                lifecycleStages = JArray.Parse(request.lifecycle_stages);
            }
            catch (JsonReaderException)
            {
                return BadRequest("Định dạng JSON của lifecycle_stages không hợp lệ.");
            }
            Console.WriteLine(lifecycleStages);

            if (request.marker == null || request.marker.Length == 0)
            {
                return BadRequest("Image file Không hợp lệ");
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(request.marker.FileName, request.marker.OpenReadStream()),
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = true
                };
                var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                }
                // Lấy URL ảnh từ Cloudinary
                var imageUrl = uploadResult.SecureUrl.ToString();
                // Gắn URL ảnh vào request
                var finalRequest = new AssetCagetoriesRequest
                {
                    cagetory_name = request.cagetory_name,
                    attributes_schema = attributesSchema,
                    geometry_type = request.geometry_type,
                    lifecycle_stages = lifecycleStages,
                    marker_url = imageUrl,
                };

                var category = await _Service.CreateAssetCagetories(finalRequest);
                if (category == null)
                {
                    return BadRequest("Failed to create asset category.");
                }
                return CreatedAtAction(nameof(GetAssetCagetoriesById), new { id = category.cagetory_id }, category);
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
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateAssetCagetories(int id, [FromForm] AssetCagetoryImageModel request)
        {
            // Parse attributes_schema
            JObject attributesSchema;
            try
            {
                attributesSchema = JObject.Parse(request.attributes_schema);
            }
            catch (JsonReaderException)
            {
                return BadRequest("Định dạng JSON của attributes_schema không hợp lệ.");
            }

            Console.WriteLine(attributesSchema);

            // Parse lifecycle_stages
            JArray lifecycleStages;
            try
            {
                lifecycleStages = JArray.Parse(request.lifecycle_stages);
            }
            catch (JsonReaderException)
            {
                return BadRequest("Định dạng JSON của lifecycle_stages không hợp lệ.");
            }

            try
            {
                // Lấy danh mục hiện tại để kiểm tra marker_url cũ
                var existingCategory = await _Service.GetAssetCagetoriesByid(id);
                if (existingCategory == null)
                {
                    return NotFound("Asset category does not exist");
                }

                string imageUrl = existingCategory.marker_url; // Giữ URL cũ nếu không có ảnh mới

                // Nếu có file marker mới, xử lý xóa ảnh cũ và tải ảnh mới
                if (request.marker != null && request.marker.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(existingCategory.marker_url))
                    {
                        var publicId = Path.GetFileNameWithoutExtension(new Uri(existingCategory.marker_url).AbsolutePath);
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
                        File = new FileDescription(request.marker.FileName, request.marker.OpenReadStream()),
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
                var finalRequest = new AssetCagetoriesRequest
                {
                    cagetory_name = request.cagetory_name,
                    attributes_schema = attributesSchema,
                    geometry_type = request.geometry_type,
                    lifecycle_stages = lifecycleStages,
                    marker_url = imageUrl // Dùng URL mới hoặc giữ URL cũ
                };

                var updatedCategory = await _Service.UpdateAssetCagetories(id, finalRequest);
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
        public async Task<ActionResult> DeleteAssetCagetories(int id)
        {
            try
            {
                var existingCategory = await _Service.GetAssetCagetoriesByid(id);
                if (existingCategory == null)
                {
                    return NotFound("Asset category does not exist");
                }

                var result = await _Service.DeleteAssetCagetories(id);
                if (!result)
                {
                    return BadRequest("Failed to delete asset category.");
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