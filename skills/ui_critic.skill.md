---
name: UI Critic — Three-Brain Judgment Framework
description: A structured review process for evaluating WPF views across three independent dimensions — functionality, usability, and business impact — with prioritized fix output.
---

# UI Critic — Three-Brain Judgment Framework

Apply this skill whenever evaluating a completed or in-progress view in Arcadia Tracker. Run all three brains independently, then produce a unified report. A view must pass all three before being considered shippable.

---

## Brain 1: Functionality (Does It Work?)

Evaluate mechanical correctness. Every item is binary: pass or fail.

### Button & Command Checklist
- [ ] Every `Button` is bound to a `[RelayCommand]` (or `ICommand`) in the ViewModel
- [ ] Commands with async operations show a loading indicator while running
- [ ] `CanExecute` is wired — buttons that should be disabled ARE disabled
- [ ] No dead buttons (bound to a command that does nothing silently)

### Toggle & State Propagation
- [ ] `CheckBox` and `ToggleButton` bindings use `TwoWay` mode where user input is expected
- [ ] Toggling in the view updates the ViewModel property (verify via `[ObservableProperty]`)
- [ ] State persists correctly when navigating away and returning (ViewModel is not re-instantiated)

### Data-Bound Fields
- [ ] All `TextBlock`, `Label`, and read-only display fields bind to ViewModel properties
- [ ] No hardcoded placeholder values left in production XAML (e.g., `Text="1,234"`)
- [ ] `DataGrid` and `ListView` bind to `ObservableCollection<T>` — updates reflect in real time

### Empty & Error States
- [ ] Empty state: when a collection has 0 items, a helpful message is shown (not a blank space)
- [ ] Error state: when a `Result<T>` failure occurs, the error message is surfaced to the user
- [ ] Loading state: async data fetches show a `ProgressBar` or spinner while pending
- [ ] Error messages are specific (not "An error occurred") and tell the user what to do next

**Fail threshold:** Any unchecked item is a blocker. Fix before proceeding.

---

## Brain 2: UX (Is It Usable?)

Evaluate quality of experience. Score each criterion 1–5.

### Contrast & Legibility
- **WCAG AA body text**: minimum 4.5:1 contrast ratio (body copy, labels, nav items)
- **WCAG AA large text**: minimum 3:1 contrast ratio (headings 18px+ or bold 14px+)
- Check `TextMuted (#606070)` on `Background (#0A0A0F)` — this combination is borderline; confirm ratio
- Verify error and warning text is never gray-on-gray

### Visual Hierarchy
Hierarchy must flow: **Heading > Subheader > Body > Muted**

| Level | Token | Size | Weight |
|-------|-------|------|--------|
| Heading | `HeadingLargeText` | 37px | Bold |
| Header | `HeaderText` | 28px | Bold |
| Subheader | `SubheaderText` | 15px | Regular |
| Body | `BodyText` | 13px | Regular |
| Muted | `MutedText` | 13px | Regular, `TextMuted` color |
| Nav Category | `NavCategoryText` | 10px | SemiBold, uppercase |

- [ ] Page title uses `HeadingLargeText` style — one per view, not repeated
- [ ] Section headers use `HeaderText` or `SubheaderText`
- [ ] Data values do not visually compete with their labels (values larger or bolder than labels)

### Interactive Feedback
- [ ] All clickable elements have a visible hover state (background shift, border glow, or cursor change)
- [ ] Active/pressed state is distinct from hover (at minimum `SurfaceHigh` vs `SurfaceHover`)
- [ ] Primary CTA (`PrimaryButton`) is visually dominant — not the same weight as secondary actions
- [ ] Premium-gated items show a clear lock or premium indicator before the capability check passes

### Spacing & Layout
- [ ] Spacing follows the 8px grid: `xs=4, sm=8, md=16, lg=24, xl=32, 2xl=48`
- [ ] No two interactive elements are spaced less than 8px apart (touch/click target safety)
- [ ] Content does not extend to the raw edge of the window — minimum `md=16px` padding
- [ ] Related items are grouped visually (card or section background); unrelated items have clear separation

### Loading & Feedback
- [ ] Long operations (>300ms) show visible progress
- [ ] After a user action completes (save, copy, activate), there is feedback (status text, brief color change, or notification)
- [ ] Feedback is dismissed automatically or has a clear dismiss action — it does not persist indefinitely

**Score summary (1–5 per criterion):** Record and average. Score below 3.5 on any criterion triggers a redesign recommendation.

---

## Brain 3: Business Impact (Does It Matter?)

Evaluate whether the feature earns its place in the product.

### Player Value Questions
Answer each with Yes / No / Partially:
1. Does this feature improve a player's efficiency at a core Star Rupture task?
2. Does it surface data the player cannot easily see inside the game itself?
3. Does it reduce tedious manual tracking (spreadsheets, note-taking, mental math)?
4. Would a new player understand the value within 10 seconds of seeing it?
5. Does it create a reason to return to Arcadia Tracker during a play session?

### Impact Score (1–5)
Rate the feature's overall impact on player value:

| Score | Meaning |
|-------|---------|
| 5 | Core loop — players open the app specifically for this |
| 4 | Strong utility — noticeably improves sessions |
| 3 | Nice-to-have — players appreciate it but would not miss it |
| 2 | Marginal — limited audience or easily replicated without the app |
| 1 | Noise — no clear player benefit |

**Flag for redesign if score < 3.** Before flagging, ask: "Could this feature be scoped down to a smaller, higher-value version that scores a 4?"

### Monetization Alignment
- [ ] Free features deliver enough value to demonstrate the app is worth using
- [ ] Paid features are visible to free users (non-discoverable is for the nav item, not the feature description)
- [ ] Paid feature gating does not dead-end the user — there is a clear upgrade path shown
- [ ] Premium badge or lock icon is used consistently, not invented per-view

---

## Anti-Patterns to Reject

These patterns appear in generic AI-generated UIs and must be caught and eliminated:

| Anti-Pattern | Description | WPF Manifestation |
|---|---|---|
| Generic AI aesthetic | Default rounded cards, soft gradients, pastel colors with no identity | Removing `StarRuptureTheme.xaml` styles and falling back to WPF defaults |
| Predictable grid layout | Every view is a uniform 2-column card grid regardless of content shape | `UniformGrid` used for data that has natural hierarchy |
| Context-free copy | Labels like "Data", "Value", "Status" with no game-specific language | `TextBlock Text="Status:"` instead of `TextBlock Text="Grid Status:"` |
| Decorative loading | Spinner shown even when data loads in <50ms | `IsVisible` bound to a flag that is never false |
| Symmetry over clarity | Equal visual weight given to every element to avoid hierarchy decisions | All buttons use the same style regardless of priority |
| Feature theater | A view exists but offers no actionable information for the player | Stat cards that show values the player already knows from the in-game HUD |

---

## Output Format

After running all three brains, produce a report in this structure:

```
## Three-Brain Report: [View Name]

### Functionality Brain
PASS / FAIL
Failures:
- [item]: [description of failure]

### UX Brain
Average Score: [X.X / 5]
Low scores:
- [criterion] (Score: X): [what needs to change]

### Business Brain
Impact Score: [X / 5]
Player value: [Yes/No/Partially answers]
Flag for redesign: [Yes / No]
Reason: [one sentence]

### Prioritized Fixes
P0 (Blocker — do not ship):
1. [fix]

P1 (Ship after fix — high confidence):
1. [fix]

P2 (Polish — ship now, fix in next iteration):
1. [fix]

### Verdict
[SHIP / HOLD / REDESIGN]
```

---

## When to Run This Skill

- After implementing a new view
- After a significant refactor of an existing view
- Before any commit that modifies XAML or ViewModel files
- When a user reports that a feature "feels off" without being able to articulate why
