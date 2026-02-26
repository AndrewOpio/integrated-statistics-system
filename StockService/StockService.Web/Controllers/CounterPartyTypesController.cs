using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.CounterPartyTypes;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/counterpartytypes")]
    [Authorize]
    public class CounterPartyTypesController(ICounterPartyTypeRepository _counterPartyTypeRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetCounterPartyTypes()
        {
            var types = await _counterPartyTypeRepository.FindAllAsync();
            return Ok(types);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetCounterPartyType(int id)
        {
            var type = await _counterPartyTypeRepository.FindByIdAsync(id);
            if (type == null)
                return NotFound();
            return Ok(type);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateCounterPartyType([FromBody] CounterPartyType type)
        {
            if (type == null)
                return BadRequest();

            try
            {
                var created = await _counterPartyTypeRepository.CreateAsync(type);
                return CreatedAtAction(nameof(GetCounterPartyType), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateCounterPartyType(int id, [FromBody] CounterPartyType type)
        {
            if (type == null || type.Id != id)
                return BadRequest();

            var exists = await _counterPartyTypeRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _counterPartyTypeRepository.UpdateAsync(type);
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
