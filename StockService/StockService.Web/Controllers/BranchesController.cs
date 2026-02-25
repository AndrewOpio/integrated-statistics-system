using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.Branches;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/branches")]
    [Authorize]
    public class BranchesController(IBranchRepository _branchRepository) : ControllerBase
    {
        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _branchRepository.FindAllAsync();
            return Ok(branches);
        }


        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _branchRepository.FindByIdAsync(id);

            if (branch == null)
                return NotFound();

            return Ok(branch);
        }


        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateBranch([FromBody] Branch branch)
        {
            if (branch == null)
                return BadRequest();

            try
            {
                var created = await _branchRepository.CreateAsync(branch);
                return CreatedAtAction(nameof(GetBranch), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] Branch branch)
        {
            if (branch == null || branch.Id != id)
                return BadRequest();

            var exists = await _branchRepository.FindByIdAsync(id);

            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _branchRepository.UpdateAsync(branch);

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
