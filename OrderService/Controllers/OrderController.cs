using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderServiceController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public OrderServiceController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        
        [HttpGet("HostInfo")]
        public string Get()
        {
            string host = string.Empty;
            host = _httpContextAccessor.HttpContext.Request.Host.Value;
            return host;
        }
    }
}