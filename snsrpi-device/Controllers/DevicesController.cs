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
            var result = new Dictionary<string,string>();
            result.Add("result","Success");
            result.Add("message",$"Device {id} successfully stopped");   

            return new JsonResult(result);
        }

        [HttpPost("{id}/stop")]
        public IActionResult Stop(string id)
        {
            var result = new Dictionary<string,string>();

            if (!_loggerManager.CheckDevice(id)) return NotFound();
            
            _loggerManager.StopDevice(id);
            result.Add("result","Success");
            result.Add("message",$"Device {id} successfully stopped");            
            return new JsonResult(result);
        }

        [HttpGet]
        public IEnumerable<string> List()
        {
            var devices = _loggerManager.ListDevices();
            return devices;
        }

        [HttpPost("{id}")]
        public IActionResult Operate(string id, bool active)
        {
            if (!_loggerManager.CheckDevice(id)) return NotFound();
            var device = _loggerManager.GetDevice(id);

            _logger.LogDebug($"Active recieved {active}");
            if (active && !device.IsActive)
            {
                _loggerManager.StartDevice(id);
            } else if (!active && device.IsActive) {
                _loggerManager.StopDevice(id);
            } else {
                return BadRequest();
            }
            var result = _loggerManager.HealthCheck();

            return new JsonResult(result);
        }
    }
}
