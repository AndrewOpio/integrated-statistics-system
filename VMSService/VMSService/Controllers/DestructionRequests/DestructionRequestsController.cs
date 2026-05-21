
using Microsoft.AspNetCore.Mvc;
using VMSService.Data;
using VMSService.Data.Entities;
using VMSService.Models;

namespace VMSService.Controllers.DestructionRequests
{
    public class DestructionRequestsController : Controller
    {
    //    private readonly VMSServiceDbContext _context;
    //    public DestructionRequestsController(VMSServiceDbContext context)
    //    {
    //        _context = context;
    //    }

    //    // GET: /DestructionRequests/Create
    //    public IActionResult Create()
    //    {
    //        return View(new DestructionRequestCreateViewModel());
    //    }

    //    // POST: /DestructionRequests/Create
    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public IActionResult Create(DestructionRequestCreateViewModel model)
    //    {
    //        if (ModelState.IsValid)
    //        {
    //            var request = new DestructionRequest
    //            {
    //                Date = model.Date,
    //                ReferenceNumber = model.ReferenceNumber,
    //                PanelA = model.PanelA,
    //                PanelB = model.PanelB,
    //                VerificationOfficer = model.VerificationOfficer,
    //                OtherMembers = model.OtherMembers,
    //                DestructionRequestDenominations = model.Denominations.Select(d => new DestructionRequestDenomination
    //                {
    //                    DenominationId = d.DenominationId,
    //                    NumberOfBags = d.NumberOfBags,
    //                    Amount = d.Amount
    //                }).ToList(),
    //                DestructionRequestWitnesses = model.Witnesses.Select(w => new DestructionRequestWitness
    //                {
    //                    IDNumber = w.IDNumber,
    //                    FullName = w.FullName
    //                }).ToList()
    //            };
    //            _context.DestructionRequests.Add(request);
    //            _context.SaveChanges();
    //            return RedirectToAction("Index", "Home");
    //        }
    //        return View(model);
    //    }
    }
}
