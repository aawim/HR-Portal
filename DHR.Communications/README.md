# Communication Hub development status

The projects belong to `HRM.sln`. Worker/UI compilation and a targeted run of 46 Communications tests plus four shared attendance-workflow regression tests have passed. SQL integration and live-provider verification remain outstanding. Other platforms shown in the UI are placeholders.

## Worker configuration

Configure the worker's existing HRM SQL Server connection using `ConnectionStrings__DefaultConnection`. Provision credentials through the deployment secret store or protected environment; do not commit tokens to configuration files.

A connection whose credential reference is `telegram-primary` resolves its bot token from `Communications__Credentials__telegram-primary`. The UI's **Verify bot** action calls Telegram `getMe` through HRM.Api and requires that reference to be configured securely in the API host as well. **Verify access** on a saved destination checks the bot identity, chat type and membership/posting rights through `getMe`, `getChat` and `getChatMember`. These read-only checks neither send a message nor enable the connection or destination. Worker secret configuration remains separate; permissions can change after verification.

Outbound sending is disabled by default. Set `Communications__OutboundEnabled=true` only after credentials, destinations and routing are verified. Connections, destinations and automation rules must also be explicitly enabled. Rules requiring approval create held message snapshots visible in the administration page. **Submit for approval** creates a central Request linked to the snapshot and starts the existing workflow engine in the same SQL transaction. Configure a request type named `Communication Announcement` and its organisation/Job Type flow and state transitions first. Set its view URL to `/online/communication-approvals`. The submitting administrator requires active approved employment. Missing setup rolls submission back.

## Announcement decisions

Assigned reviewers and submitters use `/online/communication-approvals`; notification links for the Communication Announcement request type open the specific request on this page. Reviewers do not need integration administrator rights. The server uses the shared workflow processor to enforce current assignments, independent approvers, sequential/All/Any/Quorum stage behaviour, allowed request-state transitions and workflow audit records. Submitters can cancel their own pending request. Rejection and cancellation require a comment. Duplicate, stale and closed decisions are rejected.

Only final approval marks the saved message Approved and creates one Pending delivery to the saved destination, within the same SQL transaction as the workflow decision. Intermediate approvals do not create deliveries. Release rechecks organisation, source publication/audience, expiry and destination availability. Destination ID, connection ID and external chat ID are snapshotted when the held message is created; changed or missing snapshots prevent release. Older held messages lacking those fields must be recreated. Reject/cancel marks the held message accordingly and creates no delivery. The worker still observes its outbound switch and rechecks source/destination eligibility before sending.

Workflow tests use EF InMemory to verify business behaviour; they do not prove SQL locking, migration execution, transaction rollback or multi-worker concurrency. Those remain integration-test requirements.

For an explicit delivery test, select **Send test** on a saved destination and confirm the named destination. This queues a fixed test announcement through the worker; it does not call Telegram synchronously from the API. The destination and connection must be enabled. Requests are limited to one per minute per destination and become ineligible after ten minutes. No automation rule is required for this explicit test; the worker's global outbound switch still applies. Follow the result in delivery history. No live test has been executed as part of automated verification.

Set `Communications__OutboundEnabled=false` and restart the worker to stop new sends. A request already accepted by the provider cannot be recalled. Internal HRM notifications are not controlled by this switch.

Routing continues while outbound sending is disabled: eligible events can create pending deliveries, which will send when the switch is enabled. Review the queue before enabling. Events with no eligible rule are marked processed. Pending deliveries expire after `Communications:DeliveryMaxAgeHours` (default 24, range 1–168). Sends interrupted for more than ten minutes are marked failed with `IndeterminateWorkerInterruption`; review these rather than resetting them blindly.

## Current limitations

The router processes every enabled rule with an explicitly selected destination. Rules requiring approval are held; other rules generate deliveries immediately. Each rule has one destination; configure multiple rules for multiple destinations. Only explicitly organisation-wide source events qualify, with audience and source status checked again before dispatch. Department, team, individual and excluded audiences are skipped. Do not use organisation-wide destinations for private announcements. Facebook Pages, Messenger, WhatsApp and Teams dispatch are not implemented.

Build verification uses `--artifacts-path tmp/comms-verify2` to avoid access-denied errors in existing project `obj` files. SQL concurrency, migration execution and end-to-end business flows still require testing before production use.
