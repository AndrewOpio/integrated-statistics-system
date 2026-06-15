using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockService.Core.Models.Denominations;
using StockService.Core.Models.DenominationTypes;
using StockService.Core.Repository.Denominations;
using StockService.Data.Entities;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/denominations")]
    //[Authorize]
    public class DenominationsController(IDenominationRepository _denominationRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDenominations()
        {
            var items = await _denominationRepository.FindAllAsync();
            var result = items.Select(d => new DenominationDto
            {
                Id = d.Id,
                Name = d.Name,
                Value = d.Value,
                NumberOfPiecesInABox = d.NumberOfPiecesInABox,
                NumberOfPiecesInABundle = d.NumberOfPiecesInABundle,
                DenominationTypeId = d.DenominationTypeId,
                DenominationType = d.DenominationType == null ? null : new DenominationTypeDto
                {
                    Id = d.DenominationType.Id,
                    Name = d.DenominationType.Name
                }
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetDenomination(int id)
        {
            var item = await _denominationRepository.FindByIdAsync(id);
            if (item == null)
                return NotFound();

            var result = new DenominationDto
            {
                Id = item.Id,
                Name = item.Name,
                Value = item.Value,
                NumberOfPiecesInABox = item.NumberOfPiecesInABox,
                NumberOfPiecesInABundle = item.NumberOfPiecesInABundle,
                DenominationTypeId = item.DenominationTypeId,
                DenominationType = item.DenominationType == null ? null : new DenominationTypeDto
                {
                    Id = item.DenominationType.Id,
                    Name = item.DenominationType.Name
                }
            };
            return Ok(result);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateDenomination([FromBody] Denomination item)
        {
            if (item == null)
                return BadRequest();
            try
            {
                var created = await _denominationRepository.CreateAsync(item);
                return CreatedAtAction(nameof(GetDenomination), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateDenomination(int id, [FromBody] Denomination item)
        {
            if (item == null || item.Id != id)
                return BadRequest();

            var exists = await _denominationRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _denominationRepository.UpdateAsync(item);
                if (!updated)
                    return StatusCode(500, "A problem happened while handling your request.");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
