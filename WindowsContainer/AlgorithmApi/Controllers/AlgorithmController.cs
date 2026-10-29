using System.Web.Http;
using AlgorithmApi.Models;
using AlgorithmApi.Services;

namespace AlgorithmApi.Controllers
{
    [RoutePrefix("api/algorithm")]
    public class AlgorithmController : ApiController
    {
        private readonly AlgorithmService _service = new AlgorithmService();

        [HttpPost, Route("sort")]
        public IHttpActionResult Sort([FromBody] SortRequest request)
        {
            if (request?.Numbers == null) return BadRequest("Numbers are required.");
            return Ok(new { sorted = _service.BubbleSort(request.Numbers) });
        }

        [HttpPost, Route("search")]
        public IHttpActionResult Search([FromBody] SearchRequest request)
        {
            if (request?.Numbers == null) return BadRequest("Numbers are required.");
            int index = _service.BinarySearch(request.Numbers, request.Target);
            return Ok(new { index, found = index >= 0 });
        }

        [HttpGet, Route("palindrome/{word}")]
        public IHttpActionResult Palindrome(string word)
        {
            return Ok(new { word, isPalindrome = _service.IsPalindrome(word) });
        }
    }
}
