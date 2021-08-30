using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using snsrpi.Models;
using snsrpi.Services;
using snsrpi.Interfaces;


namespace snsrpi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        
        private readonly ILogger<SettingsController> _logger;
        private readonly ILoggerManager _loggerManager;

        public SettingsController(ILogger<SettingsController> logger, ILoggerManager loggerManager)
        {
            _logger = logger;
            _loggerManager = loggerManager;
        }

        [HttpGet]
        public ActionResult Normal()
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public ActionResult<AcqusitionSettings> GetSettings(string id)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();

            var settings = _loggerManager.GetDevice(id).Settings;

            return settings;
        }

        [HttpPost("{id}")]
        public IActionResult UpdateSettings(string id, AcqusitionSettings settings)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();

            var device = _loggerManager.GetDevice(id);
            device.Settings = settings;

            return NoContent();
        }

            
    }
}
