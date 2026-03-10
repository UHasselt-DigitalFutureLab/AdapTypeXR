# AdapTypeXR — Agent Ecosystem

This repository includes four AI agent definitions stored in `.claude/agents/`.
When this repo is opened in Claude Code on any machine, these agents are
automatically available via the Agent tool or `@agent-name` mentions.

---

## How to Use Agents

In Claude Code, invoke an agent with:
```
@orchestrator plan sprint 1
@xr-developer implement the fixation detection algorithm
@typography-readability recommend font spacing for ADHD readers
@neurodivergent-expert review the comprehension question set
```

Or ask Claude to use a specific agent for a task.

---

## Agent Descriptions

### `orchestrator`
**File:** `.claude/agents/orchestrator.md`

Sprint planning and execution coordinator. Maintains the project backlog,
breaks work into 2-week research sprints, and coordinates between agents.
Invoke when deciding what to build next, planning a sprint, or reviewing progress.

### `xr-developer`
**File:** `.claude/agents/xr-developer.md`

Senior XR developer for Unity 6 + Varjo XR-4. Writes documented, elegant,
SOLID/GRASP-compliant C# code. Knows the Varjo SDK eye tracking API, OpenXR
configuration, XR Interaction Toolkit, TextMeshPro, and Unity performance
optimisation for 90 fps XR. Invoke for all code implementation and review tasks.

### `typography-readability`
**File:** `.claude/agents/typography-readability.md`

Typography researcher and readability specialist. Translates peer-reviewed
font science into `FontProfile` and `TypographyConfig` specifications.
Covers: OpenDyslexic, Atkinson Hyperlegible, RSVP, bionic reading, BeeLine,
word-by-word highlight, XR text rendering ergonomics. Invoke for all font
and text presentation decisions.

### `neurodivergent-expert`
**File:** `.claude/agents/neurodivergent-expert.md`

Neurodivergent cognition and reading psychology specialist. Covers dyslexia,
ADHD, autism spectrum, cognitive load theory, ethical study design, and
participant profile schemas. Ensures research is grounded in cognitive science
and centres participant welfare. Invoke for study design, measurement decisions,
and participant-facing content.

---

## Agent Collaboration Pattern

```
Researcher request
       │
  ┌────▼─────┐
  │orchestrator│  ← breaks into tasks, assigns to agents
  └────┬─────┘
       │
  ┌────┴──────────────────────┐
  │                           │
┌─▼──────────┐    ┌──────────▼──────────┐
│xr-developer│    │typography-readability│
│  (builds)  │    │  (specifies fonts)   │
└────────────┘    └──────────────────────┘
                           │
               ┌───────────▼────────────┐
               │  neurodivergent-expert  │
               │  (validates research    │
               │   basis & ethics)       │
               └────────────────────────┘
```

---

## Adding New Agents

Create a new markdown file in `.claude/agents/` with a YAML frontmatter block:

```markdown
---
name: agent-name
description: >
  When to invoke this agent. Describe the trigger conditions clearly.
---

# Agent Content
...
```

The agent will be available on any machine that clones this repository.
