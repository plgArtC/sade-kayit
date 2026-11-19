// SadeKayıt — OBS'siz, tek tuşla kamera kaydı (NVIDIA Broadcast uyumlu). MIT Lisansı © 2026 Emre Bulut (plgArtC)
// https://github.com/plgArtC/sade-kayit
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EkbKayit
{
    static class P { [STAThread] static void Main() { Application.EnableVisualStyles(); Application.Run(new F()); } }

    public class F : Form
    {
        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SadeKayit");
        static readonly string AyarF = Path.Combine(Dir, "ayar.txt");
        static readonly string FF = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ffmpeg.exe");
        const int MAX_SN = 30 * 60;

        Color Arka = Color.FromArgb(18, 14, 12), Kutu = Color.FromArgb(40, 31, 24), Tur = Color.FromArgb(255, 138, 30), Yazi = Color.FromArgb(240, 232, 220), Soluk = Color.FromArgb(170, 150, 130);
        ComboBox cmbVid, cmbAud, cmbKalite, cmbSayim; Label lblSayac; int sayimSn; CheckBox chkHiz; TextBox txtKlasor; Button btnKayit, btnKlasor, btnAc, btnKontrol; Label lblSure, lblPv; CheckBox chkUst;
        PictureBox pv; RichTextBox rtB;
        System.Windows.Forms.Timer t;
        Process ff; bool kayitta, nvencYok, kapaniyor; DateTime bas, sonKare = DateTime.MinValue; string sonDosya;
        Dictionary<string, string> A = new Dictionary<string, string>();
        List<string> vidList = new List<string>(), audList = new List<string>();
        double yesilOran = -1, siyahOran = -1, parlaklik = -1;
        StringBuilder log = new StringBuilder();

        public F()
        {
            Text = "SadeKayıt — tek tuşla kamera kaydı"; BackColor = Arka; ForeColor = Yazi; Font = new Font("Segoe UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; ClientSize = new Size(900, 792);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            AyarOku();
            int y = 12;
            Lbl("Kamera", 12, y); cmbVid = Cmb(12, y + 22); y += 58;
            Lbl("Mikrofon", 12, y); cmbAud = Cmb(12, y + 22); y += 58;
            Lbl("Kalite", 12, y); cmbKalite = Cmb(12, y + 22);
            cmbKalite.Items.AddRange(new object[] { "Küçük dosya (720p, ~250 MB / 30 dk)", "Dengeli (720p, ~450 MB / 30 dk)", "Yüksek (1080p, ~900 MB / 30 dk)" });
            cmbKalite.SelectedIndex = Math.Min(2, Math.Max(0, Int("kalite", 0))); y += 58;
            Lbl("Kayıt klasörü", 12, y);
            txtKlasor = new TextBox { Left = 12, Top = y + 22, Width = 300, BackColor = Kutu, ForeColor = Yazi, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, Text = Al("klasor", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "SadeKayit")) };
            Controls.Add(txtKlasor);
            btnKlasor = Btn("Seç", 318, y + 20, 52); btnAc = Btn("Aç", 374, y + 20, 52);
            btnKlasor.Click += (s, e) => { using (var d = new FolderBrowserDialog { SelectedPath = txtKlasor.Text }) if (d.ShowDialog() == DialogResult.OK) { txtKlasor.Text = d.SelectedPath; Kaydet(); } };
            btnAc.Click += (s, e) => { try { Directory.CreateDirectory(txtKlasor.Text); Process.Start("explorer.exe", sonDosya != null && File.Exists(sonDosya) ? "/select,\"" + sonDosya + "\"" : txtKlasor.Text); } catch { } };
            y += 64;
            btnKayit = new Button { Text = "● KAYDI BAŞLAT", Left = 12, Top = y, Width = 414, Height = 56, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(150, 30, 20), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 15f), Cursor = Cursors.Hand };
            btnKayit.FlatAppearance.BorderColor = Tur; Controls.Add(btnKayit);
            btnKayit.Click += (s, e) => { if (!kayitta) KayitBasla(); else KayitDurdur(); };
            y += 64;
            lblSure = new Label { Left = 12, Top = y, Width = 196, Height = 30, Font = new Font("Consolas", 16f, FontStyle.Bold), ForeColor = Tur, Text = "00:00 / 30:00" };
            cmbSayim = new ComboBox { Left = 212, Top = y + 2, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Kutu, ForeColor = Yazi, FlatStyle = FlatStyle.Flat };
            cmbSayim.Items.AddRange(new object[] { "Sayım yok", "3 sn sayım", "5 sn sayım" });
            cmbSayim.SelectedIndex = Math.Min(2, Math.Max(0, Int("sayim", 2)));
            cmbSayim.SelectedIndexChanged += (s, e) => Kaydet(); Controls.Add(cmbSayim);
            chkUst = new CheckBox { Left = 342, Top = y + 4, Width = 90, Text = "Üstte tut", ForeColor = Yazi, Checked = Al("ust", "0") == "1" };
            chkUst.CheckedChanged += (s, e) => { TopMost = chkUst.Checked; Kaydet(); }; TopMost = chkUst.Checked;
            Controls.AddRange(new Control[] { lblSure, chkUst });

            // önizleme
            Lbl("Canlı önizleme (kayda giren görüntü)", 450, 12);
            pv = new PictureBox { Left = 450, Top = 34, Width = 436, Height = 246, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            lblPv = new Label { Left = 450, Top = 34, Width = 436, Height = 246, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Soluk, BackColor = Color.Black, Text = "Önizleme başlatılıyor..." };
            Controls.Add(lblPv); Controls.Add(pv); lblPv.BringToFront();
            lblSayac = new Label { Left = 450 + 168, Top = 34 + 73, Width = 100, Height = 100, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 54f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(150, 30, 20), Visible = false };
            Controls.Add(lblSayac);
            btnKontrol = new Button { Text = "✔ NVIDIA Broadcast'i Kontrol Et", Left = 450, Top = 288, Width = 436, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 70, 25), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 11f), Cursor = Cursors.Hand };
            btnKontrol.FlatAppearance.BorderColor = Color.FromArgb(118, 185, 0); Controls.Add(btnKontrol);
            btnKontrol.Click += (s, e) => Kontrol();
            chkHiz = new CheckBox { Left = 450, Top = 332, Width = 436, Text = "⚡ Yüksek kare hızı (60 FPS) — daha akıcı, gecikme daha az", ForeColor = Yazi, Checked = Al("hiz", "0") == "1" };
            chkHiz.CheckedChanged += (s, e) => { Kaydet(); if (!kayitta && ff != null) { Bildir("i", chkHiz.Checked ? "60 FPS açıldı. Broadcast'te kamera 60 FPS'e ayarlı olmalı." : "30 FPS'e dönüldü."); YenidenOnizle(); } };
            Controls.Add(chkHiz);

            // bildirimler
            Lbl("Bildirimler", 12, 352); KadrajKur();
            rtB = new RichTextBox { Left = 12, Top = 374, Width = 426, Height = 408, ReadOnly = true, BackColor = Color.FromArgb(10, 8, 7), ForeColor = Yazi, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
            Controls.Add(rtB);

            t = new System.Windows.Forms.Timer { Interval = 500 }; t.Tick += (s, e) => Tik(); t.Start();
            cmbVid.SelectedIndexChanged += (s, e) => { if (!kayitta && ff != null) { Kaydet(); YenidenOnizle(); } };
            cmbAud.SelectedIndexChanged += (s, e) => { if (!kayitta && ff != null) { Kaydet(); YenidenOnizle(); } };
            cmbKalite.SelectedIndexChanged += (s, e) => { if (!kayitta && ff != null) { Kaydet(); GecikmeliYeniden(); } };
            FormClosing += (s, e) =>
            {
                if (kayitta || bitiriliyor) { e.Cancel = true; Bildir("!", bitiriliyor ? "Kayıt dosyası hazırlanıyor, birkaç saniye bekle." : "Önce kaydı durdur, sonra kapat."); return; }
                kapaniyor = true; var p = ff; if (p != null) { Durdur(); try { p.WaitForExit(3000); } catch { } try { if (!p.HasExited) p.Kill(); } catch { } }
                Kaydet();
                try { if (capDir != null && Directory.Exists(capDir)) { foreach (var f in Directory.GetFiles(capDir)) try { File.Delete(f); } catch { } Directory.Delete(capDir); } } catch { }
            };
            Shown += (s, e) => { Cihazlar(); Onizle(); Bildir("i", "Program hazır. Kayıt klasörü: " + txtKlasor.Text); };
        }

        // ---------- yardımcılar ----------
        Label Lbl(string t, int x, int y) { var l = new Label { Text = t, Left = x, Top = y, AutoSize = true, ForeColor = Tur }; Controls.Add(l); return l; }
        ComboBox Cmb(int x, int y) { var c = new ComboBox { Left = x, Top = y, Width = 414, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Kutu, ForeColor = Yazi, FlatStyle = FlatStyle.Flat }; Controls.Add(c); return c; }
        Button Btn(string t, int x, int y, int w) { var b = new Button { Text = t, Left = x, Top = y, Width = w, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Kutu, ForeColor = Yazi }; b.FlatAppearance.BorderColor = Tur; Controls.Add(b); return b; }
        string Al(string k, string d) { return A.ContainsKey(k) ? A[k] : d; }
        int Int(string k, int d) { int v; return int.TryParse(Al(k, ""), out v) ? v : d; }
        void AyarOku() { try { foreach (var l in File.ReadAllLines(AyarF, Encoding.UTF8)) { int i = l.IndexOf('='); if (i > 0) A[l.Substring(0, i)] = l.Substring(i + 1); } } catch { } }
        void Kaydet()
        {
            try
            {
                A["klasor"] = txtKlasor.Text; A["ust"] = chkUst.Checked ? "1" : "0"; A["kalite"] = cmbKalite.SelectedIndex.ToString(); if (cmbSayim != null) A["sayim"] = cmbSayim.SelectedIndex.ToString(); if (chkHiz != null) A["hiz"] = chkHiz.Checked ? "1" : "0";
                if (cmbVid.SelectedItem != null) A["kamera"] = cmbVid.SelectedItem.ToString();
                if (cmbAud.SelectedItem != null) A["mikrofon"] = cmbAud.SelectedItem.ToString();
                Directory.CreateDirectory(Dir); File.WriteAllLines(AyarF, A.Select(kv => kv.Key + "=" + kv.Value), Encoding.UTF8);
            }
            catch { }
        }
        void Bildir(string tur, string m)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)(() => Bildir(tur, m))); } catch { } return; }
            Color c = tur == "ok" ? Color.FromArgb(120, 220, 90) : tur == "x" ? Color.FromArgb(255, 90, 70) : tur == "!" ? Color.FromArgb(255, 200, 60) : tur == "h" ? Tur : Soluk;
            string isaret = tur == "ok" ? "✔ " : tur == "x" ? "✖ " : tur == "!" ? "⚠ " : tur == "h" ? "▶ " : "• ";
            rtB.SelectionStart = rtB.TextLength; rtB.SelectionColor = Soluk; rtB.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  ");
            rtB.SelectionColor = c; rtB.SelectionFont = tur == "h" ? new Font(rtB.Font, FontStyle.Bold) : rtB.Font; rtB.AppendText(isaret + m + "\n");
            rtB.SelectionStart = rtB.TextLength; rtB.ScrollToCaret();
        }

        // ---------- cihazlar ----------
        void Cihazlar()
        {
            vidList.Clear(); audList.Clear();
            if (!File.Exists(FF)) { Bildir("x", "ffmpeg.exe bulunamadı: " + FF + " — programı yeniden kur."); return; }
            try
            {
                var p = new Process { StartInfo = new ProcessStartInfo(FF, "-hide_banner -list_devices true -f dshow -i dummy") { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true, StandardErrorEncoding = Encoding.UTF8 } };
                p.Start(); string o = p.StandardError.ReadToEnd(); p.WaitForExit(8000);
                bool v = true;
                foreach (var l in o.Split('\n'))
                {
                    if (l.Contains("DirectShow video devices")) { v = true; continue; }
                    if (l.Contains("DirectShow audio devices")) { v = false; continue; }
                    if (l.Contains("Alternative name")) continue;
                    var m = Regex.Match(l, "\\]\\s+\"(.+)\"");
                    if (!m.Success) continue;
                    var n = m.Groups[1].Value;
                    if (l.Contains("(audio)")) audList.Add(n); else if (l.Contains("(video)")) vidList.Add(n); else if (v) vidList.Add(n); else audList.Add(n);
                }
            }
            catch (Exception ex) { Bildir("x", "Cihazlar okunamadı: " + ex.Message); }
            vidList = vidList.Distinct().ToList(); audList = audList.Distinct().ToList();
            cmbVid.Items.Clear(); cmbVid.Items.AddRange(vidList.ToArray());
            cmbAud.Items.Clear(); cmbAud.Items.Add("(Ses yok)"); cmbAud.Items.AddRange(audList.ToArray());
            Sec(cmbVid, Al("kamera", ""), "NVIDIA Broadcast"); Sec(cmbAud, Al("mikrofon", ""), "NVIDIA Broadcast");
            if (vidList.Count == 0) Bildir("x", "Hiç kamera bulunamadı. NVIDIA Broadcast açık mı?");
        }
        void Sec(ComboBox c, string kayitli, string tercih)
        {
            int i = c.Items.IndexOf(kayitli);
            if (i < 0) for (int k = 0; k < c.Items.Count; k++) if (c.Items[k].ToString().IndexOf(tercih, StringComparison.OrdinalIgnoreCase) >= 0) { i = k; break; }
            if (i < 0 && c.Items.Count > 0) i = (c.Items.Count > 1 && c.Items[0].ToString() == "(Ses yok)") ? 1 : 0;
            if (i >= 0) c.SelectedIndex = i;
        }

        // ---------- yakalama: kamera program açıkken HİÇ kapanmaz, kayıt 1 sn'lik parçalardan birleştirilir ----------
        bool BroadcastMic { get { return cmbAud.SelectedItem != null && cmbAud.SelectedItem.ToString().IndexOf("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase) >= 0; } }
        string Kazanc { get { return BroadcastMic || sesKazanc == 100 ? "" : "volume=" + N(sesKazanc / 100.0) + ","; } }
        bool Hiz { get { return chkHiz != null && chkHiz.Checked; } }
        bool SesVar { get { return cmbAud.SelectedItem != null && cmbAud.SelectedItem.ToString() != "(Ses yok)"; } }
        const string METRE = "asetnsamples=n=4800:p=0,astats=metadata=1:reset=1,ametadata=mode=print:key=lavfi.astats.Overall.RMS_level,ametadata=mode=print:key=lavfi.astats.Overall.Peak_level";
        string PV { get { return "fps=" + (Hiz ? 30 : 15) + ",scale=436:-2"; } }
        const string PVOUT = "-c:v mjpeg -q:v 6 -f image2pipe pipe:1";
        string capDir; string Gecici { get { return capDir ?? Path.Combine(txtKlasor.Text, ".gecici"); } }
        bool capNvenc, yeniden; DateTime capBas; int capDeneme;
        int recBasIdx = -1; bool bitiriliyor;

        string YakalaArg(bool nvenc)
        {
            bool ses = SesVar;
            string v = cmbVid.SelectedItem.ToString();
            int q = cmbKalite.SelectedIndex; int h = q == 2 ? 1080 : 720; string fps = Hiz ? "60" : "30";
            string br = q == 0 ? "1000k" : q == 1 ? "1800k" : "3500k", mx = q == 0 ? "1500k" : q == 1 ? "2700k" : "5000k";
            string cq = q == 0 ? "30" : q == 1 ? "27" : "24", crf = q == 0 ? "28" : q == 1 ? "25" : "22";
            string enc = nvenc ? "-c:v h264_nvenc -preset p5 -tune hq -rc vbr -cq " + cq + " -b:v " + br + " -maxrate " + mx + " -bufsize " + mx + " -forced-idr 1"
                               : "-c:v libx264 -preset veryfast -crf " + crf + " -maxrate " + mx + " -bufsize " + mx + " -sc_threshold 0";
            string giris = "-fflags nobuffer -flags low_delay -f dshow -use_video_device_timestamps 0 " + (Hiz ? "-framerate 60 " : "") + "-rtbufsize 256M -thread_queue_size 1024 " + (ses ? "-audio_buffer_size 50 " : "") + "-i video=\"" + v + "\"" + (ses ? ":audio=\"" + cmbAud.SelectedItem + "\"" : "");
            string fc = "[0:v]split=2[r][p];[r]" + CropF() + "scale=-2:" + h + ":flags=lanczos,format=yuv420p[rec];[p]" + PV + "[pv]" + (ses ? ";[0:a]" + Kazanc + "aresample=async=1,asplit=2[arec][am0];[am0]" + METRE + "[am]" : "");
            string seg = "-map \"[rec]\"" + (ses ? " -map \"[arec]\" -c:a aac -b:a 128k -ar 48000" : "") + " -r " + fps + " " + enc
                       + " -g " + fps + " -keyint_min " + fps + " -force_key_frames \"expr:gte(t,n_forced*1)\""
                       + " -f segment -segment_time 1 -segment_format mpegts -reset_timestamps 0 \"" + Path.Combine(Gecici, "seg_%06d.ts") + "\"";
            return "-hide_banner -y " + giris + " -filter_complex \"" + fc + "\" " + seg + " -map \"[pv]\" " + PVOUT + (ses ? " -map \"[am]\" -f null NUL" : "");
        }

        void YakalaBaslat()
        {
            if (cmbVid.SelectedItem == null || kapaniyor || ff != null) return;
            capDir = Path.Combine(txtKlasor.Text, ".gecici");
            try { Directory.CreateDirectory(Gecici); try { File.SetAttributes(Gecici, FileAttributes.Directory | FileAttributes.Hidden); } catch { } if (!kayitta) foreach (var f in Directory.GetFiles(Gecici, "seg_*.ts")) try { File.Delete(f); } catch { } }
            catch (Exception ex) { Bildir("x", "Geçici klasör açılamadı: " + ex.Message); return; }
            capNvenc = !nvencYok; capBas = DateTime.Now;
            lock (log) log.Clear();
            var p = new Process { StartInfo = new ProcessStartInfo(FF, YakalaArg(capNvenc)) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }, EnableRaisingEvents = true };
            p.ErrorDataReceived += (s, e) =>
            {
                var d = e.Data; if (d == null) return;
                if (d.Contains("lavfi.astats")) { SesOku(d); return; }
                if (d.StartsWith("frame:") && d.Contains("pts_time")) return;
                lock (log) { log.AppendLine(d); if (log.Length > 30000) log.Remove(0, 15000); }
            };
            p.Exited += (s, e) => { try { BeginInvoke((Action)(() => YakalaBitti(p))); } catch { } };
            ff = p; p.Start(); p.BeginErrorReadLine();
            new Thread(() => Okuyucu(p)) { IsBackground = true }.Start();
        }
        void Onizle() { YakalaBaslat(); }
        void Durdur() { var p = ff; if (p == null) return; try { p.StandardInput.Write("q"); p.StandardInput.Flush(); } catch { } }
        void YenidenOnizle()
        {
            if (kayitta) { Bildir("!", "Kayıt sırasında bu ayar değiştirilemez."); return; }
            if (ff == null) { YakalaBaslat(); return; }
            yeniden = true; Durdur();
        }
        System.Windows.Forms.Timer gecikT;
        void GecikmeliYeniden()
        {
            if (gecikT == null) { gecikT = new System.Windows.Forms.Timer { Interval = 700 }; gecikT.Tick += (s, e) => { gecikT.Stop(); YenidenOnizle(); }; }
            gecikT.Stop(); gecikT.Start();
        }

        void YakalaBitti(Process p)
        {
            if (p != ff) return;
            ff = null;
            string l; lock (log) l = log.ToString();
            if (kapaniyor) return;
            if (yeniden) { yeniden = false; YakalaBaslat(); return; }
            bool erken = (DateTime.Now - capBas).TotalSeconds < 8;
            if (erken && capNvenc && (l.IndexOf("nvenc", StringComparison.OrdinalIgnoreCase) >= 0 || l.Contains("Cannot load") || l.Contains("No capable devices") || l.Contains("OpenEncodeSession")))
            { nvencYok = true; Bildir("!", "NVIDIA hızlandırma bu sürücüde açılamadı, işlemciyle kaydedilecek."); YakalaBaslat(); return; }
            if (erken && Hiz && (l.Contains("Could not set video options") || l.Contains("could not set")))
            { chkHiz.Checked = false; Bildir("!", "Kamera 60 FPS vermiyor. NVIDIA Broadcast'te \"... @ 60 FPS\" seç. Şimdilik 30 FPS."); YakalaBaslat(); return; }
            lblPv.Visible = true; lblPv.BringToFront(); lblPv.Text = "Kamera görüntüsü kesildi.\nYeniden bağlanılıyor...";
            Hata(l, "Kamera bağlantısı");
            if (kayitta) KayitDurdur();
            if (++capDeneme <= 20) { var tt = new System.Windows.Forms.Timer { Interval = 3000 }; tt.Tick += (s, e) => { tt.Stop(); tt.Dispose(); YakalaBaslat(); }; tt.Start(); }
        }

        void Okuyucu(Process p)
        {
            try
            {
                var st = p.StandardOutput.BaseStream; var buf = new byte[1 << 16]; var ms = new MemoryStream(); int n;
                while ((n = st.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, n);
                    var d = ms.GetBuffer(); int len = (int)ms.Length; int bas_ = -1, son = -1, i;
                    for (i = len - 2; i >= 1; i--) if (d[i] == 0xFF && d[i + 1] == 0xD9) { son = i + 2; break; }
                    if (son < 0) { if (len > 8 << 20) ms.SetLength(0); continue; }
                    for (i = son - 3; i >= 0; i--) if (d[i] == 0xFF && d[i + 1] == 0xD8 && d[i + 2] == 0xFF) { bas_ = i; break; }
                    if (bas_ >= 0)
                    {
                        try
                        {
                            var bmp = new Bitmap(new MemoryStream(d, bas_, son - bas_, false));
                            Analiz(bmp);
                            var gor = Kirp(bmp); if (!ReferenceEquals(gor, bmp)) bmp.Dispose();
                            BeginInvoke((Action)(() => { var eski = pv.Image; pv.Image = gor; if (eski != null) eski.Dispose(); lblPv.Visible = false; }));
                            sonKare = DateTime.Now; capDeneme = 0;
                        }
                        catch { }
                    }
                    var kalan = len - son; var yeni = new MemoryStream(); if (kalan > 0) yeni.Write(d, son, kalan); ms = yeni;
                }
            }
            catch { }
        }

        int analizSay;
        void Analiz(Bitmap b)
        {
            if (analizSay++ % 6 != 0) return;
            try
            {
                var r = new Rectangle(0, 0, b.Width, b.Height);
                var bd = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var px = new byte[bd.Stride * b.Height]; Marshal.Copy(bd.Scan0, px, 0, px.Length); b.UnlockBits(bd);
                int top = 0, ye = 0, si = 0; long par = 0;
                for (int y = 0; y < b.Height; y += 4) for (int x = 0; x < b.Width; x += 4)
                    {
                        int o = y * bd.Stride + x * 3; int B = px[o], G = px[o + 1], R = px[o + 2];
                        top++; par += (R + G + B) / 3;
                        if (G > 150 && R < 110 && B < 110) ye++;
                        if (R < 20 && G < 20 && B < 20) si++;
                    }
                yesilOran = top > 0 ? (double)ye / top : 0; siyahOran = top > 0 ? (double)si / top : 0; parlaklik = top > 0 ? (double)par / top : 0;
            }
            catch { }
        }

        // ---------- segment yardımcıları ----------
        List<int> Segmentler()
        {
            var l = new List<int>();
            try { foreach (var f in Directory.GetFiles(Gecici, "seg_*.ts")) { int i; if (int.TryParse(Path.GetFileNameWithoutExtension(f).Substring(4), out i)) l.Add(i); } } catch { }
            l.Sort(); return l;
        }
        int SonSeg() { var l = Segmentler(); return l.Count == 0 ? -1 : l[l.Count - 1]; }
        string SegYol(int i) { return Path.Combine(Gecici, "seg_" + i.ToString("000000") + ".ts"); }
        void Temizle()
        {
            if (bitiriliyor) return;
            var l = Segmentler(); if (l.Count == 0) return;
            int sinir = kayitta && recBasIdx >= 0 ? recBasIdx - 2 : l[l.Count - 1] - 3;
            foreach (var i in l) if (i < sinir) try { File.Delete(SegYol(i)); } catch { }
        }

        // ---------- kayıt ----------
        void KayitBasla()
        {
            if (bitiriliyor) { Bildir("!", "Önceki kayıt hâlâ kaydediliyor, birkaç saniye bekle."); return; }
            if (cmbVid.SelectedItem == null) { Cihazlar(); if (cmbVid.SelectedItem == null) return; }
            if ((DateTime.Now - sonKare).TotalSeconds > 3) { Bildir("x", "Kameradan görüntü gelmiyor, kayıt başlatılamadı. NVIDIA Broadcast açık mı?"); if (ff == null) YakalaBaslat(); return; }
            Kaydet();
            sonDosya = DosyaAdi();
            sayimSn = cmbSayim.SelectedIndex == 2 ? 5 : cmbSayim.SelectedIndex == 1 ? 3 : 0;
            kayitta = true; bas = DateTime.Now.AddSeconds(sayimSn); recBasIdx = -1;
            btnKayit.Text = "■ KAYDI DURDUR"; btnKayit.BackColor = Color.FromArgb(70, 70, 70);
            cmbVid.Enabled = cmbAud.Enabled = cmbKalite.Enabled = btnKlasor.Enabled = false; cmbSayim.Enabled = false; chkHiz.Enabled = false; chkAd.Enabled = txtAd.Enabled = false; KazancDurum();
            if (sayimSn > 0) Bildir("h", sayimSn + " sn geri sayım, sonra kayıt: " + Path.GetFileName(sonDosya));
            else KayitAnı();
        }
        void KazancDurum()
        {
            if (trkSes == null) return;
            bool nv = BroadcastMic;
            trkSes.Enabled = bSifir.Enabled = !nv && !kayitta; lblKazanc.ForeColor = nv ? Soluk : Tur; lblKazanc.Text = nv ? "kapalı" : "%" + sesKazanc;
            lblSesNot.Text = nv ? "NVIDIA Broadcast mikrofonu: ses olduğu gibi kaydedilir, ayar kapalı (sadece gösterge)."
                                : "Normal mikrofon: ses yükselt / kıs ayarı aktif (sadece bu kayda uygulanır).";
        }
        string Senkron(string dizi)
        {
            int ms = Int("senkron_ms", 0);
            if (ms == 0) return "-map 0:v -map 0:a? ";
            return "-fflags +genpts -itsoffset " + N(ms / 1000.0) + " -i \"concat:" + dizi + "\" -map 0:v -map 1:a? ";
        }
        string DosyaAdi()
        {
            string zaman = DateTime.Now.ToString("HH-mm"), ad = "";
            if (chkAd.Checked)
            {
                ad = txtAd.Text.Trim(); foreach (var c in Path.GetInvalidFileNameChars()) ad = ad.Replace(c.ToString(), "");
                ad = ad.Trim().TrimEnd('.'); if (ad.Length > 30) ad = ad.Substring(0, 30);
            }
            string ana = ad.Length > 0 ? zaman + "_" + ad : "Kayit_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), yol = Path.Combine(txtKlasor.Text, ana + ".mp4");
            for (int i = 1; File.Exists(yol); i++) yol = Path.Combine(txtKlasor.Text, ana + " (" + i + ").mp4");
            return yol;
        }
        void KayitAnı()
        {
            recBasIdx = Math.Max(0, SonSeg());
            lblSayac.Visible = false; Bildir("ok", "KAYIT BAŞLADI (anında, kamera kesintisiz) → " + Path.GetFileName(sonDosya));
        }
        void UIHazir()
        {
            kayitta = false;
            btnKayit.Text = "● KAYDI BAŞLAT"; btnKayit.BackColor = Color.FromArgb(150, 30, 20);
            cmbVid.Enabled = cmbAud.Enabled = cmbKalite.Enabled = btnKlasor.Enabled = true; cmbSayim.Enabled = true; chkHiz.Enabled = true; chkAd.Enabled = true; txtAd.Enabled = chkAd.Checked; KazancDurum(); lblSayac.Visible = false;
            lblSure.Text = "00:00 / 30:00"; lblSure.ForeColor = Tur;
        }
        void KayitDurdur()
        {
            if (!kayitta) return;
            if (recBasIdx < 0) { UIHazir(); Bildir("!", "Geri sayım sırasında durduruldu, kayıt alınmadı."); return; }
            int basI = recBasIdx, sonI = SonSeg(); string hedef = sonDosya; var basZ = bas;
            bitiriliyor = true; UIHazir(); btnKayit.Enabled = false; btnKayit.Text = "Kaydediliyor...";
            Bildir("i", "Kayıt durdu, dosya hazırlanıyor...");
            new Thread(() => Birlestir(basI, sonI, hedef, basZ)) { IsBackground = true }.Start();
        }
        void Birlestir(int basI, int sonI, string hedef, DateTime basZ)
        {
            string sonuc; bool ok = false;
            try
            {
                // o an yazılan parçanın kapanmasını bekle (en fazla 2.5 sn)
                for (int k = 0; k < 25 && SonSeg() <= sonI && ff != null; k++) Thread.Sleep(100);
                var parcalar = Enumerable.Range(basI, sonI - basI + 1).Where(i => File.Exists(SegYol(i))).ToList();
                if (parcalar.Count == 0) throw new Exception("kayıt parçası bulunamadı");
                var dizi = string.Join("|", parcalar.Select(i => SegYol(i)));
                var p = new Process { StartInfo = new ProcessStartInfo(FF, "-hide_banner -y -fflags +genpts -i \"concat:" + dizi + "\" " + Senkron(dizi) + "-c copy -avoid_negative_ts make_zero -movflags +faststart \"" + hedef + "\"") { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true } };
                p.Start(); string err = p.StandardError.ReadToEnd(); p.WaitForExit(120000);
                if (File.Exists(hedef) && new FileInfo(hedef).Length > 5000)
                {
                    ok = true;
                    foreach (var i in parcalar) try { File.Delete(SegYol(i)); } catch { }
                    sonuc = "Kaydedildi: " + Path.GetFileName(hedef) + "  (" + (new FileInfo(hedef).Length / 1048576.0).ToString("0.0") + " MB, ~" + parcalar.Count + " sn)";
                }
                else { sonuc = "Dosya birleştirilemedi: " + string.Join(" | ", err.Split('\n').Where(x => x.Trim().Length > 0).Reverse().Take(2).Reverse()); try { File.WriteAllText(Path.Combine(Dir, "son_hata.txt"), err); } catch { } }
            }
            catch (Exception ex) { sonuc = "Kayıt kaydedilemedi: " + ex.Message; }
            try { BeginInvoke((Action)(() => { bitiriliyor = false; btnKayit.Enabled = true; btnKayit.Text = "● KAYDI BAŞLAT"; Bildir(ok ? "ok" : "x", sonuc); })); } catch { }
        }
        void Hata(string l, string baslik)
        {
            var son = l.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith("frame=")).Reverse().Take(2).Reverse();
            Bildir("x", baslik + ": " + string.Join(" | ", son));
            try { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, "son_hata.txt"), l, Encoding.UTF8); } catch { }
        }

        int tikSay;
        void Tik()
        {
            if (tikSay++ % 4 == 0) Temizle();
            if (!kayitta) return;
            var kalanSn = (bas - DateTime.Now).TotalSeconds;
            if (kalanSn > 0)
            {
                lblSayac.Visible = true; lblSayac.BringToFront(); lblSayac.Text = ((int)Math.Ceiling(kalanSn)).ToString();
                lblSure.Text = "Hazırlan..."; lblSure.ForeColor = Tur; return;
            }
            if (recBasIdx < 0) KayitAnı();
            var g = (int)(DateTime.Now - bas).TotalSeconds;
            lblSure.Text = string.Format("{0:00}:{1:00} / 30:00", g / 60, g % 60);
            lblSure.ForeColor = (g % 2 == 0) ? Color.FromArgb(255, 70, 50) : Tur;
            if (g >= MAX_SN) { Bildir("i", "30 dakika doldu."); KayitDurdur(); }
        }

        // ---------- kadraj (dijital yakınlaştırma / konum) ----------
        double zum = 1.0, kx = 0.5, ky = 0.5; Label lblKadraj; TrackBar trkZum;
        static string N(double v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }
        void KadrajKur()
        {
            double.TryParse(Al("zum", "1"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out zum);
            double.TryParse(Al("kx", "0.5"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kx);
            double.TryParse(Al("ky", "0.5"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ky);
            zum = Math.Max(1, Math.Min(3, zum)); kx = Math.Max(0, Math.Min(1, kx)); ky = Math.Max(0, Math.Min(1, ky));
            Lbl("Kadraj (yakınlaştır / konum)", 450, 362);
            int[] gz = { 100, 120, 145 }; string[] ga = { "Geniş", "Orta", "Yakın" };
            for (int i = 0; i < 3; i++) { int z = gz[i]; var b = Btn(ga[i], 450 + i * 92, 388, 86); b.Height = 34; b.Click += (s, e) => { zum = z / 100.0; KadrajUygula(); }; }
            Controls.Add(new Label { Text = "Zoom", Left = 450, Top = 434, AutoSize = true, ForeColor = Soluk });
            trkZum = new TrackBar { Left = 500, Top = 430, Width = 220, Minimum = 100, Maximum = 300, SmallChange = 5, LargeChange = 20, TickStyle = TickStyle.None, Value = (int)Math.Round(zum * 100) };
            trkZum.ValueChanged += (s, e) => { if (Math.Abs(trkZum.Value / 100.0 - zum) > 0.001) { zum = trkZum.Value / 100.0; KadrajUygula(); } };
            Controls.Add(trkZum);
            lblKadraj = new Label { Left = 450, Top = 474, Width = 280, Height = 22, ForeColor = Tur };
            Controls.Add(lblKadraj);
            var bVar = Btn("↺ Varsayılana getir", 450, 502, 270); bVar.Height = 34;
            bVar.Click += (s, e) => { zum = 1; kx = ky = 0.5; KadrajUygula(); Bildir("i", "Kadraj varsayılana döndü."); };
            int cx = 790, cy = 430, z2 = 40;
            Ok("▲", cx, cy - 44, z2, () => ky -= 0.1); Ok("▼", cx, cy + 44, z2, () => ky += 0.1);
            Ok("◀", cx - 44, cy, z2, () => kx -= 0.1); Ok("▶", cx + 44, cy, z2, () => kx += 0.1);
            Ok("◎", cx, cy, z2, () => { kx = ky = 0.5; });
            var not_ = new Label { Left = 450, Top = 544, Width = 436, Height = 40, ForeColor = Soluk, Text = "Önizlemede gördüğün kadraj aynen kaydedilir. Kayıt sırasında değiştirilemez; kayıttan önce ayarla. Oklar zoom > %100 iken çalışır." };
            Controls.Add(not_);
            KadrajYaz();
            SesKur();
        }
        void Ok(string t, int x, int y, int w, Action a)
        {
            var b = Btn(t, x, y, w); b.Height = w; b.Font = new Font("Segoe UI", 12f);
            b.Click += (s, e) => { if (kayitta) return; a(); kx = Math.Max(0, Math.Min(1, kx)); ky = Math.Max(0, Math.Min(1, ky)); KadrajUygula(); };
        }
        void KadrajUygula()
        {
            if (kayitta) { Bildir("!", "Kayıt sırasında kadraj değiştirilemez."); return; }
            zum = Math.Max(1, Math.Min(3, zum));
            if (trkZum != null && trkZum.Value != (int)Math.Round(zum * 100)) trkZum.Value = (int)Math.Round(zum * 100);
            A["zum"] = N(zum); A["kx"] = N(kx); A["ky"] = N(ky); Kaydet(); KadrajYaz(); if (ff != null) GecikmeliYeniden();
        }
        void KadrajYaz()
        {
            if (lblKadraj == null) return;
            lblKadraj.Text = "Zoom %" + (int)Math.Round(zum * 100) + "   Yatay " + KonumAd(kx, "sol", "sağ") + "   Dikey " + KonumAd(ky, "üst", "alt");
        }
        static string KonumAd(double v, string a, string b) { return Math.Abs(v - 0.5) < 0.01 ? "orta" : (v < 0.5 ? a : b) + " %" + (int)Math.Round(Math.Abs(v - 0.5) * 200); }
        string CropF()
        {
            if (zum <= 1.001) return "";
            return "crop=trunc(iw/" + N(zum) + "/2)*2:trunc(ih/" + N(zum) + "/2)*2:(iw-iw/" + N(zum) + ")*" + N(kx) + ":(ih-ih/" + N(zum) + ")*" + N(ky) + ",";
        }
        Bitmap Kirp(Bitmap b)
        {
            if (zum <= 1.001) return b;
            int w = Math.Max(2, (int)(b.Width / zum)), h = Math.Max(2, (int)(b.Height / zum));
            int x = (int)((b.Width - w) * kx), y = (int)((b.Height - h) * ky);
            try { return b.Clone(new Rectangle(x, y, w, h), b.PixelFormat); } catch { return b; }
        }


        // ---------- mikrofon seviyesi ----------
        CheckBox chkAd; TextBox txtAd; int sesKazanc = 100; TrackBar trkSes; Label lblKazanc, lblSesNot; Button bSifir; System.Windows.Forms.Timer kazancT;
        double sonM = -70; DateTime sonSesVeri = DateTime.MinValue, sesliAn = DateTime.MinValue;
        Panel pnlSes; Label lblSesDb; 
        System.Windows.Forms.Timer sesT; bool sesUyarildi;
        void SesKur()
        {
            Lbl("Mikrofon sesi", 450, 590);
            lblSesDb = new Label { Left = 700, Top = 590, Width = 186, Height = 20, TextAlign = ContentAlignment.TopRight, ForeColor = Soluk, Text = "-" };
            Controls.Add(lblSesDb);
            pnlSes = new Panel { Left = 450, Top = 612, Width = 436, Height = 70, BackColor = Color.FromArgb(10, 8, 7), BorderStyle = BorderStyle.FixedSingle };
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(pnlSes, true, null);
            pnlSes.Paint += SesCiz; Controls.Add(pnlSes);
            sesKazanc = Math.Max(0, Math.Min(300, Int("kazanc", 100)));
            lblSesNot = new Label { Left = 450, Top = 684, Width = 436, Height = 18, ForeColor = Soluk, Font = new Font("Segoe UI", 8.5f) }; Controls.Add(lblSesNot);
            Controls.Add(new Label { Text = "Ses yükselt / kıs", Left = 450, Top = 708, AutoSize = true, ForeColor = Soluk });
            trkSes = new TrackBar { Left = 570, Top = 704, Width = 230, Minimum = 0, Maximum = 300, SmallChange = 5, LargeChange = 25, TickStyle = TickStyle.None, AutoSize = false, Height = 28, Value = sesKazanc };
            lblKazanc = new Label { Left = 806, Top = 708, Width = 40, ForeColor = Tur, Text = "%" + sesKazanc };
            bSifir = Btn("%100", 846, 704, 40); bSifir.Font = new Font("Segoe UI", 8f);
            Controls.Add(trkSes); Controls.Add(lblKazanc);
            kazancT = new System.Windows.Forms.Timer { Interval = 700 };
            kazancT.Tick += (s, e) => { kazancT.Stop(); A["kazanc"] = sesKazanc.ToString(); Kaydet(); if (!kayitta && ff != null) YenidenOnizle(); };
            trkSes.ValueChanged += (s, e) => { if (kayitta) { trkSes.Value = sesKazanc; return; } sesKazanc = trkSes.Value; lblKazanc.Text = "%" + sesKazanc; kazancT.Stop(); kazancT.Start(); };
            bSifir.Click += (s, e) => { trkSes.Value = 100; };
            cmbAud.SelectedIndexChanged += (s, e) => KazancDurum();
            KazancDurum();
            chkAd = new CheckBox { Left = 450, Top = 740, Width = 436, Text = "✎ Video ismini kendim koyacağım (en fazla 30 harf)", ForeColor = Yazi, Checked = false };
            txtAd = new TextBox { Left = 450, Top = 764, Width = 436, MaxLength = 30, BackColor = Kutu, ForeColor = Yazi, BorderStyle = BorderStyle.FixedSingle, Enabled = false };
            chkAd.CheckedChanged += (s, e) => { txtAd.Enabled = chkAd.Checked; if (chkAd.Checked) txtAd.Focus(); };
            Controls.Add(chkAd); Controls.Add(txtAd);
            sesT = new System.Windows.Forms.Timer { Interval = 60 }; sesT.Tick += (s, e) => SesTik(); sesT.Start();
        }
        double sonPk = -70, tepe = -70; DateTime tepeAn = DateTime.MinValue;
        void SesOku(string l)
        {
            int i = l.IndexOf("lavfi.astats.Overall."); if (i < 0) return;
            var m = Regex.Match(l.Substring(i), @"Overall\.(RMS|Peak)_level=(-?inf|-?[\d.]+)");
            if (!m.Success) return;
            double v; if (m.Groups[2].Value.Contains("inf") || !double.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) v = -70;
            v = Math.Max(-70, Math.Min(0, v));
            if (m.Groups[1].Value == "RMS") { sonM = v; if (v > -50) sesliAn = DateTime.Now; }
            else { sonPk = v; if (v >= tepe || (DateTime.Now - tepeAn).TotalSeconds > 1.5) { tepe = v; tepeAn = DateTime.Now; } }
            sonSesVeri = DateTime.Now;
        }
        double gosM = -70, gosP = -70;
        void SesTik()
        {
            if (pnlSes == null) return;
            bool veri = (DateTime.Now - sonSesVeri).TotalSeconds < 1.5;
            double hm = veri ? sonM : -70, hp = veri ? sonPk : -70;
            // OBS gibi: hızlı yüksel, yavaş düş
            gosM = hm > gosM ? hm : Math.Max(hm, gosM - 1.5);
            gosP = hp > gosP ? hp : Math.Max(hp, gosP - 1.5);
            if (!veri && (DateTime.Now - tepeAn).TotalSeconds > 1.5) tepe = -70;
            if (!SesVar) { lblSesDb.Text = "Mikrofon seçili değil"; lblSesDb.ForeColor = Soluk; }
            else if (!veri) { lblSesDb.Text = "Ses verisi yok"; lblSesDb.ForeColor = Soluk; }
            else
            {
                string durum = sonPk > -3 ? "ÇOK YÜKSEK / patlıyor" : sonM > -20 ? "iyi" : sonM > -40 ? "kısık" : "sessiz";
                lblSesDb.Text = (sonPk <= -69 ? "-∞" : sonPk.ToString("0.0")) + " dB  " + durum;
                lblSesDb.ForeColor = sonPk > -3 ? Color.FromArgb(255, 90, 70) : sonM > -20 ? Color.FromArgb(120, 220, 90) : sonM > -40 ? Color.FromArgb(240, 200, 70) : Soluk;
            }
            if (kayitta && recBasIdx >= 0 && SesVar)
            {
                if ((DateTime.Now - sesliAn).TotalSeconds > 8 && (DateTime.Now - bas).TotalSeconds > 8) { if (!sesUyarildi) { sesUyarildi = true; Bildir("!", "8 saniyedir mikrofondan ses gelmiyor! Mikrofon / Broadcast'i kontrol et."); } }
                else if ((DateTime.Now - sesliAn).TotalSeconds < 1) sesUyarildi = false;
            }
            pnlSes.Invalidate();
        }
        // OBS mikser tarzı yatay dB göstergesi: -60 ... 0 dB, yeşil / sarı / kırmızı, tepe çizgisi
        void SesCiz(object o, PaintEventArgs e)
        {
            var g = e.Graphics; int W = pnlSes.ClientSize.Width, H = pnlSes.ClientSize.Height;
            const double MIN = -60; int L = 8, Rr = W - 8, bw = Rr - L;
            Func<double, int> X = db => L + (int)Math.Round(bw * (Math.Max(MIN, Math.Min(0, db)) - MIN) / -MIN);
            Color yK = Color.FromArgb(40, 70, 35), sK = Color.FromArgb(75, 70, 30), kK = Color.FromArgb(80, 35, 30);
            Color yA = Color.FromArgb(80, 220, 70), sA = Color.FromArgb(250, 215, 60), kA = Color.FromArgb(255, 70, 50);
            Action<int, int, double, bool> Bar = (y, h, db, ince) =>
            {
                int xs = X(-20), xk = X(-9), xv = X(db);
                using (var b1 = new SolidBrush(yK)) g.FillRectangle(b1, L, y, xs - L, h);
                using (var b2 = new SolidBrush(sK)) g.FillRectangle(b2, xs, y, xk - xs, h);
                using (var b3 = new SolidBrush(kK)) g.FillRectangle(b3, xk, y, Rr - xk, h);
                if (db > MIN)
                {
                    using (var b1 = new SolidBrush(yA)) g.FillRectangle(b1, L, y, Math.Min(xv, xs) - L, h);
                    if (xv > xs) using (var b2 = new SolidBrush(sA)) g.FillRectangle(b2, xs, y, Math.Min(xv, xk) - xs, h);
                    if (xv > xk) using (var b3 = new SolidBrush(kA)) g.FillRectangle(b3, xk, y, xv - xk, h);
                }
            };
            // üst çubuk: tepe (peak) — alt çubuk: ortalama (RMS)
            Bar(8, 14, gosP, false);
            Bar(25, 14, gosM, false);
            // segment aralıkları (OBS görünümü)
            using (var ara = new SolidBrush(Color.FromArgb(10, 8, 7))) for (int x = L + 3; x < Rr; x += 4) g.FillRectangle(ara, x, 8, 1, 31);
            // tepe tutma çizgisi
            if (tepe > MIN + 0.5) { int tx = X(tepe); using (var pt = new SolidBrush(tepe > -9 ? kA : tepe > -20 ? sA : yA)) g.FillRectangle(pt, Math.Min(Rr - 2, tx - 1), 6, 2, 35); }
            // ölçek
            using (var f = new Font("Segoe UI", 7f)) using (var tb = new SolidBrush(Color.FromArgb(150, 140, 130))) using (var pn = new Pen(Color.FromArgb(110, 100, 90)))
                for (int db = -60; db <= 0; db += 5)
                {
                    int x = X(db); g.DrawLine(pn, x, 41, x, db % 10 == 0 ? 47 : 44);
                    if (db % 10 == 0) { string t = db.ToString(); var sz = g.MeasureString(t, f); g.DrawString(t, f, tb, Math.Max(0, Math.Min(W - sz.Width, x - sz.Width / 2)), 49); }
                }
        }

        // ---------- NVIDIA Broadcast kontrolü ----------
        void Kontrol()
        {
            Bildir("h", "NVIDIA BROADCAST KONTROLÜ");
            bool calisiyor = Process.GetProcesses().Any(p => { try { return p.ProcessName.IndexOf("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } });
            Bildir(calisiyor ? "ok" : "x", "1) NVIDIA Broadcast programı " + (calisiyor ? "açık." : "AÇIK DEĞİL — önce NVIDIA Broadcast'i aç."));
            Cihazlar();
            bool vk = vidList.Any(x => x.IndexOf("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase) >= 0);
            bool vm = audList.Any(x => x.IndexOf("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase) >= 0);
            Bildir(vk ? "ok" : "x", "2) Sanal kamera \"Camera (NVIDIA Broadcast)\" " + (vk ? "var." : "bulunamadı."));
            Bildir(vm ? "ok" : "!", "3) Sanal mikrofon (NVIDIA Broadcast) " + (vm ? "var." : "bulunamadı — Broadcast'in Mikrofon sekmesinde mikrofonunu seç."));
            string sk = cmbVid.SelectedItem == null ? "" : cmbVid.SelectedItem.ToString(), sm = cmbAud.SelectedItem == null ? "" : cmbAud.SelectedItem.ToString();
            Bildir(sk.Contains("NVIDIA Broadcast") ? "ok" : "!", "4) Seçili kamera: " + sk + (sk.Contains("NVIDIA Broadcast") ? "" : "  (Broadcast efektleri bu kamerada YOK)"));
            Bildir(sm.Contains("NVIDIA Broadcast") ? "ok" : "!", "5) Seçili mikrofon: " + sm + (sm.Contains("NVIDIA Broadcast") ? "  (gürültü giderme bu mikrofonda çalışır)" : ""));
            bool kareVar = (DateTime.Now - sonKare).TotalSeconds < 3;
            Bildir(kareVar ? "ok" : "x", "6) Kameradan görüntü " + (kareVar ? "geliyor." : "GELMİYOR — kamera başka bir programda (OBS vb.) açık olabilir."));
            if (kareVar)
            {
                if (parlaklik >= 0 && parlaklik < 12) Bildir("!", "   Görüntü neredeyse tamamen siyah. Broadcast'te kamera kaynağı (MX Brio) seçili mi?");
                if (yesilOran > 0.2) Bildir("ok", "7) Arka plan: YEŞİL perde aktif (%" + (int)(yesilOran * 100) + " yeşil). Edit'te Kroma anahtarı ile silinir.");
                else if (siyahOran > 0.25) Bildir("!", "7) Arka plan siyah görünüyor (\"Kaldır\" modu). Edit için \"Değiştir\" + yeşil resim daha iyi.");
                else Bildir("!", "7) Arka plan efekti görünmüyor — odanın arkası görünüyor. Broadcast'te Sanal arka plan açık mı?");
            }
            Bildir("i", "8) Broadcast ayarları (açık / kapalı):");
            var ayarlar = BroadcastAyarlari();
            if (ayarlar.Count == 0) Bildir("!", "   Bu Broadcast sürümünün ayarları okunamadı. Yukarıdaki görüntü testine güvenebilirsin.");
            foreach (var a in ayarlar) Bildir(a.Value == "AÇIK" ? "ok" : "i", "   " + a.Key + ": " + a.Value);
            Bildir("h", "Kontrol bitti.");
        }

        static readonly string[][] Esle = {
            new[]{"BackgroundReplacement","Arka plan değiştirme"}, new[]{"BGReplace","Arka plan değiştirme"},
            new[]{"BackgroundRemoval","Arka plan kaldırma"}, new[]{"BGRemoval","Arka plan kaldırma"},
            new[]{"BackgroundBlur","Arka plan bulanıklaştırma"}, new[]{"BGBlur","Arka plan bulanıklaştırma"},
            new[]{"AutoFrame","Otomatik kare"}, new[]{"EyeContact","Göz teması"}, new[]{"Vignette","Vinyet"},
            new[]{"VideoNoise","Video gürültüsü giderme"}, new[]{"VideoDenoise","Video gürültüsü giderme"},
            new[]{"SuperRes","RTX Video Super Resolution"}, new[]{"FrameGen","RTX Video Frame Generation"},
            new[]{"KeyLight","Sanal tuş ışığı"}, new[]{"RoomEcho","Oda yankısı giderme"}, new[]{"Echo","Yankı giderme"},
            new[]{"NoiseRemoval","Gürültü giderme"}, new[]{"Denoise","Gürültü giderme"}, new[]{"NoiseReduction","Gürültü giderme"},
            new[]{"StudioVoice","Stüdyo sesi"}, new[]{"SpeakerFocus","Konuşmacı odağı"} };

        Dictionary<string, string> BroadcastAyarlari()
        {
            var sonuc = new Dictionary<string, string>();
            foreach (var kok in new[] { Registry.CurrentUser, Registry.LocalMachine })
                foreach (var yol in new[] { @"Software\NVIDIA Corporation\NVIDIA Broadcast", @"Software\NVIDIA Corporation\Global\NvBroadcast", @"Software\NVIDIA Corporation\NVIDIA RTX Voice" })
                    try { using (var k = kok.OpenSubKey(yol)) if (k != null) Tara(k, "", sonuc, 0); } catch { }
            return sonuc;
        }
        void Tara(RegistryKey k, string on, Dictionary<string, string> s, int derin)
        {
            if (derin > 4) return;
            foreach (var ad in k.GetValueNames())
            {
                var tam = on + "/" + ad;
                foreach (var e in Esle)
                    if (tam.IndexOf(e[0], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var v = k.GetValue(ad); string deger;
                        if (v is int) deger = (int)v == 0 ? "kapalı" : "AÇIK";
                        else { var str = Convert.ToString(v); deger = str == "0" || str.Equals("false", StringComparison.OrdinalIgnoreCase) ? "kapalı" : str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase) ? "AÇIK" : str; }
                        var anahtar = (tam.IndexOf("mic", StringComparison.OrdinalIgnoreCase) >= 0 ? "Mikrofon – " : tam.IndexOf("speak", StringComparison.OrdinalIgnoreCase) >= 0 ? "Hoparlör – " : tam.IndexOf("cam", StringComparison.OrdinalIgnoreCase) >= 0 ? "Kamera – " : "") + e[1];
                        if (deger.Length > 40) deger = deger.Substring(0, 40) + "…";
                        if (!s.ContainsKey(anahtar) || deger == "AÇIK") s[anahtar] = deger;
                        break;
                    }
            }
            foreach (var alt in k.GetSubKeyNames())
                try { using (var sk = k.OpenSubKey(alt)) if (sk != null) Tara(sk, on + "/" + alt, s, derin + 1); } catch { }
        }
    }
}
