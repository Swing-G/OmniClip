---
name: OmniClip Design System
colors:
  surface: '#fcf9f8'
  surface-dim: '#dcd9d9'
  surface-bright: '#fcf9f8'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f6f3f2'
  surface-container: '#f0eded'
  surface-container-high: '#eae7e7'
  surface-container-highest: '#e5e2e1'
  on-surface: '#1b1b1b'
  on-surface-variant: '#404752'
  inverse-surface: '#313030'
  inverse-on-surface: '#f3f0ef'
  outline: '#717783'
  outline-variant: '#c0c7d4'
  surface-tint: '#0060ab'
  primary: '#005faa'
  on-primary: '#ffffff'
  primary-container: '#0078d4'
  on-primary-container: '#ffffff'
  inverse-primary: '#a3c9ff'
  secondary: '#5d5f5f'
  on-secondary: '#ffffff'
  secondary-container: '#dcdddd'
  on-secondary-container: '#5f6161'
  tertiary: '#974700'
  on-tertiary: '#ffffff'
  tertiary-container: '#bc5b00'
  on-tertiary-container: '#ffffff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d3e3ff'
  primary-fixed-dim: '#a3c9ff'
  on-primary-fixed: '#001c39'
  on-primary-fixed-variant: '#004883'
  secondary-fixed: '#e2e2e2'
  secondary-fixed-dim: '#c6c6c7'
  on-secondary-fixed: '#1a1c1c'
  on-secondary-fixed-variant: '#454747'
  tertiary-fixed: '#ffdbc8'
  tertiary-fixed-dim: '#ffb689'
  on-tertiary-fixed: '#311300'
  on-tertiary-fixed-variant: '#743500'
  background: '#fcf9f8'
  on-background: '#1b1b1b'
  surface-variant: '#e5e2e1'
typography:
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.02em
  metadata:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '400'
    lineHeight: 14px
  code-snippet:
    fontFamily: Geist
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  card-padding: 12px
  gutter: 8px
---

## Brand & Style
The design system is engineered for productivity and deep integration within the Windows 11 ecosystem. It leverages the **Fluent Design System** philosophy, focusing on light, depth, and material honesty. The brand personality is professional, dependable, and invisible—it stays out of the way until the moment of action.

The aesthetic combines **Minimalism** with **Glassmorphism**, utilizing layered translucency to maintain user context. It avoids unnecessary ornamentation, favoring precise alignment and functional motion. The goal is to evoke a sense of high-performance utility, where the clipboard feels like a native extension of the operating system's memory.

## Colors
The palette is centered around **Action Blue**, used exclusively for primary interactions and active states. 

- **Surface Strategy:** The system uses "Mica" for the main application window—a subtle, opaque material that takes its tint from the user's desktop wallpaper. "Acrylic" is used for transient surfaces like context menus and flyouts to provide a sense of depth and focus.
- **Semantic Accents:** Content types are color-coded to allow for rapid visual scanning. These colors should be used sparingly: as 4px vertical "type indicators" on the left edge of cards or as the fill color for small iconography.
- **Typography:** Use a high-contrast Neutral Gray (#1C1C1C) for primary text and a softer Mid-Gray (#5E5E5E) for metadata like timestamps and source applications.

## Typography
This design system uses **Inter** for its modern, neutral character and exceptional legibility at small sizes. 

- **Hierarchy:** Headlines are reserved for window titles and group headers. Content snippets use `body-md` for maximum readability. 
- **Metadata:** Timestamps and source app names use the `metadata` role with a reduced opacity (60%) to ensure they don't compete with the content.
- **Monospace Exception:** While the system defaults to Inter, the "Code" content type should utilize **Geist** (or a system monospaced font) within its preview card to distinguish syntax from natural language.

## Layout & Spacing
The layout follows a strict **4px baseline grid**. 

- **Clipboard Feed:** Use a single-column list view for the main window, where each item is a card. Cards should have an 8px gutter between them to maintain distinct boundaries while staying compact.
- **Alignment:** Content within cards is left-aligned. Action buttons (copy, pin, delete) should appear on hover in the top-right corner of the card to reduce visual noise during passive browsing.
- **Responsiveness:** On desktop, the app often resides in a narrow sidebar (320px - 400px). Ensure typography remains legible and buttons are touch/click friendly within this width.

## Elevation & Depth
Elevation is communicated through **Tonal Layers** and **Ambient Shadows**, mirroring the Windows 11 environment.

- **Resting State:** Content cards sit on the Mica background with a 1px subtle stroke (#000000 at 10% opacity) and no shadow.
- **Hover State:** When a user hovers over a card, it gains a subtle elevation using a soft, diffused shadow (0px 4px 8px rgba(0,0,0,0.08)) and a slight increase in background brightness.
- **Active/Selection:** The active card is outlined with a 2px "Action Blue" border.
- **Pop-overs:** Any context menus or detail views use "Acrylic" with a higher elevation shadow (0px 8px 16px rgba(0,0,0,0.12)) to appear physically above the feed.

## Shapes
In alignment with modern Windows aesthetics, the design system utilizes **Rounded** shapes.

- **Cards & Inputs:** Use a standard 8px radius (`rounded-md`) to feel soft yet precise.
- **Primary Buttons:** Use 8px radius for a structured look.
- **Search Bars:** Use a 20px or pill-shaped radius to distinguish the search function from content cards.
- **Type Indicators:** The vertical semantic color strips on the left side of cards should have a 2px radius on their right-facing corners for a polished finish.

## Components

- **Content Cards:** The core component. Must feature a `Type Indicator` (semantic color strip), an `Icon` representing the source app, `Body Text` (clipped to 3 lines), and `Metadata` (timestamp).
- **Action Toolbar:** A hidden-on-rest toolbar that appears on card hover. Icons should be 16px, using "Action Blue" for the hover state of the "Pin" icon.
- **Search Bar:** A persistent input at the top of the interface. Use a "Glass" background with a search icon and a clear button that appears only when text is present.
- **Content Type Chips:** Small, filter-style chips at the top of the feed to filter by "Text", "Link", "Image", etc. These should use the semantic colors as a subtle background (10% opacity) when active.
- **Empty State:** When no items are present, use a centered, low-contrast illustration with "Inter" metadata-style text to guide the user (e.g., "Copy something to see it here").
- **Iconography:** Use line-based icons (1.5px stroke). For content types:
    - **Text:** Document lines.
    - **Link:** Link chain.
    - **Code:** Angle brackets `< >`.
    - **Image:** Landscape/Photo icon.
    - **File:** Paperclip or Folder.