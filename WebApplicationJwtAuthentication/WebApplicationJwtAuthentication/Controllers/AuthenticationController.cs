using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplicationJwtAuthentication.Dtos;
using WebApplicationJwtAuthentication.Model;
using WebApplicationJwtAuthentication.NewFolder;

namespace WebApplicationJwtAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        /*private readonly JwtConfig _jwtConfig;*/
        private readonly IConfiguration _configuration;
        public AuthenticationController(IConfiguration configuration,UserManager<IdentityUser> userManager, JwtConfig jwtConfig)
        {
            _userManager = userManager;
            /*_jwtConfig = jwtConfig;*/
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            //validate the incoming request
            
            if (ModelState.IsValid)
            {
                //check if the email already exist
                var existEmail =  await _userManager.FindByEmailAsync(requestDto.Email);
                
                if (existEmail != null)
                {
                    return BadRequest(new AuthResult
                    {
                        Result = false,
                        Errors = new List<string> {
                            "Email already exist"
                        }
                    });
                }
                //next step is to create a user

                var user = new IdentityUser()
                {
                    Email = requestDto.Email,
                    UserName = requestDto.Email
                };

                var createUser = await _userManager.CreateAsync(user, requestDto.Password);
                if (createUser.Succeeded)
                {
                    var token = GenerateJwtToken(user);
                    return Ok(new AuthResult()
                    {
                        Result = true,
                        Token = token
                    });
                }
                return BadRequest(new AuthResult()
                {
                    Errors = new List<string> {
                
                        "Server Error"
                    },
                    Result = false
                });
            } 

            return BadRequest();
        }


        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserRequestLoginDto loginRequestDto)
        {
            if (ModelState.IsValid)
            {
                //Check if the user Exist
                
                var existingUser = await _userManager.FindByEmailAsync(loginRequestDto.Email);
                
                if (existingUser == null)
                {
                    return BadRequest(new AuthResult
                    {
                        Errors = new List<string>
                        {
                             "Invalid Payload",
                        },
                        Result= false
                    });
                }
                
                var isCorrectUserPassword = await _userManager.CheckPasswordAsync(existingUser, loginRequestDto.Password);
                
                if (!isCorrectUserPassword) 
                {
                    return BadRequest(new AuthResult
                    {
                        Errors = new List<string>
                        {
                            "Invalid Credentials"
                        },
                        Result= false
                    });
                }

                // Generate Jwt Token
                var token = GenerateJwtToken(existingUser);
                return Ok(new AuthResult
                {
                    Result = true,
                    Token = token
                });
            }
            return BadRequest(new AuthResult
            {
                Result = false,
                Errors = new List<string>{
                    "Invalid Payload",
                }
            }); 
           

        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration.GetSection(key:"JwtConfig:Secret").Value);

            //generate token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(type:"Id", value: user.Id),
                    new Claim(type:JwtRegisteredClaimNames.Sub, value:user.Email),
                    new Claim(type:JwtRegisteredClaimNames.Email, value:user.Email),
                    new Claim(type:JwtRegisteredClaimNames.Jti, value:Guid.NewGuid().ToString()),
                    new Claim(type:JwtRegisteredClaimNames.Iat, value:DateTime.Now.ToUniversalTime().ToString()),
                    /*new Claim(type:JwtRegisteredClaimNames.Role, value: string.Join(",", roles)),*/
                }),

                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)

            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);
            return jwtToken;

        }
        
        /*private async Task CreateRole()
        {
            var roles = new[]
            {
                "Admin",
                "Moderator",
                "User",
                "Guest"
            };
            foreach (var role in roles)
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    *//*return BadRequest(new AuthResult
                    {
                        Result = false,
                        Errors = new List<string> {
                        $"Failed to create role {role}"
                        }
                    });*//*
                    throw new Exception($"Failed to create role {role}");
                }
            }

        }*/
    }
    


}
