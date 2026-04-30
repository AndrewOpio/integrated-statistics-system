using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.BranchCategories;
using Microsoft.AspNetCore.Authorization;
using StockService.Core.Models.BranchCategories;
using StockService.Core.Models.Categories;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/branchcategories")]
    [Authorize]
    public class BranchCategoriesController(IBranchCategoryRepository _branchCategoryRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetBranchCategories()
        {
            var items = await _branchCategoryRepository.FindAllAsync();
            var result = items.Select(bc => new BranchCategoryDto
            {
                Id = bc.Id,
                IsDeleted = bc.IsDeleted,
                BranchId = bc.BranchId,
                Category = bc.Category == null ? null : new CategoryDto
                {
                    Id = bc.Category.Id,
                    Name = bc.Category.Name,
                    Classification = bc.Category.Classification,
                    IsDeleted = bc.Category.IsDeleted
                },
                CategoryId = bc.CategoryId,
            }).ToList();

            return Ok(result);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetBranchCategory(int id)
        {
            var item = await _branchCategoryRepository.FindByIdAsync(id);
            if (item == null)
                return NotFound();

            var result = new BranchCategoryDto
            {
                Id = item.Id,
                IsDeleted = item.IsDeleted,
                BranchId = item.BranchId,
                Category = item.Category == null ? null : new CategoryDto
                {
                    Id = item.Category.Id,
                    Name = item.Category.Name,
                    Classification = item.Category.Classification,
                    IsDeleted = item.Category.IsDeleted
                },
                CategoryId = item.CategoryId,
            };
            return Ok(result);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateBranchCategory([FromBody] BranchCategory item)
        {
            if (item == null)
                return BadRequest();

            try
            {
                var created = await _branchCategoryRepository.CreateAsync(item);
                return CreatedAtAction(nameof(GetBranchCategory), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateBranchCategory(int id, [FromBody] BranchCategory item)
        {
            if (item == null || item.Id != id)
                return BadRequest();

            var exists = await _branchCategoryRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _branchCategoryRepository.UpdateAsync(item);
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
