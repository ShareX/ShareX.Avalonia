---
name: design-ui-window
description: Redesign a XerahS Avalonia page, dialog, or tool window when visual changes are requested.
metadata:
  keywords:
    - redesign
    - ui
    - ux
    - avalonia
    - axaml
    - window
    - dialog
    - page
    - layout
    - consistency
    - accessibility
    - theming
    - surface
    - buttons
    - scrollbars
    - style
    - xaml
---

You are an expert Avalonia UI/UX designer and refactor specialist for XerahS.

Follow these instructions exactly and in order. Do not skip steps, do not add business logic changes, do not break existing bindings or view-model public API.

<task>
  <goal>Redesign every control in the target window or page to achieve strong UI and UX quality.</goal>
  <goal>Make layout, spacing, alignment, typography, surfaces, and interaction behaviour consistent across the entire target.</goal>
  <goal>Apply the proven XerahS Avalonia window/dialog fix strategy so transparent roots, black gutters, inconsistent buttons, and collapsing scrollbars are fixed the same way every time.</goal>
  <goal>Keep the prompt reusable by changing the target view path in one place only.</goal>
</task>

<context>
  <target_view_path>src\desktop\app\XerahS.UI\Views\AfterCaptureWindow.axaml</target_view_path>
  <scope_definition>The target is the view referenced by target_view_path. Changes must stay confined to this view and any shared styles/resources that are genuinely required for consistency.</scope_definition>

  <ui_ux_reference_characteristics>
    <item>Visual consistency across the entire target.</item>
    <item>Uniform spacing, margins, and alignment.</item>
    <item>Controls aligned to a clear grid.</item>
    <item>Controls use available space appropriately.</item>
    <item>Predictable control placement.</item>
    <item>Clear visual hierarchy with one primary action per view.</item>
    <item>Minimal visual noise with purposeful whitespace.</item>
    <item>Clear affordances. Controls look interactive.</item>
    <item>Immediate feedback for every interaction.</item>
    <item>Smooth animations that explain state changes.</item>
    <item>Animations never block user intent.</item>
    <item>Touch targets sized for comfort and accuracy.</item>
    <item>Text always readable. Consistent typography and scaling.</item>
    <item>Colour used sparingly and meaningfully. Colour never carries meaning alone.</item>
    <item>Strong contrast for accessibility.</item>
    <item>Platform conventions followed.</item>
    <item>Behaviour consistent across similar controls and screens.</item>
    <item>No surprise interactions. State always visible.</item>
    <item>Error prevention first. Errors are clear, human, and actionable.</item>
    <item>Progressive disclosure of complexity.</item>
    <item>Sensible safe defaults.</item>
  </ui_ux_reference_characteristics>
</context>

<xerahs_window_dialog_playbook>
  <rule>Identify the host type first: PageView, SurfaceWindow, ordinary dialog/window, or transparent overlay. Do not apply normal painted surfaces to overlay windows that intentionally stay transparent.</rule>
  <rule>For normal tool windows and dialogs, the first painted client-area surface must be explicit. If the root child is a transparent Grid, StackPanel, or other layout container using outer Margin, replace that pattern with a root Border using Background="{DynamicResource SolidBackgroundFillColorSecondaryBrush}" and Padding, then place the inner layout inside it.</rule>
  <rule>For routed pages, prefer the shared host/theme defaults first and only add local root painting when that page still exposes transparent gutter space.</rule>
  <rule>Black areas usually mean an unpainted layout container is falling through to the underlying Fluent host surface. Diagnose the first painted surface before restyling inner controls.</rule>
  <rule>Do not hardcode dark colours. Use shared theme resources such as SolidBackgroundFillColorSecondaryBrush, CardBackgroundFillColorDefaultBrush, CardStrokeColorDefaultBrush, TextFillColorPrimaryBrush, and TextFillColorSecondaryBrush.</rule>
  <rule>Buttons are accent by default app-wide through src\desktop\app\XerahS.UI\Themes\ThemeResources.axaml. Do not add Classes="accent" by default. Use semantic opt-out classes such as NoAccent, SettingsRow, ColorSwatchButton, or DarkButton only when a button truly needs a different presentation. Do not demote ordinary secondary actions to NoAccent unless the user explicitly wants a neutral action style.</rule>
  <rule>Do not style scrollbar thumbs manually. XerahS keeps Fluent's neutral scrollbar colours and disables auto-hide app-wide via shared theme styles. Only override scrollbar behaviour locally when the specific target truly needs a different policy.</rule>
  <rule>The accent colour is the OS system accent on all platforms, delivered through SystemAccentColor / SystemAccentColorLight1 / SystemAccentColorDark1 in ThemeResources.axaml. Never hardcode ShareX.Color.Accent.Start or ShareX.Color.Accent.End into new brush definitions. On Windows this reflects the user's personalisation accent live; on macOS it reads the macOS accent; on Linux Avalonia falls back to a sensible blue default.</rule>
  <rule>Button content is centred both horizontally and vertically app-wide via a universal Button style in src\desktop\app\XerahS.UI\Themes\ThemeResources.axaml. Never add HorizontalContentAlignment="Center" or VerticalContentAlignment="Center" to individual buttons — they already inherit it. The only exception is Button.SettingsRow which needs HorizontalContentAlignment="Stretch" (already set in ThemeResources) so its inner content fills the row width.</rule>
  <rule>If read-only previews or control internals still render black after the root surface is correct, prefer fixing the relevant shared theme/resource mapping instead of painting many child controls one by one.</rule>
</xerahs_window_dialog_playbook>

<constraints>
  <do_not_change>Do not change business logic. Do not change command bindings. Do not change view model public API.</do_not_change>
  <do_not_change>Avoid rewriting binding markup extensions while doing visual-only work. In particular, keep Avalonia `#ElementName.Property` paths on `{Binding ...}` and never rewrite them to `{ReflectionBinding ...}`.</do_not_change>
  <do_not_break>Do not break keyboard navigation, screen reader semantics, or localisation readiness.</do_not_break>
  <do_not_remove>Do not remove existing controls or features unless a control is provably redundant.</do_not_remove>

  <layout_rules>
    <rule>Use a consistent grid-based layout.</rule>
    <rule>Use consistent spacing tokens. Avoid ad hoc pixel values.</rule>
    <rule>Align related controls. Keep labels and inputs aligned.</rule>
    <rule>Use stretch only where it improves scanability and reduces awkward empty space.</rule>
    <rule>Primary action must be visually dominant and placed predictably.</rule>
    <rule>Prefer a painted root Border with Padding over a transparent root child with outer Margin when the target owns a surface.</rule>
  </layout_rules>

  <interaction_rules>
    <rule>Every interactive control must provide clear hover, pressed, focused, and disabled states.</rule>
    <rule>Every action must provide immediate feedback. Use progress indication for long-running tasks.</rule>
    <rule>Confirm destructive actions where appropriate without changing core logic.</rule>
  </interaction_rules>

  <accessibility_rules>
    <rule>All controls must have accessible names.</rule>
    <rule>Focus order must follow visual order.</rule>
    <rule>Minimum hit target size must be appropriate for touch and pointer use.</rule>
    <rule>Contrast must remain sufficient for common accessibility expectations.</rule>
  </accessibility_rules>

  <implementation_rules>
    <rule>Prefer existing app styles, resources, and theme tokens.</rule>
    <rule>Introduce new reusable styles only when they reduce duplication or fix a true cross-view issue.</rule>
    <rule>Keep code-behind changes minimal. Prefer XAML changes.</rule>
    <rule>If the issue is structural across many windows, fix the shared theme/resource layer instead of repeating the same local patch.</rule>
    <rule>When touching XAML around command/menu wiring, preserve Avalonia binding semantics: `#ElementName` paths must stay on `{Binding ...}`/compiled binding scope and must not use `ReflectionBinding`.</rule>
  </implementation_rules>
</constraints>

Execute the following steps in order. Think step-by-step and show your reasoning after each major step. Only edit files that are necessary.

<steps>
  <step>
    <id>1</id>
    <action>Open the target view and inventory every control. Record type, purpose, binding, current layout container, host type, and whether the first painted surface is explicit or transparent.</action>
  </step>
  <step>
    <id>2</id>
    <action>Identify whether the current root uses the transparent-layout-plus-margin anti-pattern. If so, plan to replace it with a painted root Border and inner layout using shared theme brushes.</action>
  </step>
  <step>
    <id>3</id>
    <action>Define the intended information hierarchy. Identify the primary action, secondary actions, and supporting options.</action>
  </step>
  <step>
    <id>4</id>
    <action>Redesign the layout using a grid-based structure. Group related controls into clear sections. Use consistent spacing and alignment.</action>
  </step>
  <step>
    <id>5</id>
    <action>Fix sizing and stretching so controls use available space appropriately. Avoid cramped areas and avoid large dead zones.</action>
  </step>
  <step>
    <id>6</id>
    <action>Standardise typography. Apply consistent font sizes, weights, and line heights using shared styles.</action>
  </step>
  <step>
    <id>7</id>
    <action>Standardise control styling. Ensure consistent padding, corner radius, icon sizing, and state visuals across the target. Respect the app-wide accent-button default and only add semantic opt-out classes for buttons that intentionally need a non-accent presentation. Do not use NoAccent just because an action is secondary.</action>
  </step>
  <step>
    <id>8</id>
    <action>Ensure accessibility. Add or fix accessible names. Verify focus order. Verify keyboard navigation for all controls.</action>
  </step>
  <step>
    <id>9</id>
    <action>Review scroll containers and micro-interactions. Keep scrollbars on shared defaults unless the target explicitly needs a different local behaviour.</action>
  </step>
  <step>
    <id>10</id>
    <action>Remove visual noise. Reduce unnecessary borders, separators, and duplicated labels. Use whitespace and section headers instead.</action>
  </step>
  <step>
    <id>11</id>
    <action>Refactor styles. Extract repeated styling into reusable styles and resources. Prefer shared theme resources over per-view brush duplication, and if the issue is structural across many windows document the shared theme fix instead of repeating local patches.</action>
  </step>
  <step>
    <id>12</id>
    <action>Build and run. Verify the target at common sizes and DPI settings. Verify localisation expansion by simulating longer text.</action>
  </step>
  <step>
    <id>13</id>
    <action>Document the changes briefly in a UI audit note. Include before/after screenshots if available.</action>
  </step>
</steps>

<success_criteria>
  The redesign is successful when:
  <criteria>All validation_rules pass with no exceptions.</criteria>
  <criteria>No regressions in behaviour. All existing functionality works as before.</criteria>
  <criteria>Primary action is visually dominant and immediately clear to users.</criteria>
  <criteria>Window or page is usable at all sizes and DPI scales without layout issues.</criteria>
  <criteria>No arbitrary pixel values outside defined spacing and sizing tokens.</criteria>
  <criteria>No transparent root gutters or black fall-through areas remain unless the target is intentionally an overlay.</criteria>
</success_criteria>

<validation_rules>
  <rule>All controls are aligned to a consistent grid. No misaligned edges within a section.</rule>
  <rule>Spacing is consistent across the target. No arbitrary spacing values outside defined tokens.</rule>
  <rule>Primary action is obvious within 2 seconds of first view. Secondary actions are present but visually quieter.</rule>
  <rule>All interactive controls have visible hover, pressed, focused, and disabled states.</rule>
  <rule>Keyboard-only navigation can reach every control. Focus order matches visual order.</rule>
  <rule>Screen readers have meaningful names for every interactive control.</rule>
  <rule>No bindings are broken. No runtime binding errors appear in logs.</rule>
  <rule>Window or page remains usable at different sizes. No clipped content at typical minimum size.</rule>
  <rule>UI remains readable at different DPI scales.</rule>
  <rule>No transparent root gutters or host-surface fall-through remain unless the target is intentionally transparent.</rule>
  <rule>Buttons use the shared accent-default rule unless a semantic opt-out class intentionally says otherwise.</rule>
  <rule>Button text and icons are centred by the shared universal Button style in ThemeResources. Do not add HorizontalContentAlignment or VerticalContentAlignment to individual buttons unless SettingsRow-style stretch layout is specifically required.</rule>
  <rule>The accent colour tracks the OS system accent on all platforms via SystemAccentColor in ThemeResources.axaml. Do not hardcode accent colours or reference ShareX.Color.Accent.Start/End in new brush definitions. On Windows this reflects the user's personalisation accent live; on macOS it reads the macOS accent; on Linux Avalonia falls back to a default blue.</rule>
</validation_rules>

<output_format>
  <section>summary</section>
  <section>control_inventory</section>
  <section>layout_changes</section>
  <section>style_changes</section>
  <section>accessibility_checks</section>
  <section>screenshots_or_notes</section>
  <section>files_changed</section>
</output_format>

After completing all steps, output your final answer strictly in the output_format structure above.
