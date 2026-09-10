---
title: "Automation"
parent: "English"
nav_order: 15
lang: "en"
permalink: "/en/automation/"
alternate: "ru/automation.md"
previous_page: "en/shortcuts.md"
next_page: "en/troubleshooting.md"
---

# Automation

The C# / JSON API can create documents, import images, arrange layers, set effects and paint strokes.
An optional Unity Pipeline adapter exposes `sprite_editor_*` commands through Unity CLI;
the C# API works without it.

[Agent guide](https://github.com/DCFApixels/Unity-SpriteEditor/blob/main/AGENTS.md) · [API reference](../AgentAPI.md) ·
[JSON examples](../Examples/index.md) · [Changelog](https://github.com/DCFApixels/Unity-SpriteEditor/blob/main/CHANGELOG.md)

## A safe authoring loop

1. Select the intended Unity project and discover commands with `sprite_editor_describe`.
2. Inspect existing documents with `sprite_editor_inspect`: use stable layer IDs and revision, not names.
3. Import a local image, create a File layer and set its Transform.
4. Validate an unfamiliar batch with `dryRun:true`, then execute it.
5. Save, render and inspect the result. Check API `success` separately from CLI exit status.

API colors are explicit: the window's HDR picker preference does not change an agent request.
Window area selections do not clip API strokes. API v1 cannot author Shader FX code;
existing code and unrelated settings survive partial updates.

The [full API specification](../AgentAPI.md) and JSON examples are maintained in English as one
protocol reference. The user guide is available in both languages.

Respect project rules for compilation, dependency installation and asset changes.
After an uncertain response, inspect before retrying: add operations are not idempotent.
