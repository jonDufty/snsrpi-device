using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using snsrpi.Models;
using snsrpi.Services;
using snsrpi.Interfaces;
using Newtonsoft.Json;


namespace snsrpi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ILoggerManager _loggerManager;

        public HealthController(ILogger<HealthController> logger, ILoggerManager loggerManager)
        {
            _logger = logger;
            _loggerManager = loggerManager;
        }

        [HttpGet]
        public IActionResult Healthcheck()
        {
            return Ok(_loggerManager.HealthCheck());
        }

        
    }
}
