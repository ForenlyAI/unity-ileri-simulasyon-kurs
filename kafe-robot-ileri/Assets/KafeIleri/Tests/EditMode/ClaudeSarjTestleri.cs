using System;
using System.Linq;
using NUnit.Framework;

namespace KafeIleri.Testler
{
    public class ClaudeSarjTestleri
    {
        private const int NoktaSayisi = 16;
        private const double Mesafe = 5.0;

        private static MesafeTablosu SabitMesafeTablosu()
        {
            var m = new double[NoktaSayisi, NoktaSayisi];
            for (int i = 0; i < NoktaSayisi; i++)
            {
                for (int j = 0; j < NoktaSayisi; j++)
                {
                    m[i, j] = i == j ? 0.0 : Mesafe;
                }
            }
            return new MesafeTablosu(m);
        }

        private static VardiyaAyar DusukPilliAyar()
        {
            return new VardiyaAyar
            {
                robotHizi = 1.0,
                vardiya = 3600.0,
                ortalamaGelis = 60.0,
                pil = new PilAyar
                {
                    baslangic = 20.0,
                    esik = 30.0,
                    hedef = 90.0,
                    yuruTuketim = 0.05,
                    bekleTuketim = 0.01,
                    sarjHizi = 0.5
                }
            };
        }

        private static VardiyaAyar EsikUstundeYogunAyar()
        {
            return new VardiyaAyar
            {
                robotHizi = 1.0,
                vardiya = 1800.0,
                ortalamaGelis = 15.0,
                pil = new PilAyar
                {
                    baslangic = 32.0,
                    esik = 30.0,
                    hedef = 90.0,
                    yuruTuketim = 0.05,
                    bekleTuketim = 0.01,
                    sarjHizi = 0.5
                }
            };
        }

        private static string[] Parcala(string gecis)
        {
            return gecis.Split(new[] { " > " }, StringSplitOptions.None)
                        .Select(p => p.Trim())
                        .ToArray();
        }

        [Test]
        public void PilEsigin_Altindayken_IlkGecis_SarjOlmali()
        {
            var ayar = DusukPilliAyar();
            var tablo = SabitMesafeTablosu();

            var (_, program) = HizliBenzetim.Kos(ayar, tablo);

            Assert.That(program, Is.Not.Null, "RobotProgrami null döndü.");
            Assert.That(program.makine, Is.Not.Null, "Durum makinesi null.");
            var gunluk = program.makine.Gunluk;
            Assert.That(gunluk, Is.Not.Null.And.Not.Empty, "Günlük boş; hiç geçiş kaydedilmemiş.");

            var ilk = Parcala(gunluk[0]);
            Assert.That(ilk.Length, Is.EqualTo(3), $"İlk günlük kaydı beklenen biçimde değil: \"{gunluk[0]}\"");
            Assert.That(ilk[0], Is.EqualTo("Bekle"), $"İlk geçiş Bekle durumundan başlamalı: \"{gunluk[0]}\"");
            Assert.That(ilk[1], Is.EqualTo("PilAzaldi"), $"İlk olay PilAzaldi olmalı; pil azken müşteri alınmamalı: \"{gunluk[0]}\"");
            Assert.That(ilk[2], Is.EqualTo("SarjOl"), $"İlk geçiş SarjOl durumuna gitmeli: \"{gunluk[0]}\"");
        }

        [Test]
        public void SarjOlurken_MusteriGeldi_HicKabulEdilmemeli()
        {
            var ayar = DusukPilliAyar();
            var tablo = SabitMesafeTablosu();

            var (_, program) = HizliBenzetim.Kos(ayar, tablo);

            Assert.That(program, Is.Not.Null, "RobotProgrami null döndü.");
            Assert.That(program.makine, Is.Not.Null, "Durum makinesi null.");
            var gunluk = program.makine.Gunluk;
            Assert.That(gunluk, Is.Not.Null, "Günlük null.");

            var kabulEdilenler = gunluk
                .Select((kayit, sira) => new { kayit, sira, parca = Parcala(kayit) })
                .Where(x => x.parca.Length >= 3
                            && x.parca[0] == "SarjOl"
                            && x.parca[1] == "MusteriGeldi"
                            && x.parca[2] != "RED")
                .Select(x => $"#{x.sira}: {x.kayit}")
                .ToList();

            Assert.That(kabulEdilenler, Is.Empty,
                "Şarj sırasında MusteriGeldi olayı kabul edilmemeli. Hatalı geçişler:\n" +
                string.Join("\n", kabulEdilenler));
        }

        [Test]
        public void Sirada_Musteri_Varken_Pil_Azalinca_Once_Sarj()
        {
            var ayar = EsikUstundeYogunAyar();
            var tablo = SabitMesafeTablosu();

            var (sonuc, program) = HizliBenzetim.Kos(ayar, tablo);

            Assert.That(sonuc, Is.Not.Null, "VardiyaSonucu null döndü.");
            Assert.That(program, Is.Not.Null, "RobotProgrami null döndü.");
            Assert.That(program.makine, Is.Not.Null, "Durum makinesi null.");
            var gunluk = program.makine.Gunluk;
            Assert.That(gunluk, Is.Not.Null.And.Not.Empty, "Günlük boş; hiç geçiş kaydedilmemiş.");

            var sarjaGecisler = gunluk
                .Select(Parcala)
                .Count(p => p.Length == 3
                            && p[0] == "Bekle"
                            && p[1] == "PilAzaldi"
                            && p[2] == "SarjOl");

            Assert.That(sarjaGecisler, Is.GreaterThanOrEqualTo(1),
                "Vardiya boyunca en az bir \"Bekle > PilAzaldi > SarjOl\" geçişi olmalı; " +
                "sırada müşteri varken pil azaldığında robot önce şarja gitmeli. Günlük:\n" +
                string.Join("\n", gunluk));

            Assert.That(sonuc.enDusukPil, Is.GreaterThanOrEqualTo(25.0),
                $"En düşük pil 25'in altına inmemeli; ölçülen: {sonuc.enDusukPil:F2}. " +
                "Pil azken müşteriye gidilmiş olabilir.");
        }
    }
}
