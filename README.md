# AdapTypeXR

**Intelligent typography for neurodivergent readers in Extended Reality**

AdapTypeXR is a Unity research platform developed at **UHasselt — Digital Future Lab** that studies how typography, animated text, and adaptive text presentation affect reading performance and comprehension in neurodivergent users within XR environments.

---

## Research Goal

Using real-time eye-tracking and physiological sensors on the **Varjo XR-4**, AdapTypeXR measures how font choice, text layout, and presentation dynamics influence:

- Reading speed and efficiency
- Comprehension and recall
- Cognitive load (pupil dilation, fixation duration)
- Oculomotor behaviour (saccades, regressions, fixations)

Primary participant profiles: **dyslexia**, **ADHD**, **autism spectrum**, and neurotypical controls.

---

## Device & Platform

| Component | Specification |
|-----------|---------------|
| Headset | Varjo XR-4 |
| Eye Tracking | Varjo XR SDK (200 Hz, binocular, pupillometry) |
| Render Pipeline | Unity URP |
| Unity Version | Unity 6 (6000.0.x LTS) |
| XR Framework | OpenXR + XR Interaction Toolkit |
| Text Rendering | TextMeshPro SDF |

---

## Project Structure

```
Assets/AdapTypeXR/
├── Scripts/
│   ├── Core/
│   │   ├── Interfaces/          — Service contracts (IEyeTrackingService, etc.)
│   │   ├── Models/              — Domain types (GazeDataPoint, TypographyConfig, etc.)
│   │   └── Events/              — ReadingEventBus + typed domain events
│   ├── Controllers/             — ReadingSessionController (session state machine)
│   ├── Presenters/              — BookPresenter (3D book + page management)
│   ├── Services/                — VarjoEyeTrackingService, MockEyeTrackingService
│   ├── Repositories/            — CsvDataCollectionRepository (async CSV output)
│   ├── Typography/              — TextRendererController, TypographyAnimator, FontProfileFactory
│   └── Infrastructure/          — AppBootstrapper (composition root), asmdef
├── Scenes/                      — Unity scenes
├── Fonts/                       — TMP SDF font assets
├── Materials/
├── Prefabs/
└── Data/Sessions/               — Output data (gitignored)
```

---

## Architecture

The system follows **SOLID** and **GRASP** principles with clear separation of concerns:

```
AppBootstrapper (Composition Root)
    │
    ├── ReadingSessionController  [Controller — session lifecycle]
    │       ├── IEyeTrackingService  [Adapter — Varjo SDK / Mock]
    │       ├── IDataCollectionRepository  [Repository — CSV]
    │       └── IBookPresenter  [Presenter — 3D book]
    │               └── ITextRenderer  [TextRendererController + TypographyAnimator]
    │                       └── ITypographyAnimationStrategy  [Strategy — RSVP, WordByWord, etc.]
    │
    └── ReadingEventBus  [Observer — cross-system events]
```

Key patterns used: **Adapter**, **Strategy**, **Observer**, **Factory**, **Repository**, **Composite**, **Façade**, **Builder**.

---

## Sprint 0 Typography Conditions

| ID | Font | Animation | Research Hypothesis |
|----|------|-----------|---------------------|
| C1 | Arial | None | Neutral baseline |
| C2 | OpenDyslexic | None | Dyslexia-targeted letterforms reduce decoding effort |
| C3 | Atkinson Hyperlegible | None | High disambiguation reduces fixation count |
| C4 | Atkinson Hyperlegible | Word-by-word highlight | Guided attention reduces ADHD lost-place errors |
| C5 | Arial | RSVP 250 WPM | Eliminating saccades improves speed for dyslexic readers |
| C6 | Atkinson Hyperlegible | Bionic Reading | Bold anchors reduce regression rate |

---

## Setup

### Prerequisites
- Unity 6 (6000.0.x LTS)
- Varjo Base installed and running
- Varjo XR-4 connected
- Varjo XR SDK for Unity (download from Varjo Developer Portal)

### Steps

1. Clone this repository
2. Open in Unity Hub — select Unity 6
3. Import Varjo XR SDK: `Window > Package Manager > + > Add package from disk`
4. Configure OpenXR: `Edit > Project Settings > XR Plug-in Management`
   - Enable **OpenXR**
   - Enable **Varjo** feature set
   - Enable **Eye Tracking** in Varjo settings
5. Open `Assets/AdapTypeXR/Scenes/MainReadingScene.unity`
6. Import font assets into `Assets/AdapTypeXR/Fonts/` and generate TMP SDF atlases
7. Press Play — on non-Varjo machines, `AppBootstrapper` will use `MockEyeTrackingService`

### Editor Testing (no headset)
Set `Use Varjo Eye Tracking = false` on the `AppBootstrapper` component in the scene.
Enable `Auto Start Demo Session` to auto-launch a demo reading session on Play.

---

## Data Output

Sessions are written to:
- **macOS/iOS:** `~/Library/Application Support/<company>/<app>/Sessions/<sessionId>/`
- **Windows:** `%APPDATA%\..\LocalLow\<company>\<app>\Sessions\<sessionId>\`

Each session folder contains:
- `session.json` — participant metadata
- `gaze.csv` — raw gaze samples (200 Hz)
- `metrics.csv` — per-passage aggregated metrics

---

## Agents

This repository includes four AI agent configurations in `.claude/agents/`:

| Agent | Role |
|-------|------|
| `orchestrator` | Sprint planning, backlog management, coordination |
| `xr-developer` | Unity/Varjo implementation, architecture, code review |
| `typography-readability` | Font science, text layout, animation research |
| `neurodivergent-expert` | Cognitive load, participant profiles, ethics, study design |

See [AGENTS.md](AGENTS.md) for full documentation.

---

## Ethical Considerations

- All participants provide informed consent before data collection
- Gaze and physiological data are anonymised from the point of capture
- Participants may withdraw at any time
- Study protocol approved by UHasselt ethics committee before data collection begins
- Maximum continuous XR exposure: 20 minutes per session block

---

## Research Team

UHasselt — Digital Future Lab
Faculty of Sciences and Technology

---

## References

- Rello, L. & Baeza-Yates, R. (2013). Good fonts for dyslexia. *ASSETS '13.*
- Rayner, K. (1998). Eye movements in reading. *Psychological Bulletin.*
- Sweller, J. (1988). Cognitive load during problem solving. *Cognitive Science.*
- Beatty, J. (1982). Task-evoked pupillary responses. *Psychological Bulletin.*
- Braille Institute (2019). Atkinson Hyperlegible font design rationale.
