# Communication Hub development status

The projects belong to `HRM.sln`. The worker and UI have passed compilation and 16 focused template, transport and mocked Telegram tests pass. SQL integration and live-provider verification remain outstanding. Other platforms shown in the UI are placeholders.

## Worker configuration

Configure the worker's existing HRM SQL Server connection using `ConnectionStrings__DefaultConnection`. Provision credentials through the deployment secret store or protected environment; do not commit tokens to configuration files.

A connection whose credential reference is `telegram-primary` resolves its bot token from `Communications__Credentials__telegram-primary`. The UI's **Verify bot** action calls Telegram `getMe` through HRM.Api and requires that reference to be configured securely in the API host as well. **Verify access** on a saved destination checks the bot identity, chat type and membership/posting rights through `getMe`, `getChat` and `getChatMember`. These read-only checks neither send a message nor enable the connection or destination. Worker secret configuration remains separate; permissions can change after verification.

Outbound sending is disabled by default. Set `Communications__OutboundEnabled=true` only after credentials, destinations and routing are verified. Connections, destinations and automation rules must also be explicitly enabled. Rules requiring approval are currently skipped because the approval queue is not implemented.

For an explicit delivery test, select **Send test** on a saved destination and confirm the named destination. This queues a fixed test announcement through the worker; it does not call Telegram synchronously from the API. The destination and connection must be enabled. Requests are limited to one per minute per destination and become ineligible after ten minutes. No automation rule is required for this explicit test; the worker's global outbound switch still applies. Follow the result in delivery history. No live test has been executed as part of automated verification.

Set `Communications__OutboundEnabled=false` and restart the worker to stop new sends. A request already accepted by the provider cannot be recalled. Internal HRM notifications are not controlled by this switch.

Routing continues while outbound sending is disabled: eligible events can create pending deliveries, which will send when the switch is enabled. Review the queue before enabling. Events with no eligible rule are marked processed. Pending deliveries expire after `Communications:DeliveryMaxAgeHours` (default 24, range 1–168). Sends interrupted for more than ten minutes are marked failed with `IndeterminateWorkerInterruption`; review these rather than resetting them blindly.

## Current limitations

The router processes every enabled rule that does not require approval and has an explicitly selected destination. Each rule has one destination; configure multiple rules for multiple destinations. Only explicitly organisation-wide source events qualify, with audience and source status checked again before dispatch. Department, team, individual and excluded audiences are skipped. Do not use organisation-wide destinations for private announcements. Facebook Pages, Messenger, WhatsApp and Teams dispatch are not implemented.

Build verification uses `--artifacts-path tmp/comms-verify2` to avoid access-denied errors in existing project `obj` files. SQL concurrency, migration execution and end-to-end business flows still require testing before production use.
