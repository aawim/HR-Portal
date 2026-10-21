-- Preserve existing decisions; new decisions record the closing contract year
-- and distinguish its final earned credit from the new year's entitlement.
IF COL_LENGTH('dbo.LeaveCarryForwards','PeriodStart') IS NULL
    ALTER TABLE dbo.LeaveCarryForwards ADD PeriodStart date NULL;
IF COL_LENGTH('dbo.LeaveCarryForwards','PeriodEnd') IS NULL
    ALTER TABLE dbo.LeaveCarryForwards ADD PeriodEnd date NULL;
IF COL_LENGTH('dbo.LeaveCarryForwards','ClosingGrant') IS NULL
    ALTER TABLE dbo.LeaveCarryForwards ADD ClosingGrant decimal(10,2) NULL;
IF COL_LENGTH('dbo.LeaveCarryForwards','RenewalGrant') IS NULL
    ALTER TABLE dbo.LeaveCarryForwards ADD RenewalGrant decimal(10,2) NULL;
IF COL_LENGTH('dbo.LeaveCarryForwards','NewBalance') IS NULL
    ALTER TABLE dbo.LeaveCarryForwards ADD NewBalance decimal(10,2) NULL;
