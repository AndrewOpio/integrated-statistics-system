SELECT details.Id, details.CreatedDate, ledgerRecord.StoreId BranchId, ledgerRecord.ReferenceCode, 
denom.Name DenominationName, details.NumberOfBoxes, details.NumberOfBundles, details.NumberOfPieces, 
category.Name CategoryName, subcategory.Name SubCategoryName, ledgerRecord.CounterPartyId, 
ledgerRecord.CounterPartyTypeId, counterPartyType.Name CounterPartyName, ledgerRecord.IsIn, 
details.Value, ledgerRecord.StockMovementId, ledgerRecord.Notes FROM [dbo].[StockLedgerDetails] details 
JOIN [dbo].[StockLedger] ledgerRecord ON ledgerRecord.Id = details.StockLedgerId
JOIN [dbo].[Denominations] denom ON details.DenominationId = denom.Id
JOIN [dbo].[Categories] category ON details.CategoryId = category.Id
LEFT JOIN [dbo].[SubCategories] subcategory ON details.SubCategoryId = subcategory.Id
JOIN [dbo].[CounterPartyTypes] counterPartyType ON ledgerRecord.CounterPartyTypeId = counterPartyType.Id;

