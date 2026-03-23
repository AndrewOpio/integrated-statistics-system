using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CommercialBankBranches;

namespace VMSService.Web.Controllers
{
    //public class CbWithdrawRequestsController : Controller
    //{

    //[Authorize(Policy = RolePolicy.CbOnly)]
    public class CbWithdrawRequestsController(
    IAuthorizationService authorizationService,
    IBranchRepository _branchRepo, 
    ICommercialBankBranchRepository _cbBranchRepo,
    IAgentDetailRepository _agentRepo, 
    IMapper _mapper) : BaseWithdrawController(authorizationService)
    {
        public IActionResult Index()
        {
            return View();
        }
        //public IActionResult Create()
        //{
        //    var stepVm = WithdrawalStepViewModelFactory.Build(
        //        currentStepKey: "CbMaker",
        //        isRequestApproved: false,
        //        user: User);

        //    return View(stepVm);
        //}
        // ── Create ────────────────────────────────
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var stepVm = WithdrawalStepViewModelFactory.Build(
                currentStepKey: "CbMaker",
                isRequestApproved: false,
                user: User);

            var xCbBranches = await _branchRepo.FindAll();

            var branchVm = new BranchSelectionViewModel
            {
                CbBranches = await _branchRepo.FindAll(),
                BouBranches = await _branchRepo.FindAll(),
            };

            var agentDetails = (await _agentRepo.FindAll()).Where(a => a.IsDeleted == false);
            var agentDetailsVm = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

            var agentVm = new AgentSelectionViewModel
            {
                Agents = agentDetailsVm,
            };

            return View(new CreateWithdrawViewModel
            {
                Steps = stepVm,
                Branches = branchVm,
                Agents = agentVm,
            });
        }

        // ── Branch details endpoint (AJAX) ────────
        [HttpGet("branch-details")]
        public async Task<IActionResult> BranchDetails(string type, int id, CancellationToken ct)
        {
            if (type == "cb")
            {
                var branch = await _branchRepo.FindById(id);

                if (branch is null) 
                    return NotFound();

                return Json(new { branch.Name, branch.Id });
            }

            if (type == "bou")
            {
                var branch = await _branchRepo.FindById(id);
                
                if (branch is null) 
                    return NotFound();

                return Json(new { branch.Name, branch.Id });
            }

            return BadRequest("Invalid branch type.");
        }

        //[HttpGet("agent-details")]
        public async Task<IActionResult> AgentDetails(int id)
        {
            var agentDetail = await _agentRepo.FindById(id);
            if (agentDetail == null)
            {
                return NotFound();
            }

            var agentDetailVm = _mapper.Map<AgentDetailViewModel>(agentDetail);

            return Json(new
            {
                agentDetailVm.AgentNames,
                agentDetailVm.NIN,
                agentDetailVm.WorkIdScan,
                agentDetailVm.PhoneNumber,
                agentDetailVm.CommercialBankBranchId,
                agentDetailVm.BranchId,
                agentDetailVm.PassportPhoto,
                agentDetailVm.SignatureScan,
            });
        }
        public IActionResult Approve()
        {
            return View();
        }
    }
}
