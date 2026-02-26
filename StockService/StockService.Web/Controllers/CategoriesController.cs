using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.Categories;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController(ICategoryRepository _categoryRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryRepository.FindAllAsync();
            return Ok(categories);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryRepository.FindByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (category == null)
                return BadRequest();

            try
            {
                var created = await _categoryRepository.CreateAsync(category);
                return CreatedAtAction(nameof(GetCategory), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (category == null || category.Id != id)
                return BadRequest();

            var exists = await _categoryRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _categoryRepository.UpdateAsync(category);
                if (!updated)
                    return StatusCode(500, "A problem happened while handling your request.");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
