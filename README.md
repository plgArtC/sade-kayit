# SadeKayıt 🎥

**OBS'siz, tek tuşla kamera kaydı.** NVIDIA Broadcast ile yeşil ekranlı anlatıcı / yorum videoları kaydetmek isteyenler için küçük, ücretsiz ve açık kaynak bir Windows programı.

> OBS harika bir program ama sadece kameranı kaydetmek istiyorsan sahneler, kaynaklar, profiller... gereğinden fazla. SadeKayıt tek bir iş yapar: kamerayı ve mikrofonu aç, bas, kaydet.

Ticari bir amacı yoktur; isteyen herkes kullansın, değiştirsin, paylaşsın.

## ⬇️ İndir
**[Releases](../../releases/latest) → `SadeKayit_Kurulum.exe`**

Windows "bilgisayarınız korundu" uyarısı verirse: **Ek bilgi → Yine de çalıştır.** (Program dijital imzasız olduğu için; kaynak kodu tamamen burada.)
Yönetici izni istemez, kendi kullanıcı klasörüne kurulur.

## ✨ Özellikler
- **Tek tuş kayıt** → sıkıştırılmış MP4 (720p küçük/dengeli, 1080p yüksek), en fazla 30 dk
- **Kamera hiç kapanmaz:** kayıt anında başlar, başta donma / kapanıp açılma yok
- **Canlı önizleme** + 3/5 sn **geri sayım**
- **NVIDIA hızlandırma (NVENC)**; yoksa otomatik işlemciyle kaydeder
- **NVIDIA Broadcast uyumlu:** sanal kamera + gürültüsüz mikrofon; "Kontrol Et" düğmesi ayarları sırayla denetler, yeşil arka planı ölçer
- **Kadraj:** yakınlaştır, kaydır, ortala (Logitech uygulamasındaki gibi)
- **OBS tarzı yatay ses göstergesi** (dB). Mikrofon NVIDIA Broadcast ise sese dokunmaz; normal mikrofonda ses yükselt/kıs
- **60 FPS** seçeneği, **üstte tut**, isteğe bağlı **video ismi** (`SS-DD_isim.mp4`)
- Ses-görüntü senkronu korunur

## 🟩 Yeşil ekran (arkası şeffaf video) için
1. NVIDIA Broadcast → Video → **Sanal arka plan: Değiştir** → tek renk **#00FF00** yeşil bir resim seç.
2. Kamera çözünürlüğünü 1920x1080 yap (kenarlar daha temiz olur).
3. SadeKayıt'ta kamera: **Camera (NVIDIA Broadcast)**, mikrofon: **Mikrofon (NVIDIA Broadcast)**.
4. Kurgu programında **Kroma anahtarı (Chroma Key)** ile yeşili sil.

NVIDIA Broadcast şart değil: SadeKayıt her web kamerasıyla çalışır.

## Gereksinimler
Windows 10/11 (64 bit), .NET Framework 4.x (Windows'ta hazır gelir). NVIDIA hızlandırma için NVIDIA ekran kartı (isteğe bağlı).

## 🔧 Kaynaktan derleme
Ubuntu / WSL:
```bash
sudo apt-get install -y mono-complete nsis python3-pip unzip
bash build.sh        # → SadeKayit_Kurulum.exe
```
Her `main` güncellemesinde GitHub Actions kurulumu otomatik derleyip Releases'e koyar.

## Lisans
SadeKayıt kodu **MIT** lisanslıdır — bkz. [LICENSE](LICENSE). Kurulum, ayrı bir program olarak **FFmpeg (GPL)** içerir — bkz. [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
NVIDIA Broadcast, NVIDIA Corporation'ın markasıdır; bu proje NVIDIA ile bağlantılı değildir.

Geliştiren: **Emre Bulut ([@plgArtC](https://github.com/plgArtC))** — Claude (Anthropic) yardımıyla yazıldı.

---

# SadeKayıt (English)

**One-click webcam recorder — no OBS needed.** A small, free, open-source Windows app for recording green-screen narrator / commentary videos with NVIDIA Broadcast (works with any webcam).

**Download:** [Releases](../../releases/latest) → `SadeKayit_Kurulum.exe`. If Windows SmartScreen warns you: *More info → Run anyway* (the app is unsigned; full source is here).

**Features:** one-click MP4 recording (up to 30 min) · camera never reopens, recording starts instantly · live preview + countdown · NVENC hardware encoding with automatic CPU fallback · NVIDIA Broadcast check button (virtual camera/mic, green-screen detection) · digital zoom/pan framing · OBS-style horizontal dB meter · 60 FPS option · always-on-top · custom file names · A/V sync preserved.

**Green screen:** in NVIDIA Broadcast set *Virtual Background → Replace* with a solid #00FF00 image, then remove it with Chroma Key in your editor.

UI is in Turkish (English UI may come later). **License:** MIT for SadeKayıt's code; the installer bundles FFmpeg (GPL) as a separate executable — see THIRD_PARTY_NOTICES.md. Not affiliated with NVIDIA. No commercial purpose — free for everyone.
