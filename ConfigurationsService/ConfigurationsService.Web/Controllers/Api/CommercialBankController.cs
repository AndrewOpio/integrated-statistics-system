using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationsService.Core.Repositories.CommercialBanks;
using ConfigurationsService.Dto;

namespace ConfigurationsService.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class CommercialBankController : ControllerBase
    {
        private readonly ICommercialBankRepository _commercialBankRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CommercialBankController> _logger;

        public CommercialBankController(ICommercialBankRepository commercialBankRepository, IMapper mapper, ILogger<CommercialBankController> logger)
        {
            _commercialBankRepository = commercialBankRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all commercial banks
        /// </summary>
        /// <returns>List of all commercial banks</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CommercialBankDto>>> GetAll()
        {
            try
            {
                var banks = await _commercialBankRepository.FindAll();
                var bankDtos = _mapper.Map<List<CommercialBankDto>>(banks);
                return Ok(bankDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all commercial banks");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving commercial banks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific commercial bank by ID
        /// </summary>
        /// <param name="id">Commercial Bank ID</param>
        /// <returns>Commercial Bank details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CommercialBankDto>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid commercial bank ID" });

                var bank = await _commercialBankRepository.FindById(id);
                if (bank == null)
                    return NotFound(new { message = "Commercial bank not found" });

                var bankDto = _mapper.Map<CommercialBankDto>(bank);
                return Ok(bankDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving commercial bank with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the commercial bank", error = ex.Message });
            }
        }
        
    }
}