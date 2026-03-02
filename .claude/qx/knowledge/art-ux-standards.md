# Art & UX Standards Knowledge Base

> Injected into artist-ux agent context. Contains UI/UX design standards and asset production guidelines for Unity games.

## UX Design Principles

### Player-Centered Design
- Minimize cognitive load: players should spend brain power on gameplay, not UI
- Progressive disclosure: show only what's needed NOW, reveal complexity as players advance
- Consistent patterns: same action → same visual feedback everywhere
- Accessibility: consider colorblind modes, font sizes, control remapping

### Information Hierarchy
1. **Critical** — Health, ammo, minimap (always visible, HUD-level)
2. **Important** — Quest objectives, cooldowns (semi-persistent)
3. **Reference** — Inventory, stats, settings (on-demand screens)
4. **Detail** — Tooltips, lore entries, achievement progress (deep dive)

### Feedback Loops
- Every player action needs acknowledgment within 100ms
- Visual → Audio → Haptic (layer feedback channels)
- Positive feedback > negative feedback (reward, don't punish)
- Error states: clear message + clear recovery path

## UI Architecture

### UI Framework Integration
> **Note:** Read the project's CLAUDE.md for the specific UI framework (FairyGUI, UGUI, UIToolkit, etc.) and its integration patterns. This section covers universal UI architecture principles.

### UI Component Naming Convention
| Type | Pattern | Example |
|------|---------|---------|
| Window | `UI{Feature}Window` | `UIBagWindow` |
| Panel | `UI{Feature}Panel` | `UISkillPanel` |
| Item | `UI{Feature}Item` | `UIItemSlot` |
| Popup | `UI{Feature}Popup` | `UIConfirmPopup` |
| HUD element | `UI{Feature}HUD` | `UIHealthHUD` |

### Screen Layout Guidelines
- **Safe area**: Respect mobile notch/cutout safe areas
- **Aspect ratios**: Design for 16:9, adapt for 18:9, 21:9, 4:3
- **Touch targets**: Minimum 44x44 dp (mobile), generous click areas (PC)
- **Z-ordering**: HUD (top) > Popups > Windows > World UI

## Visual Design Standards

### Color System
```
Primary:    Game-specific brand color
Secondary:  Supporting actions, less emphasis
Success:    #4CAF50 (green) — positive outcomes
Warning:    #FF9800 (orange) — caution states
Error:      #F44336 (red) — failures, damage
Info:       #2196F3 (blue) — informational
Text:       #FFFFFF (light theme) / #212121 (dark theme)
Disabled:   50% opacity of normal state
```

### Typography Hierarchy
| Level | Usage | Size (relative) |
|-------|-------|-----------------|
| H1 | Screen titles | 1.5x base |
| H2 | Section headers | 1.25x base |
| Body | General text | 1x base (14-16sp mobile) |
| Caption | Secondary info | 0.85x base |
| Micro | Timestamps, IDs | 0.7x base |

### Icon Standards
- Consistent style across all icons (outline vs filled — pick one)
- Minimum 32x32 for mobile, 24x24 for PC
- Provide @1x, @2x, @3x for resolution independence
- Colorize programmatically when possible (reduce asset count)

## Animation & Motion

### Timing Guidelines
| Animation Type | Duration | Easing |
|---------------|----------|--------|
| Button press | 50-100ms | ease-out |
| Panel slide in | 200-300ms | ease-out |
| Panel slide out | 150-250ms | ease-in |
| Fade in | 200ms | linear |
| Fade out | 150ms | linear |
| Tooltip appear | 100-150ms | ease-out |
| Loading spinner | continuous | linear rotation |

### Motion Principles
- Entrance: elements come FROM the direction of their source
- Exit: elements go TO their destination or fade
- Never animate without purpose (decoration is not purpose)
- Disable animations option for accessibility

## Responsive Design

### Resolution Strategy
- Base design at 1920x1080 (PC) or 1080x1920 (mobile portrait)
- Use relative positioning (% of parent, not absolute pixels)
- Use the UI framework's layout/relation system for responsive layouts
- Test at minimum: 1280x720, 1920x1080, 2560x1440

### Platform Adaptations
| Aspect | Mobile | PC |
|--------|--------|-----|
| Input | Touch, swipe, pinch | Mouse, keyboard, gamepad |
| Info density | Lower (bigger elements) | Higher (more data visible) |
| Navigation | Bottom tabs, hamburger | Side nav, top menu |
| Text input | Minimize (auto-complete) | Standard |
| Hover states | N/A | Required for discoverability |

## Asset Production Pipeline

### Sprite Workflow
1. Design in vector tool (Figma/Sketch) → export @1x, @2x, @3x PNG
2. Import to UI editor → create components → set 9-slice where needed
3. Publish UI package → integrate with project's asset pipeline
4. Asset management system handles loading and caching

### Atlas Guidelines
- Group related sprites into atlases (one atlas per UI screen)
- Max atlas size: 2048x2048 (mobile), 4096x4096 (PC)
- Separate frequently-changing sprites from static ones
- Use texture compression: ASTC (mobile), DXT (PC)

## Quality Checklist

### Before Handoff to Development
- [ ] All states designed (normal, hover, pressed, disabled, loading, error, empty)
- [ ] Responsive behavior defined for target resolutions
- [ ] Animation specs documented (duration, easing, triggers)
- [ ] Color values extracted (hex codes, not "that blue")
- [ ] Font specifications documented (family, weight, size, line-height)
- [ ] Touch/click target sizes verified (44dp minimum mobile)
- [ ] Accessibility reviewed (contrast ratios, screen reader labels)

### Before Release
- [ ] Tested on all target devices/resolutions
- [ ] Localization text fits in all languages
- [ ] Performance: UI doesn't cause frame drops
- [ ] Memory: atlas sizes within budget
- [ ] Consistent with existing UI patterns in the game
