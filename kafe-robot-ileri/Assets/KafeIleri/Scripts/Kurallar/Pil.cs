// Ders 2.3 · Şarj Kararı — pil yüzdesi (saf C#). Yürümek, beklemek ve şarj olmak yüzdeyi değiştirir.
namespace KafeIleri
{
    public class Pil
    {
        readonly PilAyar a;
        public double Yuzde { get; private set; }
        public double EnDusuk { get; private set; }
        public bool Bitti { get; private set; }
        public Pil(PilAyar ayar) { a = ayar; Yuzde = EnDusuk = ayar.baslangic; }

        public bool Azaldi => Yuzde <= a.esik;
        public bool Doldu => Yuzde >= a.hedef;
        public double DolmaSuresi => System.Math.Max(0, a.hedef - Yuzde) / a.sarjHizi;

        public void Yuru(double sn) => Dus(sn * a.yuruTuketim);
        public void Bekle(double sn) => Dus(sn * a.bekleTuketim);
        public void Sarj(double sn) { Yuzde = System.Math.Min(100, Yuzde + sn * a.sarjHizi); }

        void Dus(double d)
        {
            Yuzde -= d;
            if (Yuzde <= 0) { Yuzde = 0; Bitti = true; }
            if (Yuzde < EnDusuk) EnDusuk = Yuzde;
        }
    }
}
