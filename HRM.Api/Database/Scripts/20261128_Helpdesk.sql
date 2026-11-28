-- Cross-Organisation Helpdesk (Bot-First + Human Handover)
-- Prefix: HD_ per TableNaming.md

IF OBJECT_ID('dbo.HD_HelpdeskTickets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskTickets
    (
        Id                         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HD_HelpdeskTickets PRIMARY KEY,
        TicketNumber               NVARCHAR(32)         NOT NULL,
        OrganisationId             INT                  NOT NULL,
        RequesterUserId            INT                  NOT NULL,
        Subject                    NVARCHAR(200)        NOT NULL,
        Category                   NVARCHAR(60)         NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_Category DEFAULT ('General'),
        Status                     NVARCHAR(32)         NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_Status DEFAULT ('BotActive'),
        Priority                   NVARCHAR(20)         NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_Priority DEFAULT ('Normal'),
        AssignedAgentUserId        INT                  NULL,
        EscalationReason           NVARCHAR(300)        NULL,
        BotUnresolvedTurns         INT                  NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_BotUnresolvedTurns DEFAULT (0),
        Rating                     INT                  NULL,
        CreatedAtUtc               DATETIME2(7)         NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc               DATETIME2(7)         NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        EscalatedAtUtc             DATETIME2(7)         NULL,
        ResolvedAtUtc              DATETIME2(7)         NULL,
        RequesterLastReadMessageId BIGINT               NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_RequesterLastRead DEFAULT (0),
        AgentLastReadMessageId     BIGINT               NOT NULL CONSTRAINT DF_HD_HelpdeskTickets_AgentLastRead DEFAULT (0),
        CONSTRAINT FK_HD_HelpdeskTickets_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_HD_HelpdeskTickets_Requester FOREIGN KEY (RequesterUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_HD_HelpdeskTickets_AssignedAgent FOREIGN KEY (AssignedAgentUserId) REFERENCES dbo.Users(UserID)
    );

    CREATE UNIQUE INDEX UX_HD_HelpdeskTickets_TicketNumber ON dbo.HD_HelpdeskTickets(TicketNumber);
    CREATE INDEX IX_HD_HelpdeskTickets_Requester_Updated ON dbo.HD_HelpdeskTickets(RequesterUserId, UpdatedAtUtc DESC);
    CREATE INDEX IX_HD_HelpdeskTickets_Status_Updated ON dbo.HD_HelpdeskTickets(Status, UpdatedAtUtc DESC);
    CREATE INDEX IX_HD_HelpdeskTickets_Org_Status ON dbo.HD_HelpdeskTickets(OrganisationId, Status);
END
GO

IF OBJECT_ID('dbo.HD_HelpdeskMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskMessages
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HD_HelpdeskMessages PRIMARY KEY,
        TicketId         BIGINT               NOT NULL,
        SenderType       NVARCHAR(16)         NOT NULL,
        SenderUserId     INT                  NULL,
        SenderName       NVARCHAR(150)        NOT NULL,
        Body             NVARCHAR(4000)       NOT NULL,
        IsInternalNote   BIT                  NOT NULL CONSTRAINT DF_HD_HelpdeskMessages_IsInternalNote DEFAULT (0),
        MatchedArticleId INT                  NULL,
        Confidence       DECIMAL(5,4)         NULL,
        SentAtUtc        DATETIME2(7)         NOT NULL CONSTRAINT DF_HD_HelpdeskMessages_SentAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HD_HelpdeskMessages_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.HD_HelpdeskTickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HD_HelpdeskMessages_Sender FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(UserID)
    );

    CREATE INDEX IX_HD_HelpdeskMessages_Ticket_Id ON dbo.HD_HelpdeskMessages(TicketId, Id);
END
GO

IF OBJECT_ID('dbo.HD_HelpdeskArticles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskArticles
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HD_HelpdeskArticles PRIMARY KEY,
        Category     NVARCHAR(60)      NOT NULL,
        Title        NVARCHAR(200)     NOT NULL,
        Keywords     NVARCHAR(500)     NOT NULL,
        Answer       NVARCHAR(4000)    NOT NULL,
        IsActive     BIT               NOT NULL CONSTRAINT DF_HD_HelpdeskArticles_IsActive DEFAULT (1),
        SortOrder    INT               NOT NULL CONSTRAINT DF_HD_HelpdeskArticles_SortOrder DEFAULT (0),
        UpdatedAtUtc DATETIME2(7)      NOT NULL CONSTRAINT DF_HD_HelpdeskArticles_UpdatedAtUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_HD_HelpdeskArticles_Active_Category ON dbo.HD_HelpdeskArticles(IsActive, Category, SortOrder);
END
GO

-- Seed default starter Knowledge Base articles if table is empty
IF NOT EXISTS (SELECT 1 FROM dbo.HD_HelpdeskArticles)
BEGIN
    INSERT INTO dbo.HD_HelpdeskArticles (Category, Title, Keywords, Answer, IsActive, SortOrder, UpdatedAtUtc)
    VALUES
    ('Leave & Attendance', 'How to apply for leave or cancel a draft leave',
     'leave, apply leave, annual leave, sick leave, cancel leave, draft leave, balance, vacation',
     'To apply for leave, go to **Leave** (`/online/leaves`) in the Employee Portal, click **New Leave Application**, select your leave type and dates, and submit it for approval.' + CHAR(10) + CHAR(10) +
     'To cancel an unsubmitted draft or pending leave request, open **My Leave Applications**, switch to the **Drafts** or **Pending** filter, and click **Cancel** on the request. Your leave balance is only deducted for approved leaves.',
     1, 10, SYSUTCDATETIME()),

    ('Leave & Attendance', 'Missing attendance punch, late check-in, or out-of-office request',
     'attendance, punch, check in, check out, biometric, late, late check-in, out of office, ooo, missing punch, time log',
     'If you missed a biometric punch, arrived late, or were working out of office, open **Attendance** (`/online/attendance`) from the left menu.' + CHAR(10) + CHAR(10) +
     'From there you can submit a **Late Check-In Request**, **Out-of-Office Request**, or attendance correction directly to your supervisor and HR for verification.',
     1, 20, SYSUTCDATETIME()),

    ('Leave & Attendance', 'Submitting and tracking overtime (OT) requests',
     'overtime, ot, pre-approval, extra hours, overtime request, ot claim',
     'Open **Overtime** (`/online/ot`) in the Employee Portal to submit an Overtime Pre-Approval or Overtime Claim. Select the date, hours, and justification, then submit it for supervisor and HR approval.',
     1, 30, SYSUTCDATETIME()),

    ('Payroll & Payslips', 'Viewing and downloading monthly payslips',
     'payslip, salary, pay slip, payroll, allowance, deduction, bank, pay, download payslip',
     'You can view and download your monthly payslips anytime from **My Payslips** (`/online/payslips`). For allowances, deductions, or goods loans, check **Allowances & Deductions** (`/online/allowances-deductions`) or **Goods Loans** (`/online/goods-loans`).' + CHAR(10) + CHAR(10) +
     'If you spot a discrepancy in your salary calculation, click **Talk to a Real Person** below so a Helpdesk Officer can check with your organisation''s HR/Payroll team.',
     1, 40, SYSUTCDATETIME()),

    ('Requests & Approvals', 'Requesting office items, assets, or HR certificates',
     'item request, asset, certificate, employment letter, salary letter, document, request status, approval, pending',
     '• **Items & Supplies**: Submit via **Item Requests** (`/online/item-requests`).' + CHAR(10) +
     '• **Employment / Salary Certificates**: Request via **Certificates** (`/online/certificates`).' + CHAR(10) +
     '• **My Assets & Documents**: View assigned items under **My Assets** (`/online/my-assets`) and **My Documents** (`/online/documents`).' + CHAR(10) + CHAR(10) +
     'Each page shows the live approval stage and approver history for your requests.',
     1, 50, SYSUTCDATETIME()),

    ('Account & Access', 'Portal access, roles, permissions, or wrong organisation shown',
     'login, access, permission, admin portal, role, organisation, wrong org, cannot open, unauthorized, forbidden, switch portal',
     'Your active organisation in GHR is determined automatically from your **active approved job**.' + CHAR(10) + CHAR(10) +
     'If you recently transferred organisations, need **Admin Portal** access, or receive a permission error (`Unauthorized`), click **Talk to a Real Person** so a Helpdesk Officer can verify your active job and role assignments.',
     1, 60, SYSUTCDATETIME()),

    ('Technical Issue', 'Reporting a system error or page not loading',
     'bug, error, crash, broken, not loading, unavailable, 500, failed, technical issue, slow, stuck',
     'Sorry you ran into an issue! Please try a quick browser refresh (`Ctrl + F5`) first. If the error continues, share:' + CHAR(10) +
     '1. The exact page URL or menu name' + CHAR(10) +
     '2. What action you were performing' + CHAR(10) +
     '3. Any error message shown on screen' + CHAR(10) + CHAR(10) +
     'Click **Talk to a Real Person** anytime to escalate this directly to a Helpdesk Officer.',
     1, 70, SYSUTCDATETIME());
END
GO
