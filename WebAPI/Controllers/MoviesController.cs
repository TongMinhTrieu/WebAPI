using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebAPI.Data;
using WebAPI.Models;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;


namespace WebAPI.Controllers
{
    [Route("api/Movies")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly MovieContext _movieContext;
        private readonly ILogger<MoviesController> _logger; // Thêm logger
        private readonly IMemoryCache _cache;
        private const string MoviesCacheKey = "MoviesList";

        public MoviesController(MovieContext movieContext, ILogger<MoviesController> logger, IMemoryCache cache)
        {
            _movieContext = movieContext;
            _logger = logger;
            _cache = cache;
        }

        //Get Method: api/Movies
        //Sử dụng cache để lưu các get hay sử dụng
        [Authorize]
        //[HttpGet("/{waitSeconds:int}")]

        [HttpGet]
        [RequestTimeout("customdelegatepolicy")]
        public async Task<ActionResult<IEnumerable<Movie>>> GetMovies() //[FromRoute] int waitSeconds (bỏ vào biến đầu vào)
        {
            //await Task.Delay(TimeSpan.FromSeconds(waitSeconds), HttpContext.RequestAborted);
            if (!_cache.TryGetValue(MoviesCacheKey, out List<Movie> movies))
            {
                _logger.LogInformation("Fetching data from database.");
                movies = await _movieContext.Movies.ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)) // Hết hạn sau 10 phút không sử dụng
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Hoàn toàn hết hạn sau 1 giờ

                _cache.Set(MoviesCacheKey, movies, cacheEntryOptions);
            }
            else
            {
                _logger.LogInformation("Returning data from cache.");
            }
            return Ok(movies);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Movie>> GetMovie(int id)
        {
            if (_movieContext.Movies is null)
            {
                return NotFound();
            }
            var movie = await _movieContext.Movies.FindAsync(id);
            if (movie is null)
            {
                return NotFound();
            }
            return movie;
        }

        //Post Method: api/Movies
        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Movie>> PostMovie(Movie movie)
        {
            _movieContext.Movies.Add(movie);
            await _movieContext.SaveChangesAsync();
            _cache.Remove(MoviesCacheKey);
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a valid Excel file.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên
                        var rowCount = worksheet.Dimension.Rows; // Số dòng

                        for (int row = 2; row <= rowCount; row++) // Bỏ qua tiêu đề
                        {
                            var movie = new Movie
                            {
                                Title = worksheet.Cells[row, 1].Value.ToString(),
                                Genre = worksheet.Cells[row, 2].Value.ToString(),
                                //Genre = worksheet.Cells[row, 3].Value.ToString(),
                                ReleaseDay = DateTime.Parse(worksheet.Cells[row, 3].Value.ToString())
                            };
                            _movieContext.Movies.Add(movie);
                        }

                        await _movieContext.SaveChangesAsync();
                    }
                }

                return Ok("Data imported successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Put Method: api/Movies
        [HttpPut]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Movie>> PutMovie(int id, Movie movie)
        {
            if (id != movie.Id)
            {
                return BadRequest();
            }
            _movieContext.Entry(movie).State = EntityState.Modified;
            try
            {
                await _movieContext.SaveChangesAsync();
                _cache.Remove(MoviesCacheKey);
            }
            catch (DbUpdateConcurrencyException) 
            {
                if (!MovieExist(id)) { return NotFound(); }
                else {throw; } 
            }
            return NoContent();
        }

        private bool MovieExist(int id)
        {
            return (_movieContext.Movies?.Any(movie => movie.Id == id)).GetValueOrDefault();
        }

        //Delete Method: api/Movies
        [HttpDelete]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Movie>> DeleteMovie(int id)
        {
            if (_movieContext.Movies is null)
            {
                return NotFound();
            }
            var movie = await _movieContext.Movies.FindAsync(id);
            if (movie is null)
            {
                return NotFound();
            }
            _movieContext.Movies.Remove(movie);
            await _movieContext.SaveChangesAsync();
            _cache.Remove(MoviesCacheKey);
            return NoContent();
        }
    } 
}
