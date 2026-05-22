# Agent Teams — Master Reference Guide

> Source: https://code.claude.com/docs/en/agent-teams  
> Compiled: 2026-05-12  
> Requires: Claude Code v2.1.32+, `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1`

---

## Table of Contents

1. [What Agent Teams Are](#1-what-agent-teams-are)
2. [When to Use (and Not Use) Agent Teams](#2-when-to-use-and-not-use-agent-teams)
3. [Agent Teams vs Subagents](#3-agent-teams-vs-subagents)
4. [Enabling Agent Teams](#4-enabling-agent-teams)
5. [Architecture](#5-architecture)
6. [Starting a Team](#6-starting-a-team)
7. [Display Modes](#7-display-modes)
8. [Controlling the Team](#8-controlling-the-team)
9. [Task System](#9-task-system)
10. [Communication Model](#10-communication-model)
11. [Permissions](#11-permissions)
12. [Subagent Definitions as Teammates](#12-subagent-definitions-as-teammates)
13. [Quality Gates via Hooks](#13-quality-gates-via-hooks)
14. [Token Costs](#14-token-costs)
15. [Best Practices](#15-best-practices)
16. [Use Case Examples](#16-use-case-examples)
17. [Troubleshooting](#17-troubleshooting)
18. [Known Limitations](#18-known-limitations)
19. [Quick-Reference Cheatsheet](#19-quick-reference-cheatsheet)

---

## 1. What Agent Teams Are

An **agent team** is a set of coordinated Claude Code sessions working together on a shared problem. One session is the **team lead**; the rest are **teammates**.

- The lead creates the team, spawns teammates, assigns/delegates tasks, and synthesizes results.
- Each teammate runs in its own independent context window.
- Teammates can message each other directly — they are not limited to reporting back to the lead.
- A shared **task list** coordinates work without the lead having to micromanage every step.

This is distinct from subagents (which live inside a single session and can only report back to the caller).

---

## 2. When to Use (and Not Use) Agent Teams

### Strong use cases

| Use Case | Why Teams Help |
|---|---|
| Parallel research / review | Multiple investigators explore different angles simultaneously |
| New independent modules or features | Each teammate owns a separate file set with no overlap |
| Debugging with competing hypotheses | Teammates actively try to disprove each other — avoids anchoring bias |
| Cross-layer changes (frontend + backend + tests) | Each layer owned by a different teammate |
| PR reviews across multiple dimensions (security, perf, coverage) | Each reviewer applies a distinct lens |

### When NOT to use agent teams

- Sequential tasks (each step depends on the last)
- Same-file edits (causes overwrites and conflicts)
- Tasks with many mutual dependencies
- Routine/simple tasks where a single session is cheaper and faster
- Any work where inter-agent communication adds no value (use subagents instead)

---

## 3. Agent Teams vs Subagents

| Dimension | Subagents | Agent Teams |
|---|---|---|
| Context | Own context window; results return to caller | Own context window; fully independent |
| Communication | Report results back to main agent only | Teammates message each other directly |
| Coordination | Main agent manages all work | Shared task list with self-coordination |
| Best for | Focused tasks where only the result matters | Complex work requiring discussion and collaboration |
| Token cost | Lower — results summarized back to main context | Higher — each teammate is a separate Claude instance |

**Decision rule:** Use subagents when you need quick focused workers that report back. Use agent teams when workers need to share findings, challenge each other, and coordinate autonomously.

---

## 4. Enabling Agent Teams

Agent teams are **disabled by default**. Enable via environment variable or settings:

**Option A — local project settings (recommended):**
```json
{
  "env": {
    "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1"
  }
}
```
Place in `.claude/settings.local.json` (gitignored) for personal use, or `.claude/settings.json` for team-wide enablement.

**Option B — shell environment:**
```bash
export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1
```

---

## 5. Architecture

```
┌─────────────────────────────────────────────────────┐
│                    TEAM LEAD                        │
│  (main Claude Code session — creates & coordinates) │
└───────────────┬─────────────────────────────────────┘
                │  spawns / messages
    ┌───────────┼───────────┐
    ▼           ▼           ▼
┌────────┐ ┌────────┐ ┌────────┐
│Teammate│ │Teammate│ │Teammate│   ← each has own context window
│   A    │ │   B    │ │   C    │
└────┬───┘ └───┬────┘ └───┬────┘
     │         │          │
     └─────────┴──────────┘
        direct messaging (mailbox)
                │
        ┌───────▼────────┐
        │  Shared Task   │
        │     List       │
        └────────────────┘
```

### Storage locations (auto-managed, do not hand-edit)

| Resource | Path |
|---|---|
| Team config | `~/.claude/teams/{team-name}/config.json` |
| Task list | `~/.claude/tasks/{team-name}/` |

The team config holds runtime state (session IDs, tmux pane IDs). It is **overwritten on every state update** — editing it manually will be clobbered.

### Team config `members` array

Contains each teammate's name, agent ID, and agent type. Teammates can read this file to discover each other.

---

## 6. Starting a Team

Tell the lead in natural language — Claude figures out team structure and spawns teammates:

```
I'm designing a CLI tool that helps developers track TODO comments across
their codebase. Create an agent team to explore this from different angles:
one teammate on UX, one on technical architecture, one playing devil's advocate.
```

Claude will:
1. Create a team with a shared task list
2. Spawn teammates for each role
3. Have teammates explore the problem
4. Synthesize findings
5. Attempt cleanup when finished

You can also specify exactly what you want:
```
Create a team with 4 teammates to refactor these modules in parallel.
Use Sonnet for each teammate.
```

**Two initiation paths:**
- You explicitly request a team
- Claude proposes a team (you confirm before it proceeds — Claude never creates a team without approval)

---

## 7. Display Modes

### In-process (default when not in tmux)
- All teammates run inside the main terminal
- `Shift+Down` — cycle through teammates
- Type to send a message to the focused teammate
- `Enter` — view a teammate's session
- `Escape` — interrupt their current turn
- `Ctrl+T` — toggle the task list

### Split panes (requires tmux or iTerm2)
- Each teammate gets its own pane
- Click into a pane to interact directly
- See all output simultaneously

### Configuration

```json
// ~/.claude/settings.json
{
  "teammateMode": "in-process"   // or "tmux" or "auto" (default)
}
```

```bash
# Per-session override
claude --teammate-mode in-process
```

`"auto"` uses split panes if already inside tmux, otherwise in-process.

**tmux note:** Known limitations on some OSes; `tmux -CC` in iTerm2 is the recommended entry point.

**Split panes NOT supported in:** VS Code integrated terminal, Windows Terminal, Ghostty.

### iTerm2 setup for split panes
1. Install the `it2` CLI
2. Enable Python API: **iTerm2 → Settings → General → Magic → Enable Python API**

---

## 8. Controlling the Team

Everything is done in natural language to the lead. Key operations:

### Require plan approval before implementation
```
Spawn an architect teammate to refactor the authentication module.
Require plan approval before they make any changes.
```
- Teammate works in read-only plan mode until the lead approves
- Lead can approve or reject with feedback
- If rejected, teammate revises and resubmits
- Lead makes approval decisions autonomously — give it criteria in your prompt:
  - `"only approve plans that include test coverage"`
  - `"reject plans that modify the database schema"`

### Talk to a specific teammate directly
- In-process: `Shift+Down` to the teammate, then type
- Split pane: click into the pane

### Redirect a teammate
Give them additional instructions, ask follow-up questions, or change their approach directly via messaging.

### Shut down a teammate
```
Ask the researcher teammate to shut down
```
The lead sends a shutdown request; the teammate can approve or reject with an explanation.

### Clean up the team
```
Clean up the team
```
**Always use the lead for cleanup.** Teammates should not run cleanup — their team context may not resolve correctly, leaving resources inconsistent. Cleanup fails if any teammates are still running; shut them down first.

---

## 9. Task System

### Task states
- `pending` — not yet started
- `in_progress` — claimed by a teammate
- `completed` — done

### Task dependencies
- A pending task with unresolved dependencies cannot be claimed
- When a dependency completes, blocked tasks unblock automatically — no manual intervention needed

### Task claiming
- **Lead assigns:** tell the lead which task goes to which teammate
- **Self-claim:** after finishing, a teammate picks up the next unassigned, unblocked task automatically
- File locking prevents race conditions when multiple teammates try to claim simultaneously

### Task sizing guidance
- **Too small:** coordination overhead exceeds benefit
- **Too large:** long runs without check-ins increase risk of wasted effort
- **Just right:** self-contained unit producing a clear deliverable (a function, a test file, a review)
- Target **5–6 tasks per teammate** to keep everyone productive without excessive context switching

---

## 10. Communication Model

### Message delivery
- Messages sent by teammates are delivered automatically to recipients
- The lead does not need to poll for updates

### Idle notifications
- When a teammate finishes and stops, it automatically notifies the lead

### Addressing
- Send to one specific teammate by name
- To reach everyone, send one message per recipient (no broadcast)
- The lead assigns names at spawn time — to get predictable names, specify them in your spawn instruction

### Context isolation
- Teammates load: CLAUDE.md, MCP servers, skills (same as a regular session)
- Teammates receive: the spawn prompt from the lead
- Teammates do NOT inherit: the lead's conversation history

---

## 11. Permissions

- Teammates start with the **lead's permission settings**
- If the lead uses `--dangerously-skip-permissions`, all teammates inherit that
- You can change individual teammate modes **after** spawning
- You **cannot** set per-teammate modes at spawn time
- Pre-approve common operations in permission settings before spawning to reduce interruptions

---

## 12. Subagent Definitions as Teammates

You can reference a named subagent type when spawning a teammate. This lets you define roles once (e.g., `security-reviewer`, `test-runner`) and reuse them both as delegated subagents and as agent team teammates.

```
Spawn a teammate using the security-reviewer agent type to audit the auth module.
```

**What carries over from the subagent definition:**
- `tools` allowlist
- `model`
- Body appended to the teammate's system prompt (does not replace it)
- Team coordination tools (`SendMessage`, task management) are always available even when `tools` restricts others

**What does NOT carry over:**
- `skills` frontmatter field
- `mcpServers` frontmatter field

(Teammates load skills and MCP servers from project/user settings instead.)

Subagent scopes supported: project, user, plugin, or CLI-defined.

---

## 13. Quality Gates via Hooks

Three hook events fire specifically for agent teams:

### `TeammateIdle`
Fires when a teammate is about to go idle.

| Exit code | Effect |
|---|---|
| `0` | Teammate goes idle normally |
| `2` | stderr shown to Claude as error; teammate stays working |
| JSON `{ "continue": false }` | Stops the teammate entirely (same as Stop hook behavior) |

No matcher support — fires on every teammate idle event.

### `TaskCreated`
Fires when a task is being created via `TaskCreate`.

| Exit code / JSON | Effect |
|---|---|
| `2` | Rolls back task creation; stderr shown to Claude as feedback |
| `{ "decision": "block", "reason": "..." }` | Blocks creation with explanation |

### `TaskCompleted`
Fires when a task is being marked complete.

| Exit code / JSON | Effect |
|---|---|
| `2` | Prevents task completion; stderr shown to Claude as feedback |
| `{ "decision": "block", "reason": "..." }` | Blocks completion with explanation |

### Example: enforce tests before task completion
```bash
#!/bin/bash
# Hook: TaskCompleted
# Prevents marking done if tests fail
npm test 2>&1 | grep -q "FAIL" && {
  echo "Tests are failing — task cannot be marked complete"
  exit 2
}
```

---

## 14. Token Costs

Agent teams use **significantly more tokens** than a single session. Each teammate has its own context window and consumes tokens independently.

### Cost multiplier
- Teams running in plan mode use approximately **7× more tokens** than standard sessions
- Token usage scales roughly linearly with team size

### Cost reduction strategies

| Strategy | Impact |
|---|---|
| Use Sonnet for teammates (not Opus) | Large savings |
| Keep teams small (3–5 teammates) | Linear reduction |
| Keep spawn prompts focused | Reduces per-teammate starting context |
| Clean up teams when work is done | Stops idle token consumption |
| Keep CLAUDE.md under 200 lines | Loaded into every teammate's context |
| Use skills instead of CLAUDE.md for specialized instructions | Skills load on-demand only |
| Delegate verbose operations (logs, test runs) to subagents | Keeps verbose output out of team context |

### Rate limit recommendations (API users)

| Team size | TPM per user | RPM per user |
|---|---|---|
| 1–5 users | 200k–300k | 5–7 |
| 5–20 users | 100k–150k | 2.5–3.5 |
| 20–50 users | 50k–75k | 1.25–1.75 |
| 50–100 users | 25k–35k | 0.62–0.87 |
| 100–500 users | 15k–20k | 0.37–0.47 |
| 500+ users | 10k–15k | 0.25–0.35 |

---

## 15. Best Practices

### Team composition
- **3–5 teammates** is the sweet spot for most workflows
- Scale up only when work genuinely benefits from true parallelism
- Three focused teammates often outperform five scattered ones

### Task design
- Target 5–6 tasks per teammate
- Each task should produce a clear, self-contained deliverable
- Ask the lead to split work into smaller pieces if it isn't creating enough tasks

### Avoiding conflicts
- **Never have two teammates edit the same file** — this causes overwrites
- Structure work so each teammate owns a distinct file set

### Context for teammates
- Include task-specific details explicitly in spawn prompts — teammates don't inherit the lead's history
- Example spawn prompt:
  ```
  Review the authentication module at src/auth/ for security vulnerabilities.
  Focus on token handling, session management, and input validation.
  The app uses JWT tokens stored in httpOnly cookies.
  Report any issues with severity ratings.
  ```

### Steering the team
- Check in regularly — don't leave teams running unattended for long
- Redirect approaches that aren't working early
- If the lead starts implementing instead of delegating: `"Wait for your teammates to complete their tasks before proceeding"`
- If a task appears stuck: check if the work is done and update status manually or tell the lead to nudge the teammate

### Starting out
- Begin with research/review tasks (clear boundaries, no code conflicts)
- This shows the value of parallel exploration before tackling parallel implementation

### CLAUDE.md
- CLAUDE.md works normally — all teammates read it from their working directory
- Use it to provide project-specific guidance to the entire team
- Keep it under 200 lines; move specialized workflow instructions to skills

---

## 16. Use Case Examples

### Parallel PR review
```
Create an agent team to review PR #142. Spawn three reviewers:
- One focused on security implications
- One checking performance impact
- One validating test coverage
Have them each review and report findings.
```

### Competing hypothesis debugging
```
Users report the app exits after one message instead of staying connected.
Spawn 5 agent teammates to investigate different hypotheses. Have them talk to
each other to try to disprove each other's theories, like a scientific
debate. Update the findings doc with whatever consensus emerges.
```
*The debate structure is the key mechanism — sequential investigation suffers from anchoring bias. Multiple investigators actively trying to disprove each other converge on the true root cause faster.*

### Multi-angle design exploration
```
I'm designing a CLI tool that helps developers track TODO comments across
their codebase. Create an agent team to explore this from different angles:
one teammate on UX, one on technical architecture, one playing devil's advocate.
```

### Parallel module implementation
```
Create a team with 4 teammates to refactor these modules in parallel.
Use Sonnet for each teammate.
```

---

## 17. Troubleshooting

### Teammates not appearing
1. In in-process mode: press `Shift+Down` — they may already be running but not visible
2. Check if the task was complex enough (Claude decides whether to spawn)
3. If split panes requested: `which tmux` to verify tmux is in PATH
4. For iTerm2: verify `it2` CLI installed and Python API enabled

### Too many permission prompts
Pre-approve common operations in permission settings before spawning teammates.

### Teammates stopping on errors
Use `Shift+Down` (in-process) or click the pane (split) to check output, then either give additional instructions directly or spawn a replacement teammate.

### Lead shuts down before work is done
Tell it to keep going, or: `"Wait for teammates to finish before proceeding."`

### Orphaned tmux sessions
```bash
tmux ls
tmux kill-session -t <session-name>
```

---

## 18. Known Limitations

| Limitation | Workaround |
|---|---|
| No session resumption with in-process teammates (`/resume`, `/rewind` don't restore teammates) | After resuming, spawn new teammates |
| Task status can lag — teammates sometimes fail to mark tasks complete | Update status manually or tell lead to nudge the teammate |
| Shutdown can be slow (teammate finishes current request before stopping) | Wait it out or send another shutdown request |
| One team at a time per lead | Clean up before creating a new team |
| No nested teams (teammates cannot spawn their own teams) | Only the lead manages teams |
| Lead is fixed for team lifetime (no leadership transfer) | Plan who the lead is before starting |
| Permissions set at spawn (can change after, not before per-teammate) | Set lead permissions correctly before spawning |
| Split panes not supported in VS Code, Windows Terminal, Ghostty | Use in-process mode |

---

## 19. Quick-Reference Cheatsheet

### Setup
```json
// .claude/settings.local.json
{ "env": { "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1" } }
```

### Key keyboard shortcuts (in-process mode)
| Shortcut | Action |
|---|---|
| `Shift+Down` | Cycle to next teammate |
| `Enter` | View teammate's session |
| `Escape` | Interrupt teammate's current turn |
| `Ctrl+T` | Toggle task list |

### Natural language commands to the lead
| Goal | What to say |
|---|---|
| Create a team | `"Create an agent team with 3 teammates: one for X, one for Y, one for Z"` |
| Require plan approval | `"Require plan approval before they make any changes"` |
| Control team size | `"Create a team with 4 teammates using Sonnet"` |
| Stop a teammate | `"Ask the [name] teammate to shut down"` |
| Clean up | `"Clean up the team"` |
| Keep lead waiting | `"Wait for your teammates to complete their tasks before proceeding"` |

### Hook events for teams
| Event | Blocks what | Exit 2 effect |
|---|---|---|
| `TeammateIdle` | Teammate going idle | Keeps teammate working |
| `TaskCreated` | Task creation | Rolls back the task |
| `TaskCompleted` | Task marked done | Prevents completion |

### Team size guidance
| Scenario | Recommended size |
|---|---|
| Research / review | 3 teammates |
| Feature with independent modules | 3–4 teammates |
| Debugging (competing hypotheses) | 4–5 teammates |
| Large parallel refactor | 4–5 teammates, 5–6 tasks each |
| Sequential / dependent work | Don't use teams — use single session |
