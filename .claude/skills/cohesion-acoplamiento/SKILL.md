---
name: cohesion-acoplamiento
description: Reviews the cohesion and coupling of the classes in a folder or layer and proposes improvements, one at a time. Use it when the user asks to review the design of classes, their cohesion or coupling, or duplicated logic between them, or before opening a PR.
argument-hint: "[folder, layer or class]"
---

# Review cohesion and coupling

Scope: $ARGUMENTS. If no scope is given, ask for one before starting.

## Process

1. Read every class in the scope. For each one, list its responsibilities and
   its dependencies on project types (ignore .NET base types).
2. Mark responsibilities that do not belong to the class (SRP, Information
   Expert).
3. Mark unnecessary dependencies, dependencies on concrete classes instead of
   abstractions, and dependencies on the wrong layer (Low Coupling, DIP).
4. Compare sibling classes (same interface or same base class) and list the
   methods whose logic is repeated between them.
5. Rate each class with the criteria below. Do not use any other scale.
6. Write the report in the format below, show it, and save it to
   `docs/design-reviews/cohesion-<scope>-<yyyyMMdd-HHmm>.md`.
7. If an earlier report for the same scope exists in that folder, add the
   "Before / after" table comparing the totals.
8. List the proposed changes, numbered, each with the principle it applies
   and the expected effect on the ratings.
9. Ask which proposal to apply. Apply only the ones the user approves, one at
   a time, and run `dotnet build` and `dotnet test` after each one.

## Rating criteria

- Cohesion
  - High: every responsibility serves one purpose.
  - Medium: two or more related purposes.
  - Low: unrelated purposes.
- Coupling (dependencies on project types)
  - Low: up to 2, and only abstractions or the class's own layer.
  - Medium: 3 or 4, or one concrete class from another layer.
  - High: 5 or more, or a dependency that breaks the layer rules in AGENTS.md.

## Report format

For each class:

### `<ClassName>`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| ... | ... (count) | ... (count) | principle: finding |

Cohesion: High/Medium/Low · Coupling: Low/Medium/High

Then a summary:

| Class | Responsibilities | Dependencies | Duplicated methods | Cohesion | Coupling |
|---|---|---|---|---|---|
| ... | n | n | n | ... | ... |
| **Total** | n | n | n | | |

Before / after (only when an earlier report exists):

| Metric | Before | After |
|---|---|---|
| Classes | n | n |
| Duplicated methods | n | n |
| Findings | n | n |
| Classes with Low cohesion or High coupling | n | n |

## Restrictions

- Applying a proposal is a refactor: do not change behavior, and do not
  change tests to make them pass.
- Do not edit any file outside the scope, except the report.
- If a finding is ambiguous, say so instead of guessing.
