// Testlerin ölçtüğü sayıları <proje>/Olcumler/<ad>.json dosyasına yazar. Ders metinlerindeki her sayı buradan gelir.
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace KafeIleri.Testler
{
    public static class Olcum
    {
        public static string Klasor => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Olcumler"));

        public static void Yaz(string ad, Dictionary<string, object> veri)
        {
            Directory.CreateDirectory(Klasor);
            var sb = new StringBuilder();
            Json(sb, veri, 0);
            File.WriteAllText(Path.Combine(Klasor, ad + ".json"), sb.ToString() + "\n");
            Debug.Log($"[Olcum] {ad}: {sb.ToString().Replace("\n", " ")}");
        }

        static void Json(StringBuilder sb, object v, int g)
        {
            string ic = new string(' ', (g + 1) * 2), dis = new string(' ', g * 2);
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
                case int i: sb.Append(i); break;
                case long l: sb.Append(l); break;
                case float f: sb.Append(((double)f).ToString("0.######", CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString("0.######", CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> m:
                    sb.Append("{\n"); int n = 0;
                    foreach (var kv in m) { sb.Append(ic).Append('"').Append(kv.Key).Append("\": "); Json(sb, kv.Value, g + 1); sb.Append(++n < m.Count ? ",\n" : "\n"); }
                    sb.Append(dis).Append('}'); break;
                case IEnumerable e:
                    sb.Append('['); bool ilk = true;
                    foreach (var x in e) { if (!ilk) sb.Append(", "); Json(sb, x, g + 1); ilk = false; }
                    sb.Append(']'); break;
                default: sb.Append('"').Append(v.ToString()).Append('"'); break;
            }
        }

        public static double Yuvarla(double x, int hane) => System.Math.Round(x, hane);
    }
}
