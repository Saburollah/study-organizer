# Issue tracker: GitHub

Issues and specifications for this repository live as GitHub Issues. Use the `gh` CLI for all operations.

## Repository

The repository is inferred from `git remote -v`:

`Saburollah/study-organizer`

## Conventions

- **Create an issue:** `gh issue create --title "..." --body "..."`
- **Read an issue:** `gh issue view <number> --comments`
- **List issues:** `gh issue list --state open`
- **Comment:** `gh issue comment <number> --body "..."`
- **Add a label:** `gh issue edit <number> --add-label "..."`
- **Remove a label:** `gh issue edit <number> --remove-label "..."`
- **Assign to yourself:** `gh issue edit <number> --add-assignee @me`
- **Close:** `gh issue close <number> --comment "..."`

## Pull requests as a triage surface

**PRs as a request surface: no.**

GitHub uses the same number range for issues and pull requests. If a reference such as `#42` is unclear, try `gh pr view 42` and then `gh issue view 42`.

## Publishing

When a skill says “publish to the issue tracker”, create a GitHub Issue.

When a skill says “fetch the relevant ticket”, run:

`gh issue view <number> --comments`

## Wayfinding operations

Wayfinder stores its decision map and decision tickets in GitHub Issues.

### Map

The map is one GitHub Issue with the label `wayfinder:map`. It contains:

- the destination,
- notes,
- decisions made so far,
- areas not yet specified,
- and excluded scope.

### Decision tickets

Each decision ticket is a child issue of the map.

Use one of these labels:

- `wayfinder:research`
- `wayfinder:prototype`
- `wayfinder:grilling`
- `wayfinder:task`

Where GitHub sub-issues are available, use the native sub-issue relationship. Otherwise, add the child issue to a task list in the map and place `Part of #<map-number>` at the beginning of the child issue.

### Blocking relationships

Use GitHub’s native issue dependencies when available.

If native dependencies are unavailable, add this line to the child issue:

`Blocked by: #<issue-number>`

A decision ticket is ready when:

- all blocking issues are closed,
- the ticket is still open,
- and nobody is assigned to it.

### Claiming a ticket

Before working on a decision ticket, assign it to yourself:

`gh issue edit <number> --add-assignee @me`

### Resolving a ticket

1. Add the answer as a comment.
2. Close the ticket.
3. Add a short summary and link to the map under “Decisions so far”.
4. Create newly discovered decision tickets if necessary.