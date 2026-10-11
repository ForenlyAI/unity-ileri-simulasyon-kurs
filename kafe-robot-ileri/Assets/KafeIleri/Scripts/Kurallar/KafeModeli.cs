// Kafenin durumu (saf C#): masalar, müşteriler, sipariş sırası. Hem hızlı hesap hem canlı benzetim bunu kullanır.
// Nokta numaraları: 0 bekleme yeri, 1 tezgâh, 2 şarj istasyonu, 3 kapı, 4.. masaların servis noktaları, sonra oturma noktaları.
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafeIleri
{
    public static class Nokta
    {
        public const int Ev = 0, Tezgah = 1, Sarj = 2, Kapi = 3, IlkMasa = 4;
        public static int Servis(int masa) => IlkMasa + masa;
        public static int Oturma(int masa, int masaSayisi) => IlkMasa + masaSayisi + masa;
    }

    public interface IMesafe { double Mesafe(int a, int b); }

    public class Musteri
    {
        public int no, masa = -1;
        public double kapi, oturma = -1, siparis = -1, servis = -1, kalkis = -1;
        public bool kacti, alindi, kalkti;
        public double Bekleme => siparis - oturma;
    }

    public class KafeModeli
    {
        public readonly int masaSayisi;
        public readonly List<Musteri> musteriler = new List<Musteri>();
        readonly bool[] dolu;
        readonly List<double> gelisler;
        int sonraki;
        readonly Random r;
        public Func<Musteri, double> oturmaSuresi;   // hızlı hesapta kapı → masa yürüyüşü; canlıda null (ajan varınca Oturdu çağrılır)
        public event Action<Musteri> KapidaBelirdi;
        public int EnUzunSira { get; private set; }

        public KafeModeli(int masaSayisi, VardiyaAyar a)
        {
            this.masaSayisi = masaSayisi;
            dolu = new bool[masaSayisi];
            gelisler = MusteriAkisi.Gelisler(a.tohum, a.ortalamaGelis, a.vardiya);
            r = new Random(a.tohum * 31 + 1);
        }

        public int GelisSayisi => gelisler.Count;

        void Bosalt(double t)
        {
            foreach (var m in musteriler)
                if (m.masa >= 0 && !m.kalkti && m.kalkis >= 0 && m.kalkis <= t) { dolu[m.masa] = false; m.kalkti = true; }   // her müşteri masasını bir kez boşaltır
        }

        public void Ilerle(double t)
        {
            while (sonraki < gelisler.Count && gelisler[sonraki] <= t)
            {
                double g = gelisler[sonraki];
                Bosalt(g);
                var m = new Musteri { no = sonraki + 1, kapi = g };
                m.masa = MusteriAkisi.MasaSec(r, dolu);
                if (m.masa < 0) m.kacti = true;
                else
                {
                    dolu[m.masa] = true;
                    if (oturmaSuresi != null) m.oturma = g + oturmaSuresi(m);
                }
                musteriler.Add(m);
                sonraki++;
                if (!m.kacti) KapidaBelirdi?.Invoke(m);
            }
            Bosalt(t);
            int sira = Bekleyenler(t).Count();
            if (sira > EnUzunSira) EnUzunSira = sira;
        }

        public void Oturdu(Musteri m, double t) { if (m.oturma < 0) m.oturma = t; }

        public IEnumerable<Musteri> Bekleyenler(double t) =>
            musteriler.Where(m => !m.kacti && !m.alindi && m.oturma >= 0 && m.oturma <= t);

        public Musteri Sec(double t, Politika p, int robotNokta, IMesafe d)
        {
            var b = Bekleyenler(t).ToList();
            if (b.Count == 0) return null;
            var s = p == Politika.SiraIle
                ? b.OrderBy(m => m.oturma).First()
                : b.OrderBy(m => d.Mesafe(robotNokta, Nokta.Servis(m.masa))).ThenBy(m => m.oturma).First();
            s.alindi = true;
            return s;
        }
    }
}
