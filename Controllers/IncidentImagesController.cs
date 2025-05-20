using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.ImageUpload;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentImagesController : ControllerBase
    {
        private readonly IIncidentImageService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        private readonly ILogger<IncidentImagesController> _logger; 

        public IncidentImagesController(IIncidentImageService Service, IConfiguration configuration, Cloudinary cloudinary, ILogger<IncidentImagesController> logger) 
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllIncidentImages()
        {
            try
            {
                _logger.LogInformation("Received request to get all incident images");
                var incidentImages = await _Service.GetAllIncidentImages();
                _logger.LogInformation("Returned {Count} incident images", incidentImages.Count()); 
                return Ok(incidentImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all incident images"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentImagesById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get incident image with ID {IncidentImageId}", id);
                var incidentImages = await _Service.GetIncidentImageById(id);
                if (incidentImages == null)
                {
                    _logger.LogWarning("Incident image with ID {IncidentImageId} not found", id); 
                    return NotFound("Incident Images does't exist");
                }
                _logger.LogInformation("Returned incident image with ID {IncidentImageId}", id);
                return Ok(incidentImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident image with ID {IncidentImageId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("incidentid/{id}")]
        public async Task<ActionResult> GetAllIncidentImagesByIncidentId(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get incident images for incident ID {IncidentId}", id); 
                var incidentImages = await _Service.GetAllIncidentImagesByIncidentId(id);
                _logger.LogInformation("Returned {Count} incident images for incident ID {IncidentId}", incidentImages.Count(), id); 
                return Ok(incidentImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident images for incident ID {IncidentId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidentImages([FromForm] IncidentImageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to create incident image for incident ID {IncidentId}", request.incident_id); 
                string imageUrl = null;
                string imageName = null;
                string imagePublicId = null;
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
                        _logger.LogError("Failed to upload image for incident image creation: {Error}", uploadResult.Error.Message);
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    imageUrl = uploadResult.SecureUrl.ToString();
                    imageName = uploadResult.OriginalFilename;
                    imagePublicId = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded image for incident image creation: PublicId {PublicId}", imagePublicId); 
                }

                var finalRequest = new IncidentImageRequest
                {
                    incident_id = request.incident_id,
                    image_url = imageUrl,
                    image_name = imageName,
                    image_public_id = imagePublicId,
                };

                var incidentImage = await _Service.CreateIncidentImage(finalRequest);
                if (incidentImage == null)
                {
                    _logger.LogError("Failed to create incident image for incident ID {IncidentId}", request.incident_id); 
                    return BadRequest("Failed to create incidentImage.");
                }
                _logger.LogInformation("Created incident image with ID {IncidentImageId} successfully", incidentImage.incident_image_id);
                return CreatedAtAction(nameof(GetIncidentImagesById), new { id = incidentImage.incident_image_id }, incidentImage);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating incident image: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create incident image: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating incident image for incident ID {IncidentId}", request.incident_id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateIncidentImages([FromForm] IncidentImageUploadRequest request, int id)
        {
            try
            {
                _logger.LogInformation("Received request to update incident image with ID {IncidentImageId}", id);
                var existingIncidentImage = await _Service.GetIncidentImageById(id); 
                if (existingIncidentImage == null)
                {
                    _logger.LogWarning("Incident image with ID {IncidentImageId} not found for update", id); 
                    return NotFound("Incident Image does not exist");
                }
                string imageUrl = existingIncidentImage.image_url;
                string imageName = existingIncidentImage.image_name;
                string imagePublicId = existingIncidentImage.image_public_id;
                if (request.image != null && request.image.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingIncidentImage.image_url))
                    {
                        var deletionParams = new DeletionParams(imagePublicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete old image for incident image with ID {IncidentImageId}: {Error}", id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old image for incident image with ID {IncidentImageId}: PublicId {PublicId}", id, imagePublicId); 
                        }
                    }

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
                        _logger.LogError("Failed to upload new image for incident image with ID {IncidentImageId}: {Error}", id, uploadResult.Error.Message); 
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    imageUrl = uploadResult.SecureUrl.ToString();
                    imageName = uploadResult.OriginalFilename; 
                    imagePublicId = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded new image for incident image with ID {IncidentImageId}: PublicId {PublicId}", id, imagePublicId); 
                }

                var finalRequest = new IncidentImageRequest
                {
                    incident_id = request.incident_id,
                    image_url = imageUrl,
                    image_name = imageName,
                    image_public_id = imagePublicId
                };

                var incidentImage = await _Service.UpdateIncidentImage(id, finalRequest);
                if (incidentImage == null)
                {
                    _logger.LogError("Failed to update incident image with ID {IncidentImageId}", id); 
                    return BadRequest("Failed to update incident image.");
                }
                _logger.LogInformation("Updated incident image with ID {IncidentImageId} successfully", id); 
                return Ok(incidentImage);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating incident image with ID {IncidentImageId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update incident image with ID {IncidentImageId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating incident image with ID {IncidentImageId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIncidentImages(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete incident image with ID {IncidentImageId}", id); 
                var incidentImage = await _Service.GetIncidentImageById(id);
                if (incidentImage == null)
                {
                    _logger.LogWarning("Incident image with ID {IncidentImageId} not found for deletion", id); 
                    return NotFound();
                }

                var imageUrl = incidentImage.image_url;
                var imageName = incidentImage.image_name;
                var imagePublicId = incidentImage.image_public_id;

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var deletionParams = new DeletionParams(imagePublicId);
                    var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        _logger.LogError("Failed to delete image for incident image with ID {IncidentImageId}: {Error}", id, deletionResult.Error?.Message); 
                    }
                    else
                    {
                        _logger.LogInformation("Deleted image for incident image with ID {IncidentImageId}: PublicId {PublicId}", id, imagePublicId);
                    }
                }

                var result = await _Service.DeleteIncidentImage(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete incident image with ID {IncidentImageId}", id); 
                    return BadRequest();
                }
                _logger.LogInformation("Deleted incident image with ID {IncidentImageId} successfully", id); 
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting incident image with ID {IncidentImageId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("incidentid/{id}")]
        public async Task<ActionResult> DeleteIncidentImageByIncidentId(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete incident images for incident ID {IncidentId}", id); 
                var incidentImages = await _Service.GetAllIncidentImagesByIncidentId(id);
                if (incidentImages == null || !incidentImages.Any())
                {
                    _logger.LogWarning("No incident images found for incident ID {IncidentId}", id); 
                    return NotFound();
                }

                foreach (var image in incidentImages)
                {
                    if (!string.IsNullOrEmpty(image.image_url) && !string.IsNullOrEmpty(image.image_public_id))
                    {
                        var deletionParams = new DeletionParams(image.image_public_id);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete image for incident image with ID {IncidentImageId}: {Error}", image.incident_image_id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted image for incident image with ID {IncidentImageId}: PublicId {PublicId}", image.incident_image_id, image.image_public_id); 
                        }
                    }
                }

                var result = await _Service.DeleteIncidentImageByIncidentId(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete incident images for incident ID {IncidentId}", id); 
                    return BadRequest();
                }
                _logger.LogInformation("Deleted incident images for incident ID {IncidentId} successfully", id); 
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting incident images for incident ID {IncidentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}