using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.DenominationTypes;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/denominationtypes")]
    [Authorize]
    public class DenominationTypesController(IDenominationTypeRepository _denominationTypeRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetDenominationTypes()
        {
            var items = await _denominationTypeRepository.FindAllAsync();
            return Ok(items);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetDenominationType(int id)
        {
            var item = await _denominationTypeRepository.FindByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateDenominationType([FromBody] DenominationType item)
        {
            if (item == null)
                return BadRequest();

            try
            {
                var created = await _denominationTypeRepository.CreateAsync(item);
                return CreatedAtAction(nameof(GetDenominationType), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateDenominationType(int id, [FromBody] DenominationType item)
        {
            if (item == null || item.Id != id)
                return BadRequest();

            var exists = await _denominationTypeRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _denominationTypeRepository.UpdateAsync(item);
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
