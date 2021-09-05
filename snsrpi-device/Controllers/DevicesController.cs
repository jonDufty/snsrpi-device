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
    public class DevicesController : ControllerBase
    {
        private readonly ILogger<DevicesController> _logger;
        private readonly ILoggerManager _loggerManager;

        public DevicesController(ILogger<DevicesController> logger, ILoggerManager loggerManager)
        {
            _logger = logger;
            _loggerManager = loggerManager;
        }

        [HttpPost("{id}/start")]
        public IActionResult Post(string id)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();
            _logger.LogInformation("Device found...");
            _loggerManager.StartDevice(id);
            return Ok($"Device {id} has started");
        }

        [HttpPost("{id}/stop")]
        public IActionResult Stop(string id)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();
             _loggerManager.StopDevice(id);
            return Ok("All devices stopped");
        }

        [HttpGet]
        public IEnumerable<string> List()
        {
            var devices = _loggerManager.ListDevices();
            return devices;
        }

        [HttpPost("{id}")]
        public IActionResult Operate(string id, string action)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();
            
            _logger.LogDebug($"action recieved {action}");
            if (action.Equals("start"))
            {
                _loggerManager.StartDevice(id);
                return Ok($"Device {id} has started");
            } else if (action.Equals("stop")) {
                _loggerManager.StopDevice(id);
                return Ok($"Device {id} has stopped");
            } else {
                return BadRequest("Incorrect action given");
            }
            // var jsonBody = JsonConvert.DeserializeObject(body);
        }
    }
}
