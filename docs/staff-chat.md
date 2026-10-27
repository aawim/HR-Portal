# Staff chat

Open **Chat** in either portal sidebar or the employee/login menu (`/online/chat`, also `/admin/chat`). Search active staff in the current organisation, or filter HR / My supervisors. HR labels use the existing HR / Human Resource User Groups; supervisor suggestions use the existing department and team supervisor assignments. Chat does not require admin portal access.

Select a colleague for a direct chat, or choose **New group**, name the group and select colleagues (up to 50 members including yourself). A direct conversation can also be converted under **Make this a group**, retaining its ID and messages. Both original participants become group managers when converting. New messages and unread counts refresh every five seconds. The notification bell shows a chat alert linked to the conversation; unread alerts are coalesced per conversation. Background browser tabs do not mark newly polled messages read. Reading the latest messages clears its unread count and associated chat alert. Older messages can be loaded in pages of 50.

Use **Attach a request reference** to search and select a request before sending. All Request Types are supported while `RequestStates.IsFinalState` is false, including Incomplete. Use the open-status and submitted-date filters to find a request. Approved, Cancelled and Rejected requests cannot be newly attached. A stale selection is rechecked when sending. Previously sent references remain in chat history after the request becomes final. The sender must be authorised to reference the request. Other conversation members can see the shared reference as information. Only the requester and current supervisor can open the protected request-detail view; this is rechecked on every load. Attaching a reference grants no permission to the underlying workflow.

Conversations are private to their current members; administrators and developers do not receive a blanket read bypass. Every operation checks the signed-in context, current tenant, active approved employment and current approved position. Recipients must remain active to receive messages. Messages are plain text (1–4,000 characters), displayed with normal Razor escaping. The server limits sending to 30 messages per minute and uses a unique client key for retry protection. There is no message deletion or editing, keeping attribution/history intact.

New physical tables:

- `CH_Conversations`: Id, OrganisationId, FirstUserId, SecondUserId, FirstReadMessageId, SecondReadMessageId, CreatedAtUtc, UpdatedAtUtc. The participant pair is ordered and unique per tenant.
- `CH_Messages`: Id, ConversationId, SenderUserId, ClientMessageId, Body, RequestId (optional), SentAtUtc. User/request/conversation references are restrictive.

Existing `SN_StaffNotifications` supplies notification bell delivery. Migration `20261026100840_StaffChat` is additive; it does not seed or send any live messages, assign permissions or alter request workflows. The deployment utility's SQL smoke sends messages only inside a verified isolated restored test database, then drops that test database. No external messaging provider is used.

## Group membership and retained history

Open the conversation's **Members** section. Managers can rename the group, add colleagues from the staff search, remove members and make other members managers. Members can leave; the last active manager must appoint another active manager before leaving or being demoted. There is no administrator/developer bypass into private conversations.

Removal closes a membership interval instead of deleting it. All messages, authors and request references remain stored. Removed members lose list/read/send/reference-search access and stop receiving new message alerts. New members see messages sent after they joined, protecting earlier private/direct discussions. Rejoining members regain the messages from their previous membership periods and their new period, but cannot see messages sent during their absence. Becoming a manager does not expand message-history visibility.

Request references require sender authorisation at send time. A newly joined member cannot read earlier references through the message-history endpoint. Other members receive reference-only information; private links remain individually requester/supervisor-checked when read.

Migration `20261027030613_StaffChatGroups` adds `IsGroup` and `Title` to `CH_Conversations`. The original FirstUserId/SecondUserId pair remains as historical/direct-chat identity data; it is never an authorisation fallback. The direct-pair unique index now applies only to direct conversations. Existing direct members and unread cursors are backfilled without changing messages.

- `CH_Memberships`: Id, ConversationId, UserId, IsManager, JoinedAtUtc, AddedByUserId, JoinedAfterMessageId, LastReadMessageId, LeftAtUtc, RemovedByUserId, LeftAfterMessageId. Each rejoin creates a new row; a filtered unique index permits only one current membership per user and conversation.
- `CH_Events`: Id, ConversationId, ActorUserId, SubjectUserId, Action, Detail, CreatedAtUtc. Group creation, conversion, renaming, member and manager changes are retained as events.

No history is cascade-deleted. The down migration refuses destructive rollback; use the reviewed pre-upgrade backup. Membership notices use existing staff notifications, and message alerts fan out only to current eligible members.

## Informational references and private request details

A sender authorised to reference an open request may share its reference number and request type with any current chat members. Recipients do not need request permissions to see that informational card. This does not grant request access. Other members see **Information only**, with no request-detail URL.

Only the requester and their current department/team supervisor receive a link to the protected chat request view at `/online/chat/{conversationId}/requests/{requestId}`. The server enforces this separately from HR, administrator or chat-manager privileges, even when a URL is pasted directly. The viewer must also be a current conversation member and have a visible message containing the reference. Removal, join-date boundaries and supervisor changes are rechecked. The view is read-only and exposes live request metadata and supported leave/OT details to those eligible viewers. It does not change workflow approval permissions elsewhere in the application.

Previously shared reference cards remain informational history even after a recipient loses request-view permissions or the request becomes final. New attachments still require sender authorisation, the same tenant and a non-final state. New group members cannot use the detail endpoint to bypass their message-history boundary.

Find Request combines open-status and Submitted from/to filters. Date bounds include the whole selected Maldives local day and are converted to UTC ApplicationDate bounds on the server. Either date can be used alone. Final states are never selectable, and Reset filters restores all open requests available to the sender.

The default sidebar list is **Your conversations** and contains current memberships only. The staff directory is collapsed under **New chat or add members**. Bell links for chats/invitations are also membership-filtered, including read notifications, so removed/unrelated conversations do not continue to appear there. Personal removal notices are retained without a conversation deep link.

The attachment picker UI uses status and submitted-date filters only; changes apply automatically. Available requests load when the conversation opens, and Reset filters restores the default open-request list. The text search box and Find button have been removed.
