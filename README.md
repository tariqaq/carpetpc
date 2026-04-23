# CarpetPC

CarpetPC is a local-only Windows desktop assistant prototype. It is designed to wake on `Hey Carpet`, transcribe a voice command, observe the current screen, and control Windows apps through safe, inspectable actions.

## Goals

- Run on Windows 10 and Windows 11.
- Keep idle load low enough to leave room for games.
- Use free/local components only: `llama.cpp`, `whisper.cpp`, and an open wake-word runtime.
- Prompt before downloading any model files.
- Keep model files, screenshots, transcripts, and debug dumps out of git.

## Planned Runtime Stack

- App shell: C#/.NET 10 WPF.
- Local LLM: Gemma 4 E2B Q4-class GGUF through bundled `llama.cpp`.
- Speech-to-text: `whisper.cpp` on CPU.
- Wake word: custom `Hey Carpet` ONNX model through an open wake-word runtime.
- Automation: Windows UI Automation plus screenshot-based vision and guarded keyboard/mouse actions.

## Repository Layout

- `src/CarpetPC.App`: Windows tray app and control panel.
- `src/CarpetPC.Core`: service contracts, action schema, resource profiles, and orchestration primitives.
- `tests/CarpetPC.Tests`: unit tests for safety/resource/model setup behavior.

## Development Notes

Install the .NET 10 SDK before building:

```powershell
dotnet build
dotnet test
```

Models are not committed. The app stores downloaded models under `%LOCALAPPDATA%\CarpetPC\Models` after explicit user confirmation.

## Current App Behavior

- The normal UI no longer exposes a fake wake button. A manual wake trigger exists only behind `Developer mode`.
- `Model Setup` shows required assets, confirms before downloads, and stores assets under `%LOCALAPPDATA%\CarpetPC`.
- The mic selector and live volume bar use the selected Windows recording device.
- `Talk to Carpet` records from the selected mic, transcribes with `whisper.cpp`, and sends the transcript into the agent loop.
- If a compatible llama.cpp `mmproj` GGUF is present under `%LOCALAPPDATA%\CarpetPC\Models\VisionProjector`, the agent sends the current screenshot to the local model through the OpenAI-compatible vision chat endpoint.
- If no vision projector is present, the agent falls back to the UI Automation element list plus text screen summary.
- The app distinguishes setup states: `Setup required`, `Voice test ready`, and `Agent ready`.
- The app still needs live ONNX wake-word inference wiring before `Hey Carpet` works hands-free.

## Wake-Word Training

Run the folder preparation script:

```powershell
powershell -ExecutionPolicy Bypass -File tools\wake-training\Prepare-WakeTrainingFolders.ps1
```

Put positive and negative training audio under `%LOCALAPPDATA%\CarpetPC\WakeTraining`. The exported wake model must be placed at:

```text
%LOCALAPPDATA%\CarpetPC\Models\WakeWordModel\hey-carpet.onnx
```
