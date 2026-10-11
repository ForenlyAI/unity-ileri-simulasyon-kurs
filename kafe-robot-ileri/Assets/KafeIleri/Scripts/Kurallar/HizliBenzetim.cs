// Ders 2.2 ve 2.3 · Hızlı hesap: robot programı, saat zıplatılarak koşar. Yürüme süresi = NavMesh yol uzunluğu ÷ robot hızı.
// Bir saatlik vardiya bir saniyenin altında biter; politika ve eşik karşılaştırmaları bununla yapılır.
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KafeIleri
{
    public class MesafeTablosu : IMesafe
    {
        public readonly double[,] m;
        public MesafeTablosu(double[,] m) { this.m = m; }
        public double Mesafe(int a, int b) => m[a, b];
        public int Boyut => m.GetLength(0);
    }

    public class HizliBeden : IBeden
    {
        readonly IMesafe d; readonly double hiz;
        public double Simdi { get; private set; }
        public int Nokta { get; private set; } = KafeIleri.Nokta.Ev;
        public double Yol { get; private set; }
        public HizliBeden(IMesafe d, double hiz) { this.d = d; this.hiz = hiz; }

        public IEnumerator Yuru(int hedef, Pil pil)
        {
            double m = d.Mesafe(Nokta, hedef), sn = m / hiz;
            Simdi += sn; Yol += m; pil.Yuru(sn); Nokta = hedef;
            yield break;
        }
        public IEnumerator Bekle(double sn, Pil pil) { Simdi += sn; pil.Bekle(sn); yield break; }
        public IEnumerator Sarj(Pil pil) { double sn = pil.DolmaSuresi; Simdi += sn; pil.Sarj(sn); yield break; }
    }

    public class VardiyaSonucu
    {
        public int gelen, kacan, oturan, servis, bekleyen, sarj, kabul, red, enUzunSira;
        public double ortBekleme, enUzunBekleme, ortServis, enDusukPil, yol, bitis;
        public bool pilBitti;

        public static VardiyaSonucu Hesapla(RobotProgrami p, IBeden b)
        {
            var ms = p.kafe.musteriler;
            var alinan = ms.Where(m => m.siparis >= 0).ToList();
            var biten = ms.Where(m => m.servis >= 0).ToList();
            return new VardiyaSonucu
            {
                gelen = ms.Count, kacan = ms.Count(m => m.kacti), oturan = ms.Count(m => m.oturma >= 0),
                servis = biten.Count, bekleyen = ms.Count(m => !m.kacti && m.servis < 0),
                ortBekleme = alinan.Count > 0 ? alinan.Average(m => m.Bekleme) : 0,
                enUzunBekleme = alinan.Count > 0 ? alinan.Max(m => m.Bekleme) : 0,
                ortServis = biten.Count > 0 ? biten.Average(m => m.servis - m.oturma) : 0,
                sarj = p.SarjSayisi, enDusukPil = p.pil.EnDusuk, pilBitti = p.pil.Bitti, yol = b.Yol, bitis = b.Simdi,
                kabul = p.makine.Kabul, red = p.makine.Red, enUzunSira = p.kafe.EnUzunSira,
            };
        }

        public Dictionary<string, object> Sozluk() => new Dictionary<string, object>
        {
            { "gelen", gelen }, { "kacan", kacan }, { "oturan", oturan }, { "servis", servis }, { "bekleyen", bekleyen },
            { "ort_bekleme_s", System.Math.Round(ortBekleme, 1) }, { "en_uzun_bekleme_s", System.Math.Round(enUzunBekleme, 1) },
            { "ort_servis_s", System.Math.Round(ortServis, 1) }, { "sarj", sarj }, { "en_dusuk_pil", System.Math.Round(enDusukPil, 1) },
            { "pil_bitti", pilBitti }, { "yol_m", System.Math.Round(yol, 1) }, { "kabul", kabul }, { "red", red }, { "en_uzun_sira", enUzunSira },
        };
    }

    public static class HizliBenzetim
    {
        // İç içe IEnumerator'ları sırayla tüketir (Unity'nin coroutine'i gibi, ama beklemeden).
        public static void Tuket(IEnumerator e)
        {
            var yigin = new Stack<IEnumerator>(); yigin.Push(e);
            while (yigin.Count > 0)
            {
                var ust = yigin.Peek();
                if (!ust.MoveNext()) { yigin.Pop(); continue; }
                if (ust.Current is IEnumerator ic) yigin.Push(ic);
            }
        }

        public static (VardiyaSonucu sonuc, RobotProgrami program) Kos(VardiyaAyar a, MesafeTablosu d)
        {
            int masa = (d.Boyut - KafeIleri.Nokta.IlkMasa) / 2;
            var kafe = new KafeModeli(masa, a);
            kafe.oturmaSuresi = m => d.Mesafe(KafeIleri.Nokta.Kapi, KafeIleri.Nokta.Oturma(m.masa, masa)) / a.musteriHizi;
            var p = new RobotProgrami(a, kafe, d);
            var b = new HizliBeden(d, a.robotHizi);
            Tuket(p.Calis(b));
            return (VardiyaSonucu.Hesapla(p, b), p);
        }
    }
}
