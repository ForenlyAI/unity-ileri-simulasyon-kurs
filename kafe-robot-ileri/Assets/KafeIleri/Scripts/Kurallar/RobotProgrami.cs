// Robotun görev programı (saf C#). Aynı program iki bedenle çalışır:
//   HizliBeden — saat zıplatılarak, NavMesh yol uzunluklarıyla (saniyeler içinde bir saatlik vardiya)
//   CanliBeden — Unity'de Play modunda, NavMeshAgent yürüyerek (Bilesenler/CanliBeden.cs)
using System.Collections;

namespace KafeIleri
{
    public interface IBeden
    {
        double Simdi { get; }
        int Nokta { get; }
        double Yol { get; }
        IEnumerator Yuru(int hedef, Pil pil);
        IEnumerator Bekle(double sn, Pil pil);
        IEnumerator Sarj(Pil pil);
    }

    public class RobotProgrami
    {
        public readonly GorevMakinesi makine = new GorevMakinesi();
        public readonly Pil pil;
        public readonly KafeModeli kafe;
        readonly VardiyaAyar a;
        readonly IMesafe mesafe;
        public int SarjSayisi { get; private set; }

        public RobotProgrami(VardiyaAyar ayar, KafeModeli kafe, IMesafe mesafe)
        {
            a = ayar; this.kafe = kafe; this.mesafe = mesafe; pil = new Pil(ayar.pil);
        }

        public IEnumerator Calis(IBeden b)
        {
            while (b.Simdi < a.vardiya && !pil.Bitti)
            {
                kafe.Ilerle(b.Simdi);
                if (makine.Simdiki == Durum.Bekle && pil.Azaldi)
                {
                    makine.Gec(Olay.PilAzaldi);
                    yield return b.Yuru(KafeIleri.Nokta.Sarj, pil);
                    SarjSayisi++;
                    yield return b.Sarj(pil);
                    makine.Gec(Olay.PilDoldu);
                    continue;
                }
                var m = kafe.Sec(b.Simdi, a.politika, b.Nokta, mesafe);
                if (m != null)
                {
                    makine.Gec(Olay.MusteriGeldi);
                    yield return b.Yuru(KafeIleri.Nokta.Servis(m.masa), pil);
                    m.siparis = b.Simdi;
                    yield return b.Bekle(a.siparisAlma, pil);
                    makine.Gec(Olay.SiparisAlindi);
                    yield return b.Yuru(KafeIleri.Nokta.Tezgah, pil);
                    yield return b.Bekle(a.hazirlama, pil);
                    if (pil.Azaldi) makine.Gec(Olay.PilAzaldi);   // serviste şarja gidilmez: istek reddedilir, önce servis biter
                    yield return b.Yuru(KafeIleri.Nokta.Servis(m.masa), pil);
                    yield return b.Bekle(a.servis, pil);
                    m.servis = b.Simdi;
                    m.kalkis = b.Simdi + a.kalma;
                    makine.Gec(Olay.ServisBitti);
                    continue;
                }
                if (b.Nokta != KafeIleri.Nokta.Ev) yield return b.Yuru(KafeIleri.Nokta.Ev, pil);
                else yield return b.Bekle(1.0, pil);
            }
            kafe.Ilerle(b.Simdi);
        }
    }
}
