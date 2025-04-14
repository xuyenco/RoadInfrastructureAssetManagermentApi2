using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Jwt;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class IncidentImagesController : ControllerBase
    {
        private readonly IIncidentImageService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        public IncidentImagesController(IIncidentImageService Service, IConfiguration configuration, Cloudinary cloudinary)
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllIncidentImages()
        {
            return Ok(await _Service.GetAllIncidentImages());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentImagesById(int id)
        {
            var incidentImages = await _Service.GetIncidentImageById(id);
            if (incidentImages == null)
            {
                return NotFound("Incident Images does't exist");
            }
            return Ok(incidentImages);
        }

        [HttpGet("incidentid/{id}")]
        public async Task<ActionResult> GetAllIncidentImagesByIncidentId(int id)
        {
            return Ok(await _Service.GetAllIncidentImagesByIncidentId(id));
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidentImages([FromForm] IncidentImageUploadRequest request)
        {
            try
            {
                string imageUrl = null;
                if (request.image != null)
                {
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
                    imageUrl = uploadResult.SecureUrl.ToString();
                }

                var finalRequest = new IncidentImageRequest
                {
                    incident_id = request.incident_id,
                    image_url = imageUrl
                };

                var incidentImage = await _Service.CreateIncidentImage(finalRequest);
                if (incidentImage == null)
                {
                    return BadRequest("Failed to create incidentImage.");
                }
                return CreatedAtAction(nameof(GetIncidentImagesById), new { id = incidentImage.incident_images_id }, incidentImage);
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
        public async Task<ActionResult> UpdateIncidentImages([FromForm] IncidentImageUploadRequest request, int id)
        {
            try
            {
                var exexistingIncidentImage = await _Service.GetIncidentImageById(id);
                if (exexistingIncidentImage == null)
                {
                    return NotFound("Incident Image does not exist");
                }
                string ImageUrl = exexistingIncidentImage.image_url;
                if (request.image != null && request.image.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(exexistingIncidentImage.image_url))
                    {
                        var publicId = Path.GetFileNameWithoutExtension(new Uri(exexistingIncidentImage.image_url).AbsolutePath);
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
                    ImageUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                }
                var finalRequest = new IncidentImageRequest
                {
                    incident_id = request.incident_id,
                    image_url = ImageUrl
                };

                var IncidentImage = await _Service.UpdateIncidentImage(id, finalRequest);
                if (IncidentImage == null)
                {
                    return BadRequest("Failed to update incident image.");
                }
                return Ok(IncidentImage);
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
        public async Task<ActionResult> DeleteIncidentImages(int id)
        {
            var incidentImage = await _Service.GetIncidentImageById(id);
            if (incidentImage == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteIncidentImage(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();
        }
    }
}
