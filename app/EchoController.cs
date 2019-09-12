using Microsoft.AspNetCore.Mvc;

namespace App
{
    [Route("api/[controller]")]
    [ApiController]
    public class EchoController : ControllerBase
    {
        // GET api/echo
        [HttpGet]
        public ActionResult<string> Get()
        {
            return Ok("Echo");
        }

        // GET api/echo/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return $"Echo {id}";
        }

        // POST api/echo
        [HttpPost]
        public ActionResult<string> Post([FromBody] string value)
        {
            return $"Echo {value}";
        }
    }
}
