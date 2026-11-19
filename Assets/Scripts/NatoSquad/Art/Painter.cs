using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NatoSquad
{
    /// <summary>
    /// A tiny anti-aliased vector rasterizer: polygons, ellipses and lines in normalized coordinates (0..1, y up) are
    /// painted into a Color buffer and turned into a Sprite. All game art is drawn with it at startup, so the game ships
    /// with no image files except the app icon.
    /// Shapes are given as point strings ("x,y x,y ...") so they are easy to read and tweak.
    /// </summary>
    public class Painter
    {
        public readonly int W, H;
        readonly Color[] px;
        public bool Mirror;      // x → 1 - x (enemies face left)
        const int SS = 4;        // 4x4 supersampling

        public Painter(int w, int h)
        {
            W = w; H = h;
            px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        }

        // ------------------------------------------------------------------ blending
        void Blend(int i, Color c, float cov)
        {
            float a = c.a * cov;
            if (a <= 0f) return;
            var d = px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 0.0001f) return;
            float k = d.a * (1f - a);
            px[i] = new Color((c.r * a + d.r * k) / outA, (c.g * a + d.g * k) / outA, (c.b * a + d.b * k) / outA, outA);
        }

        static Vector2[] Parse(string pts)
        {
            var parts = pts.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var list = new Vector2[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var xy = parts[i].Split(',');
                list[i] = new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture));
            }
            return list;
        }

        Vector2 ToPx(Vector2 n) => new Vector2((Mirror ? 1f - n.x : n.x) * W, n.y * H);

        static bool Inside(Vector2[] poly, float x, float y)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > y) != (poly[j].y > y)) &&
                    (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    c = !c;
            }
            return c;
        }

        // ------------------------------------------------------------------ shapes
        /// <summary>Filled polygon. Optional bottom color makes a vertical gradient (top color = c).</summary>
        public void Poly(Color c, string pts, Color? bottom = null)
        {
            var n = Parse(pts);
            var p = new Vector2[n.Length];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n.Length; i++)
            {
                p[i] = ToPx(n[i]);
                minX = Mathf.Min(minX, p[i].x); maxX = Mathf.Max(maxX, p[i].x);
                minY = Mathf.Min(minY, p[i].y); maxY = Mathf.Max(maxY, p[i].y);
            }
            Fill(p, c, bottom ?? c, minX, minY, maxX, maxY);
        }

        void Fill(Vector2[] p, Color top, Color bottom, float minX, float minY, float maxX, float maxY)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(maxY));
            float span = Mathf.Max(1f, maxY - minY);
            for (int y = y0; y <= y1; y++)
            {
                var col = Color.Lerp(bottom, top, (y + 0.5f - minY) / span);
                for (int x = x0; x <= x1; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                            if (Inside(p, x + (sx + 0.5f) / SS, y + (sy + 0.5f) / SS)) hit++;
                    if (hit > 0) Blend(y * W + x, col, hit / (float)(SS * SS));
                }
            }
        }

        public void Ellipse(Color c, float cx, float cy, float rx, float ry, Color? bottom = null)
        {
            var center = ToPx(new Vector2(cx, cy));
            float prx = rx * W, pry = ry * H;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - prx)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(center.x + prx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - pry)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(center.y + pry));
            var bot = bottom ?? c;
            for (int y = y0; y <= y1; y++)
            {
                var col = Color.Lerp(bot, c, Mathf.InverseLerp(center.y - pry, center.y + pry, y + 0.5f));
                for (int x = x0; x <= x1; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            float dx = (x + (sx + 0.5f) / SS - center.x) / prx, dy = (y + (sy + 0.5f) / SS - center.y) / pry;
                            if (dx * dx + dy * dy <= 1f) hit++;
                        }
                    if (hit > 0) Blend(y * W + x, col, hit / (float)(SS * SS));
                }
            }
        }

        public void Rect(Color c, float x0, float y0, float x1, float y1, Color? bottom = null)
        {
            Poly(c, F(x0, y0) + " " + F(x1, y0) + " " + F(x1, y1) + " " + F(x0, y1), bottom);
        }

        /// <summary>Thick line from a to b (thickness in normalized height units).</summary>
        public void Line(Color c, float ax, float ay, float bx, float by, float thick)
        {
            var a = new Vector2(ax * W, ay * H); var b = new Vector2(bx * W, by * H);
            var d = (b - a).normalized; var nrm = new Vector2(-d.y, d.x) * (thick * H * 0.5f);
            var A = a + nrm; var B = b + nrm; var C = b - nrm; var D = a - nrm;
            Poly(c, F(A.x / W, A.y / H) + " " + F(B.x / W, B.y / H) + " " + F(C.x / W, C.y / H) + " " + F(D.x / W, D.y / H));
        }

        static string F(float x, float y) => x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture);

        /// <summary>Soft radial blob (clouds, glows, smoke). Alpha falls off smoothly to the edge.</summary>
        public void Soft(Color c, float cx, float cy, float rx, float ry, float hardness = 0.35f)
        {
            var center = new Vector2(cx * W, cy * H);
            float prx = rx * W, pry = ry * H;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - prx)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(center.x + prx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - pry)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(center.y + pry));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - center.x) / prx, dy = (y + 0.5f - center.y) / pry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float cov = d < hardness ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - hardness) / (1f - hardness));
                    Blend(y * W + x, c, cov);
                }
        }

        /// <summary>Darkens the outer edge of the drawn shape (1px outline), which makes small sprites read well.</summary>
        public void Outline(Color c, float threshold = 0.4f)
        {
            var copy = (Color[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (copy[i].a < threshold) continue;
                    bool edge = x == 0 || y == 0 || x == W - 1 || y == H - 1 ||
                                copy[i - 1].a < threshold || copy[i + 1].a < threshold || copy[i - W].a < threshold || copy[i + W].a < threshold;
                    if (edge) px[i] = new Color(Mathf.Lerp(copy[i].r, c.r, c.a), Mathf.Lerp(copy[i].g, c.g, c.a), Mathf.Lerp(copy[i].b, c.b, c.a), copy[i].a);
                }
        }

        /// <summary>Direct pixel access for procedural textures (ground, skylines).</summary>
        public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = c; }
        public void BlendPx(int x, int y, Color c, float cov = 1f) { if (x >= 0 && y >= 0 && x < W && y < H) Blend(y * W + x, c, cov); }

        public Texture2D ToTexture(bool smooth = true, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            t.wrapMode = wrap;
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }

        public Sprite ToSprite(float ppu, Vector2? pivot = null, bool smooth = true)
        {
            var t = ToTexture(smooth);
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, W, H), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }
    }

    /// <summary>Small helpers for colors.</summary>
    public static class Col
    {
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        public static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);
    }
}
