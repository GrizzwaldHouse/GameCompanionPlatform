# Portfolio UI Skills — Setup Prompt for Marcus

This document lists Claude Code skill files recommended for the portfolio website project (`D:\portfolio-website`). These skills sharpen Claude's UI/UX judgment beyond its defaults, providing opinionated frameworks for aesthetic decisions, design system generation, and anti-pattern detection.

---

## Recommended Skills to Install

### 1. Anthropic Official — Frontend Design Skill

**Source:** https://github.com/anthropics/claude-code/blob/main/plugins/frontend-design/skills/frontend-design/SKILL.md

**What it adds:**
- Opinionated aesthetic direction (bold choices over safe defaults)
- Anti-pattern catalog: generic AI aesthetics, symmetry-over-clarity, predictable grid layouts
- Context-specific character: the UI should feel like it belongs to the product, not a template
- Typography-first approach: establishes type scale before any other visual decision
- Multi-step design critique process before any implementation begins

**Install:**
```
Copy SKILL.md contents to:
D:\portfolio-website\.claude\skills\frontend-design.skill.md
```

---

### 2. UI/UX Pro Max Skill

**Source:** https://github.com/nextlevelbuilder/ui-ux-pro-max-skill

**What it adds:**
- Structured evaluation across Visual Design, UX Flow, Accessibility, and Performance dimensions
- Wireframe-to-code pipeline with explicit critique at each stage
- Component library audit checklist — surfaces inconsistencies before they compound
- Interaction design patterns: hover states, focus rings, skeleton loaders, error boundaries

**Install:**
```
Copy SKILL.md contents to:
D:\portfolio-website\.claude\skills\ui-ux-pro-max.skill.md
```

---

### 3. Claude Code UI Agents — Design System Generator

**Source:** https://github.com/mustafakendiguzel/claude-code-ui-agents

**What it adds:**
- Automated design system scaffolding: tokens, components, and documentation generated from a brief
- Design token naming conventions aligned with Tailwind CSS variables
- Component composition patterns: how to build complex UI from primitives without prop explosion
- Storybook-compatible output format

**Install:**
```
Copy SKILL.md contents to:
D:\portfolio-website\.claude\skills\ui-design-system.skill.md
```

---

### 4. Awesome Claude Skills Index

**Source:** https://github.com/ComposioHQ/awesome-claude-skills

**What it adds:**
Not a single skill — this is a curated index of community skills. Use it to find additional skills relevant to the current task (animation, data visualization, form design, etc.).

**How to use:**
Browse the index and install individual skills from it as needed. Do not install all of them at once — only install skills you plan to invoke in the near term.

---

## Installation Instructions (All Skills)

1. Navigate to the skill's GitHub page at the URL listed above.
2. Open the `SKILL.md` file (or equivalent, e.g., `frontend-design/SKILL.md`).
3. Copy the raw file contents.
4. Create the destination file at `D:\portfolio-website\.claude\skills\<skill-name>.skill.md`.
5. Paste the contents.
6. Verify the file has a valid YAML frontmatter block at the top (lines starting and ending with `---`).

Claude Code automatically discovers skill files in `.claude/skills/` when the project is opened. No additional configuration is required.

---

## Adapting Web-Focused Skills to WPF/XAML

The skills above are written for web projects (React, Tailwind CSS, TypeScript). The implementation details do not transfer directly to WPF. However, the **design principles** are fully applicable.

### What Transfers Directly

| Web Concept | WPF Equivalent |
|---|---|
| Design tokens (CSS variables) | `StaticResource` keys in `Colors.xaml` / `Styles.xaml` |
| Tailwind spacing scale | Arcadia Tracker spacing scale (xs/sm/md/lg/xl/2xl) |
| Component variants (primary/secondary/ghost) | Named `Style` resources (`PrimaryButton`, `SecondaryButton`) |
| Typography scale | `TextBlock` `Style` resources (`HeadingLargeText`, `BodyText`, etc.) |
| Hover/focus states | WPF `Trigger` blocks inside `ControlTemplate` |
| Skeleton loaders | `IsIndeterminate` `ProgressBar` or animated placeholder `Border` |
| Error boundary | `Result<T>` failure path wired to error state `Visibility` |

### What Does Not Transfer

| Web Concept | WPF Reality |
|---|---|
| Tailwind utility classes | WPF has no utility class system — all styling is explicit Style or inline |
| CSS animations / `transition` | WPF uses `Storyboard` and `DoubleAnimation` |
| Flexbox / CSS Grid | WPF uses `StackPanel`, `Grid`, `WrapPanel`, `DockPanel` |
| React component props | WPF uses `DependencyProperty` or `DataContext` bindings |
| Storybook | No WPF equivalent — verify visually by running the app |

### Recommended Approach When Using These Skills in Arcadia Tracker

1. Ask Claude to apply the **design principles** from the installed skill, not the implementation syntax.
2. Explicitly state: "Apply the aesthetic direction and anti-pattern checks from the frontend-design skill, but implement using WPF/XAML patterns."
3. Cross-reference every suggestion against `docs/arcadia_design_tokens.md` to ensure token consistency.
4. After any AI-generated XAML, run the Three-Brain judgment from `skills/ui_critic.skill.md`.

---

## Quick Reference: Anti-Patterns (Applicable to Both Web and WPF)

These are lifted from the Anthropic frontend-design skill and apply equally to WPF views in Arcadia Tracker:

- **Generic AI aesthetic**: Soft rounded corners, pastel gradients, and no visual identity. In WPF: relying on default control templates without any customization.
- **Predictable grid layout**: Every view is a 2-column card grid regardless of content. In WPF: `UniformGrid` applied to data that has natural hierarchy.
- **Lack of context-specific character**: Labels and copy that could belong to any app. In WPF: `TextBlock Text="Status"` instead of `TextBlock Text="Power Grid Status"`.
- **Decorative interactivity**: Hover states or animations that serve no functional purpose and add visual noise.
- **Symmetry over clarity**: Equal visual weight given to every element to avoid the discomfort of making a hierarchy decision.
- **Feature theater**: A view or component that exists but delivers no actionable value to the user.
