using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Model.ImageUpload;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceDocumentController : ControllerBase
    {
        private readonly IMaintenanceDocumentService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;

        public MaintenanceDocumentController(IMaintenanceDocumentService Service, IConfiguration configuration, Cloudinary cloudinary)
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllMaintenanceDocuments()
        {
            try
            {
                var costs = await _Service.GetAllMaintenanceDocuments();
                return Ok(costs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMaintenanceDocumentById(int id)
        {
            try
            {
                var cost = await _Service.GetMaintenanceDocumentById(id);
                if (cost == null)
                {
                    return NotFound("Maintenance Document does not exist");
                }
                return Ok(cost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateMaintenanceDocument([FromForm] MaintenanceDocumentFileUpload request)
        {
            try
            {
                string fileUrl = null;
                if (request.file != null)
                {
                    // Configure Cloudinary for generic file upload
                    var extension = Path.GetExtension(request.file.FileName).ToLowerInvariant();
                    if (new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(request.file.FileName, request.file.OpenReadStream()),
                            UseFilename = true,
                            UniqueFilename = true,
                            Overwrite = true
                        };
                        var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                        if (uploadResult.Error != null)
                        {
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                    }
                    else
                    {
                        var uploadParams = new RawUploadParams
                        {
                            File = new FileDescription(request.file.FileName, request.file.OpenReadStream()),
                            UseFilename = true,
                            UniqueFilename = true,
                            Overwrite = true
                        };
                        var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                        if (uploadResult.Error != null)
                        {
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                    }
                }

                var finalRequest = new MaintenanceDocumentRequest
                {
                    maintenance_id = request.maintenance_id,
                    file_url = fileUrl
                };

                var maintenanceDocument = await _Service.CreateMaintenanceDocument(finalRequest);
                if (maintenanceDocument == null)
                {
                    return BadRequest("Failed to create maintenance document.");
                }
                return CreatedAtAction(nameof(GetMaintenanceDocumentById), new { id = maintenanceDocument.document_id }, maintenanceDocument);
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
        public async Task<ActionResult> UpdateMaintenanceDocument(int id, [FromForm] MaintenanceDocumentFileUpload request)
        {
            try
            {
                // Validate the maintenance document exists
                var existingDocument = await _Service.GetMaintenanceDocumentById(id);
                if (existingDocument == null)
                {
                    return NotFound("Maintenance document does not exist");
                }

                string fileUrl = existingDocument.file_url; // Preserve existing file URL if no new file is uploaded
                if (request.file != null)
                {
                    // Determine file type and configure Cloudinary upload
                    var extension = Path.GetExtension(request.file.FileName).ToLowerInvariant();
                    if (new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
                    {
                        // Handle image uploads
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(request.file.FileName, request.file.OpenReadStream()),
                            UseFilename = true,
                            UniqueFilename = true,
                            Overwrite = true
                        };
                        var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                        if (uploadResult.Error != null)
                        {
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                    }
                    else
                    {
                        // Handle non-image files (PDFs, Word docs, etc.)
                        var uploadParams = new RawUploadParams
                        {
                            File = new FileDescription(request.file.FileName, request.file.OpenReadStream()),
                            UseFilename = true,
                            UniqueFilename = true,
                            Overwrite = true
                        };
                        var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                        if (uploadResult.Error != null)
                        {
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                    }
                }

                // Create the request object for updating
                var updateRequest = new MaintenanceDocumentRequest
                {
                    maintenance_id = request.maintenance_id,
                    file_url = fileUrl
                };

                // Update the maintenance document
                var updatedDocument = await _Service.UpdateMaintenanceDocument(id, updateRequest);
                if (updatedDocument == null)
                {
                    return BadRequest("Failed to update maintenance document.");
                }

                return Ok(updatedDocument); // Or NoContent() if you prefer not to return data
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMaintenanceDocument(int id)
        {
            try
            {
                var existingCost = await _Service.GetMaintenanceDocumentById(id);
                if (existingCost == null)
                {
                    return NotFound("Maintenance document does not exist");
                }

                var result = await _Service.DeleteMaintenanceDocument(id);
                if (!result)
                {
                    return BadRequest("Failed to delete maintenance document.");
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
