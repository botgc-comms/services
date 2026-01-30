// Controllers/NgrokController.cs
using BOTGC.MemberPortal.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers
{
    [ApiController]
    [Route("ngrok")]
    public sealed class NgrokController : ControllerBase
    {
        private readonly NgrokState _state;

        public NgrokController(NgrokState state)
        {
            _state = state;
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new { url = _state.PublicUrl });
        }
    }
}
