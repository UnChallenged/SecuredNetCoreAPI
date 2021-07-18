using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecuredNetCoreApi.Enum;
using SecuredNetCoreApi.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SecuredNetCoreApi
{
    public interface IUserService
    {
        Task<UserManagerResponse> RegisterUserAsync(RegisterModel model);
        Task<UserManagerResponse> LoginUserAsync(LoginModel model);
    }
    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;
        private IConfiguration _configuration;
        private RoleManager<IdentityRole> _roleManager;
        public UserService(RoleManager<IdentityRole> roleManager,UserManager<IdentityUser> userManager,IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
            _roleManager = roleManager;
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
                    var claims = new[]
                    {
                        new Claim("Email",model.Email),
                        new Claim(ClaimTypes.NameIdentifier,user.Id),
                       
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
                        ExpireDate = token.ValidTo
                    };
                }
            }
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
    }
}
