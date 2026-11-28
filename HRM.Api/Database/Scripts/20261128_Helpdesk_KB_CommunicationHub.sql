-- Seed Communication Hub setup guides (WhatsApp, Telegram, MS Teams, Facebook Page, Troubleshooting)
-- into dbo.HD_HelpdeskArticles (idempotent).
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
('General', 'How to set up WhatsApp in the Communication Hub',
 'whatsapp, whatup, whats app, meta, cloud api, phone number id, whatsapp template, setup whatsapp, configure whatsapp, communication hub',
 'To configure WhatsApp notifications in GHR Communication Hub (`/admin/settings/communications`):' + CHAR(10) + CHAR(10) +
 '1. **Meta Setup**: In Meta for Developers (`developers.facebook.com`), open your Business app with **WhatsApp** enabled. Note your numeric **Phone Number ID** (under WhatsApp → API Setup) and generate a **System User Permanent Access Token** with `whatsapp_business_messaging`.' + CHAR(10) +
 '2. **Approved Template**: In Meta WhatsApp Manager, create and get an approved **Utility** template using lowercase letters, digits, and underscores only (e.g. `schedule_update_notice`), locale code (e.g. `en_US`), and **one** body placeholder `{{1}}`.' + CHAR(10) +
 '3. **Server Config (`HRM.Communications.Worker`)**: Never paste the token in the UI. On the worker server config, set:' + CHAR(10) +
 '   • `Communications:OutboundEnabled` = `true`' + CHAR(10) +
 '   • `Communications:Meta:GraphApiVersion` = `v21.0`' + CHAR(10) +
 '   • `Communications:Organisations:{OrgId}:Credentials:{CredentialReference}` = your permanent Meta token' + CHAR(10) +
 '4. **Communication Hub UI**: Select **WhatsApp**, save the **Connection** (enabled), add each opted-in employee as a **Destination** (international digits only, no `+` or spaces, e.g. `9607771234`, Individual ID, consent checked + opt-in evidence), and save an enabled **Automation Rule** (body max 1,024 characters).',
 82),

('General', 'How to set up Telegram in the Communication Hub',
 'telegram, botfather, chat id, telegram channel, telegram bot, setup telegram, configure telegram, verify bot, communication hub',
 'To configure Telegram announcements in GHR Communication Hub (`/admin/settings/communications`):' + CHAR(10) + CHAR(10) +
 '1. **Create Bot**: Message `@BotFather` on Telegram to create a bot and get its bot token. Add the bot as an administrator to your target Telegram channel or group.' + CHAR(10) +
 '2. **Server Config**: Store the bot token on the server under `Communications:Organisations:{OrgId}:Credentials:{CredentialReference}` (never paste the raw token into the web form) and ensure `Communications:OutboundEnabled` is `true` on `HRM.Communications.Worker`.' + CHAR(10) +
 '3. **Save Connection**: Select **Telegram** in Communication Hub, enter a friendly **Name** and matching **Credential reference** (e.g. `telegram-prod`), check **Enable this connection**, save, and click **Verify bot**.' + CHAR(10) +
 '4. **Save Destination & Rule**: Add the target **Chat ID** (channels/supergroups usually start with `-100`), click **Verify access**, optionally run **Send test**, then create an **Organisation-event automation** rule using `{{Title}}`, `{{Description}}`, `{{StartDateTime}}`, and `{{EndDateTime}}`.',
 84),

('General', 'How to set up Microsoft Teams in the Communication Hub',
 'teams, ms teams, microsoft teams, workflow url, power automate, teams webhook, setup teams, configure teams, communication hub',
 'To send announcements to Microsoft Teams from GHR Communication Hub (`/admin/settings/communications`):' + CHAR(10) + CHAR(10) +
 '1. **Create Teams Workflow**: In Microsoft Teams / Power Automate, create a channel **Workflow** (Incoming Webhook via Power Automate) and copy its HTTPS Workflow URL (`*.environment.api.powerplatform.com` or `*.logic.azure.com`).' + CHAR(10) +
 '2. **Server Config**: Store the HTTPS Workflow URL on the server under `Communications:Organisations:{OrgId}:Credentials:{CredentialReference}` and ensure `Communications:OutboundEnabled` is `true`.' + CHAR(10) +
 '3. **Communication Hub UI**: Select **Microsoft Teams**, save the **Connection** with your **Credential reference**, create a **Destination** keeping the identifier as `workflow`, and configure an enabled **Automation Rule**.',
 86),

('General', 'How to set up Facebook Page in the Communication Hub',
 'facebook, facebook page, fb page, page id, meta page, public event, setup facebook, configure facebook, communication hub',
 'To publish public organisation events to a Facebook Page from GHR Communication Hub (`/admin/settings/communications`):' + CHAR(10) + CHAR(10) +
 '1. **Meta Page Token**: Generate a Meta **Page Access Token** with posting permissions and note your **Numeric Facebook Page ID** (from your Page''s About → Page Transparency section).' + CHAR(10) +
 '2. **Server Config**: Set `Communications:Meta:GraphApiVersion` (e.g. `v21.0`), `Communications:OutboundEnabled = true`, and store the Page Access Token under `Communications:Organisations:{OrgId}:Credentials:{CredentialReference}`.' + CHAR(10) +
 '3. **Communication Hub UI**: Select **Facebook Page**, save the **Connection**, add the **Numeric Facebook Page ID** as a **Destination**, and save an **Automation Rule**.' + CHAR(10) +
 '4. **Important**: Only organisation events explicitly marked **Public** can be posted to Facebook, and **manual Communication Hub approval** is always required before publishing.',
 88),

('Technical Issue', 'Troubleshooting Communication Hub delivery failures',
 'communication hub, delivery failed, worker, heartbeat, outbound, credentialnotconfigured, invalidrecipientnumber, invalidwhatsappconfiguration, queued message',
 'If messages stay queued or fail in **Recent delivery history** (`/admin/settings/communications`):' + CHAR(10) + CHAR(10) +
 '1. **Check Worker Status**: Confirm the banner at the top shows **Worker: Running · Outbound enabled**. If the heartbeat is stale or outbound is disabled, start `HRM.Communications.Worker` with `Communications:OutboundEnabled = true`.' + CHAR(10) +
 '2. **Common Error Codes**:' + CHAR(10) +
 '   • `CredentialNotConfigured`: Check `Communications:Organisations:{OrgId}:Credentials:{CredentialReference}` on the worker server.' + CHAR(10) +
 '   • `InvalidWhatsAppConfiguration`: Verify Phone Number ID (digits only), lowercase template name (`a-z0-9_`), and language code (`en_US`).' + CHAR(10) +
 '   • `InvalidRecipientNumber`: Use international digits only (7–15 digits, no `+`, spaces, or dashes, e.g. `9607771234`).' + CHAR(10) +
 '   • `InvalidMetaConfiguration`: Ensure `Communications:Meta:GraphApiVersion` matches format `v21.0`.' + CHAR(10) +
 '3. **Approvals**: If a rule has **Require approval** enabled, approve the queued message at `/admin/settings/communications/approvals`.',
 90);

INSERT INTO dbo.HD_HelpdeskArticles (Category, Title, Keywords, Answer, IsActive, SortOrder, UpdatedAtUtc)
SELECT a.Category, a.Title, a.Keywords, a.Answer, 1, a.SortOrder, SYSUTCDATETIME()
FROM @Articles a
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.HD_HelpdeskArticles existing
    WHERE existing.Title = a.Title
);

SELECT COUNT(*) AS TotalActiveArticles FROM dbo.HD_HelpdeskArticles WHERE IsActive = 1;
GO
