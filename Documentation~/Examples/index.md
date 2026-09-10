---
title: "JSON examples"
parent: "Technical reference"
nav_order: 2
permalink: "/reference/examples/"
---

# JSON examples

Copy a request and adapt all asset paths to the intended Unity project before executing it.
Read the [API workflow](../AgentAPI.md#generated-image--compositor) first.

- [Create from a source image](create-image.json): a transformed File layer inside a group.
- [Create with Drawing strokes](create-drawing.json): a document authored with the stroke API.

Inspect existing documents for stable IDs and revisions. Run unfamiliar batches with `dryRun:true`.
Do not replay a request after an uncertain result without inspecting: adding layers is not idempotent.
