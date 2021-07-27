using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecuredNetCoreApi.Enum;
using SecuredNetCoreApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SecuredNetCoreApi
{
    public interface IUserService
    {
        Task<UserManagerResponse> RegisterUserAsync(RegisterModel model);
        Task<UserManagerResponse> LoginUserAsync(LoginModel model);
        Task<UserManagerResponse> RefreshRequest(RefreshRequest refreshrequest);
    }
    public class UserService : IUserService
    {
        private ApplicationDbContext _db;
        private UserManager<IdentityUser> _userManager;
        private IConfiguration _configuration;
        private RoleManager<IdentityRole> _roleManager;
        private NetSecuredAPIContext _netSecuredAPIContext;
        public UserService(NetSecuredAPIContext netSecuredAPIContext, RoleManager<IdentityRole> roleManager,UserManager<IdentityUser> userManager,IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _netSecuredAPIContext = netSecuredAPIContext;
        }

        public async Task<UserManagerResponse> LoginUserAsync(LoginModel model)
        {
            if (model == null)
            {
                throw new NullReferenceException("Register model is null");
            }
            else
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user==null)
                {
                    return new UserManagerResponse
                    {
                        Message = "there is no user with that email",
                        IsSuccess = false,
                    };
                }
                var result = await _userManager.CheckPasswordAsync(user, model.Password);
                    if(!result)
                    {
                    return new UserManagerResponse
                    {
                        Message = "Invalid Password",
                        IsSuccess = false,
                    };
                   
                }
                else
                {
                    RefreshToken refreshtoken = GenerateRefreshToken();
                    refreshtoken.UserId = user.Id;

                    _netSecuredAPIContext.RefreshTokens.Add(refreshtoken);
                    await _netSecuredAPIContext.SaveChangesAsync();

                    var role = string.Join(",", await _userManager.GetRolesAsync(user));
                    var claims = new[]
                    {
                        new Claim("Email",model.Email),
                        new Claim("ID",user.Id),
                        new Claim("Role",role),
                       
                    };
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["secretkey:key"]));
                    var token = new JwtSecurityToken(
                      issuer: _configuration["secretkey:ValidIssuer"],
                      audience: _configuration["secretkey:ValidAudience"],
                      claims: claims,
                      expires: DateTime.Now.AddDays(30),
                      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
                    string tokenasString = new JwtSecurityTokenHandler().WriteToken(token);
                    return new UserManagerResponse
                    {
                        Message = tokenasString,
                        IsSuccess = true,
                        ExpireDate = token.ValidTo,
                        RefreshToken= refreshtoken.Token
                    };
                }
            }
        }
        private RefreshToken GenerateRefreshToken()
        {
            RefreshToken refreshtoken = new RefreshToken();
            
            var randomNumber = new Byte[32];
            using(var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshtoken.Token = Convert.ToBase64String(randomNumber);
            }
            refreshtoken.ExpireDate = DateTime.UtcNow.AddDays(30);
            return refreshtoken;
        }
        public async Task<UserManagerResponse> RegisterUserAsync(RegisterModel model)
        {
            if(model==null)
            {
                throw new NullReferenceException("Register model is null");
            }
            if(model.Password!=model.ConfirmPassword)
            {
                return new UserManagerResponse
                {
                    Message = "Confirm Password didn't matched",
                    IsSuccess = false,
                };
            }
            var identityUser = new IdentityUser
            {
                Email = model.Email,
                UserName = model.UserName,
            };
            var result = await _userManager.CreateAsync(identityUser, model.Password);
            if(result.Succeeded)
            {
                if (!(await _roleManager.RoleExistsAsync(Roles.Basic.ToString())))
                {
                    await _roleManager.CreateAsync(new IdentityRole(Roles.Basic.ToString()));
                }
                await _userManager.AddToRoleAsync(identityUser, Roles.Basic.ToString());
                return new UserManagerResponse
                {
                    Message = "User Created Successfully",
                    IsSuccess = true,
                };
            }
            else
            {
                return new UserManagerResponse
                {
                    Message = "User did not created",
                    IsSuccess = false,
                    Error = result.Errors.Select(e => e.Description)
                };
            }
            
        }
        public async Task<string> GetRoleFromUserIDAsync(IdentityUser user)
        {
            
            var roles = await _userManager.GetRolesAsync(user);
            return string.Join(",", roles);
        }

        public async Task<UserManagerResponse> RefreshRequest(RefreshRequest refreshrequest)
        {
            AspNetUser user = GetUserFromAccessToken(refreshrequest.AccessToken);
            if(user!=null && ValidateRefreshToken(user,refreshrequest.RefreshToken))
            {
                var user_ = await _userManager.FindByEmailAsync(user.Email);
                RefreshToken refreshtoken = GenerateRefreshToken();
                refreshtoken.UserId = user.Id;
                var role = string.Join(",", await _userManager.GetRolesAsync(user_));
                var claims = new[]
                {
                        new Claim("Email",user_.Email),
                        new Claim("ID",user_.Id),
                        new Claim("Role",role),

                    };
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["secretkey:key"]));
                var token = new JwtSecurityToken(
                  issuer: _configuration["secretkey:ValidIssuer"],
                  audience: _configuration["secretkey:ValidAudience"],
                  claims: claims,
                  expires: DateTime.Now.AddDays(30),
                  signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
                string tokenasString = new JwtSecurityTokenHandler().WriteToken(token);
                return new UserManagerResponse
                {
                    Message = tokenasString,
                    IsSuccess = true,
                    ExpireDate = token.ValidTo,
                    RefreshToken = refreshtoken.Token
                };
            }
            return null;
        }

        private bool ValidateRefreshToken(AspNetUser user, string refreshToken)
        {
          RefreshToken refreshtoken= _netSecuredAPIContext.RefreshTokens.Where(rt => rt.Token == refreshToken)
                .OrderByDescending(rt => rt.ExpireDate)
                .FirstOrDefault(); 
            if(refreshToken!=null &&
                refreshtoken.UserId==user.Id &&
                refreshtoken.ExpireDate>DateTime.UtcNow)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private AspNetUser GetUserFromAccessToken(string accessToken)
        {
            var tokenhandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["secretkey:key"]));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _configuration["secretkey:ValidAudience"],
                ValidIssuer = _configuration["secretkey:ValidIssuer"],
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["secretkey:key"])),
                ValidateIssuerSigningKey = true
            };
            SecurityToken securityToken;


           var principle= tokenhandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);
            JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;
            if(jwtSecurityToken!=null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,StringComparison.InvariantCultureIgnoreCase))
            {
                var userid = principle.FindFirst(ClaimTypes.Name)?.Value;
               return _netSecuredAPIContext.AspNetUsers.Where(u => u.Id == userid).FirstOrDefault();
            }
            else
            {
                return null;
            }
            
        }
    }
}
