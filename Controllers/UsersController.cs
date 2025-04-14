using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Jwt;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Microsoft.AspNetCore.Razor.Hosting;
using System.Runtime.ConstrainedExecution;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _Service;
        private readonly IConfiguration _Configuration;
        private readonly Cloudinary _Cloudinary;
        public UsersController(IUsersService Service, IConfiguration configuration, Cloudinary cloudinary)
        {
            _Service = Service;
            _Configuration = configuration;
            _Cloudinary = cloudinary;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllUsers()
        {
            return Ok(await _Service.GetAllUsers());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUsersById(int id)
        {
            var users = await _Service.GetUserById(id);
            if (users == null)
            {
                return NotFound("Users does't exist");
            }
            return Ok(users);
        }
        
        [HttpPost]
        public async Task<ActionResult> CreateUsers([FromForm] UserImageModel request)
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

                var finalRequest = new UsersRequest
                {
                    username = request.username,
                    password = request.password_hash,
                    full_name = request.full_name,
                    email = request.email,
                    role = request.role,
                    //i = imageUrl
                };

                var user = await _Service.CreateUser(finalRequest);
                if (user == null)
                {
                    return BadRequest("Failed to create user.");
                }
                return CreatedAtAction(nameof(GetUsersById), new { id = user.user_id }, user);
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
        public async Task<ActionResult> UpdateUsers([FromForm] UserImageModel request, int id)
        {
            try
            {
                var exexistingUser = await _Service.GetUserById(id);
                if (exexistingUser == null)
                {
                    return NotFound("User does not exist");
                }
                //string ImageUrl = exexistingUser.image_url;
                //if (request.image != null && request.image.Length > 0)
                //{
                //    // Xóa ảnh cũ trên Cloudinary nếu tồn tại
                //    if (!string.IsNullOrEmpty(exexistingUser.image_url))
                //    {
                //        var publicId = Path.GetFileNameWithoutExtension(new Uri(exexistingUser.image_url).AbsolutePath);
                //        var deletionParams = new DeletionParams(publicId);
                //        var deletionResult = await _Cloudinary.DestroyAsync(deletionParams);
                //        if (deletionResult.Result != "ok")
                //        {
                //            Console.WriteLine($"Failed to delete old image: {deletionResult.Error?.Message}");
                //        }
                //    }

                //    // Tải ảnh mới lên Cloudinary
                //    var uploadParams = new ImageUploadParams
                //    {
                //        File = new FileDescription(request.image.FileName, request.image.OpenReadStream()),
                //        UseFilename = true,
                //        UniqueFilename = true,
                //        Overwrite = true
                //    };
                //    var uploadResult = await _Cloudinary.UploadAsync(uploadParams);
                //    if (uploadResult.Error != null)
                //    {
                //        return StatusCode((int)uploadResult.StatusCode, uploadResult.Error.Message);
                //    }
                //    ImageUrl = uploadResult.SecureUrl.ToString(); // Cập nhật URL mới
                //}
                var finalRequest = new UsersRequest
                {
                    username = request.username,
                    password = request.password_hash,
                    full_name = request.full_name,
                    email = request.email,
                    role = request.role,
                    //image_url = ImageUrl,
                };

                var User = await _Service.UpdateUser(id,finalRequest);
                if (User == null)
                {
                    return BadRequest("Failed to update asset category.");
                }
                return Ok(User);
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
        public async Task<ActionResult> DeleteUsers(int id)
        {
            var user = await _Service.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteUser(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login(LoginRequest userInput)
        {
            var user = await _Service.Login(userInput);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var accessToken = JwtTokenHelper.GenerateJwtToken(
                user.username,
                user.role,
                _Configuration["Jwt:Key"],
                _Configuration["Jwt:Issuer"],
                _Configuration["Jwt:Audience"]
            );

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = user.refresh_token,
                Username = user.username,
                Role = user.role
            });
        }
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var user = await _Service.RefreshToken(request.RefreshToken);
            if (user == null)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            var accessToken = JwtTokenHelper.GenerateJwtToken(
                user.username,
                user.role,
                _Configuration["Jwt:Key"],
                _Configuration["Jwt:Issuer"],
                _Configuration["Jwt:Audience"]
            );

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = user.refresh_token,
                Username = user.username,
                Role = user.role
            });
        }

    }
}
