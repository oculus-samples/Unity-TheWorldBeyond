# Agent Instructions — The World Beyond

Unity mixed-reality showcase that demonstrates Scene API, Passthrough, Interaction SDK, Voice SDK, and Audio Spatializer features as a reference / template for MR projects on Meta Quest.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup and instructions
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions (MRUK, Meta XR Interaction SDK, Voice SDK, Audio Spatializer)
- `Documentation/` — per-feature deep dives (Scene API, Passthrough, Voice, Interaction, Audio Spatializer, Sample Scenes)
- `LICENSE` — license terms

## Quest / Horizon-specific notes

- Scene API requires that **Room Setup has already been run on the headset before connecting Quest Link**; if you launch the editor while connected to a Quest with no captured room, the Scene data will be empty. Disconnect Link, run Room Setup on-device, then reconnect.
- macOS users cannot use Quest Link — build an APK and deploy to device instead.
- When Unity prompts to migrate hand tracking, **keep `OVRHands`** (not the new XR Hands package); the sample is wired to OVRHands.
- During APK build, choose **"Yes (Ignore and continue)"** when prompted about "Unsupported Input Handling".
- Git LFS is required (`git lfs install` before cloning) per the README.

# Meta Quest tooling

This is a Meta Quest / Horizon OS sample. The bespoke intro above is the source of truth for what this project is and how it's built — use it (and the files it points at) instead of restating facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: <https://github.com/meta-quest/agentic-tools>. Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
