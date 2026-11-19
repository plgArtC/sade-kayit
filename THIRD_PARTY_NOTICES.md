# Üçüncü taraf yazılımlar / Third-party software

## FFmpeg
SadeKayıt kurulumu, kayıt ve kodlama için ayrı bir program olarak **FFmpeg 9.0.2** (`ffmpeg.exe`, Windows x64) içerir.
The SadeKayıt installer bundles **FFmpeg 9.0.2** (`ffmpeg.exe`, Windows x64) as a separate executable.

- Lisans / License: GNU General Public License (GPL) v3 — bu derleme libx264 içerdiği için GPL'dir / this build includes libx264 and is therefore GPL.
- FFmpeg kaynak kodu / source code: https://ffmpeg.org/download.html — https://git.ffmpeg.org/ffmpeg.git
- İkili dosyanın kaynağı / binary source: `imageio-ffmpeg` 0.8.1 (PyPI, win_amd64), https://github.com/imageio/imageio-ffmpeg
- FFmpeg is a trademark of Fabrice Bellard. SadeKayıt is not affiliated with the FFmpeg project.

SadeKayıt'ın kendi kodu (SadeKayit.cs) MIT lisanslıdır; FFmpeg ayrı bir program olarak çağrılır ve kendi GPL lisansına tabidir.
SadeKayıt's own code is MIT-licensed; FFmpeg is invoked as a separate program and remains under its own GPL license.

## NVIDIA Broadcast
NVIDIA ve NVIDIA Broadcast, NVIDIA Corporation'ın ticari markalarıdır. SadeKayıt bağımsız, ücretsiz bir programdır; NVIDIA ile bağlantısı yoktur.
NVIDIA and NVIDIA Broadcast are trademarks of NVIDIA Corporation. SadeKayıt is an independent free tool, not affiliated with NVIDIA.
