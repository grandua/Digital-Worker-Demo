# Requirements Audit: GH issue #27 — encapsulate smell detector

Audit date: 2026-11-07. Sources: issue #27 body (authoritative), Docs/_Current/prompt.md
(plan + acceptance criteria), issues.md, framework-design-checklist-result.md,
encapsulate-review-blockers.md, encapsulate-resume-summary.md, crc-cards.md.
Note: prompt.md last-write is 2026-11-06; issues/blockers/summary group is 2026-11-07 —
both groups audited (issue body governs where they differ).

## Traceability matrix (Fully=1.0, Partial=0.5, None=0.0)

| # | Criterion (issue #27) | Score | Evidence |
|---|---|---|---|
| 1 | Zero-config C# console app 'encapsulate' | 1.0 | Encapsulate/Encapsulate.csproj (net10.0), Program.cs, no config files/flags |
| 2 | 1 optional CLI arg: file or directory | 1.0 | Program.cs arg handling; manual run: 1 arg works, 2 args -> usage+exit 2 |
| 3 | Omitted arg defaults to CWD | 1.0 | README + ProgramTests; SourceTree default |
| 4 | Directory -> recursive .cs scan | 1.0 | Data/SourceTree.cs; run on UrlShortener produced per-file findings |
| 5 | Output grouped by file, header once, 'name (line, col)' | 1.0 | Presentation/Report.cs; observed output matches spec format |
| 6 | Feature Envy rule (>=2 member accesses, same external receiver, excl. this/base) | 1.0 | Domain/FeatureEnvyRule via Analysis; FeatureEnvyRuleTests + clarified decisions C2-C4 |
| 7 | Primitive Obsession rule (>=2 same-receiver member args in one call) | 1.0 | PrimitiveObsessionRuleTests; observed 'Primitive Obsession (50, 31)' on LinkEndpoints.cs |
| 8 | Deterministic, no heuristics | 1.0 | Plan §3 determinism; ordinal sorts; README rules section |
| 9 | Tests for the rules/CLI | 1.0 | 114/114 passing, independently re-run on Linux host (dotnet test Encapsulate.slnx) |
| 10 | Cross-platform Windows, Linux, macOS | 0.5 | Verified on Linux only; no OS-specific APIs used, but Win/macOS execution unverified |
| 11 | Final code review completed | 0.0 | BLOCKED: review-unstaged diff-only scope excludes untracked dirs; ReSharper wrapper CRLF failure (encapsulate-review-blockers.md) |
| 12 | Changes delivered (commit/PR) | 0.0 | All work untracked/unstaged in worktree; no commit, no PR |
| 13 | Zero open issues | 0.5 | 1 open low issue: inconsistent sealed convention on CorpusRegressionTests (issues.md #1) |

Coverage = 100 * (11.0) / 13 = **84.6%** of stated+delivery criteria.
Feature scope alone (criteria 1-9): 97.2% (only cross-platform partial).

## Additional findings
- 'dotnet test --no-restore' fails on this host: cached package
  Microsoft.CodeAnalysis.Analyzers 5.9.0-1.26328.17 missing -> plain 'dotnet test' (with restore) works. Environmental, not code.
- Semantic-binding limitation (TPA references only) documented in README; acceptable per plan.

## Recommended way forward (unblocks both reported blockers)
1. Diff-only review: run 'git add -N' (intent-to-add) on Encapsulate/, Encapsulate.UnitTests/,
   Encapsulate.slnx and Docs/_Current/*.md — then 'git diff' includes untracked files and the
   diff-scoped review workflow covers them without staging content.
   Alternative: run review workflow in 'review all code' mode over the two new directories.
2. ReSharper CRLF: fix wrapper line endings once — sed -i 's/\r$//' <wrapper>.sh (or dos2unix),
   or invoke 'jb inspectcode'/the inspectcode entry point directly, or run wrapper via
   'bash <(tr -d "\r" < wrapper.sh)'. Root cause: wrapper script checked out/mounted with CRLF.
3. After review: commit untracked work on branch feature-card-issue-1237211400-27-..., push, open PR
   referencing issue #27, and reply on the issue that review is unblocked by git add -N.
4. Resolve the single low issues.md finding (sealed convention) — document decision or unseal.
5. Optional: run Stryker mutation testing to substantiate test quality (agent claimed none).
6. Optional: verify on Windows/macOS before closing the cross-platform criterion.
