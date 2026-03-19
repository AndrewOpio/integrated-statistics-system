using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Repositories.Branches;
using ConfigurationsService.Dto;

namespace ConfigurationsService.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchController> _logger;

        public BranchController(IBranchRepository branchRepository, IMapper mapper, ILogger<BranchController> logger)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all branches
        /// </summary>
        /// <returns>List of all branches</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BranchDto>>> GetAll()
        {
            try
            {
                var branches = await _branchRepository.FindAll();
                var branchDtos = _mapper.Map<List<BranchDto>>(branches);
                return Ok(branchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all branches");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while retrieving branches", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific branch by ID
        /// </summary>
        /// <param name="id">Branch ID</param>
        /// <returns>Branch details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BranchDto>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid branch ID" });

                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                    return NotFound(new { message = "Branch not found" });

                var branchDto = _mapper.Map<BranchDto>(branch);
                return Ok(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving branch with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the branch", error = ex.Message });
            }
        }
    }
}