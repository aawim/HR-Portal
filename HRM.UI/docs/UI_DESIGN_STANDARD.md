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

- **Position & Sticky Behavior**:
  - Full-width sticky header (`sticky top-0 z-30`).
  - Occupies 100% of the available width regardless of whether content is standard width or full width.
  - Border: `border-b border-slate-200 bg-white/95 backdrop-blur-xs px-3 py-3 shadow-xs`.
  - Margins / Padding: `m-0` with no extraneous outer top/left offsets.
- **Header Elements**:
  - Title: `text-lg font-bold text-slate-900`.
  - Subtitle / Description: `text-xs text-slate-500 mt-0.5`.
  - Category / Mode Badges: rounded pill badges (`rounded-full px-2.5 py-0.5 text-xs font-semibold`).
  - Actions Container: `flex flex-wrap items-center gap-2`.
  - Header actions include:
    - **Full Width / Shrink View** toggle button.
    - Secondary actions (Refresh `↻`, Print / Report notes, Export).
    - Primary action (e.g. `+ New Request` with `bg-blue-600 text-white hover:bg-blue-700`).

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
- **Primary Action**:
  - `inline-flex items-center justify-center gap-1.5 rounded-lg bg-blue-600 px-3.5 py-1.5 text-xs font-semibold text-white shadow-xs hover:bg-blue-700 disabled:opacity-50 transition-colors`
- **Secondary / Outline**:
  - `inline-flex items-center justify-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-xs hover:bg-slate-50 disabled:opacity-50 transition-colors`
- **Subtle / Ghost**:
  - `inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-slate-50 px-2.5 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100 transition`
- **Success / Positive**:
  - `inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 shadow-xs transition`
- **Danger / Destructive**:
  - `inline-flex items-center gap-1.5 rounded-lg border border-rose-300 bg-white px-3 py-1.5 text-xs font-semibold text-rose-700 hover:bg-rose-50 transition`
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
- **Validation & Error Banners**:
  - Alert: `flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800`
- **Sub-cards / Fieldsets within Forms**:
  - Highlighted sub-card: `rounded-xl border border-blue-200 bg-blue-50/40 p-3.5 flex flex-col gap-2.5`
  - Neutral sub-card: `rounded-xl border border-slate-200 bg-slate-50/70 p-3 flex flex-col gap-2`

### Cards & Panels
- **Container Panel**: `rounded-2xl border border-slate-200 bg-white p-4 shadow-xs`
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

---

## 6. Antigravity Agent Directive

For every future UI creation, redesign, or modification within `HRM.UI`, consult `HRM.UI/docs/UI_DESIGN_STANDARD.md` first. Use `/online/item-requests` as the primary visual and interaction reference. Maintain consistent header, buttons, forms, panels, tables, `p-2` container padding, and Full Width / Shrink View behavior. Do not introduce conflicting design styles. Work only within `HRM.UI` unless explicit permission is obtained.
