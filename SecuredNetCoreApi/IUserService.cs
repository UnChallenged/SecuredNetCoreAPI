using Microsoft.AspNetCore.Identity;
using SecuredNetCoreApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecuredNetCoreApi
{
    public interface IUserService
    {
        Task<UserManagerResponse> RegisterUserAsync(RegisterModel model);
    }
    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;
        public UserService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
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
                //Email = model.Email,
                UserName = model.UserName,
            };
            var result = await _userManager.CreateAsync(identityUser, model.Password);
            if(result.Succeeded)
            {
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
