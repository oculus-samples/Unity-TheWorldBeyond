// CODE TAKEN FROM: https://wiki.unity3d.com/index.php/Triangulator
using System.Collections.Generic;
using UnityEngine;

namespace TheWorldBeyond.Utils
{
    public class Triangulator
    {
        private List<Vector2> m_points = new();

        public Triangulator(Vector2[] points) => m_points = new List<Vector2>(points);

        public int[] Triangulate()
        {
            var indices = new List<int>();

            var n = m_points.Count;
            if (n < 3)
            {
                return indices.ToArray();
            }


            var vArray = new int[n];
            if (Area() > 0)
            {
                for (var v = 0; v < n; v++)
                {
                    vArray[v] = v;
                }
            }
            else
            {
                for (var v = 0; v < n; v++)
                {
                    vArray[v] = n - 1 - v;
                }
            }

            var nv = n;
            var count = 2 * nv;
            for (var v = nv - 1; nv > 2;)
            {
                if (count-- <= 0)
                {
                    return indices.ToArray();
                }

                var u = v;
                if (nv <= u)
                {
                    u = 0;
                }
                v = u + 1;
                if (nv <= v)
                {
                    v = 0;
                }
                var w = v + 1;
                if (nv <= w)
                {
                    w = 0;
                }

                if (Snip(u, v, w, nv, vArray))
                {
                    int a, b, c, s, t;
                    a = vArray[u];
                    b = vArray[v];
                    c = vArray[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                    {
                        vArray[s] = vArray[t];
                    }
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            var n = m_points.Count;
            var a = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                var pval = m_points[p];
                var qval = m_points[q];
                a += pval.x * qval.y - qval.x * pval.y;
            }
            return a * 0.5f;
        }

        private bool Snip(int u, int v, int w, int n, int[] vs)
        {
            int p;
            var a = m_points[vs[u]];
            var b = m_points[vs[v]];
            var c = m_points[vs[w]];
            if (Mathf.Epsilon > ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)))
            {
                return false;
            }
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                var point = m_points[vs[p]];
                if (InsideTriangle(a, b, c, point))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = c.x - b.x; ay = c.y - b.y;
            bx = a.x - c.x; by = a.y - c.y;
            cx = b.x - a.x; cy = b.y - a.y;
            apx = p.x - a.x; apy = p.y - a.y;
            bpx = p.x - b.x; bpy = p.y - b.y;
            cpx = p.x - c.x; cpy = p.y - c.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return (aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f);
        }
    }
}
