using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationsService.Core.Repositories.CommercialBankBranches;
using ConfigurationsService.Dto;

namespace ConfigurationsService.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class CommercialBankBranchController : ControllerBase
    {
        private readonly ICommercialBankBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CommercialBankBranchController> _logger;

        public CommercialBankBranchController(ICommercialBankBranchRepository branchRepository, IMapper mapper, ILogger<CommercialBankBranchController> logger)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all commercial bank branches
        /// </summary>
        /// <returns>List of all commercial bank branches</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CommercialBankBranchDto>>> GetAll()
        {
            try
            {
                var branches = await _branchRepository.FindAll();
                var branchDtos = _mapper.Map<List<CommercialBankBranchDto>>(branches);
                return Ok(branchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all commercial bank branches");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving commercial bank branches", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all commercial bank branches
        /// </summary>
        /// <returns>List of all commercial bank branches</returns>
        [HttpGet("cb/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CommercialBankBranchDto>>> GetAllByCommercialBankById(int id)
        {
            try
            {
                var cbBranches = await _branchRepository.FindByCommercialBankId(id);
                var cbBranchDtos = _mapper.Map<List<CommercialBankBranchDto>>(cbBranches);
                return Ok(cbBranchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all commercial bank branches");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving commercial bank branches", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific commercial bank branch by ID
        /// </summary>
        /// <param name="id">Commercial Bank Branch ID</param>
        /// <returns>Commercial Bank Branch details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CommercialBankBranchDto>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid commercial bank branch ID" });

                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                    return NotFound(new { message = "Commercial bank branch not found" });

                var branchDto = _mapper.Map<CommercialBankBranchDto>(branch);
                return Ok(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving commercial bank branch with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the commercial bank branch", error = ex.Message });
            }
        }
    }
}