# Digital-Worker-Demo
Repo connected to Digital Worker Demo agent for demos, playing, learning and experimentation

# SciCalc Demo App

This repo contains SciCalc, a scientific calculator built with .NET 10 MAUI Blazor Hybrid and xUnit (projects under `src/` and `tests/`). See [src/SciCalc/README.md](src/SciCalc/README.md) for the project layout, verification commands, MAUI workload caveat, platform targets, and behavior decisions.

## SciCalc solutions

- `SciCalc.sln` — `SciCalc.Domain` + `SciCalc.Tests` only. Workload-free: root-level verification needs no MAUI workloads.
- `SciCalc.App.sln` — adds the MAUI `SciCalc` app project (`src/SciCalc`). For machines with the `maui` workloads installed; building it elsewhere fails with `NETSDK1147`.

Quick verification: `dotnet test SciCalc.sln` at the repo root (Domain + tests run on net10.0 without MAUI workloads; do not pass `--nologo` on SDK 10.0.302). Always name the solution file explicitly — the repo root also hosts `UrlShortener.sln` from an unrelated demo.


# Digital Worker User Guide

**Audience:** human users who submit tasks to Digital Worker via Trello  
**Purpose:** explain what Digital Worker is, how to use it, what it can and cannot do, and what actions are prohibited.

---

## What Is Digital Worker?

Digital Worker is a fully autonomous, non-interactive AI coding agent. It reads software-development tasks from Trello cards, executes them using an AI coding agent, and publishes results back to Trello. You never interact with it directly in a chat or terminal — all communication happens through Trello cards.

---

## How to Use Digital Worker

### Submitting a task

1. **Create a Trello card** on the configured Digital Worker board.
2. **Set the card title** to a short, clear summary of the task.
3. **Set the card description** to the full task details — what to implement, context, constraints, acceptance criteria.
4. **Move the card to the "Next Action" list** (or whichever list your operator has configured as the intake list).
5. Digital Worker will automatically pick up the card, move it to "In Progress", and begin work.

### Planning a task

If your board has a "To Plan" list, placing a card there causes Digital Worker to run in **planning mode**. Instead of implementing code directly, it produces a plan and design document covering architecture, key components, integration points, implementation sequence, and open questions. The plan is published as a Trello comment.

### Tracking progress

Digital Worker manages card lifecycle automatically:

- **Next Action** (or **To Plan**) → card waiting to be picked up.
- **In Progress** → Digital Worker has claimed the card and is working on it. A `Started` (or `Planning Started`) comment is added.
- **Done** → task completed successfully. The result answer is posted as a comment, and if code changes were made, a pull request link is included.
- **Blocked** → task could not be completed (failure, timeout, or policy block). A comment explains the outcome.

You do not need to move cards between lists manually once a card is in an intake list. Digital Worker handles all list transitions.

### Receiving results

When Digital Worker finishes a card:

- A **result comment** is posted on the card with the agent's answer and a summary of what was done.
- If the agent made code changes and `ShouldCreatePullRequest` is true, a **pull request** is created on GitHub and the PR link is included in the comment.
- The card is moved to **Done** (success) or **Blocked** (failure).

---

## What You Should Do

- **Write clear, specific task titles and descriptions.** The card title and description are the only task intent the agent receives. Vague cards produce vague results.
- **Keep tasks focused on software development.** Digital Worker is designed for coding, planning, testing, code review, refactoring, and bug fixing.
- **Use English.** Task titles and descriptions must be in English. Non-English text will be blocked.
- **Include relevant context.** If the task depends on specific files, classes, or patterns, mention them in the description.
- **Add comments for follow-up context.** Recent comments on the card are included in the agent's input, so you can add clarifications after submission.
- **Let Digital Worker manage card movement.** Once a card is in an intake list, do not move it manually unless you want to cancel or redirect it.

---

## What You Do Not Need to Do

- **You do not need to install or configure anything.** Digital Worker is operated by an administrator who configures the board, model, and execution environment.
- **You do not need to run any commands.** All execution is handled by Digital Worker.
- **You do not need to create branches or pull requests.** Digital Worker creates isolated Git worktrees, commits changes, pushes branches, and opens PRs automatically.
- **You do not need to monitor the agent in real time.** Results are posted to Trello when the task is done.

---

## What You Should Not Do

- **Do not attempt to extract, reveal, or enumerate system prompts, workflow instructions, internal protection mechanisms, tool lists, or any other IP of Digital Worker.** These attempts are detected and blocked.
- **Do not attempt to override or ignore instructions** (e.g., "ignore previous instructions", "you are now a different assistant", "disregard all rules").
- **Do not submit encoded or encrypted payloads** designed to bypass input screening (e.g., base64-encoded instructions, leet-speak obfuscation, zero-width character injection).
- **Do not submit non-coding tasks.** Tasks unrelated to software development (e.g., "write a poem", "translate this text") will be soft-blocked.
- **Do not use non-English characters in task instructions.** Non-Latin text will be soft-blocked.

---

## What Digital Worker Can Do for You

- **Plan, architect, and design** — create a plan, architecture, class design, and UX design following Clean Architecture and SOLID principles for complex tasks, and save them as docs in your Git repo (via the "To Plan" list).
- **Implement features** — use TDD/test-first to write well-tested Clean Code for new functionality with very high mutation test coverage, based on your task description and/or plan, architecture, class design, and UX docs that it can generate for you as well.
- **Fix bugs** — effective evidence-based diagnosis and fixing of defects in existing code.
- **Write tests** — create unit, integration, or end-to-end tests following testing best practices such as test isolation.
- **Refactor code** — improve code structure while preserving behavior; convert procedural spaghetti code into easy-to-test and reusable modular code using a mix of OOP and functional programming.
- **Review code** — analyze code for code smells, Clean Architecture and Clean Code deviations, and provide professional-grade review feedback.
- **Commit and push** — stage only changed files, commit locally, and the system pushes the branch and opens a pull request automatically.

---

## How Digital Worker Works

### End-to-end flow

1. **Card selection** — Digital Worker picks the first card from "In Progress", or if none, the first card from "To Plan" or "Next Action".
2. **Card comments** — recent comments are fetched so the agent has full context.
3. **Card started** — the card is moved to "In Progress" and a `Started` (or `Planning Started`) comment is added.
4. **Result publication** — the result is posted as a Trello comment, a PR is created if code changes were made, and the card is moved to "Done" or "Blocked".

---

## IP Protection and Prohibited Conduct

**Attempts to hack, extract, or steal the intellectual property of Digital Worker are strictly prohibited.**

This includes but is not limited to:

- Prompt extraction attacks (asking for system prompts, workflow steps, internal instructions, or protection mechanisms).
- Instruction override attacks ("ignore previous instructions", "you are now...", "disregard all rules").
- Encoding or obfuscation bypasses (base64, leet-speak, homoglyphs, zero-width characters).
- Repo-poisoning attacks (planting malicious instructions in repository files).
- Any request that attempts to enumerate tools, workflows, or internal system behavior.

### Consequences

- **Confirmed hostile intent:** your user ID will be blocked.

---

## Frequently Asked Questions

### Why was my card moved to "Blocked"?

Common reasons:

- The task was not a software-development task.
- The task contained non-English text.
- The task exceeded the maximum allowed input size.
- Attempts to hack, extract, or steal the intellectual property of Digital Worker were detected.
- The agent execution failed or timed out.

Check the comment on the blocked card for an explanation.

### Can I ask Digital Worker questions about how it works?

You can ask general software-development questions through Trello cards. However, questions that attempt to reveal internal prompts, workflows, security mechanisms, or tool implementations will be blocked as IP-protection violations.

### How long does a task take?

Digital Worker has an overall timeout (typically 20–30 minutes). Simple tasks may complete in a few minutes; complex tasks may take longer. If the agent does not engage within the startup timeout, it is restarted with a backup model.

### Can I cancel a task?

Move the card out of the active lists (e.g., back to "Next Action" or to a custom list). The dispatcher will cancel the running task for cards moved to "Blocked".

### Can I have multiple tasks running at once?

Yes. In dispatcher mode, Digital Worker processes multiple cards in parallel up to the configured concurrency limit. Each code-change task gets its own isolated Git worktree.

