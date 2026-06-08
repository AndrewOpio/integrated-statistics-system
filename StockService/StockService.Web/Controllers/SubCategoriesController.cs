using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.SubCategories;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/subcategories")]
    [Authorize]
    public class SubCategoriesController(ISubCategoryRepository _subCategoryRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetSubCategories()
        {
            var subCategories = await _subCategoryRepository.FindAllAsync();
            return Ok(subCategories);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(id);
            if (subCategory == null)
                return NotFound();

            return Ok(subCategory);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateSubCategory([FromBody] SubCategory subCategory)
        {
            if (subCategory == null)
                return BadRequest();

            try
            {
                var created = await _subCategoryRepository.CreateAsync(subCategory);
                return CreatedAtAction(nameof(GetSubCategory), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] SubCategory subCategory)
        {
            if (subCategory == null || subCategory.Id != id)
                return BadRequest();

            var exists = await _subCategoryRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _subCategoryRepository.UpdateAsync(subCategory);
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