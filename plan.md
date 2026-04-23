# Real CarpetPC V1: Train Wake Word, Then Build Local Agent

## Summary
First train a free/open `Hey Carpet` wake-word model using the RTX 4060, then replace the current stub/demo app with a real local workflow: user downloads required models in-app, says `Hey Carpet`, the app transcribes the command, captures the screen, asks a local `llama.cpp` model for structured actions, and controls the desktop safely.

## Phase 1: Wake-Word Training
- Use a free/open wake-word stack, preferably `livekit-wakeword`, to train/export a custom `Hey Carpet` ONNX model.
- Create a training workspace outside git-tracked model folders, using the RTX 4060 for training.
- Collect or synthesize positive samples for `Hey Carpet`, plus negative/background samples for normal speech, keyboard noise, game audio, music, and silence.
- Export the trained model as ONNX and store the release artifact under `%LOCALAPPDATA%\CarpetPC\Models\wake\hey-carpet.onnx`.
- Add an internal evaluation script/report for false accepts and false rejects before wiring it into the app.
- Keep raw audio, generated datasets, checkpoints, ONNX files, and evaluation dumps out of git.

## Phase 2: Model Setup UI
- Add an in-app Model Setup screen with selectable downloads, progress bars, cancel/retry, model folder open button, and explicit confirmation before any download starts.
- Use Hugging Face direct downloads for curated models. Default catalog:
  - Agent model: Gemma 4 E2B Q4 GGUF-class model.
  - Low-spec model: Gemma 4 E2B Q3/Q4 smaller quant if needed.
  - Speech model: Whisper `tiny.en` and `base.en` through `whisper.cpp`.
  - Runtime: latest compatible `llama.cpp` Windows x64 CUDA/CPU release.
- Store assets under `%LOCALAPPDATA%\CarpetPC\Models` and `%LOCALAPPDATA%\CarpetPC\Runtimes`, never in the repo.
- If required models are missing, show `Setup required` and do not pretend the app is ready/listening.

## Phase 3: Real Voice And Agent Runtime
- Replace `StubWakeWordService` with the trained ONNX `Hey Carpet` detector.
- Replace `StubSpeechTranscriber` with real microphone capture, mic selector, live volume bar, VAD, and Whisper command transcription.
- Replace `StubModelRuntime` with a `llama.cpp` wrapper that launches local `llama-server.exe` with selected model, context size, and GPU offload profile.
- Replace `StubScreenObserver` with screenshot capture plus Windows UI Automation extraction.
- Remove visible `Simulate Hey Carpet`; keep a dev-only manual trigger behind a `Developer mode` checkbox.

## Phase 4: Desktop Control Loop
- Use this loop: wake detected -> transcribe command -> capture screenshot/UIA -> ask model for JSON action -> validate risk/confidence -> execute action -> wait -> observe again.
- Implement first real actions: `open_url`, `open_app`, `type`, `keypress`, `click`, `wait`, `finish`, `abort`, and `ask_confirmation`.
- Keep only one fresh screenshot per decision step; preserve prior progress as compact text.
- Confirm risky actions before purchases, installs, deletes, account changes, sending messages, elevated/admin actions, or low-confidence clicks.
- Keep `Ctrl+Alt+Esc` emergency pause and add voice `stop` during active execution.

## Resource Policy
- Use dynamic profiles:
  - `High`: more GPU offload when free VRAM is above about `5.5 GB`.
  - `Balanced`: cap around `3 GB VRAM`.
  - `CPU Safe`: mostly CPU if VRAM is constrained.
  - `Wake Only`: unload LLM if VRAM/RAM is critical.
- Changing GPU offload reloads `llama.cpp`; do not try to move layers live mid-inference.
- Training can use the RTX 4060 freely, but the runtime app should still preserve VRAM for games.

## Test Plan
- Unit tests for model catalog, no-download-without-confirmation, settings persistence, download state, wake model presence, resource profiles, action JSON parsing, and risky-action validation.
- Wake-word evaluation: measure false accepts/false rejects using held-out positive and negative samples.
- Integration tests with fake model/audio/screen services for wake -> command -> observe -> action -> execute.
- Manual acceptance test:
  - App starts in setup-required state if models are missing.
  - User can choose mic and see live volume.
  - User can download selected models from inside the app.
  - Saying `Hey Carpet, open Web WhatsApp in Brave` triggers wake, transcription, screen capture, local LLM planning, and desktop action.
  - `stop` and `Ctrl+Alt+Esc` pause execution.
  - Build/test/publish pass.

## Assumptions
- We train and ship our own `Hey Carpet` ONNX model instead of relying on non-commercial pretrained wake models.
- Hugging Face direct downloads are acceptable, with explicit user confirmation.
- Models, runtimes, logs, screenshots, raw audio, datasets, checkpoints, and debug dumps stay out of git.
