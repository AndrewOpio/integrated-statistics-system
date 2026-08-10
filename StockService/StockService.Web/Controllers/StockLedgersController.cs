using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockService.Core.Models.CounterPartyTypes;
using StockService.Core.Models.Denominations;
using StockService.Core.Models.DenominationTypes;
using StockService.Core.Models.StockLedger;
using StockService.Core.Repository.StockLedgers;
using StockService.Data.Entities;
using StockService.Core.Models.Categories;
using StockService.Core.Models.SubCategories;

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
            return Ok(items.Select(MapToDto));
        }

        // Branch-scoped lookup — filters at the SQL level so callers that only need one
        // branch's position (e.g. the stock position matrix) don't pay to fetch and decrypt
        // every ledger in the system.
        [HttpGet]
        [Route("get-by-store/{storeId}")]
        public async Task<IActionResult> GetStockLedgersByStore(int storeId)
        {
            var items = await _stockLedgerRepository.FindByStoreIdAsync(storeId);
            return Ok(items.Select(MapToDto));
        }

        private static StockLedgerDto MapToDto(StockLedger sl) => new StockLedgerDto
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
            StoreId = sl.StoreId,
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
                Value = decimal.TryParse(d.Value, out decimal val) ? val : 0,
                SubCategoryId = d.SubCategoryId ?? 0,
                SubCategory = d.SubCategory == null ? null : new SubCategoryDto
                {
                    Id = d.SubCategory.Id,
                    Name = d.SubCategory.Name
                }
            }).ToList() ?? new List<StockLedgerDetailDto>()
        };

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetStockLedger(int id)
        {
            var item = await _stockLedgerRepository.FindByIdAsync(id);
            if (item == null)
                return NotFound();

            return Ok(MapToDto(item));
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateStockLedger([FromBody] CreateStockLedgerDto itemDto)
        {
            if (itemDto == null)
                return BadRequest(new { message = "item is required" });

            try
            {
                // Convert DTO to entity
                var item = new StockLedger
                {
                    ReferenceCode = itemDto.ReferenceCode,
                    StockMovementId = itemDto.StockMovementId,
                    IsIn = itemDto.IsIn,
                    CounterPartyTypeId = itemDto.CounterPartyTypeId,
                    CounterPartyId = itemDto.CounterPartyId,
                    StoreId = itemDto.StoreId,
                    Notes = itemDto.Notes,
                    StockLedgerDetails = itemDto.StockLedgerDetails?.Select(d => new StockLedgerDetail
                    {
                        DenominationId = d.DenominationId,
                        CategoryId = d.CategoryId,
                        NumberOfPieces = d.NumberOfPieces,
                        NumberOfBoxes = d.NumberOfBoxes,
                        NumberOfBundles = d.NumberOfBundles,
                        Value = d.Value.ToString(), // Convert decimal to string
                        SubCategoryId = d.SubCategoryId
                    }).ToList() ?? new List<StockLedgerDetail>()
                };

                var created = await _stockLedgerRepository.CreateAsync(item);
                return CreatedAtAction(nameof(GetStockLedger), new { id = created.Id }, MapToDto(created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
