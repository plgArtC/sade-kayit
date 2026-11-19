#!/bin/bash
# SadeKayıt — derleme (Ubuntu / WSL / GitHub Actions)
# Gerekenler: sudo apt-get install -y mono-complete nsis python3-pip unzip
set -e
cd "$(dirname "$0")"
if [ ! -f ffmpeg.exe ]; then
  # ffmpeg 9.0.2 (Windows x64, NVENC + libx264 + dshow) — imageio-ffmpeg paketinden
  python3 -m pip download imageio-ffmpeg==0.8.1 --only-binary=:all: --platform win_amd64 --no-deps -d ff_tmp
  unzip -o -q ff_tmp/imageio_ffmpeg-0.8.1-py3-none-win_amd64.whl -d ff_tmp
  cp ff_tmp/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v9.0.2.exe ffmpeg.exe
  rm -rf ff_tmp
fi
mcs -target:winexe -sdk:4.5 -win32icon:sadekayit.ico -out:SadeKayit.exe -r:System.Windows.Forms.dll -r:System.Drawing.dll SadeKayit.cs
makensis -V2 sadekayit.nsi
echo "HAZIR: SadeKayit_Kurulum.exe"
