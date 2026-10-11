// Ders 2.1 · Durumlar ve Geçişler — robotun görev durum makinesi (saf C#, Unity'ye bağlı değil).
// Dört durum, beş izinli geçiş. Tabloda olmayan her istek reddedilir ve sayılır.
using System.Collections.Generic;

namespace KafeIleri
{
    public enum Durum { Bekle, SiparisAl, ServisYap, SarjOl }
    public enum Olay { MusteriGeldi, SiparisAlindi, ServisBitti, PilAzaldi, PilDoldu }

    public class GorevMakinesi
    {
        static readonly Dictionary<(Durum, Olay), Durum> gecisler = new Dictionary<(Durum, Olay), Durum>
        {
            { (Durum.Bekle, Olay.MusteriGeldi), Durum.SiparisAl },
            { (Durum.SiparisAl, Olay.SiparisAlindi), Durum.ServisYap },
            { (Durum.ServisYap, Olay.ServisBitti), Durum.Bekle },
            { (Durum.Bekle, Olay.PilAzaldi), Durum.SarjOl },
            { (Durum.SarjOl, Olay.PilDoldu), Durum.Bekle },
        };

        public static int GecisSayisi => gecisler.Count;
        public static int DurumSayisi => System.Enum.GetValues(typeof(Durum)).Length;
        public static IEnumerable<(Durum, Olay, Durum)> Tablo()
        {
            foreach (var kv in gecisler) yield return (kv.Key.Item1, kv.Key.Item2, kv.Value);
        }

        public Durum Simdiki { get; private set; } = Durum.Bekle;
        public int Kabul { get; private set; }
        public int Red { get; private set; }
        public readonly List<string> Gunluk = new List<string>();
        public event System.Action<Durum, Durum> Degisti;

        public bool Gec(Olay olay)
        {
            if (gecisler.TryGetValue((Simdiki, olay), out var yeni))
            {
                Gunluk.Add($"{Simdiki} > {olay} > {yeni}");
                var eski = Simdiki; Simdiki = yeni; Kabul++;
                Degisti?.Invoke(eski, yeni);
                return true;
            }
            Gunluk.Add($"{Simdiki} > {olay} > RED");
            Red++;
            return false;
        }
    }
}
