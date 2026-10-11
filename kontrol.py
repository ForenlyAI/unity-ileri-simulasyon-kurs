#!/usr/bin/env python3
"""Kendi sonuçlarınızı beklenen sonuçlarla karşılaştırır.
  python3 kontrol.py                         → beklenen/ klasörünün tutarlılığı (GitHub Actions ve Codespaces bunu koşar)
  python3 kontrol.py kafe-robot-ileri/Olcumler → kendi Unity koşumunuzun ölçümleri beklenenle aynı mı
Makineye bağlı sayılar (kare süresi, milisaniye, bellek, derleme süresi) karşılaştırılmaz; geri kalan her sayı aynı çıkmalıdır."""
import json, pathlib, sys
K = pathlib.Path(__file__).resolve().parent
B = K / "beklenen" / "olcumler"
MAKINEYE_BAGLI = ("_ms", "ms_", "bellek", "derleme_s", "profil_kb", "kare", "gc_", "en_yakin", "kapi_masa", "yurunen", "sure_s", "yol_m", "ort_", "en_uzun", "en_dusuk", "musteri_", "takilan", "paket_kb", "toplam_kb")
KURS_KOSUMU = {"hata-donme", "kayit", "web-ci", "gorsel"}   # Unity testi yazmaz: kurs hazırlanırken alınan kayıt
CANLI = {"vardiya-canli", "yuruyus", "kacinma", "profil", "darbogaz", "addr-yukleme", "addressables", "gorsel"}
def duz(d, on=""):
    for a, v in d.items():
        if isinstance(v, dict): yield from duz(v, on + a + ".")
        else: yield on + a, v
def main():
    if len(sys.argv) < 2:
        o = json.loads((K / "beklenen" / "OLCUMLER.json").read_text())
        dosya = sorted(B.glob("*.json"))
        assert o["unity_surum"] == "6000.3.26f1" and len(dosya) >= 15, "beklenen klasörü eksik"
        h = json.loads((B / "vardiya-hizli.json").read_text())
        assert h["servis"] == o["d4_4_hizli_servis"], "beklenen dosyalar birbirini tutmuyor"
        print(f"beklenen tutarlı: {len(dosya)} ölçüm dosyası · OLCUMLER {len(o)} anahtar · hızlı vardiya {h['servis']} servis")
        return 0
    sizin = pathlib.Path(sys.argv[1]); fark = 0; sayi = 0
    for f in sorted(B.glob("*.json")):
        if f.stem in KURS_KOSUMU: continue
        s = sizin / f.name
        if not s.exists(): print("YOK  ", f.name); fark += 1; continue
        for (a, v) in duz(json.loads(f.read_text())):
            if f.stem in CANLI or any(m in a for m in MAKINEYE_BAGLI): continue
            sv = dict(duz(json.loads(s.read_text()))).get(a); sayi += 1
            if sv != v: print(f"FARK  {f.name} {a}: beklenen {v} · sizin {sv}"); fark += 1
    print(f"{sayi} değer karşılaştırıldı · fark {fark}")
    return 1 if fark else 0
sys.exit(main())
