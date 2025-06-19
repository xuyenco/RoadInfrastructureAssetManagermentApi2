using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.ImageUpload;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceDocumentController : ControllerBase
    {
        private readonly IMaintenanceDocumentService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        private readonly ILogger<MaintenanceDocumentController> _logger; 

        public MaintenanceDocumentController(IMaintenanceDocumentService Service, IConfiguration configuration, Cloudinary cloudinary, ILogger<MaintenanceDocumentController> logger) 
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllMaintenanceDocuments()
        {
            try
            {
                _logger.LogInformation("Received request to get all maintenance documents"); 
                var costs = await _Service.GetAllMaintenanceDocuments();
                _logger.LogInformation("Returned {Count} maintenance documents", costs.Count()); 
                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all maintenance documents");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetMaintenanceDocumentById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get maintenance document with ID {DocumentId}", id); 
                var cost = await _Service.GetMaintenanceDocumentById(id);
                if (cost == null)
                {
                    _logger.LogWarning("Maintenance document with ID {DocumentId} not found", id); 
                    return NotFound("Maintenance Document does not exist");
                }
                _logger.LogInformation("Returned maintenance document with ID {DocumentId}", id); 
                return Ok(cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get maintenance document with ID {DocumentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("MaintenanceId/{id}")]
        [Authorize]
        public async Task<ActionResult> GetMaintenanceDocumentByMaintenanceId(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get maintenance documents for maintenance ID {MaintenanceId}", id); 
                var costs = await _Service.GetMaintenanceDocumentByMaintenanceId(id);
                _logger.LogInformation("Returned {Count} maintenance documents for maintenance ID {MaintenanceId}", costs.Count(), id); 
                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get maintenance documents for maintenance ID {MaintenanceId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> CreateMaintenanceDocument([FromForm] MaintenanceDocumentFileUpload request)
        {
            try
            {
                _logger.LogInformation("Received request to create maintenance document for maintenance ID {MaintenanceId}", request.maintenance_id); 
                string fileUrl = null;
                string filePublicId = null;
                string fileName = null;

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
                            _logger.LogError("Failed to upload image for maintenance document creation: {Error}", uploadResult.Error.Message); 
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                        filePublicId = uploadResult.PublicId;
                        fileName = uploadResult.OriginalFilename;
                        _logger.LogInformation("Uploaded image for maintenance document creation: PublicId {PublicId}", filePublicId);
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
                            _logger.LogError("Failed to upload file for maintenance document creation: {Error}", uploadResult.Error.Message); 
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                        filePublicId = uploadResult.PublicId;
                        fileName = uploadResult.OriginalFilename;
                        _logger.LogInformation("Uploaded file for maintenance document creation: PublicId {PublicId}", filePublicId); 
                    }
                }

                var finalRequest = new MaintenanceDocumentRequest
                {
                    maintenance_id = request.maintenance_id,
                    file_url = fileUrl,
                    file_public_id = filePublicId,
                    file_name = fileName
                };

                var maintenanceDocument = await _Service.CreateMaintenanceDocument(finalRequest);
                if (maintenanceDocument == null)
                {
                    _logger.LogError("Failed to create maintenance document for maintenance ID {MaintenanceId}", request.maintenance_id); 
                    return BadRequest("Failed to create maintenance document.");
                }
                _logger.LogInformation("Created maintenance document with ID {DocumentId} successfully", maintenanceDocument.document_id); 
                return CreatedAtAction(nameof(GetMaintenanceDocumentById), new { id = maintenanceDocument.document_id }, maintenanceDocument);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating maintenance document: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create maintenance document: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating maintenance document for maintenance ID {MaintenanceId}", request.maintenance_id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> UpdateMaintenanceDocument(int id, [FromForm] MaintenanceDocumentFileUpload request)
        {
            try
            {
                _logger.LogInformation("Received request to update maintenance document with ID {DocumentId}", id); 
                // Validate the maintenance document exists
                var existingDocument = await _Service.GetMaintenanceDocumentById(id);
                if (existingDocument == null)
                {
                    _logger.LogWarning("Maintenance document with ID {DocumentId} not found for update", id); 
                    return NotFound("Maintenance document does not exist");
                }

                string fileUrl = existingDocument.file_url; // Preserve existing file URL if no new file is uploaded
                string filePublicID = existingDocument.file_public_id;
                string fileName = existingDocument.file_name;

                if (request.file != null && request.file.Length > 0)
                {
                    // Delete the old file from Cloudinary if it exists
                    if (!string.IsNullOrEmpty(existingDocument.file_url))
                    {
                        var deletionParams = new DeletionParams(filePublicID);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete old file for maintenance document with ID {DocumentId}: {Error}", id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old file for maintenance document with ID {DocumentId}: PublicId {PublicId}", id, filePublicID); 
                        }
                    }

                    // Upload the new file to Cloudinary
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
                            _logger.LogError("Failed to upload new image for maintenance document with ID {DocumentId}: {Error}", id, uploadResult.Error.Message); 
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                        filePublicID = uploadResult.PublicId;
                        fileName = uploadResult.OriginalFilename;
                        _logger.LogInformation("Uploaded new image for maintenance document with ID {DocumentId}: PublicId {PublicId}", id, filePublicID); 
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
                            _logger.LogError("Failed to upload new file for maintenance document with ID {DocumentId}: {Error}", id, uploadResult.Error.Message); 
                            return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                        }
                        fileUrl = uploadResult.SecureUrl.ToString();
                        filePublicID = uploadResult.PublicId;
                        fileName = uploadResult.OriginalFilename;
                        _logger.LogInformation("Uploaded new file for maintenance document with ID {DocumentId}: PublicId {PublicId}", id, filePublicID);
                    }
                }

                // Create the request object for updating
                var updateRequest = new MaintenanceDocumentRequest
                {
                    maintenance_id = request.maintenance_id,
                    file_url = fileUrl,
                    file_public_id = filePublicID,
                    file_name = fileName
                };

                // Update the maintenance document
                var updatedDocument = await _Service.UpdateMaintenanceDocument(id, updateRequest);
                if (updatedDocument == null)
                {
                    _logger.LogError("Failed to update maintenance document with ID {DocumentId}", id); 
                    return BadRequest("Failed to update maintenance document.");
                }
                _logger.LogInformation("Updated maintenance document with ID {DocumentId} successfully", id); 
                return Ok(updatedDocument);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating maintenance document with ID {DocumentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update maintenance document with ID {DocumentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating maintenance document with ID {DocumentId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> DeleteMaintenanceDocument(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete maintenance document with ID {DocumentId}", id); 
                var existingDocument = await _Service.GetMaintenanceDocumentById(id);
                if (existingDocument == null)
                {
                    _logger.LogWarning("Maintenance document with ID {DocumentId} not found for deletion", id); 
                    return NotFound("Maintenance document does not exist");
                }
                var filePublicId = existingDocument.file_public_id;

                // Delete the file from Cloudinary if it exists
                if (!string.IsNullOrEmpty(existingDocument.file_url))
                {
                    var deletionParams = new DeletionParams(filePublicId);
                    var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        _logger.LogError("Failed to delete file for maintenance document with ID {DocumentId}: {Error}", id, deletionResult.Error?.Message); 
                    }
                    else
                    {
                        _logger.LogInformation("Deleted file for maintenance document with ID {DocumentId}: PublicId {PublicId}", id, filePublicId); 
                    }
                }

                // Delete the maintenance document from the database
                var result = await _Service.DeleteMaintenanceDocument(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete maintenance document with ID {DocumentId}", id); 
                    return BadRequest("Failed to delete maintenance document.");
                }
                _logger.LogInformation("Deleted maintenance document with ID {DocumentId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete maintenance document with ID {DocumentId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete maintenance document with ID {DocumentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting maintenance document with ID {DocumentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("MaintenanceId/{id}")]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> DeleteMaintenanceDocumentByMaintenanceId(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete maintenance documents for maintenance ID {MaintenanceId}", id);
                var existingDocument = await _Service.GetMaintenanceDocumentByMaintenanceId(id);
                if (existingDocument == null || !existingDocument.Any())
                {
                    _logger.LogWarning("No maintenance documents found for maintenance ID {MaintenanceId}", id); 
                    return NotFound("Maintenance document does not exist");
                }

                foreach (var currentDocument in existingDocument)
                {
                    // Delete the file from Cloudinary if it exists
                    if (!string.IsNullOrEmpty(currentDocument.file_url))
                    {
                        var publicId = currentDocument.file_public_id; 
                        var deletionParams = new DeletionParams(publicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            _logger.LogError("Failed to delete file for maintenance document with ID {DocumentId}: {Error}", currentDocument.document_id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted file for maintenance document with ID {DocumentId}: PublicId {PublicId}", currentDocument.document_id, publicId); 
                        }
                    }
                }

                // Delete the maintenance document from the database
                var result = await _Service.DeleteMaintenanceDocumentByMaintenanceId(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete maintenance documents for maintenance ID {MaintenanceId}", id); 
                    return BadRequest("Failed to delete maintenance document.");
                }
                _logger.LogInformation("Deleted maintenance documents for maintenance ID {MaintenanceId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete maintenance documents for maintenance ID {MaintenanceId}: {Message}", id, ex.Message); 
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete maintenance documents for maintenance ID {MaintenanceId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting maintenance documents for maintenance ID {MaintenanceId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}