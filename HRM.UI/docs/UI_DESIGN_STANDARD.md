# HRM.UI Design Standard

## 1. Official Design Reference

The **Item Requests** page (`/online/item-requests`, implemented primarily in `HRM.UI/Components/Admin/Hr/Items/StaffItemRequestPanel.razor` and hosted via `ItemStaffRequestsPage.razor`) is the official visual and interaction benchmark for all current and future `HRM.UI` pages.

Whenever creating, redesigning, or modifying any page or feature component in `HRM.UI`:
- Use `/online/item-requests` as the primary reference for layout, hierarchy, and micro-interactions.
- Maintain consistent header, button, form, panel, card, table, badge, and Full Width / Shrink View behaviors.
- Ensure outer wrappers and root section components apply consistent `p-2` outer padding.
- Do not introduce conflicting styling or alternate CSS frameworks. Work strictly within `HRM.UI`.

---

## 2. Page & Component Container Padding (`p-2` Rule)

All primary page containers and root feature section components must include standard `p-2` padding to ensure consistent spacing across viewports:

- **Page Host Container**:
  ```html
  <div class="mx-auto min-w-7xl max-w-full p-2 flex flex-col gap-2">
      <!-- Child Section / Feature Component -->
  </div>
  ```
- **Feature Section Root Component**:
  ```html
  <section class="page-name w-full min-w-0 p-2 space-y-4">
      <!-- Full-Width Sticky Header & Panels -->
  </section>
  ```

---

## 3. Header Layout & Requirements

- **Compact, Transparent, Borderless Header**:
  - Page and section headers must be **compact** with **no background fill, no border, no box shadow, and no card wrapper**.
  - Layout: `flex flex-wrap items-center justify-between gap-2 px-1 py-0.5` (transparent directly on the page canvas).
  - Do not add a redundant `← Back` link inside standard module headers.
- **Header Elements**:
  - Title: `text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100 truncate`.
  - Context / Status Badge (inline beside title): compact pill (`inline-flex items-center gap-1 rounded-full bg-teal-50 dark:bg-teal-950/60 border border-teal-200/80 dark:border-teal-800 px-2 py-0.5 text-[10px] font-semibold text-teal-700 dark:text-teal-300`) with a `h-1.5 w-1.5 rounded-full` status dot.
  - Subtitle / Description: `text-xs text-slate-500 dark:text-slate-400`.
  - Actions Container: `flex flex-wrap items-center gap-1.5` using compact `app-button-sm` controls:
    - **Full Width / Shrink View** toggle (`app-button app-button-neutral app-button-sm` with `fullscreen` / `fullscreen_exit` icon).
    - Secondary actions (`app-button app-button-secondary app-button-sm`: Refresh, Print / Report, Export).
    - Primary action (`app-button app-button-primary app-button-sm`: cobalt blue `#2563eb`).
- **Canonical Header Snippet**:
  ```html
  <header class="flex flex-wrap items-center justify-between gap-2 px-1 py-0.5">
      <div class="min-w-0">
          <div class="flex items-center gap-2 flex-wrap">
              <h1 class="text-lg font-bold text-slate-900 dark:text-slate-100 truncate">Page Title</h1>
              <span class="inline-flex items-center gap-1 rounded-full bg-teal-50 dark:bg-teal-950/60 border border-teal-200/80 dark:border-teal-800 px-2 py-0.5 text-[10px] font-semibold text-teal-700 dark:text-teal-300">
                  <span class="h-1.5 w-1.5 rounded-full bg-teal-500"></span>
                  Context
              </span>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">Concise one-line description.</p>
      </div>
      <div class="flex flex-wrap items-center gap-1.5">
          <button type="button" class="app-button app-button-neutral app-button-sm">Full Width</button>
          <button type="button" class="app-button app-button-secondary app-button-sm">Refresh</button>
          <button type="button" class="app-button app-button-primary app-button-sm">New Item</button>
      </div>
  </header>
  ```

---

## 4. Standard vs Full Width View Layout

### The 2-Panel Grid System
The standard multi-panel layout uses CSS Grid with explicit minimum sizing to avoid content blowing out the viewport or collapsing panels unexpectedly:

```html
<div class="page-panels @(isFullWidth ? "page-panels-full" : "page-panels-standard")">
    <!-- LEFT PANEL: Operations, Forms, or Selected Details -->
    <div class="page-panel-left min-w-0 rounded-2xl border border-slate-200 bg-white p-4 shadow-xs flex flex-col gap-3">
        ...
    </div>

    <!-- RIGHT PANEL: History, Summary Table, or Card List -->
    <aside class="page-panel-right min-w-0 rounded-2xl border border-slate-200 bg-white p-4 shadow-xs flex flex-col gap-3">
        ...
    </aside>
</div>
```

### Grid Styling Rules
```css
.page-panels {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    align-items: start;
    gap: 1rem;
    width: 100%;
    min-width: 0;
    padding: 0;
    margin-inline: auto;
    transition: max-width .3s ease;
    box-sizing: border-box;
}

.page-panels-standard {
    max-width: 80rem; /* max-w-7xl equivalent */
}

.page-panels-full {
    max-width: none;
}

.page-panel-left, .page-panel-right {
    min-width: 0;
    width: 100%;
    box-sizing: border-box;
}

@media (min-width: 1024px) {
    .page-panels {
        grid-template-columns: minmax(0, 7fr) minmax(0, 5fr);
    }
}
```

### Full Width / Shrink View Persistence
- Store preference in browser `localStorage` using a feature-specific key (e.g., `hrm.ot.full-width` or `hrm.item-management.full-width`).
- On initial render (`firstRender == true`), read the saved preference and call `StateHasChanged()`.
- On toggle: flip `isFullWidth` and update `localStorage`.
- Toggle button icon and text:
  - Shrink: 4 arrows pointing inward + `Shrink View`
  - Full: 4 corner arrows pointing outward + `Full Width`

---

## 5. Design Tokens & Component Patterns

### Buttons
- Use the shared `app-button` classes so controls retain the same size, focus state and colour meaning in both themes.
- **Primary Action**: `app-button app-button-primary app-button-sm`
- **Secondary Action**: `app-button app-button-secondary app-button-sm`
- **Neutral Action**: `app-button app-button-neutral app-button-sm`
- **Subtle / Ghost**: `app-button app-button-ghost app-button-sm`
- **Success / Positive**: `app-button app-button-success app-button-sm`
- **Danger / Destructive**: `app-button app-button-danger app-button-sm`
- **Icon-only Action**: add `app-button-icon` and always provide `aria-label` and `title`.
- Do not use layout-wide CSS to add spacing or wrapping to arbitrary buttons. Action containers own their `gap-*` and wrapping classes.
- **Segmented / Pill Tab Buttons**:
  - Container: `flex rounded-lg border border-slate-300 p-0.5 bg-slate-50`
  - Active: `bg-white text-blue-700 shadow-xs`
  - Inactive: `text-slate-600 hover:text-slate-900`

### Forms & Input Controls
- **Labels & Sub-headings**:
  - Field Labels: `block text-xs font-medium text-slate-700 p-1` (or `font-semibold text-slate-800 p-1`). Standardize on `p-1` for labels and subheadings to prevent `p-0` tightness above input controls.
  - Sub-card Headings: `font-bold text-slate-800 p-1`
- **Input / Select / Textarea**:
  - Base: `mt-1 w-full rounded-lg border border-slate-300 bg-white p-1 text-xs text-slate-900 placeholder:text-slate-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500`
  - Add explicit dark variants when editing a component: `dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100`.
- **Validation & Error Banners**:
  - Alert: `flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800`
- **Sub-cards / Fieldsets within Forms**:
  - Highlighted sub-card: `rounded-xl border border-blue-200 bg-blue-50/40 p-3.5 flex flex-col gap-2.5`
  - Neutral sub-card: `rounded-xl border border-slate-200 bg-slate-50/70 p-3 flex flex-col gap-2`

### Cards & Panels
- **Container Panel**: `rounded-2xl border border-slate-200 bg-white p-4 shadow-xs`
- New shared surfaces should also use `app-surface` or explicit `dark:` variants; do not add more global colour overrides.
- **Selectable History Item Cards**:
  - Base: `rounded-xl border p-2.5 text-xs transition cursor-pointer hover:shadow-xs flex flex-col gap-1.5 border-slate-200 bg-white hover:border-slate-300`
  - Active / Selected: `border-blue-500 bg-blue-50/70 shadow-xs ring-1 ring-blue-500`
- **Empty State**:
  - `rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-xs text-slate-500`

### Tables
- Container: `overflow-x-auto rounded-xl border border-slate-200`
- Table: `w-full text-left text-xs`
- Head: `bg-slate-50 text-slate-700 font-semibold border-b border-slate-200` with cells `p-2.5`
- Body: `divide-y divide-slate-100` with rows `hover:bg-slate-50/50` and cells `p-2.5`

### Status Badges
Consistent badge classes based on status semantic:
- **Pending / Submitted**: `bg-amber-100 text-amber-800 rounded-full px-2.5 py-0.5 font-semibold text-[11px]`
- **Approved / In Use / Active / Processed**: `bg-emerald-100 text-emerald-800 rounded-full px-2.5 py-0.5 font-semibold text-[11px]`
- **Rejected / Cancelled / Declined**: `bg-rose-100 text-rose-800 rounded-full px-2.5 py-0.5 font-semibold text-[11px]`
- **Returned / Archived**: `bg-purple-100 text-purple-800 rounded-full px-2.5 py-0.5 font-semibold text-[11px]`
- **Neutral / Informational**: `bg-slate-100 text-slate-700 rounded-full px-2 py-0.5 text-[11px] font-medium` or `bg-blue-50 text-blue-700`
- **Monospace Code / Reference Badges**:
  - `font-mono text-[11px] font-semibold text-blue-700 bg-blue-50 border border-blue-200 px-2 py-0.5 rounded-md`

### Icons
- Use `<AppIcon Name="..." />`, backed by the locally hosted Bootstrap Icons font.
- Do not add emoji, text glyphs, Material Symbols, Font Awesome classes, copied inline SVGs, or new CDN icon dependencies.
- Decorative icons remain hidden from assistive technology. Icon-only controls require an accessible label on the control.

---

## 6. Enterprise Portal & Workspace Visual Inventory

### Shell, Surface & Color System
- **Overall Layout**: Fixed deep-teal left sidebar (`#008b89`-like), slim white top bar with thin bottom border, and a full-width content workspace with stacked cards and tab strips.
- **Page Surface**: Very light cool-gray/off-white (`#f5f7fa`-like) canvas behind white content panels.
- **Main Sidebar**: Deep teal (`#008b89`-like) with white line icons (14–18px), subtle reduced-opacity horizontal dividers, tiny bold uppercase muted-white section titles, ~14px menu labels with 12–14px vertical padding, right-aligned collapse chevrons, lighter turquoise rounded active item, and a compact three-option segmented portal switcher at the bottom (brighter teal active pill).
- **Top Bar**: White with thin bottom border, compact date/time on the right, icon-only utilities, circular avatar, user name, and muted role metadata.
- **Typography**: Clean sans-serif (Inter/system sans). Dark navy/slate headings (`22–24px` page titles, `20–22px` hero titles), muted gray subtitles/descriptions (`12–13px`), high density without heavy decoration.
- **Spacing & Elevation**: `8px` micro-gaps, `12–16px` control/card padding, `20–24px` separation between major sections. Hierarchy relies on thin borders (`#d9e1ea` / `#e5e7eb`-like), fill contrast, and alignment rather than heavy shadows. Avoid gradients, glass effects, oversized type, or decorative illustrations.

### Workspace Hierarchy & Patterns
1. **Page Heading & Workspace Actions**: Dark navy/slate bold title + muted subtitle on the left; compact outlined icon-led utility buttons on the right (including a green-tinted workspace tools control where applicable).
2. **Organisation / Context Identity Card**: White card (`12–14px` radius, thin light-gray border), circular mint avatar with initials, bold organisation/context name, tiny mint context badge, muted ID/description line, and right-aligned outlined close/destructive-style button.
3. **Organisation / Contextual Tabs**: Horizontal scrollable row of compact tab pills above a `2–3px` teal baseline rule. Inactive tabs use pale mint/white backgrounds, dark teal text, and thin teal-adjacent borders; active tabs use saturated teal fill with white bold text.
4. **Primary Hero Card**: White rounded card with generous horizontal padding, two small pill badges above the title (blue-violet system/brand label + mint verification/status label) with tiny muted metadata beside them, strong dark title, muted multi-line description, and right-aligned controls (`Full Width`, `Refresh`, and cobalt-blue primary action).
5. **Sub-Navigation & Inline KPI Bar**: White bordered container with compact flat tabs and small amber count badges on the left, and inline KPI metrics separated by dot bullets (`·`) with dark values and tiny colored status dots on the right.
6. **Record / Content Cards**: Large white rounded containers with fine light-gray borders, header chips for document/record number, status, and classification, strong dark title, muted inline metadata with bullets, outlined preview and cobalt-blue download buttons, and a thin divider above actions.
7. **Inset Action Toolbars & Upload/Version Panels**: Very pale-gray rounded inset strips (`rounded-xl`) with small, compact, icon-led semantic buttons:
   - **Primary / Publish / Download / Info**: Cobalt-blue fill, white bold label (`7–9px` radius).
   - **Secondary**: White fill, `1px` light-gray border, dark slate text.
   - **Approve / Success**: Emerald/teal fill.
   - **Neutral** (Sign / Check-out / Archive): Neutral outline style.
   - **Destructive / Disposal**: White or pale-red background with muted crimson text/border (reserve solid red only for final destructive confirmation).
   - **Disabled Controls**: Desaturated blue-gray.
8. **Secondary Segmented Tabs**: Small bordered white segmented buttons with subtle blue/teal active emphasis and counts in parentheses.
9. **Footer**: Minimal centered tiny muted-gray text with a subtle top divider.
10. **Accessibility & Responsiveness**: Visible teal/blue keyboard focus rings, text labels/tooltips on icons, textual meaning paired with semantic colors, horizontally scrollable tab/action rows on narrow viewports, and Full Width / Shrink View support.

---

## 7. Enterprise Data Table & Filter Standard

### Page Surface & Filter Card
- **Containers**: White cards with thin blue-gray borders (`#d9e1ea`-like), `10–12px` corner radius, no visible shadows, full-width compact administrative density.
- **Filter Panel**: Single horizontally arranged white card (`14–16px` padding, `8–12px` gutters) or responsive two-column grid.
  - Labels: dark navy, `11–12px`, medium weight, positioned `5–7px` above controls.
  - Inputs & Selects: white fill, subtle gray border, `6–9px` radius, `32–40px` equal height; date inputs with right-aligned calendar icon; selects with dark downward chevron.

### Data Table Styling
- **Wrapper**: Enclosed in a rounded bordered card (`10–12px` radius).
- **Header Row**: Nearly white / softly shaded `#f6f8fa`-like fill, uppercase `10–11px` semibold/bold dark blue-gray labels with wide letter-spacing, bottom separator.
- **Rows & Cells**: Compact (`43–45px` tall), `12–15px` horizontal and `10–12px` vertical cell padding, subtle horizontal separators only (no heavy vertical borders), alternating white and pale cool-blue (`#f3f8fd`-like) zebra striping.
- **Typography & Alignment**:
  - Body text: dark navy/slate `11–12px`; primary row title `11–12px` semibold; secondary details/datetime smaller and pale blue-gray; long hashes/technical strings truncate with ellipsis.
  - Text/metadata left-aligned; date column stacks primary date over smaller secondary time; row actions grouped and right-aligned.

### Compact Table Pills, Badges & Row Actions
- **Category Chip**: Muted light-gray/blue rounded pill with small dark-blue text.
- **Calendar-Impact / Warning Badge**: Warm ivory/yellow-tinted fill, thin tan/orange border, small leading icon, amber/brown text.
- **Published / Active Status**: Pale mint/green fill, teal-green border and text, fully rounded pill.
- **Draft Status**: Pale blue fill, blue border and text, fully rounded pill.
- **Row Action Buttons**: Compact white rounded rectangles (`5–6px` radius, `1px` blue-gray border, no shadow, `11–12px` medium weight, `4–6px` gap). Contextual emphasis actions (e.g. `Edit`) use a pale blue fill with medium/cobalt blue text; other actions (`View`, `Copy`) remain neutral white with dark navy text.

---

## 8. Reusable UI Redesign Prompt

> Apply the HR Portal “Enterprise Document & Records Management” visual language to this view. Preserve its existing functionality and data, but redesign the UI to match this system exactly.
>
> Use a dense, polished government-enterprise dashboard aesthetic:
> - Fixed deep-teal left sidebar (`#008b89`-like) with white line icons, small uppercase section labels, `14px` menu labels, subtle divider rules, and a lighter turquoise rounded active item.
> - Slim white top header with a faint bottom border; place compact utility icons, profile avatar, user name, and muted role metadata on the right.
> - Main page canvas in a very light cool-gray background (`#f5f7fa`-like), with white content cards.
> - Clean modern sans-serif (Inter/system sans); dark navy/slate bold compact headings; small muted-gray descriptions.
> - Compact, high-information-density layout with `8px` micro spacing, `12–16px` internal card padding, and `20–24px` separation between major sections.
> - Content hierarchy: (1) page title/subtitle + compact right-aligned utility buttons, (2) white context/organisation identity card with mint initials avatar and badge, (3) horizontal contextual tabs with teal baseline, (4) white hero card with semantic badges and right-aligned controls, (5) narrow sub-navigation & inline KPI bar, (6) lightly bordered white record cards with status chips and semantic action toolbars, (7) pale-gray inset toolbars/upload panels, (8) segmented sub-tabs and rounded zebra-striped enterprise data tables with compact outline row actions, (9) crisp white equal-height filter controls, and (10) minimal centered muted footer.
> - Avoid gradients, glass effects, oversized type, large shadows, excessive empty space, or decorative illustrations. Ensure visible teal/blue keyboard focus, accessible contrast, icon labels/tooltips, and responsive horizontal scrolling for tab/action bars.

---

## 9. Antigravity Agent Directive

For every future UI creation, redesign, or modification within `HRM.UI`, consult `HRM.UI/docs/UI_DESIGN_STANDARD.md` first. Use `/online/item-requests` as the primary visual and interaction reference alongside the Enterprise Portal & Data Table standards documented above. Maintain consistent header, buttons, forms, panels, tables, `p-2` container padding, and Full Width / Shrink View behavior. Do not introduce conflicting design styles. Work only within `HRM.UI` unless explicit permission is obtained.
