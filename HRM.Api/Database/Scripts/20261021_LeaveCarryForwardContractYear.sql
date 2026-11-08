-- Preserve existing decisions; new decisions record the closing contract year
-- and distinguish its final earned credit from the new year's entitlement.
IF COL_LENGTH('dbo.LCF_LeaveCarryForwards','PeriodStart') IS NULL
    ALTER TABLE dbo.LCF_LeaveCarryForwards ADD PeriodStart date NULL;
IF COL_LENGTH('dbo.LCF_LeaveCarryForwards','PeriodEnd') IS NULL
    ALTER TABLE dbo.LCF_LeaveCarryForwards ADD PeriodEnd date NULL;
IF COL_LENGTH('dbo.LCF_LeaveCarryForwards','ClosingGrant') IS NULL
    ALTER TABLE dbo.LCF_LeaveCarryForwards ADD ClosingGrant decimal(10,2) NULL;
IF COL_LENGTH('dbo.LCF_LeaveCarryForwards','RenewalGrant') IS NULL
    ALTER TABLE dbo.LCF_LeaveCarryForwards ADD RenewalGrant decimal(10,2) NULL;
IF COL_LENGTH('dbo.LCF_LeaveCarryForwards','NewBalance') IS NULL
    ALTER TABLE dbo.LCF_LeaveCarryForwards ADD NewBalance decimal(10,2) NULL;
