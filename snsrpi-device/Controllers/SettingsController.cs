using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using snsrpi.Models;
using snsrpi.Services;


namespace snsrpi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        
        private readonly LoggerService LoggerManager;
        public SettingsController(LoggerService _logger)
        {
            LoggerManager = _logger;
        }

        [HttpGet("{id}")]
        public ActionResult<AcqusitionSettings> GetSettings(string id)
        {
            if (!LoggerManager.CheckDevice(id)) return NotFound();

            var settings = LoggerManager.GetDevice(id).Settings;

            return settings;
        }

        [HttpPost("{id}")]
        public IActionResult UpdateSettings(string id, AcqusitionSettings settings)
        {
            if (!LoggerManager.CheckDevice(id)) return NotFound();

            var device = LoggerManager.GetDevice(id);
            device.Settings = settings;

            return NoContent();
        }

            
    }
}
