using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecuredNetCoreApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecuredNetCoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> RegiseterAsync([FromBody]RegisterModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _userService.RegisterUserAsync(model);
                if(result.IsSuccess)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            else
            {
                return BadRequest("Properties not valid");
            }
        }
        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody]LoginModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _userService.LoginUserAsync(model);
                if(result.IsSuccess)
                {
                    return Ok(result);

                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                return BadRequest();
            }
        }
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshRequest([FromBody] RefreshRequest refreshrequest)
        {
            var result = await _userService.RefreshRequest(refreshrequest);
            if(result!=null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
