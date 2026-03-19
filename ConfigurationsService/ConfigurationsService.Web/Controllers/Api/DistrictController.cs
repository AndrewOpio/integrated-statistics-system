using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationsService.Core.Repositories.Districts;
using ConfigurationsService.Dto;

namespace ConfigurationsService.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class DistrictController : ControllerBase
    {
        private readonly IDistrictRepository _districtRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DistrictController> _logger;

        public DistrictController(IDistrictRepository districtRepository, IMapper mapper, ILogger<DistrictController> logger)
        {
            _districtRepository = districtRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all districts
        /// </summary>
        /// <returns>List of all districts</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DistrictDto>>> GetAll()
        {
            try
            {
                var districts = await _districtRepository.FindAll();
                var districtDtos = _mapper.Map<List<DistrictDto>>(districts);
                return Ok(districtDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all districts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving districts", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific district by ID
        /// </summary>
        /// <param name="id">District ID</param>
        /// <returns>District details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DistrictDto>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid district ID" });

                var district = await _districtRepository.FindById(id);
                if (district == null)
                    return NotFound(new { message = "District not found" });

                var districtDto = _mapper.Map<DistrictDto>(district);
                return Ok(districtDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving district with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the district", error = ex.Message });
            }
        }
    }
}