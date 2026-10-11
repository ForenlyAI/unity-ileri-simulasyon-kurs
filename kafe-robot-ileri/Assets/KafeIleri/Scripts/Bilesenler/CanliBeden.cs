// Robot programının Unity'deki bedeni: NavMeshAgent ile yürür, pil her karede düşer.
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace KafeIleri
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CanliBeden : MonoBehaviour, IBeden
    {
        public KafeNoktalari noktalar;
        public NavMeshAgent ajan;
        double baslangic;
        public double Simdi => Time.timeAsDouble - baslangic;
        public int Nokta { get; private set; } = KafeIleri.Nokta.Ev;
        public double Yol { get; private set; }
        public Durum GorunenDurum;

        void Awake() { ajan = GetComponent<NavMeshAgent>(); }
        public void SaatiSifirla() { baslangic = Time.timeAsDouble; }

        public static bool Vardi(NavMeshAgent a) => !a.pathPending && a.remainingDistance <= a.stoppingDistance + 0.05f;

        public IEnumerator Yuru(int hedef, Pil pil)
        {
            ajan.SetDestination(noktalar.Konum(hedef));
            yield return null;
            var once = transform.position;
            while (!Vardi(ajan))
            {
                if (ajan.velocity.magnitude > 0.05f) pil.Yuru(Time.deltaTime); else pil.Bekle(Time.deltaTime);
                Yol += Vector3.Distance(once, transform.position); once = transform.position;
                if (pil.Bitti) yield break;
                yield return null;
            }
            Yol += Vector3.Distance(once, transform.position);
            Nokta = hedef;
        }

        public IEnumerator Bekle(double sn, Pil pil)
        {
            double bit = Simdi + sn;
            while (Simdi < bit) { pil.Bekle(Time.deltaTime); yield return null; }
        }

        public IEnumerator Sarj(Pil pil)
        {
            while (!pil.Doldu) { pil.Sarj(Time.deltaTime); yield return null; }
        }
    }
}
