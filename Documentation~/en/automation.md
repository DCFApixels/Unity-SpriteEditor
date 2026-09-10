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

An agent can help assemble a document: add images as layers, arrange them, apply effects
or make simple painted marks. You can then open the result in Sprite Editor and continue by hand.

## What to ask for

Describe the image you want, its canvas size and what should remain on separate layers.
For example:

> Create a 512 × 512 document. Add this image on a File layer, keep its proportions,
> add a soft outline and save the document in Assets/Icons.

For changes to an existing image, name the document and describe the desired result.
Ask for a copy if you want to keep the original.

Check the result visually, especially after merging layers or applying effects.

## Connect an agent

Give the agent the repository's [agent instructions](https://github.com/DCFApixels/Unity-SpriteEditor/blob/main/AGENTS.md).
They explain how to use Sprite Editor in your Unity project.

Command syntax and integration setup are kept in the separate
[API reference](../AgentAPI.md), with [examples](../Examples/index.md).
You do not need them for ordinary editing.
