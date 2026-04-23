# Hey Carpet Wake-Word Training

This folder tracks the repeatable training workflow, not the dataset or model outputs.

## Local Folders

Keep training assets outside git:

- `%LOCALAPPDATA%\CarpetPC\WakeTraining\positive`: positive `Hey Carpet` samples.
- `%LOCALAPPDATA%\CarpetPC\WakeTraining\negative`: background speech/noise/game audio/silence.
- `%LOCALAPPDATA%\CarpetPC\WakeTraining\checkpoints`: training checkpoints.
- `%LOCALAPPDATA%\CarpetPC\Models\WakeWordModel\hey-carpet.onnx`: exported model consumed by the app.

## Target

Train a permissively distributable ONNX wake-word model for `Hey Carpet` using a free/open wake-word stack such as `livekit-wakeword`.

## Minimum Dataset For First Pass

- 100-200 positive clips saying `Hey Carpet`, ideally from multiple distances and mic positions.
- 500+ negative clips covering normal speech, silence, keyboard noise, game audio, music, and fan noise.
- Keep clips short, mono, and 16 kHz when possible.

## RTX 4060 Usage

Training can use CUDA on the RTX 4060. Runtime inference should still be lightweight and CPU-friendly so games keep VRAM.

## App Integration Rule

The app should not report normal listening readiness until `%LOCALAPPDATA%\CarpetPC\Models\WakeWordModel\hey-carpet.onnx` exists.
