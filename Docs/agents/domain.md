# Domain documentation

This repository uses a single-context domain model.

## Before exploring the codebase

Read these sources when they exist:

- `CONTEXT.md` at the repository root
- relevant ADRs under `Docs/adr/`

If these files do not exist yet, continue without reporting an error. The `domain-modeling` skill creates or updates them when terminology or architecture decisions are actually resolved.

## File structure

```text
study-organizer/
├── CONTEXT.md
├── Docs/
│   └── adr/
├── backend/
├── frontend/
└── AGENTS.md

```

`CONTEXT.md` contains the shared domain vocabulary for the entire Study Organizer.

`Docs/adr/` contains architecture decision records for decisions that are difficult or expensive to reverse.

## Use the domain vocabulary

When naming a domain concept in code, tests, issues, specifications or documentation, use the term defined in `CONTEXT.md`.

Avoid introducing a synonym when an existing domain term already represents the concept.

If an important concept is missing or unclear, use `domain-modeling` to clarify it before adding it to the glossary.

## Architecture decisions

Before changing architecture, read the ADRs relevant to the affected area.

If a proposed change conflicts with an existing ADR, report the conflict explicitly. Explain why the existing decision may need to be reconsidered instead of silently overriding it.