using System;
using GuideXOS;
using Microsoft.AspNetCore.Mvc;

namespace guideXOS.Web.Controllers
{
    [ApiController]
    [Route("v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly Repository _repo = new Repository();

        public class AuthRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new LoginResponse { Success = false, Message = "Username and password required." });
            Guid token; string message;
            bool ok = _repo.Login(req.Username, req.Password, out token, out message);
            return Ok(new LoginResponse { Success = ok, Message = message, LoginGuid = token });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new LoginResponse { Success = false, Message = "Username and password required." });
            Guid token; string message;
            bool ok = _repo.Register(req.Username, req.Password, out token, out message);
            return Ok(new LoginResponse { Success = ok, Message = message, LoginGuid = token });
        }
    }
}
