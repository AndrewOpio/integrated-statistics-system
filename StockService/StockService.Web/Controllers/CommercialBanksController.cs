using Microsoft.AspNetCore.Mvc;
using StockService.Data.Entities;
using StockService.Core.Repository.CommercialBanks;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/commercialbanks")]
    [Authorize]
    public class CommercialBanksController(ICommercialBankRepository _bankRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetBanks()
        {
            var banks = await _bankRepository.FindAllAsync();
            return Ok(banks);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetBank(int id)
        {
            var bank = await _bankRepository.FindByIdAsync(id);
            if (bank == null)
                return NotFound();

            return Ok(bank);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateBank([FromBody] CommercialBank bank)
        {
            if (bank == null)
                return BadRequest();

            try
            {
                var created = await _bankRepository.CreateAsync(bank);
                return CreatedAtAction(nameof(GetBank), new { id = created.Id }, created);
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
        public async Task<IActionResult> UpdateBank(int id, [FromBody] CommercialBank bank)
        {
            if (bank == null || bank.Id != id)
                return BadRequest();

            var exists = await _bankRepository.FindByIdAsync(id);
            if (exists == null)
                return NotFound();

            try
            {
                var updated = await _bankRepository.UpdateAsync(bank);
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
