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
    public class DevicesController : ControllerBase
    {
        private LoggerService LoggerManager {get;}

        public DevicesController(LoggerService loggerManager)
        {
            LoggerManager = loggerManager;
        }

        [HttpPost("{id}/start")]
        public IActionResult Start(string id)
        {
            if (LoggerManager.CheckDevice(id)) return NotFound();
            // var result = LoggerManager.StartDevice(id);
            return NoContent();
        }

        [HttpPost("{id}/stop")]
        public IActionResult Stop(string id)
        {
            if (LoggerManager.CheckDevice(id)) return NotFound();
            // var result = LoggerManager.StopDevice(id);
            return NoContent();
        }
    }
}
