-- Expand GHR Helpdesk Knowledge Base with full-platform module guides (idempotent)
SET NOCOUNT ON;

DECLARE @Articles TABLE
(
    Category  NVARCHAR(60)   NOT NULL,
    Title     NVARCHAR(200)  NOT NULL,
    Keywords  NVARCHAR(500)  NOT NULL,
    Answer    NVARCHAR(4000) NOT NULL,
    SortOrder INT            NOT NULL
);

INSERT INTO @Articles (Category, Title, Keywords, Answer, SortOrder)
VALUES
('Leave & Attendance', 'Viewing your Duty Roster and work shifts',
 'duty roster, roster, shift, schedule, working hours, timetable, off day, weekend shift, night shift',
 'Open **Duty Roster** (`/online/duty-roster`) in the Employee Portal to view your assigned shifts, off days, and monthly schedule.' + CHAR(10) + CHAR(10) +
 'If your shift is missing or needs to be swapped, contact your supervisor or department HR through **Staff Chat** (`/online/chat`).',
 25),

('Leave & Attendance', 'Assigning or accepting Leave Work Handovers',
 'handover, work handover, leave handover, cover, acting during leave, colleague handover, approve handover',
 'When a leave type requires a work handover, you select a colleague to cover your duties while submitting your leave.' + CHAR(10) + CHAR(10) +
 'To review or accept handover requests assigned to you, open **Work Handovers** (`/online/leave-handovers`) in the Employee Portal. Your leave moves forward in the approval chain once the handover is accepted.',
 28),

('Payroll & Payslips', 'Checking Allowances and Deductions',
 'allowance, allowances, deduction, deductions, pension, phone allowance, risk allowance, attendance deduction',
 'Open **Allowances & Deductions** (`/online/allowances-deductions`) in the Employee Portal to view your active allowances, recurring deductions, effective dates, and amounts linked to your current job.',
 42),

('Payroll & Payslips', 'Viewing and tracking Goods Loans',
 'loan, goods loan, installment, deduction loan, balance loan, repayment',
 'Open **Goods Loans** (`/online/goods-loans`) to view active goods loans, installment schedules, deductions taken so far, and remaining balances.',
 44),

('Requests & Approvals', 'Viewing assigned Assets and Equipment',
 'asset, assets, laptop, monitor, equipment, inventory, my assets, return asset, assigned item',
 'Open **My Assets** (`/online/my-assets`) to view all organisation assets currently assigned to you, including asset tags, handover dates, and condition records.' + CHAR(10) + CHAR(10) +
 'To request a new item or supply, submit a request under **Item Requests** (`/online/item-requests`).',
 52),

('Requests & Approvals', 'Accessing My Documents and Position Contracts',
 'document, documents, contract, position contract, appointment letter, employment contract, download document, pdf',
 'Open **My Documents** (`/online/documents`) to view and download your HR records and letters.' + CHAR(10) + CHAR(10) +
 'To view or sign your employment / position contracts, open **Position Contracts** (`/online/documents?tab=contracts`).',
 54),

('Account & Access', 'Setting up or updating your Digital Signature',
 'signature, digital signature, e-signature, sign document, draw signature, upload signature',
 'Open **My Signature** (`/online/signature`) in the Employee Portal to draw or upload your digital signature. Once saved, it can be used to sign contracts, certificates, and official HR approvals.',
 62),

('Requests & Approvals', 'Requesting a Staff Transfer between departments or organisations',
 'transfer, staff transfer, change department, move organisation, relocation, transfer request',
 'Open **Transfers** (`/online/staff-transfers`) in the Employee Portal to view or submit a staff transfer request. Once submitted, the request goes through your current organisation and receiving organisation HR approval workflow.',
 56),

('Requests & Approvals', 'Submitting a Resignation, Retirement, or Staff Separation request',
 'resign, resignation, retire, retirement, separation, termination, leave job, notice period, exit, offboarding',
 'Open **Separation** (`/online/staff-separation`) in the Employee Portal to submit a resignation or retirement request and track clearance steps.' + CHAR(10) + CHAR(10) +
 'Note: Once a staff member is officially separated/terminated, all roles and Admin Portal access are automatically revoked while historical records remain read-only.',
 58),

('General', 'Browsing and applying for Internal Vacancies (Recruitment)',
 'vacancy, vacancies, recruitment, internal job, apply job, promotion, open position, career',
 'Open **Internal Vacancies** (`/online/recruitment`) in the Employee Portal to browse open internal positions across participating organisations and submit applications online.',
 75),

('General', 'Leadership and Acting Appointments',
 'leadership, acting, head in charge, hod, director, acting appointment',
 'If you hold leadership authority or an acting appointment, open **Leadership** (`/online/leadership`) in the Employee Portal to view active leadership assignments and approvals.',
 78),

('General', 'Using Staff Chat for direct and group messages',
 'chat, staff chat, message colleague, group chat, attach request, supervisor chat, hr chat',
 'Open **Chat** (`/online/chat`) to send direct messages or create group chats with colleagues, HR staff, and your supervisors within your organisation.' + CHAR(10) + CHAR(10) +
 'You can also click the paperclip icon inside a conversation to attach an open HR request reference directly to your message.',
 80),

('Account & Access', 'How Admin Portal access and HR Requests Inbox work',
 'admin portal, hr inbox, requests inbox, approve request, hr staff, review leave, hr manager, switch to admin',
 'Staff granted **Admin Portal** access can switch to the Admin Portal using the portal switcher at the bottom of the left sidebar.' + CHAR(10) + CHAR(10) +
 'In the Admin Portal, **My Workspace** (`/admin/my/workspace`) and **Requests Inbox** (`/admin/hr/requests`) show all organisation requests requiring HR review or approval (drafts are automatically hidden until submitted).',
 64),

('Requests & Approvals', 'How request approvals work (Supervisor, HOD, and HR stages)',
 'approval, approve, pending approval, workflow, waiting for supervisor, waiting for hr, rejected, who approves',
 'HR requests in GHR follow your organisation''s configured approval workflow (typically **Supervisor → Head of Department → HR Verification → HR Approval**).' + CHAR(10) + CHAR(10) +
 'You can see the exact current stage and action history by opening the request from its Employee Portal page (Leave, Overtime, Attendance, Item Requests, or Certificates).',
 59),

('Account & Access', 'SuperAdmin Portal: Organisations, Departments, and Roles',
 'superadmin, onboard organisation, add department, add staff to department, roles, permissions, access control',
 'Platform SuperAdministrators manage cross-organisation configuration under **SuperAdmin Portal** (`/superadmin`):' + CHAR(10) +
 '• **Organisation Onboarding** (`/superadmin/organisations`): Create organisations, manage departments, and add unassigned individuals as department staff.' + CHAR(10) +
 '• **Roles & Access Control** (`/superadmin/roles`): Configure role permissions, assign/remove staff from roles, and manage access scopes.' + CHAR(10) +
 '• **Helpdesk Support** (`/superadmin/helpdesk`): Monitor bot chats, take over escalated tickets, and update the Knowledge Base.',
 66);

INSERT INTO dbo.HD_HelpdeskArticles (Category, Title, Keywords, Answer, IsActive, SortOrder, UpdatedAtUtc)
SELECT a.Category, a.Title, a.Keywords, a.Answer, 1, a.SortOrder, SYSUTCDATETIME()
FROM @Articles a
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.HD_HelpdeskArticles existing
    WHERE existing.Title = a.Title
);

SELECT COUNT(*) AS TotalArticles FROM dbo.HD_HelpdeskArticles WHERE IsActive = 1;
GO
