// Benzetimin bütün ayarları tek yerde. Robot hızı benzetim kaydından gelir (Veri/kayit_ozet.json);
// geri kalan her değer ÖRNEK'tir: gerçek bir kafenin ya da gerçek bir robotun ölçüsü değildir.
namespace KafeIleri
{
    [System.Serializable]
    public class PilAyar
    {
        public double baslangic = 60;      // % (ÖRNEK)
        public double esik = 30;           // bu yüzdenin altında, boştaysa şarja gider (ÖRNEK)
        public double hedef = 90;          // şarj bu yüzdeye kadar sürer (ÖRNEK)
        public double yuruTuketim = 0.04;  // yürürken % / sn (ÖRNEK)
        public double bekleTuketim = 0.005;// dururken % / sn (ÖRNEK)
        public double sarjHizi = 0.25;     // istasyonda % / sn (ÖRNEK)
    }

    public enum Politika { SiraIle, EnYakin }

    [System.Serializable]
    public class VardiyaAyar
    {
        public double robotHizi = 0.41;        // m / sn — benzetim kaydının düzlenmiş en büyük hızı
        public double musteriHizi = 1.0;       // m / sn (ÖRNEK)
        public double ortalamaGelis = 150;     // iki müşteri arası ortalama sn (ÖRNEK)
        public double vardiya = 3600;          // sn (ÖRNEK: bir saat)
        public double siparisAlma = 10;        // sn (ÖRNEK)
        public double hazirlama = 30;          // tezgâhta içecek hazırlama, sn (ÖRNEK)
        public double servis = 5;              // bırakma, sn (ÖRNEK)
        public double kalma = 600;             // servisten sonra müşterinin oturduğu süre, sn (ÖRNEK)
        public int tohum = 7;                  // aynı tohum, aynı müşteri akışı
        public Politika politika = Politika.SiraIle;
        public PilAyar pil = new PilAyar();
    }
}
