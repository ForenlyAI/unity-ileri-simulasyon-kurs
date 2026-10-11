// Ders 1.3 · Müşteri Akışı — kapıya müşteri geliş zamanları (saf C#).
// İki geliş arası süre üstel dağılımdan çekilir (ortalaması ÖRNEK). Aynı tohum her zaman aynı listeyi verir.
using System;
using System.Collections.Generic;

namespace KafeIleri
{
    public static class MusteriAkisi
    {
        public static List<double> Gelisler(int tohum, double ortalamaAralik, double sure)
        {
            var r = new Random(tohum);
            var liste = new List<double>();
            double t = 0;
            while (true)
            {
                t += -Math.Log(1.0 - r.NextDouble()) * ortalamaAralik;
                if (t >= sure) break;
                liste.Add(t);
            }
            return liste;
        }

        // Boş masalardan birini tohumla seçer; boş masa yoksa -1.
        public static int MasaSec(Random r, IList<bool> dolu)
        {
            var bos = new List<int>();
            for (int i = 0; i < dolu.Count; i++) if (!dolu[i]) bos.Add(i);
            return bos.Count == 0 ? -1 : bos[r.Next(bos.Count)];
        }
    }
}
