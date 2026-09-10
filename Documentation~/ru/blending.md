---
title: "Наложение и обтравка"
parent: "Русский"
nav_order: 8
lang: "ru"
permalink: "/ru/blending/"
alternate: "en/blending.md"
previous_page: "ru/effects.md"
next_page: "ru/shader-fx.md"
---

# Наложение и обтравка

Включи **Clipping Mask** в меню строки или нажми `Alt` на границе над базовым слоем.
Несколько обтравочных слоёв подряд используют первого соседа без обтравки под ними, внутри той же группы.
Они сохраняют цвета и эффекты, но не расширяют альфу базы.

## Обтравка, смешивание групп и режимы наложения

Скрытая, прозрачная или отсутствующая база скрывает цепочку. Прозрачность базы применяется к общему результату один раз.
Группы в обтравке изолируются на время участия в ней. Обтравка поддерживает Undo/Redo,
дублирование, API агентов и флаги PSD; слияние запекает её.

Обычные режимы смешивают RGB в пересечении по правилу source-over для альфы.
**Overwrite** заменяет полный RGBA; прозрачность слоя смешивает старые и новые пиксели.
**None** не меняет композицию.

Normal · Add · Subtract · Multiply · Divide · Screen · Overlay · Darken · Lighten ·
Color Dodge · Color Burn · Linear Dodge (Add) · Linear Burn · Linear Light ·
Linear Light Add/Sub · Vivid Light · Pin Light · Hard Mix · Hard Light · Soft Light ·
Difference · Exclusion · Negation · None · Overwrite.
