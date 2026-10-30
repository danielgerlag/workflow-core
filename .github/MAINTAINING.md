# Maintaining Workflow Core

This file is the policy for how Workflow Core is maintained, including what the
maintainer bot (`wfcore-manager`) can do on its own and what needs @danielgerlag.

## Bot tier (the bot handles these alone)

- Triage new issues and PRs: apply labels, ask for repros, close obvious duplicates with a link.
- Reply to usage questions and point people to docs.
- Review PRs (comments and approve or request changes).
- Approve CI runs for first-time contributors after checking the diff for anything suspicious
  (workflow changes, secrets, network calls in tests).
- Merge bug fixes, test fixes and doc fixes, but only when **all** of these are true:
  - CI is green (see the definition below).
  - There is no public API or behavior change (nothing under `src/WorkflowCore/Interface/`,
    no changed public signatures, and no new config defaults).
  - Nothing is touched under `.github/`, `*.csproj` or `Directory.Build.props`.
  - At least one review approved it, from the bot or a human.
- Cut **patch** releases (x.y.Z) that contain only merged bot-tier changes.

## Daniel tier (these need @danielgerlag)

- Public API or behavior changes, and anything under `src/WorkflowCore/Interface/`.
- Breaking changes of any kind.
- New packages or providers.
- Minor and major releases.
- Security reports. Never discuss them publicly; label `needs-daniel` and stop.
- Closing a community PR as won't-merge.
- CI workflows, secrets, NuGet publishing, and branch protection.
- Roadmap and direction.

When an item needs Daniel, the bot labels it `needs-daniel`, writes a short summary with a
recommendation, and does nothing further.

## Definition of "green CI"

Every job in the `.NET` workflow (`dotnet.yml`) must have succeeded on the PR's **latest head
commit**: Unit, Integration, Redis, PostgreSQL, SQLServer, MySQL, MongoDB, Elasticsearch, Azure
and Oracle. "Failures match master" and "it's flaky" do **not** count as green. Re-running is
allowed (at most 2 re-runs), and every flake found must get an issue labelled `type:bug` + `flaky`.

## Labels

| Label | Meaning |
|---|---|
| `type:bug` / `type:feature` / `type:question` / `type:docs` | Kind of item |
| `area:redis`, `area:postgres`, `area:sqlserver`, `area:mysql`, `area:mongodb`, `area:elasticsearch`, `area:azure`, `area:oracle`, `area:ef`, `area:dsl` | Affected provider or area |
| `flaky` | Intermittently failing test or CI job |
| `needs-repro` | Waiting on the reporter for a minimal repro |
| `needs-daniel` | A Daniel-tier decision is required |
| `bot:proposed` | The bot proposed an action (shadow mode) and is waiting for confirmation |
| `good first issue`, `help wanted` | Community-friendly |
| `stale`, `duplicate`, `wontfix`, `do not merge` | Existing workflow labels |

See `.github/labels.yml` for the canonical list. It is documentation only and isn't synced automatically.

## Rollout

1. **Shadow mode (2 weeks).** The bot takes no actions on GitHub. It logs every action it would
   have taken, with a draft, to a shadow log that Daniel reviews weekly.
2. **Triage on.** The bot applies labels and posts triage replies.
3. **Reviews on.** The bot posts PR reviews and approves first-time contributor CI runs.
4. **Auto-merge of fixes on.** The bot merges bot-tier changes under the green-CI rule, and
   releases patches.

A tier is turned on only after the previous one has run for a week without a correction from
Daniel. Any tier can be turned off by Daniel at any time.
