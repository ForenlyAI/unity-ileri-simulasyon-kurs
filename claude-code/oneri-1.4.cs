using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KafeIleri.Testler
{
    public class ClaudeYolTestleri
    {
        private const string SahneYolu = "Assets/KafeIleri/Sahneler/KafeIleri.unity";
        private const float AzamiOran = 1.6f;
        private const float OrneklemeYaricapi = 1.0f;

        private const int Tezgah = 1;
        private const int Kapi = 3;
        private const int ServisBaslangic = 4;
        private const int OturmaBaslangic = 10;

        private SceneSetup[] oncekiSahneler;
        private KafeNoktalari kafeNoktalari;

        [OneTimeSetUp]
        public void SahneyiAc()
        {
            oncekiSahneler = EditorSceneManager.GetSceneManagerSetup();

            Scene sahne = EditorSceneManager.OpenScene(SahneYolu, OpenSceneMode.Single);
            Assert.IsTrue(sahne.IsValid() && sahne.isLoaded, "Sahne açılamadı: " + SahneYolu);

            kafeNoktalari = Object.FindFirstObjectByType<KafeNoktalari>();
            Assert.IsNotNull(kafeNoktalari, "Sahnede KafeNoktalari bileşeni bulunamadı.");
            Assert.IsNotNull(kafeNoktalari.noktalar, "KafeNoktalari.noktalar atanmamış.");
            Assert.Greater(kafeNoktalari.masaSayisi, 0, "masaSayisi sıfırdan büyük olmalı.");
            Assert.GreaterOrEqual(kafeNoktalari.noktalar.Length, OturmaBaslangic + kafeNoktalari.masaSayisi,
                "noktalar dizisi masaSayisi için yeterli eleman içermiyor.");
        }

        [OneTimeTearDown]
        public void SahneyiGeriYukle()
        {
            if (oncekiSahneler == null || oncekiSahneler.Length == 0)
                return;

            foreach (SceneSetup kurulum in oncekiSahneler)
            {
                if (string.IsNullOrEmpty(kurulum.path))
                    return;
            }

            EditorSceneManager.RestoreSceneManagerSetup(oncekiSahneler);
        }

        [Test]
        public void TezgahtanHerMasaninServisNoktasinaYolVar()
        {
            var hatalar = new List<string>();
            for (int m = 0; m < kafeNoktalari.masaSayisi; m++)
            {
                YoluDenetle(Tezgah, ServisBaslangic + m, "Tezgâh -> Masa " + (m + 1) + " servis", hatalar);
            }
            HatalariBildir(hatalar);
        }

        [Test]
        public void KapidanHerMasaninOturmaNoktasinaYolVar()
        {
            var hatalar = new List<string>();
            for (int m = 0; m < kafeNoktalari.masaSayisi; m++)
            {
                YoluDenetle(Kapi, OturmaBaslangic + m, "Kapı -> Masa " + (m + 1) + " oturma", hatalar);
            }
            HatalariBildir(hatalar);
        }

        private void YoluDenetle(int baslangicIndeksi, int hedefIndeksi, string ad, List<string> hatalar)
        {
            Vector3 baslangicKonum = kafeNoktalari.Konum(baslangicIndeksi);
            Vector3 hedefKonum = kafeNoktalari.Konum(hedefIndeksi);

            if (!NavMesh.SamplePosition(baslangicKonum, out NavMeshHit baslangicIsabet, OrneklemeYaricapi, NavMesh.AllAreas))
            {
                hatalar.Add(ad + ": başlangıç noktası (" + baslangicIndeksi + ") NavMesh üzerinde değil " + baslangicKonum);
                return;
            }

            if (!NavMesh.SamplePosition(hedefKonum, out NavMeshHit hedefIsabet, OrneklemeYaricapi, NavMesh.AllAreas))
            {
                hatalar.Add(ad + ": hedef noktası (" + hedefIndeksi + ") NavMesh üzerinde değil " + hedefKonum);
                return;
            }

            var yol = new NavMeshPath();
            bool hesaplandi = NavMesh.CalculatePath(baslangicIsabet.position, hedefIsabet.position, NavMesh.AllAreas, yol);
            if (!hesaplandi || yol.status != NavMeshPathStatus.PathComplete)
            {
                hatalar.Add(ad + ": tam yol bulunamadı (durum: " + yol.status + ")");
                return;
            }

            float yolUzunlugu = YolUzunlugu(yol);
            float kusUcusu = Vector3.Distance(baslangicIsabet.position, hedefIsabet.position);
            if (kusUcusu < 0.01f)
                return;

            float oran = yolUzunlugu / kusUcusu;
            if (oran > AzamiOran)
            {
                hatalar.Add(string.Format("{0}: yol uzunluğu {1:F2} m, kuş uçuşu {2:F2} m, oran {3:F2} > {4:F2}",
                    ad, yolUzunlugu, kusUcusu, oran, AzamiOran));
            }
        }

        private static float YolUzunlugu(NavMeshPath yol)
        {
            Vector3[] koseler = yol.corners;
            float toplam = 0f;
            for (int i = 1; i < koseler.Length; i++)
            {
                toplam += Vector3.Distance(koseler[i - 1], koseler[i]);
            }
            return toplam;
        }

        private static void HatalariBildir(List<string> hatalar)
        {
            if (hatalar.Count == 0)
                return;

            var mesaj = new StringBuilder();
            mesaj.AppendLine(hatalar.Count + " yol hatası:");
            foreach (string hata in hatalar)
            {
                mesaj.AppendLine(" - " + hata);
            }
            Assert.Fail(mesaj.ToString());
        }
    }
}
