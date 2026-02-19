# Arcadia Tracker — Design Token Reference

This document is the single source of truth for all visual design decisions in Arcadia Tracker. Every color, spacing value, typography style, and component style used in the application should trace back to an entry here. When adding a new view or modifying an existing one, use these tokens — do not introduce raw hex values or hardcoded font sizes in XAML.

Tokens are defined in `Themes/Colors.xaml` (color primitives), `Themes/Styles.xaml` (component styles), and `Themes/StarRuptureTheme.xaml` (merged dictionary). All are merged into `App.xaml`.

---

## Color Palette

### Semantic Color Map

| Token Name | Hex Value | XAML Key | Usage |
|---|---|---|---|
| PrimaryColor | `#00FFC8` | `PrimaryBrush` | Active nav indicator, primary CTA background, key data highlights |
| PrimaryDark | `#00A88C` | `PrimaryDarkBrush` | Primary button hover state, pressed state |
| Secondary | `#FF5E5E` | `SecondaryBrush` | Danger indicators, delete actions, critical alerts |
| Accent | `#9C6EFF` | `AccentBrush` | Premium feature highlights, secondary stat values, progress fills |
| AccentGold | `#FFD700` | `AccentGoldBrush` | Legendary badge rarity, player diamond marker, achievement unlocks |
| Background | `#0A0A0F` | `BackgroundBrush` | Window background, base layer |
| Surface | `#1B1D2B` | `SurfaceBrush` | Card background, panel background, sidebar background |
| SurfaceHover | `#252738` | `SurfaceHoverBrush` | Card hover state, nav item hover |
| SurfaceHigh | `#3A3A5E` | `SurfaceHighBrush` | Active/pressed state, selected list item, focused input border |
| TextPrimary | `#FFFFFF` | `TextPrimaryBrush` | Headings, primary labels, active nav text |
| TextSecondary | `#B0B0B0` | `TextSecondaryBrush` | Body text, supporting labels, inactive nav text |
| TextMuted | `#606070` | `TextMutedBrush` | Placeholder text, disabled text, metadata |
| Error | `#FF4C4C` | `ErrorBrush` | Validation errors, failed state messages |
| Success | `#4CFF9C` | `SuccessBrush` | Confirmed actions, healthy status indicators |
| Warning | `#FF6B35` | `WarningBrush` | Non-critical alerts, low-resource warnings |
| Border | `#2A2A4A` | `BorderBrush` | Card borders, separator lines, input field borders |

### Glow / Ambient Variants

These are used exclusively for `DropShadowEffect` and ambient lighting — never for text or solid fills.

| Token Name | Value | XAML Key | Usage |
|---|---|---|---|
| PrimaryGlow | `#4000FFC8` | `PrimaryGlowBrush` | Cyan glow on active nav indicator, key stat numbers |
| AccentGlow | `#409C6EFF` | `AccentGlowBrush` | Purple glow on premium UI elements |
| GoldGlow | `#40FFD700` | `GoldGlowBrush` | Gold glow for Legendary badge rarity |
| ErrorGlow | `#40FF4C4C` | `ErrorGlowBrush` | Red ambient on error state cards |

### Contrast Ratios (Reference)

Verify these before shipping any new view. Values are approximate — use a contrast checker for exact figures.

| Foreground | Background | Approx. Ratio | WCAG AA (4.5:1 body) | WCAG AA (3:1 large) |
|---|---|---|---|---|
| TextPrimary `#FFFFFF` | Background `#0A0A0F` | ~21:1 | Pass | Pass |
| TextPrimary `#FFFFFF` | Surface `#1B1D2B` | ~13:1 | Pass | Pass |
| TextSecondary `#B0B0B0` | Background `#0A0A0F` | ~7.5:1 | Pass | Pass |
| TextSecondary `#B0B0B0` | Surface `#1B1D2B` | ~5.2:1 | Pass | Pass |
| TextMuted `#606070` | Background `#0A0A0F` | ~2.8:1 | Fail body | Pass large |
| TextMuted `#606070` | Surface `#1B1D2B` | ~2.2:1 | Fail | Fail |
| PrimaryColor `#00FFC8` | Background `#0A0A0F` | ~12:1 | Pass | Pass |
| Error `#FF4C4C` | Background `#0A0A0F` | ~5.4:1 | Pass | Pass |

**Note on TextMuted:** `TextMuted` fails WCAG AA on `Surface`. Use it only for truly non-essential metadata (e.g., timestamps, IDs). Never use it for instructions or error messages.

---

## Typography Scale

All text styles are defined as `Style` resources targeting `TextBlock`. Apply via `Style="{StaticResource HeadingLargeText}"`.

| Style Key | Size | Weight | Color Token | Usage |
|---|---|---|---|---|
| `HeadingLargeText` | 37px | Bold | TextPrimary | Page title — one per view |
| `HeaderText` | 28px | Bold | TextPrimary | Section heading within a view |
| `SubheaderText` | 15px | Regular | TextSecondary | Sub-section label, card title |
| `BodyText` | 13px | Regular | TextSecondary | All body copy, data labels |
| `MutedText` | 13px | Regular | TextMuted | Timestamps, IDs, metadata |
| `NavCategoryText` | 10px | SemiBold | TextMuted | Sidebar category headers (ALL CAPS) |

### Font Family

All text uses the system font stack. Explicit declaration is not required — WPF defaults to `Segoe UI`, which is correct for this app.

Do not introduce custom fonts without updating this document and the corresponding XAML resource.

---

## Spacing Scale

The spacing system uses an 8px base unit. All padding, margin, and gap values must be a multiple of 4 from this table.

| Token | Value | XAML Margin Example | Usage |
|---|---|---|---|
| `xs` | 4px | `Margin="4"` | Icon-to-label gap, tight list row padding |
| `sm` | 8px | `Margin="8"` | Control internal padding, compact card padding |
| `md` | 16px | `Margin="16"` | Card padding, section spacing, view edge margin |
| `lg` | 24px | `Margin="24"` | Between major sections |
| `xl` | 32px | `Margin="32"` | View top/bottom padding |
| `2xl` | 48px | `Margin="48"` | Hero section spacing, large modal padding |

**Minimum interactive element spacing:** 8px (`sm`) between any two clickable elements to prevent misclick.

**View edge rule:** Content must never touch the raw window/panel edge. Minimum `md=16px` padding on all four sides.

---

## Component Tokens

### Buttons

| Style Key | Background | Foreground | Border | Hover Background | Usage |
|---|---|---|---|---|---|
| `PrimaryButton` | `PrimaryColor` | `#000000` | None | `PrimaryDark` | Primary action — one per view section |
| `SecondaryButton` | Transparent | `TextSecondary` | `Border` | `SurfaceHover` | Secondary/cancel actions |
| `PillButton` | `SurfaceHigh` | `TextPrimary` | `Border` | `AccentBrush` (border only) | Toggle-style filter buttons, layer toggles on the map |
| `NavButton` | Transparent | `TextSecondary` | None | `SurfaceHover` | Standard sidebar nav item |
| `PremiumNavButton` | Transparent | `AccentBrush` | None | `SurfaceHover` | Premium sidebar nav item — collapsed by default, shown only after capability check |

### Cards

| Style Key | Background | Border | Padding | Usage |
|---|---|---|---|---|
| `CardStyle` | `Surface` | `Border` (1px) | `md=16` | Standard content card |
| `StatCard` | `Surface` | `Border` (1px) | `sm=8` | Compact stat display (number + label) |
| `PremiumCard` | `Surface` | `AccentBrush` (1px) | `md=16` | Premium feature card with accent border |

`PremiumCard` also applies a subtle `AccentGlow` `DropShadowEffect` (blur radius 8, opacity 0.3).

### Inputs

| Style Key | Background | Border Normal | Border Focused | Text | Usage |
|---|---|---|---|---|---|
| `SciFiTextBox` | `#12131C` | `Border` | `PrimaryColor` | `TextPrimary` | All text input fields |
| `SciFiComboBox` | `#12131C` | `Border` | `PrimaryColor` | `TextPrimary` | All dropdown selectors |

Focus state: the border transitions from `Border (#2A2A4A)` to `PrimaryColor (#00FFC8)`. This is the only visual indication of keyboard focus — do not remove it.

---

## Badge Rarity Styles

Used in achievement views, item tracking, and any system that classifies items by tier.

| Style Key | Background | Text Color | Border | Effect |
|---|---|---|---|---|
| `CommonBadge` | `#2A2A2A` | `TextSecondary` | `#3A3A3A` | None |
| `UncommonBadge` | `#0D2B1A` | `#4CFF9C` (Success) | `#1A4A2A` | None |
| `RareBadge` | `#0A1A3A` | `#4A9FFF` | `#1A2A5A` | None |
| `EpicBadge` | `#1A0D2B` | `#9C6EFF` (Accent) | `#2A1A4A` | `DropShadowEffect` color `AccentGlow`, blur 6, opacity 0.6 |
| `LegendaryBadge` | `#2B1F00` | `#FFD700` (AccentGold) | `#4A3800` | `DropShadowEffect` color `GoldGlow`, blur 8, opacity 0.7 |

Apply as `Style="{StaticResource LegendaryBadge}"` on a `Border` element wrapping a `TextBlock`.

---

## Adding a New Token

When a design decision requires a value not listed here:

1. Determine which category it belongs to (color, spacing, typography, component).
2. Check that no existing token serves the same purpose with a slight difference — avoid near-duplicates.
3. Add the token to this document first (source of truth), then add the XAML resource.
4. Do not ship XAML that references a token not in this document.

---

## XAML Resource File Locations

| File | Contains |
|------|---------|
| `Themes/Colors.xaml` | All `SolidColorBrush` and `Color` resources |
| `Themes/Styles.xaml` | Button, card, input, and badge `Style` resources |
| `Themes/StarRuptureTheme.xaml` | Merged dictionary — imports Colors and Styles |
| `Themes/Brushes.xaml` | Gradient brushes and glow brush variants |
| `App.xaml` | Merges `StarRuptureTheme.xaml` and `IconResources.xaml` |
