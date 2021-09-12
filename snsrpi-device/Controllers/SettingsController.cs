using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using snsrpi.Models;
using snsrpi.Services;
using snsrpi.Interfaces;
using System.Text.Json;
using Newtonsoft.Json;


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
        public ActionResult<AcquisitionSettings> GetSettings(string id)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();

            var settings = _loggerManager.GetDevice(id).Settings;

            return settings;
        }

        [HttpPut("{id}")]
        public IActionResult UpdateSettings(string id, AcquisitionSettings settings)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();
            var device = _loggerManager.GetDevice(id);
            device.Settings = settings;
            device.SaveSettings();

            return Ok(JsonConvert.SerializeObject(settings));
        }

            
    }
}
