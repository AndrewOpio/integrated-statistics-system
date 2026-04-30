IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [AgentDetails] (
        [Id] int NOT NULL IDENTITY,
        [CommercialBankBranchId] int NOT NULL,
        [BranchId] int NOT NULL,
        [AgentNames] nvarchar(max) NOT NULL,
        [NIN] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PassportPhoto] nvarchar(max) NOT NULL,
        [WorkIdScan] nvarchar(max) NOT NULL,
        [SignatureScan] nvarchar(max) NOT NULL,
        [ApprovalAttachment] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CurrentStatusId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AgentDetails] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ApprovedCurrencyProcurements] (
        [Id] int NOT NULL IDENTITY,
        [Amount] decimal(18,2) NOT NULL,
        [BoardApprovalAttachment] nvarchar(max) NOT NULL,
        [CurrentStatusId] int NOT NULL,
        [CurrentStatusName] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ApprovedCurrencyProcurements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [StockMovements] (
        [Id] int NOT NULL IDENTITY,
        [MovementType] nvarchar(max) NOT NULL,
        [Date] datetime2 NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [CurrentStatusId] int NULL,
        [CurrentStatusName] nvarchar(max) NOT NULL,
        [DispatchingBranchId] int NULL,
        [ReceivingBranchId] int NULL,
        [Categorization] nvarchar(max) NULL,
        [TotalBagsBoxes] int NULL,
        [GrandTotalValue] decimal(18,2) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [VendorTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_VendorTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [WithdrawalRequests] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionRefNumber] nvarchar(450) NULL,
        [CbId] int NOT NULL,
        [CbCode] nvarchar(max) NOT NULL,
        [BouBranchId] int NOT NULL,
        [BouBranchCode] nvarchar(max) NOT NULL,
        [CollectionDate] date NOT NULL,
        [RtgsDate] date NULL,
        [RtgsCode] nvarchar(max) NULL,
        [RequiresSpecialApproval] bit NOT NULL,
        [SpecialApprovalReason] nvarchar(max) NULL,
        [SpecialApprovalBy] nvarchar(max) NULL,
        [SpecialApprovalAt] datetime2 NULL,
        [StatusTypeId] int NOT NULL,
        [StatusTypeName] nvarchar(max) NOT NULL,
        [TotalNotesAmount] decimal(18,2) NOT NULL,
        [TotalCoinsAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WithdrawalRequests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [AgentDetailStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [AgentDetailId] int NOT NULL,
        [OldStatusId] int NOT NULL,
        [NewStatusId] int NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AgentDetailStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId] FOREIGN KEY ([AgentDetailId]) REFERENCES [AgentDetails] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ApprovedCurrencyStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [ApprovedCurrencyProcurementId] int NOT NULL,
        [OldStatusId] int NOT NULL,
        [OldStatusName] nvarchar(max) NOT NULL,
        [NewStatusId] int NOT NULL,
        [NewStatusName] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ApprovedCurrencyStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId] FOREIGN KEY ([ApprovedCurrencyProcurementId]) REFERENCES [ApprovedCurrencyProcurements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [StockMovementDenominations] (
        [Id] int NOT NULL IDENTITY,
        [DenominationId] int NOT NULL,
        [NumBagsBoxes] int NULL,
        [NumBundles] int NULL,
        [CategoryId] int NULL,
        [SubTotalValue] decimal(18,2) NOT NULL,
        [DenominationType] nvarchar(max) NOT NULL,
        [StockMovementId] int NOT NULL,
        CONSTRAINT [PK_StockMovementDenominations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovementDenominations_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [StockMovements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [StockMovementDiscrepancies] (
        [Id] int NOT NULL IDENTITY,
        [StockMovementId] int NOT NULL,
        [CurrentStatusId] int NOT NULL,
        [CurrentStatusName] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_StockMovementDiscrepancies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovementDiscrepancies_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [StockMovements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [StockMovementStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [StockMovementId] int NOT NULL,
        [OldStatusId] int NOT NULL,
        [OldStatusName] nvarchar(max) NOT NULL,
        [NewStatusId] int NOT NULL,
        [NewStatusName] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_StockMovementStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovementStatusHistory_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [StockMovements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [CurrencyVendors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Country] nvarchar(max) NOT NULL,
        [VendorTypeId] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CurrencyVendors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CurrencyVendors_VendorTypes_VendorTypeId] FOREIGN KEY ([VendorTypeId]) REFERENCES [VendorTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [WithdrawalRequestAgents] (
        [Id] uniqueidentifier NOT NULL,
        [WithdrawalRequestId] uniqueidentifier NOT NULL,
        [AgentId] int NOT NULL,
        [EnteredBy] nvarchar(450) NOT NULL,
        [AttachmentData] varbinary(max) NULL,
        [AttachmentFileName] nvarchar(max) NULL,
        [AttachmentContentType] nvarchar(max) NULL,
        [AttachmentRemarks] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WithdrawalRequestAgents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId] FOREIGN KEY ([WithdrawalRequestId]) REFERENCES [WithdrawalRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [WithdrawalRequestDenominations] (
        [Id] uniqueidentifier NOT NULL,
        [WithdrawalRequestId] uniqueidentifier NOT NULL,
        [DenominationId] int NOT NULL,
        [DenominationValue] decimal(18,2) NOT NULL,
        [DenominationTypeId] int NOT NULL,
        [Bundles] int NULL,
        [Boxes] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Origin] nvarchar(450) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WithdrawalRequestDenominations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId] FOREIGN KEY ([WithdrawalRequestId]) REFERENCES [WithdrawalRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [WithdrawalRequestStatusHistory] (
        [Id] uniqueidentifier NOT NULL,
        [WithdrawalRequestId] uniqueidentifier NOT NULL,
        [OldStatusId] int NOT NULL,
        [OldStatusName] nvarchar(max) NOT NULL,
        [NewStatusId] int NOT NULL,
        [NewStatusName] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WithdrawalRequestStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId] FOREIGN KEY ([WithdrawalRequestId]) REFERENCES [WithdrawalRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [StockMovementDenominationSerialNumberRanges] (
        [Id] int NOT NULL IDENTITY,
        [Prefix] nvarchar(max) NOT NULL,
        [Start] nvarchar(max) NOT NULL,
        [End] nvarchar(max) NOT NULL,
        [StockMovementDenominationId] int NOT NULL,
        CONSTRAINT [PK_StockMovementDenominationSerialNumberRanges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovementDenominationSerialNumberRanges_StockMovementDenominations_StockMovementDenominationId] FOREIGN KEY ([StockMovementDenominationId]) REFERENCES [StockMovementDenominations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ApprovedCurrencyDenominations] (
        [Id] int NOT NULL IDENTITY,
        [DenominationId] int NOT NULL,
        [ApprovedCurrencyId] int NOT NULL,
        [NumberOfPieces] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CurrencyVendorId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ApprovedCurrencyDenominations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId] FOREIGN KEY ([ApprovedCurrencyId]) REFERENCES [ApprovedCurrencyProcurements] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId] FOREIGN KEY ([CurrencyVendorId]) REFERENCES [CurrencyVendors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ExPrintMints] (
        [Id] int NOT NULL IDENTITY,
        [ExPrintMintReference] nvarchar(max) NOT NULL,
        [CurrencyVendorId] int NOT NULL,
        [DenominationId] int NOT NULL,
        [DeliveryDate] datetime2 NOT NULL,
        [NumberOfPieces] int NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [BoxNumberPrefix] nvarchar(max) NOT NULL,
        [BoxNumberRangeStart] int NULL,
        [BoxNumberRangeEnd] int NULL,
        [SerialNumberPrefix] nvarchar(max) NULL,
        [SerialNumberRangeStart] int NULL,
        [SerialNumberRangeEnd] int NULL,
        [Attachment] nvarchar(max) NOT NULL,
        [ApprovedCurrencyProcurementId] int NOT NULL,
        [CurrentStatusId] int NOT NULL,
        [CurrentStatusName] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [NumberOfBoxes] int NOT NULL,
        [BranchId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ExPrintMints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId] FOREIGN KEY ([ApprovedCurrencyProcurementId]) REFERENCES [ApprovedCurrencyProcurements] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ExPrintMints_CurrencyVendors_CurrencyVendorId] FOREIGN KEY ([CurrencyVendorId]) REFERENCES [CurrencyVendors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ExPrintMintStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [ExPrintMintId] int NOT NULL,
        [OldStatusId] int NOT NULL,
        [OldStatusName] nvarchar(max) NOT NULL,
        [NewStatusId] int NOT NULL,
        [NewStatusName] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ExPrintMintStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId] FOREIGN KEY ([ExPrintMintId]) REFERENCES [ExPrintMints] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ReceivedStock] (
        [Id] int NOT NULL IDENTITY,
        [ExPrintMintId] int NOT NULL,
        [DateDelivered] datetime2 NOT NULL,
        [NumberOfPieces] int NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [BoxNumberPrefix] nvarchar(max) NOT NULL,
        [BoxNumberRangeStart] int NULL,
        [BoxNumberRangeEnd] int NULL,
        [SerialNumberPrefix] nvarchar(max) NULL,
        [SerialNumberRangeStart] int NULL,
        [SerialNumberRangeEnd] int NULL,
        [CurrentStatusId] int NOT NULL,
        [CurrentStatusName] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [IsPartial] bit NOT NULL,
        [NumberOfBoxes] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ReceivedStock] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReceivedStock_ExPrintMints_ExPrintMintId] FOREIGN KEY ([ExPrintMintId]) REFERENCES [ExPrintMints] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE TABLE [ReceivedStockStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [ReceivedStockId] int NOT NULL,
        [OldStatusId] int NOT NULL,
        [OldStatusName] nvarchar(max) NOT NULL,
        [NewStatusId] int NOT NULL,
        [NewStatusName] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ReceivedStockStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId] FOREIGN KEY ([ReceivedStockId]) REFERENCES [ReceivedStock] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AgentDetailStatusHistory_AgentDetailId] ON [AgentDetailStatusHistory] ([AgentDetailId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ApprovedCurrencyDenominations_ApprovedCurrencyId] ON [ApprovedCurrencyDenominations] ([ApprovedCurrencyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ApprovedCurrencyDenominations_CurrencyVendorId] ON [ApprovedCurrencyDenominations] ([CurrencyVendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurementId] ON [ApprovedCurrencyStatusHistory] ([ApprovedCurrencyProcurementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CurrencyVendors_VendorTypeId] ON [CurrencyVendors] ([VendorTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ExPrintMints_ApprovedCurrencyProcurementId] ON [ExPrintMints] ([ApprovedCurrencyProcurementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ExPrintMints_CurrencyVendorId] ON [ExPrintMints] ([CurrencyVendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ExPrintMintStatusHistory_ExPrintMintId] ON [ExPrintMintStatusHistory] ([ExPrintMintId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReceivedStock_ExPrintMintId] ON [ReceivedStock] ([ExPrintMintId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReceivedStockStatusHistory_ReceivedStockId] ON [ReceivedStockStatusHistory] ([ReceivedStockId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_StockMovementDenominations_StockMovementId] ON [StockMovementDenominations] ([StockMovementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_StockMovementDenominationSerialNumberRanges_StockMovementDenominationId] ON [StockMovementDenominationSerialNumberRanges] ([StockMovementDenominationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_StockMovementDiscrepancies_StockMovementId] ON [StockMovementDiscrepancies] ([StockMovementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_StockMovementStatusHistory_StockMovementId] ON [StockMovementStatusHistory] ([StockMovementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequestAgents_WithdrawalRequestId_EnteredBy] ON [WithdrawalRequestAgents] ([WithdrawalRequestId], [EnteredBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequestDenominations_WithdrawalRequestId_Origin] ON [WithdrawalRequestDenominations] ([WithdrawalRequestId], [Origin]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequests_CbId] ON [WithdrawalRequests] ([CbId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequests_CbId_BouBranchId_CollectionDate] ON [WithdrawalRequests] ([CbId], [BouBranchId], [CollectionDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequests_CollectionDate] ON [WithdrawalRequests] ([CollectionDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequests_StatusTypeId] ON [WithdrawalRequests] ([StatusTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WithdrawalRequests_TransactionRefNumber] ON [WithdrawalRequests] ([TransactionRefNumber]) WHERE [TransactionRefNumber] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WithdrawalRequestStatusHistory_WithdrawalRequestId] ON [WithdrawalRequestStatusHistory] ([WithdrawalRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260402100743_InitialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260402100743_InitialMigration', N'8.0.23');
END;
GO

COMMIT;
GO

