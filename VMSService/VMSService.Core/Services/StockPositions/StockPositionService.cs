using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Static;

namespace VMSService.Core.Services.StockPositions
{
    public class StockPositionService(
        VMSServiceDbContext _context,
        IStockService _stockService,
        ILogger<StockPositionService> _logger) : IStockPositionService
    {

        public async Task<bool> UpdatePositionAsync(string entityType, string statusName, object entity)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogWarning("Invalid entity for position update");
                    return false;
                }

                StockLedger stockLedger = null;

                // Handle stock movement updates (Dispatch or Receipt)
                if (entity is StockMovement movement && (statusName == StatusConstants.StockMovement.Dispatched || statusName == StatusConstants.StockMovement.Received))
                {
                    var dbMovement = await _context.StockMovements
                        .Include(m => m.Denominations)
                        .FirstOrDefaultAsync(m => m.Id == movement.Id);

                    if (dbMovement == null)
                    {
                        _logger.LogWarning("Stock Movement {Id} not found", movement.Id);
                        return false;
                    }

                    stockLedger = statusName == StatusConstants.StockMovement.Dispatched
                        ? await CreateDispatchLedgerAsync(dbMovement)
                        : await CreateReceiptLedgerAsync(dbMovement);
                }
                // Handle discrepancy resolution
                else if (entity is StockMovementDiscrepancy discrepancy && statusName == StatusConstants.StockMovementDiscrepancy.DiscrepancyResolved)
                {
                    var dbDiscrepancy = await _context.StockMovementDiscrepancies
                        .Include(d => d.StockMovement)
                            .ThenInclude(m => m.Denominations)
                        .FirstOrDefaultAsync(d => d.Id == discrepancy.Id);

                    if (dbDiscrepancy?.StockMovement == null)
                    {
                        _logger.LogWarning("Discrepancy {Id} not found", discrepancy.Id);
                        return false;
                    }

                    var discrepancyLedgers = await CreateDiscrepancyAdjustmentLedgerAsync(dbDiscrepancy);
                    
                    // Handle multiple ledgers from discrepancy adjustment
                    if (discrepancyLedgers == null || !discrepancyLedgers.Any())
                    {
                        _logger.LogWarning("Unable to create discrepancy ledgers for entity type {EntityType} and status {Status}", 
                            entityType, statusName);
                        return false;
                    }

                    // Save all discrepancy ledgers
                    foreach (var ledger in discrepancyLedgers)
                    {
                        var discrepancyResult = await _stockService.CreateStockLedger(ledger);
                        if (discrepancyResult == null)
                        {
                            _logger.LogError("Failed to create discrepancy stock ledger");
                            return false;
                        }
                    }

                    _logger.LogInformation("Stock position updated: {EntityType} - {Status}", entityType, statusName);
                    return true;
                }

                if (stockLedger == null)
                {
                    _logger.LogWarning("Unable to create stock ledger for entity type {EntityType} and status {Status}", 
                        entityType, statusName);
                    return false;
                }

                // Call external stock service to create ledger
                var result = await _stockService.CreateStockLedger(stockLedger);

                if (result != null)
                {
                    _logger.LogInformation("Stock position updated: {EntityType} - {Status}", entityType, statusName);
                    return true;
                }

                _logger.LogError("Failed to create stock ledger");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock position for {EntityType}", entityType);
                return false;
            }
        }

        private async Task<StockLedger> CreateDispatchLedgerAsync(StockMovement movement)
        {
            var sentDenominations = movement.Denominations
                .Where(d => d.DenominationType == "Sent")
                .ToList();

            var counterPartyTypeId = await GetCounterPartyTypeIdAsync("Branch");
            var storeId = await GetInTransitBranchCategoryIdAsync(movement.ReceivingBranchId ?? 0);

            var stockLedger = new StockLedger
            {
                ReferenceCode = $"DISPATCH-{movement.ReferenceNumber}",
                IsIn = false, // Outgoing from sending branch
                CounterPartyTypeId = counterPartyTypeId,
                CounterPartyId = storeId,
                StoreId = movement.DispatchingBranchId ?? 0,
                Notes = $"Dispatch for Stock Movement {movement.ReferenceNumber}",
                StockMovementId = movement.Id,
                StockLedgerDetails = new List<StockLedgerDetail>()
            };

            foreach (var denom in sentDenominations)
            {
                stockLedger.StockLedgerDetails.Add(new StockLedgerDetail
                {
                    DenominationId = denom.DenominationId,
                    CategoryId = denom.CategoryId ?? 0,
                    NumberOfPieces = (denom.NumBundles ?? 0) * 1000,
                    NumberOfBoxes = denom.NumBagsBoxes ?? 0,
                    NumberOfBundles = denom.NumBundles ?? 0,
                    Value = denom.SubTotalValue
                });
            }

            return stockLedger;
        }

        private async Task<StockLedger> CreateReceiptLedgerAsync(StockMovement movement)
        {
            var receivedDenominations = movement.Denominations
                .Where(d => d.DenominationType == "Received")
                .ToList();

            var counterPartyTypeId = await GetCounterPartyTypeIdAsync("Branch");
            var storeId = await GetInTransitBranchCategoryIdAsync(movement.ReceivingBranchId ?? 0);
            
            var stockLedger = new StockLedger
            {
                ReferenceCode = $"RECEIPT-{movement.ReferenceNumber}",
                IsIn = true, // Incoming to receiving branch
                CounterPartyTypeId = counterPartyTypeId,
                CounterPartyId = storeId,
                StoreId = movement.ReceivingBranchId ?? 0,
                Notes = $"Receipt for Stock Movement {movement.ReferenceNumber}",
                StockMovementId = movement.Id,
                StockLedgerDetails = new List<StockLedgerDetail>()
            };

            foreach (var denom in receivedDenominations)
            {
                stockLedger.StockLedgerDetails.Add(new StockLedgerDetail
                {
                    DenominationId = denom.DenominationId,
                    CategoryId = denom.CategoryId ?? 0,
                    NumberOfPieces = (denom.NumBundles ?? 0) * 1000,
                    NumberOfBoxes = denom.NumBagsBoxes ?? 0,
                    NumberOfBundles = denom.NumBundles ?? 0,
                    Value = denom.SubTotalValue
                });
            }

            return stockLedger;
        }

        private async Task<List<StockLedger>> CreateDiscrepancyAdjustmentLedgerAsync(StockMovementDiscrepancy discrepancy)
        {
            var ledgers = new List<StockLedger>();

            var sentDenominations = discrepancy.StockMovement.Denominations
                .Where(d => d.DenominationType == "Sent")
                .ToList();

            var receivedDenominations = discrepancy.StockMovement.Denominations
                .Where(d => d.DenominationType == "Received")
                .ToList();

            // Get denomination IDs that were sent vs received
            var sentDenomIds = sentDenominations.Select(d => d.DenominationId).Distinct().ToList();
            var receivedDenomIds = receivedDenominations.Select(d => d.DenominationId).Distinct().ToList();

            // Find mismatches
            var missingDenoms = sentDenomIds.Except(receivedDenomIds).ToList();      // Sent but not received
            var extraDenoms = receivedDenomIds.Except(sentDenomIds).ToList();        // Received but not sent

            var counterPartyTypeId = await GetCounterPartyTypeIdAsync("Branch");
            var inTransitStoreId = await GetInTransitBranchCategoryIdAsync(discrepancy.StockMovement.ReceivingBranchId ?? 0);

            // Handle denomination mismatches
            if (missingDenoms.Any() || extraDenoms.Any())
            {
                // Credit back the sending branch for missing denominations
                if (missingDenoms.Any())
                {
                    var creditLedger = new StockLedger
                    {
                        ReferenceCode = $"DISC-CREDIT-{discrepancy.StockMovement.ReferenceNumber}",
                        IsIn = true, // Credit: returning to sending branch
                        CounterPartyTypeId = counterPartyTypeId,
                        CounterPartyId = discrepancy.StockMovement.ReceivingBranchId ?? 0,
                        StoreId = discrepancy.StockMovement.DispatchingBranchId ?? 0,
                        Notes = $"Reversal for missing denominations in Stock Movement {discrepancy.StockMovement.ReferenceNumber}",
                        StockMovementId = discrepancy.StockMovement.Id,
                        StockLedgerDetails = new List<StockLedgerDetail>()
                    };

                    // Add entries for each missing denomination
                    foreach (var missingDenomId in missingDenoms)
                    {
                        var sentDenom = sentDenominations.FirstOrDefault(d => d.DenominationId == missingDenomId);
                        if (sentDenom != null)
                        {
                            creditLedger.StockLedgerDetails.Add(new StockLedgerDetail
                            {
                                DenominationId = missingDenomId,
                                CategoryId = sentDenom.CategoryId ?? 0,
                                NumberOfPieces = (sentDenom.NumBundles ?? 0) * 1000,
                                NumberOfBoxes = sentDenom.NumBagsBoxes ?? 0,
                                NumberOfBundles = sentDenom.NumBundles ?? 0,
                                Value = sentDenom.SubTotalValue
                            });
                        }
                    }

                    ledgers.Add(creditLedger);
                }

                // Debit receiving branch for extra denominations
                if (extraDenoms.Any())
                {
                    var debitLedger = new StockLedger
                    {
                        ReferenceCode = $"DISC-DEBIT-{discrepancy.StockMovement.ReferenceNumber}",
                        IsIn = false, // Debit: received extra
                        CounterPartyTypeId = counterPartyTypeId,
                        CounterPartyId = discrepancy.StockMovement.ReceivingBranchId ?? 0,
                        StoreId = discrepancy.StockMovement.DispatchingBranchId ?? 0,
                        Notes = $"Extra denominations received in Stock Movement {discrepancy.StockMovement.ReferenceNumber}",
                        StockMovementId = discrepancy.StockMovement.Id,
                        StockLedgerDetails = new List<StockLedgerDetail>()
                    };

                    // Add entries for each extra denomination
                    foreach (var extraDenomId in extraDenoms)
                    {
                        var receivedDenom = receivedDenominations.FirstOrDefault(d => d.DenominationId == extraDenomId);
                        if (receivedDenom != null)
                        {
                            debitLedger.StockLedgerDetails.Add(new StockLedgerDetail
                            {
                                DenominationId = extraDenomId,
                                CategoryId = receivedDenom.CategoryId ?? 0,
                                NumberOfPieces = (receivedDenom.NumBundles ?? 0) * 1000,
                                NumberOfBoxes = receivedDenom.NumBagsBoxes ?? 0,
                                NumberOfBundles = receivedDenom.NumBundles ?? 0,
                                Value = receivedDenom.SubTotalValue
                            });
                        }
                    }

                    ledgers.Add(debitLedger);
                }
            }

            // Handle quantity differences for matching denominations
            var matchingDenomIds = sentDenomIds.Intersect(receivedDenomIds).ToList();
            if (matchingDenomIds.Any())
            {
                var creditDetailsForQuantity = new List<StockLedgerDetail>();
                var debitDetailsForQuantity = new List<StockLedgerDetail>();

                foreach (var denomId in matchingDenomIds)
                {
                    var sentDenom = sentDenominations.FirstOrDefault(d => d.DenominationId == denomId);
                    var receivedDenom = receivedDenominations.FirstOrDefault(d => d.DenominationId == denomId);

                    if (sentDenom != null && receivedDenom != null)
                    {
                        var sentValue = sentDenom.SubTotalValue;
                        var receivedValue = receivedDenom.SubTotalValue;
                        var valueDifference = receivedValue - sentValue;

                        if (valueDifference != 0)
                        {
                            if (valueDifference < 0)
                            {
                                // Received less than sent - Credit the dispatching branch
                                creditDetailsForQuantity.Add(new StockLedgerDetail
                                {
                                    DenominationId = denomId,
                                    CategoryId = sentDenom.CategoryId ?? 0,
                                    NumberOfPieces = (sentDenom.NumBundles ?? 0) * 1000 - (receivedDenom.NumBundles ?? 0) * 1000,
                                    NumberOfBoxes = (sentDenom.NumBagsBoxes ?? 0) - (receivedDenom.NumBagsBoxes ?? 0),
                                    NumberOfBundles = (sentDenom.NumBundles ?? 0) - (receivedDenom.NumBundles ?? 0),
                                    Value = Math.Abs(valueDifference)
                                });
                            }
                            else if (valueDifference > 0)
                            {
                                // Received more than sent - Debit the dispatching branch
                                debitDetailsForQuantity.Add(new StockLedgerDetail
                                {
                                    DenominationId = denomId,
                                    CategoryId = receivedDenom.CategoryId ?? 0,
                                    NumberOfPieces = (receivedDenom.NumBundles ?? 0) * 1000 - (sentDenom.NumBundles ?? 0) * 1000,
                                    NumberOfBoxes = (receivedDenom.NumBagsBoxes ?? 0) - (sentDenom.NumBagsBoxes ?? 0),
                                    NumberOfBundles = (receivedDenom.NumBundles ?? 0) - (sentDenom.NumBundles ?? 0),
                                    Value = valueDifference
                                });
                            }
                        }
                    }
                }

                // Create credit ledger for quantity shortages
                if (creditDetailsForQuantity.Any())
                {
                    var creditLedger = new StockLedger
                    {
                        ReferenceCode = $"DISC-QTY-CREDIT-{discrepancy.StockMovement.ReferenceNumber}",
                        IsIn = true, // Credit: returning to sending branch
                        CounterPartyTypeId = counterPartyTypeId,
                        CounterPartyId = discrepancy.StockMovement.ReceivingBranchId ?? 0,
                        StoreId = discrepancy.StockMovement.DispatchingBranchId ?? 0,
                        Notes = $"Credit for quantity shortages in Stock Movement {discrepancy.StockMovement.ReferenceNumber}",
                        StockMovementId = discrepancy.StockMovement.Id,
                        StockLedgerDetails = creditDetailsForQuantity
                    };
                    ledgers.Add(creditLedger);
                }

                // Create debit ledger for quantity overages
                if (debitDetailsForQuantity.Any())
                {
                    var debitLedger = new StockLedger
                    {
                        ReferenceCode = $"DISC-QTY-DEBIT-{discrepancy.StockMovement.ReferenceNumber}",
                        IsIn = false, // Debit: received extra
                        CounterPartyTypeId = counterPartyTypeId,
                        CounterPartyId = discrepancy.StockMovement.ReceivingBranchId ?? 0,
                        StoreId = discrepancy.StockMovement.DispatchingBranchId ?? 0,
                        Notes = $"Debit for quantity overages in Stock Movement {discrepancy.StockMovement.ReferenceNumber}",
                        StockMovementId = discrepancy.StockMovement.Id,
                        StockLedgerDetails = debitDetailsForQuantity
                    };
                    ledgers.Add(debitLedger);
                }
            }

            return ledgers;
        }


        private async Task<int> GetCounterPartyTypeIdAsync(string counterPartyTypeName)
        {
            try
            {
                var counterPartyTypes = await _stockService.GetAllCounterPartyTypes();
                var counterPartyType = counterPartyTypes.FirstOrDefault(c => c.Name == counterPartyTypeName);
                
                if (counterPartyType != null)
                {
                    return counterPartyType.Id;
                }

                _logger.LogWarning("Counter party type '{Name}' not found, using default ID 1", counterPartyTypeName);
                return 1; // Fallback to default
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type '{Name}'", counterPartyTypeName);
                return 1; // Fallback to default
            }
        }

        private async Task<int> GetInTransitBranchCategoryIdAsync(int branchId)
        {
            try
            {
                // First, get all categories to find the IN TRANSIT category ID
                var categories = await _stockService.GetAllCategories();
                var inTransitCategory = categories.FirstOrDefault(c => 
                    c.Name.Trim().Equals("IN TRANSIT", StringComparison.OrdinalIgnoreCase));
                    
                if (inTransitCategory == null)
                {
                    _logger.LogError("IN TRANSIT category not found in Stock Service");
                    throw new InvalidOperationException("IN TRANSIT category not found");
                }

                // Now get all branch categories and find the one with matching branch and category
                var branchCategories = await _stockService.GetAllBranchCategories();
                var inTransitBranchCategory = branchCategories.FirstOrDefault(bc =>
                    bc.BranchId == branchId &&
                    bc.Category?.Id == inTransitCategory.Id);

                if (inTransitBranchCategory != null)
                {
                    return inTransitBranchCategory.Id;
                }

                _logger.LogError("IN TRANSIT BranchCategory not found for branch {BranchId} with category ID {CategoryId}", branchId, inTransitCategory.Id);
                throw new InvalidOperationException($"IN TRANSIT BranchCategory not found for branch {branchId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IN TRANSIT branch category for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<int> GetStockPositionAsync(int branchCategoryId, int denominationId)
        {
            try
            {
                // Get all stock ledgers for the branch and denomination
                var allLedgers = await _stockService.GetAllStockLedgers();

                var incomingQuantity = allLedgers
                    .Where(l => l.StoreId == branchCategoryId && l.IsIn)
                    .SelectMany(l => l.StockLedgerDetails)
                    .Where(d => d.DenominationId == denominationId)
                    .Sum(d => d.NumberOfPieces);

                var outgoingQuantity = allLedgers
                    .Where(l => l.StoreId == branchCategoryId && !l.IsIn)
                    .SelectMany(l => l.StockLedgerDetails)
                    .Where(d => d.DenominationId == denominationId)
                    .Sum(d => d.NumberOfPieces);

                var position = incomingQuantity - outgoingQuantity;

                _logger.LogInformation("Stock position for Branch {BranchId}, Denomination {DenominationId}: {Position}",
                    branchCategoryId, denominationId, position);

                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock position");
                return 0;
            }
        }

        public async Task<Dictionary<int, Dictionary<int, decimal>>> GetStockMatrixAsync(int branchId, List<int> categoryIds, List<int> denominationIds)
        {
            try
            {
                var matrix = new Dictionary<int, Dictionary<int, decimal>>();

                // Get all stock ledgers once
                var allLedgers = await _stockService.GetAllStockLedgers();

                // Build the matrix: denomination -> category -> value
                // Using only the filtered categories and denominations
                foreach (var denominationId in denominationIds)
                {
                    matrix[denominationId] = new Dictionary<int, decimal>();

                    foreach (var categoryId in categoryIds)
                    {
                        // Sum the values for incoming ledgers (IsIn = true)
                        var incomingValue = allLedgers?
                            .Where(sl => sl.IsIn == true && sl.StockLedgerDetails != null && sl.StoreId == branchId)
                            .SelectMany(sl => sl.StockLedgerDetails)
                            .Where(sld => sld.DenominationId == denominationId && sld.CategoryId == categoryId)
                            .Sum(sld => sld.Value) ?? 0;

                        // Sum the values for outgoing ledgers (IsIn = false)
                        var outgoingValue = allLedgers?
                            .Where(sl => sl.IsIn == false && sl.StockLedgerDetails != null && sl.StoreId == branchId)
                            .SelectMany(sl => sl.StockLedgerDetails)
                            .Where(sld => sld.DenominationId == denominationId && sld.CategoryId == categoryId)
                            .Sum(sld => sld.Value) ?? 0;

                        // Calculate net position
                        var netValue = incomingValue - outgoingValue;

                        if (netValue > 0)
                        {
                            _logger.LogInformation("Denomination {DenomId}: Category {CatId} = Incoming {Incoming} - Outgoing {Outgoing} = {Net}", 
                                denominationId, categoryId, incomingValue, outgoingValue, netValue);
                        }

                        matrix[denominationId][categoryId] = netValue;
                    }
                }

                return matrix;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock matrix for branch {BranchId}", branchId);
                return new Dictionary<int, Dictionary<int, decimal>>();
            }
        }

        public async Task<bool> CreateReimbursementLedgerAsync(MonthlyShortageReimbursement reimbursement)
        {
            try
            {
                var categories        = await _stockService.GetAllCategories();
                var counterPartyTypes = await _stockService.GetAllCounterPartyTypes();

                var cfbCategory = categories?.FirstOrDefault(c =>
                    c.Name.Equals(StockConstants.StockCategories.Cfb, StringComparison.OrdinalIgnoreCase));

                var branchCounterPartyType = counterPartyTypes?.FirstOrDefault(c =>
                    c.Name.Equals(StockConstants.StockCounterPartyType.Branch, StringComparison.OrdinalIgnoreCase));

                var ledger = new StockLedger
                {
                    ReferenceCode      = $"REIMB-{reimbursement.ReferenceNumber}",
                    IsIn               = true,
                    StockMovementId    = 0,
                    CounterPartyTypeId = branchCounterPartyType?.Id ?? 0,
                    CounterPartyId     = reimbursement.BranchId,
                    StoreId            = reimbursement.BranchId,
                    Notes              = $"Shortage reimbursement approved for {reimbursement.ReferenceNumber}",
                    StockLedgerDetails = reimbursement.Denominations.Select(d => new StockLedgerDetail
                    {
                        DenominationId  = d.DenominationId,
                        CategoryId      = cfbCategory?.Id ?? 0,
                        NumberOfPieces  = d.NumberOfPieces,
                        NumberOfBoxes   = 0,
                        NumberOfBundles = d.NumberOfPieces / 1000,
                        Value           = d.Amount,
                        SubCategoryId   = 0,
                    }).ToList(),
                };

                var created = await _stockService.CreateStockLedger(ledger);
                if (!created)
                    _logger.LogWarning("Stock ledger creation returned false for reimbursement {Ref}", reimbursement.ReferenceNumber);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting stock ledger for reimbursement {Ref}", reimbursement.ReferenceNumber);
                return false;
            }
        }
    }
}