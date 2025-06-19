using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Jwt;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.ImageUpload;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        private readonly ILogger<UsersController> _logger; 

        public UsersController(IUsersService Service, IConfiguration configuration, Cloudinary cloudinary, ILogger<UsersController> logger) 
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("Received request to get all users"); 
                var users = await _Service.GetAllUsers();
                _logger.LogInformation("Returned {Count} users", users.Count()); 
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all users");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetUsersById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get user with ID {UserId}", id); 
                var users = await _Service.GetUserById(id);
                if (users == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", id); 
                    return NotFound("Users does't exist");
                }
                _logger.LogInformation("Returned user with ID {UserId}", id); 
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user with ID {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
        [HttpGet("paged")]
        [Authorize]
        public async Task<ActionResult> GetUsersPagination(int page = 1, int pageSize = 1, string searchTerm = "", int searchField = 0)
        {
            try
            {
                _logger.LogInformation("Received request to get users with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                    page, pageSize, searchTerm, searchField);

                var (users, totalCount) = await _Service.GetUsersPagination(page, pageSize, searchTerm, searchField);

                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found for Page: {Page}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                        page, searchTerm, searchField);
                    return Ok(new { users, totalCount });
                }

                _logger.LogInformation("Returned {UserCount} users for Page: {Page}, TotalCount: {TotalCount}",
                    users.Count(), page, totalCount);

                return Ok(new { users, totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                    page, pageSize, searchTerm, searchField);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> CreateUsers([FromForm] UserImageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to create user with username {Username}", request.username); 
                string imageUrl = null;
                string imageName = null;
                string imagePublicID = null;
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
                        _logger.LogError("Failed to upload image for user creation: {Error}", uploadResult.Error.Message);
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    imageUrl = uploadResult.SecureUrl.ToString();
                    imageName = uploadResult.OriginalFilename;
                    imagePublicID = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded image for user creation: PublicId {PublicId}", imagePublicID);
                }

                var finalRequest = new UsersRequest
                {
                    username = request.username,
                    password = request.password,
                    full_name = request.full_name,
                    email = request.email,
                    role = request.role,
                    department_company_unit = request.department_company_unit,
                    image_url = imageUrl,
                    image_name = imageName,
                    image_public_id = imagePublicID
                };

                var user = await _Service.CreateUser(finalRequest);
                if (user == null)
                {
                    _logger.LogError("Failed to create user with username {Username}", request.username); 
                    return BadRequest("Failed to create user.");
                }
                _logger.LogInformation("Created user with ID {UserId} successfully", user.user_id); 
                return CreatedAtAction(nameof(GetUsersById), new { id = user.user_id }, user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating user: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create user: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user with username {Username}", request.username); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateUsers([FromForm] UserImageUploadRequest request, int id)
        {
            try
            {
                _logger.LogInformation("Received request to update user with ID {UserId}", id); 
                var exexistingUser = await _Service.GetUserById(id);
                if (exexistingUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for update", id);
                    return NotFound("User does not exist");
                }
                string ImageUrl = exexistingUser.image_url;
                string ImageName = exexistingUser.image_name;
                string ImagePublicId = exexistingUser.image_public_id;
                if (request.image != null && request.image.Length > 0)
                {
                    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                    if (!string.IsNullOrEmpty(exexistingUser.image_url))
                    {
                        var deletionParams = new DeletionParams(ImagePublicId);
                        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            Console.WriteLine($"Failed to delete old image: {deletionResult.Error?.Message}");
                            _logger.LogError("Failed to delete old image for user with ID {UserId}: {Error}", id, deletionResult.Error?.Message); 
                        }
                        else
                        {
                            _logger.LogInformation("Deleted old image for user with ID {UserId}: PublicId {PublicId}", id, ImagePublicId); 
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
                        _logger.LogError("Failed to upload new image for user with ID {UserId}: {Error}", id, uploadResult.Error.Message); 
                        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                    }
                    ImageUrl = uploadResult.SecureUrl.ToString(); 
                    ImageName = uploadResult.OriginalFilename;
                    ImagePublicId = uploadResult.PublicId;
                    _logger.LogInformation("Uploaded new image for user with ID {UserId}: PublicId {PublicId}", id, ImagePublicId); 
                }
                var finalRequest = new UsersRequest
                {
                    username = request.username,
                    password = request.password,
                    full_name = request.full_name,
                    email = request.email,
                    role = request.role,
                    department_company_unit = request.department_company_unit,
                    image_url = ImageUrl,
                    image_name = ImageName,
                    image_public_id = ImagePublicId,
                };

                var User = await _Service.UpdateUser(id, finalRequest);
                if (User == null)
                {
                    _logger.LogError("Failed to update user with ID {UserId}", id);
                    return BadRequest("Failed to update asset category.");
                }
                _logger.LogInformation("Updated user with ID {UserId} successfully", id);
                return Ok(User);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating user with ID {UserId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update user with ID {UserId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user with ID {UserId}", id); 
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> DeleteUsers(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete user with ID {UserId}", id);
                var user = await _Service.GetUserById(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id); 
                    return NotFound();
                }
                var result = await _Service.DeleteUser(id);
                if (result != true)
                {
                    _logger.LogError("Failed to delete user with ID {UserId}", id); 
                    return BadRequest();
                }
                _logger.LogInformation("Deleted user with ID {UserId} successfully", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID {UserId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting user with ID {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login(LoginRequest userInput)
        {
            try
            {
                _logger.LogInformation("Received login request for username {Username}", userInput.Username); // Log gọi endpoint
                var user = await _Service.Login(userInput);
                if (user == null)
                {
                    _logger.LogWarning("Login failed for username {Username}: Invalid username or password", userInput.Username); // Log đăng nhập thất bại
                    return Unauthorized("Invalid username or password.");
                }

                var accessToken = JwtTokenHelper.GenerateJwtToken(
                    user.username,
                    user.role,
                    _Configuration["Jwt:Key"],
                    _Configuration["Jwt:Issuer"],
                    _Configuration["Jwt:Audience"]
                );

                _logger.LogInformation("User with username {Username} logged in successfully", user.username); // Log thành công
                return Ok(new
                {
                    AccessToken = accessToken,
                    RefreshToken = user.refresh_token,
                    Username = user.username,
                    Role = user.role,
                    Id = user.user_id,
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for login: {Message}", ex.Message); // Log lỗi argument
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to process login for username {Username}: {Message}", userInput.Username, ex.Message); // Log lỗi thao tác
                return StatusCode(500, "An unexpected error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for username {Username}", userInput.Username); // Log lỗi bất ngờ
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to refresh token"); // Log gọi endpoint
                var user = await _Service.RefreshToken(request.RefreshToken);
                if (user == null)
                {
                    _logger.LogWarning("Refresh token failed: Invalid or expired token"); // Log token không hợp lệ
                    return Unauthorized("Invalid or expired refresh token.");
                }

                var accessToken = JwtTokenHelper.GenerateJwtToken(
                    user.username,
                    user.role,
                    _Configuration["Jwt:Key"],
                    _Configuration["Jwt:Issuer"],
                    _Configuration["Jwt:Audience"]
                );

                _logger.LogInformation("Refreshed token for user with ID {UserId} successfully", user.user_id); // Log thành công
                return Ok(new
                {
                    AccessToken = accessToken,
                    RefreshToken = user.refresh_token,
                    Username = user.username,
                    Role = user.role,
                    Id = user.user_id,
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for refresh token: {Message}", ex.Message); // Log lỗi argument
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to process refresh token: {Message}", ex.Message); // Log lỗi thao tác
                return StatusCode(500, "An unexpected error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during refresh token"); // Log lỗi bất ngờ
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}