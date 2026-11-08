-- Migration: Add missing fields to IM_ItemStaffRequestLines (InventoryNumber, SerialNumber, Condition, ExpectedReturnDate, Remarks, ItemId, ItemUnitId)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'InventoryNumber')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD InventoryNumber nvarchar(100) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'SerialNumber')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD SerialNumber nvarchar(120) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'Condition')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD Condition nvarchar(30) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'ExpectedReturnDate')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD ExpectedReturnDate date NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'Remarks')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD Remarks nvarchar(500) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'ItemId')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD ItemId int NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines') AND name = 'ItemUnitId')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD ItemUnitId int NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IM_ItemStaffRequestLines_IM_Items_ItemId')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD CONSTRAINT FK_IM_ItemStaffRequestLines_IM_Items_ItemId
        FOREIGN KEY (ItemId) REFERENCES dbo.IM_Items (ItemId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IM_ItemStaffRequestLines_IM_ItemUnits_ItemUnitId')
BEGIN
    ALTER TABLE dbo.IM_ItemStaffRequestLines ADD CONSTRAINT FK_IM_ItemStaffRequestLines_IM_ItemUnits_ItemUnitId
        FOREIGN KEY (ItemUnitId) REFERENCES dbo.IM_ItemUnits (ItemUnitId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IM_ItemStaffRequestLines_ItemId' AND object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines'))
BEGIN
    CREATE INDEX IX_IM_ItemStaffRequestLines_ItemId ON dbo.IM_ItemStaffRequestLines (ItemId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IM_ItemStaffRequestLines_ItemUnitId' AND object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines'))
BEGIN
    CREATE INDEX IX_IM_ItemStaffRequestLines_ItemUnitId ON dbo.IM_ItemStaffRequestLines (ItemUnitId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IM_ItemStaffRequestLines_InventoryNumber' AND object_id = OBJECT_ID(N'dbo.IM_ItemStaffRequestLines'))
BEGIN
    CREATE INDEX IX_IM_ItemStaffRequestLines_InventoryNumber ON dbo.IM_ItemStaffRequestLines (InventoryNumber) WHERE InventoryNumber IS NOT NULL;
END;
