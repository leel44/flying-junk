# AGENTS.md

## Project intent

This repository contains a Unity 2D prototype for **Farm Merger**:
a cozy merge game with a farm fantasy theme.

Primary goal:
- quickly explore and validate the core merge loop

Secondary goals:
- keep the project easy to iterate on
- avoid unnecessary complexity
- preserve project stability inside Unity

## Repository priorities

When making changes, optimize for:
1. simple working prototype
2. readability
3. small diffs
4. safety for Unity assets and project structure

Do not over-engineer early systems unless explicitly requested.

## Important directories

- `Assets/` — gameplay code, scenes, prefabs, art, ScriptableObjects
- `Packages/` — Unity package configuration
- `ProjectSettings/` — engine/project settings

## Working rules

- Prefer minimal, targeted changes.
- Do not rename or move files unless necessary.
- Preserve `.meta` files and Unity asset references.
- Do not edit `ProjectSettings/` unless the task requires it.
- Do not add new Unity packages without clear reason.
- Do not commit generated or cache folders such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- If a change is risky for scenes, prefabs, or references, explicitly mention that risk.

## Code style

- Prefer clear and boring code over clever abstractions.
- Keep classes small when possible.
- Avoid creating large frameworks for prototype-only needs.
- Use descriptive names.
- Keep public API surface small unless there is a good reason.

## Unity-specific constraints

- Be careful with scene and prefab modifications.
- Avoid unnecessary serialization churn.
- Avoid changing import settings, sorting layers, tags, or project-wide settings unless required.
- If changing a prefab or scene, keep the scope tight.

## Verification

Before considering work complete:
- check that the change is internally consistent
- confirm affected files are included
- mention what was not verified if Unity/editor execution was not available

Do not claim that gameplay was tested in-editor unless it was actually tested.

## Done means

A task is done when:
- the requested change is implemented
- the diff is reasonably small
- no obviously unrelated files were changed
- any limitations or unverified assumptions are stated clearly

<!-- FPF-GLOBAL-SECTION -->
## First Principle Framework

For complex or ambiguous work, consult the global FPF reference at [C:/Users/User/.codex/references/fpf/FPF-Spec.md](C:/Users/User/.codex/references/fpf/FPF-Spec.md).

Why this is useful here:
- reduces preventable mistakes caused by shallow framing or hidden assumptions
- helps structure work across humans and AI agents with clearer boundaries and hand-offs
- improves decision quality by making alternatives, trade-offs, and missing evidence explicit

Usage guidance:
- use FPF as a file-backed reference, not as pasted prompt text
- keep final answers in plain project language unless FPF terms genuinely improve clarity
- reach for it more on architecture, strategy, decomposition, coordination, and risky decisions than on tiny routine edits

