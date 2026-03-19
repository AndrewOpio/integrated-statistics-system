using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockService.Core.Models.CounterPartyTypes;
using StockService.Core.Models.Denominations;
using StockService.Core.Models.DenominationTypes;
using StockService.Core.Models.StockLedger;
using StockService.Core.Repository.StockLedgers;
using StockService.Data.Entities;
using StockService.Core.Models.BranchCategories;
using StockService.Core.Models.Branches;
using StockService.Core.Models.Categories;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api/stockledgers")]
    [Authorize]
    public class StockLedgersController(IStockLedgerRepository _stockLedgerRepository) : ControllerBase
    {

        [HttpGet]
        [Route("get-all")]
        public async Task<IActionResult> GetStockLedgers()
        {
            var items = await _stockLedgerRepository.FindAllAsync();
            var result = items.Select(sl => new StockLedgerDto
            {
                Id = sl.Id,
                ReferenceCode = sl.ReferenceCode,
                IsIn = sl.IsIn,
                CounterPartyTypeId = sl.CounterPartyTypeId,
                CounterPartyType = sl.CounterPartyType == null ? null : new CounterPartyTypeDto
                {
                    Id = sl.CounterPartyType.Id,
                    Name = sl.CounterPartyType.Name
                },
                CounterPartyId = sl.CounterPartyId,
                StoreId  = sl.StoreId,
                Store = sl.Store == null ? null : new BranchDto
                {
                    Id = sl.Store.Id,
                    Name = sl.Store.Name
                },
                Notes = sl.Notes,
                StockLedgerDetails = sl.StockLedgerDetails?.Select(d => new StockLedgerDetailDto
                {
                    Id = d.Id,
                    DenominationId = d.DenominationId,
                    Denomination = d.Denomination == null ? null : new DenominationDto
                    {
                        Id = d.Denomination.Id,
                        Name = d.Denomination.Name,
                        Value = d.Denomination.Value,
                        NumberOfPiecesInABox = d.Denomination.NumberOfPiecesInABox,
                        DenominationTypeId = d.Denomination.DenominationTypeId,
                        DenominationType = d.Denomination.DenominationType == null ? null : new DenominationTypeDto
                        {
                            Id = d.Denomination.DenominationType.Id,
                            Name = d.Denomination.DenominationType.Name
                        }
                    },
                    CategoryId = d.CategoryId,
                    Category = d.Category == null ? null : new CategoryDto
                    {
                        Id = d.Category.Id,
                        Name = d.Category.Name
                    },
                    NumberOfPieces = d.NumberOfPieces,
                    NumberOfBoxes = d.NumberOfBoxes,
                    NumberOfBundles = d.NumberOfBundles,
                    Value = d.Value
                }).ToList() ?? new List<StockLedgerDetailDto>()
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetStockLedger(int id)
        {
            var item = await _stockLedgerRepository.FindByIdAsync(id);
            if (item == null)
                return NotFound();

            var result = new StockLedgerDto
            {
                Id = item.Id,
                ReferenceCode = item.ReferenceCode,
                IsIn = item.IsIn,
                CounterPartyTypeId = item.CounterPartyTypeId,
                CounterPartyType = item.CounterPartyType == null ? null : new CounterPartyTypeDto
                {
                    Id = item.CounterPartyType.Id,
                    Name = item.CounterPartyType.Name
                },
                CounterPartyId = item.CounterPartyId,
                StoreId = item.StoreId,
                Store = item.Store == null ? null : new BranchDto
                {
                    Id = item.Store.Id,
                    Name = item.Store.Name
                },
                Notes = item.Notes,
                StockLedgerDetails = item.StockLedgerDetails?.Select(d => new StockLedgerDetailDto
                {
                    Id = d.Id,
                    DenominationId = d.DenominationId,
                    Denomination = d.Denomination == null ? null : new DenominationDto
                    {
                        Id = d.Denomination.Id,
                        Name = d.Denomination.Name,
                        Value = d.Denomination.Value,
                        NumberOfPiecesInABox = d.Denomination.NumberOfPiecesInABox,
                        DenominationTypeId = d.Denomination.DenominationTypeId,
                        DenominationType = d.Denomination.DenominationType == null ? null : new DenominationTypeDto
                        {
                            Id = d.Denomination.DenominationType.Id,
                            Name = d.Denomination.DenominationType.Name
                        }
                    },
                    CategoryId = d.CategoryId,
                    Category = d.Category == null ? null : new CategoryDto
                    {
                        Id = d.Category.Id,
                        Name = d.Category.Name
                    },
                    NumberOfPieces = d.NumberOfPieces,
                    NumberOfBoxes = d.NumberOfBoxes,
                    NumberOfBundles = d.NumberOfBundles,
                    Value = d.Value
                }).ToList() ?? new List<StockLedgerDetailDto>()
            };
            return Ok(result);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateStockLedger([FromBody] StockLedger item)
        {
            if (item == null)
                return BadRequest();

            try
            {
                var created = await _stockLedgerRepository.CreateAsync(item);
                var result = new StockLedgerDto
                {
                    Id = created.Id,
                    ReferenceCode = created.ReferenceCode,
                    IsIn = created.IsIn,
                    CounterPartyTypeId = created.CounterPartyTypeId,
                    CounterPartyType = created.CounterPartyType == null ? null : new CounterPartyTypeDto
                    {
                        Id = created.CounterPartyType.Id,
                        Name = created.CounterPartyType.Name
                    },
                    CounterPartyId = created.CounterPartyId,
                    StoreId = created.StoreId,
                    Store = created.Store == null ? null : new BranchDto
                    {
                        Id = created.Store.Id,
                        Name = created.Store.Name
                    },
                    Notes = created.Notes,
                    StockLedgerDetails = created.StockLedgerDetails?.Select(d => new StockLedgerDetailDto
                    {
                        Id = d.Id,
                        DenominationId = d.DenominationId,
                        Denomination = d.Denomination == null ? null : new DenominationDto
                        {
                            Id = d.Denomination.Id,
                            Name = d.Denomination.Name,
                            Value = d.Denomination.Value,
                            NumberOfPiecesInABox = d.Denomination.NumberOfPiecesInABox,
                            DenominationTypeId = d.Denomination.DenominationTypeId,
                            DenominationType = d.Denomination.DenominationType == null ? null : new DenominationTypeDto
                            {
                                Id = d.Denomination.DenominationType.Id,
                                Name = d.Denomination.DenominationType.Name
                            }
                        },
                        CategoryId = d.CategoryId,
                        Category = d.Category == null ? null : new CategoryDto
                        {
                            Id = d.Category.Id,
                            Name = d.Category.Name
                        },
                        NumberOfPieces = d.NumberOfPieces,
                        NumberOfBoxes = d.NumberOfBoxes,
                        NumberOfBundles = d.NumberOfBundles,
                        Value = d.Value
                    }).ToList() ?? new List<StockLedgerDetailDto>()
                };
                return CreatedAtAction(nameof(GetStockLedger), new { id = created.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
