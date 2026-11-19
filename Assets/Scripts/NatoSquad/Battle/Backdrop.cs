using System.Collections.Generic;
using UnityEngine;

namespace NatoSquad
{
    /// <summary>Parallax scenery: sky gradient, distant silhouettes, clouds and the ground or sea strip. Everything scrolls left.</summary>
    public class Backdrop
    {
        readonly Transform root;
        readonly MissionDef m;
        SpriteRenderer sky, far, far2, ground;
        float farTile, groundTile, farOffset, groundOffset;
        readonly List<SpriteRenderer> clouds = new List<SpriteRenderer>();
        readonly List<float> cloudSpeed = new List<float>();
        public float GroundTop { get; private set; }

        public Backdrop(Transform parent, MissionDef mission)
        {
            m = mission;
            root = new GameObject("Backdrop").transform;
            root.SetParent(parent, false);

            sky = Make("Sky", Art.Sky(m.skyTop, m.skyBottom), -100);

            far = Make("Far", Art.Far(m.terrain, m.far), -90);
            far.drawMode = SpriteDrawMode.Tiled;
            farTile = far.sprite.bounds.size.x;
            // a second, darker and closer silhouette layer for depth
            far2 = Make("Far2", Art.Far(m.terrain, m.far), -85);
            far2.drawMode = SpriteDrawMode.Tiled;
            far2.color = new Color(0.8f, 0.8f, 0.85f);

            ground = Make("Ground", Art.GroundStrip(m), -20);
            ground.drawMode = SpriteDrawMode.Tiled;
            groundTile = ground.sprite.bounds.size.x;

            bool stormy = m.terrain == Terrain.Storm;
            int n = stormy ? 9 : 6;
            for (int i = 0; i < n; i++)
            {
                bool front = stormy && i % 3 == 0;
                var c = Make("Cloud", Art.Cloud(i % 4, stormy || m.terrain == Terrain.Canyon || m.terrain == Terrain.Fortress), front ? 70 : -60 + i);
                if (m.terrain == Terrain.Canyon || m.terrain == Terrain.Fortress) c.color = new Color(0.5f, 0.45f, 0.55f, 0.5f);
                if (front) c.color = new Color(1, 1, 1, 0.45f);
                float s = Random.Range(0.8f, 1.6f) * (front ? 1.6f : 1f);
                c.transform.localScale = new Vector3(s, s, 1);
                c.transform.position = new Vector3(Random.Range(-12f, 12f), Random.Range(-1f, 4.2f), 0);
                clouds.Add(c);
                cloudSpeed.Add(front ? 6f : Random.Range(0.6f, 2.2f));
            }
        }

        SpriteRenderer Make(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        public void Layout(float halfW, float halfH)
        {
            // sky fills the view
            var sb = sky.sprite.bounds.size;
            sky.transform.position = Vector3.zero;
            sky.transform.localScale = new Vector3(halfW * 2.2f / sb.x, halfH * 2.2f / sb.y, 1);

            float groundH = ground.sprite.bounds.size.y;
            GroundTop = -halfH + groundH;
            ground.size = new Vector2(halfW * 2f + groundTile * 2f, groundH);

            float farScale = 1.6f;
            far.transform.localScale = new Vector3(farScale, farScale, 1);
            far.size = new Vector2((halfW * 2f + farTile * farScale * 2f) / farScale, far.sprite.bounds.size.y);
            far2.transform.localScale = new Vector3(farScale * 0.8f, farScale * 0.75f, 1);
            far2.size = new Vector2((halfW * 2f + farTile * farScale * 2f) / (farScale * 0.8f), far2.sprite.bounds.size.y);
        }

        public void Update(float dt, float halfW, float speed)
        {
            // tiled sprites are drawn from their pivot (left-bottom for far, left-top for ground)
            farOffset = Mathf.Repeat(farOffset + speed * 0.12f * dt, farTile * 1.6f);
            far.transform.position = new Vector3(-halfW - farTile * 1.6f + (farTile * 1.6f - farOffset), GroundTop - 0.3f, 0);
            float f2t = farTile * 1.6f * 0.8f;
            float o2 = Mathf.Repeat(Time.time * speed * 0.25f, f2t);
            far2.transform.position = new Vector3(-halfW - f2t + (f2t - o2), GroundTop - 0.3f, 0);

            groundOffset = Mathf.Repeat(groundOffset + speed * dt, groundTile);
            ground.transform.position = new Vector3(-halfW - groundTile + (groundTile - groundOffset), GroundTop, 0);

            for (int i = 0; i < clouds.Count; i++)
            {
                var c = clouds[i];
                var p = c.transform.position;
                p.x -= cloudSpeed[i] * dt * (speed / 3f);
                float w = c.bounds.size.x;
                if (p.x < -halfW - w) { p.x = halfW + w * 0.6f + Random.Range(0f, 4f); p.y = Random.Range(-0.5f, 4.2f); }
                c.transform.position = p;
            }
        }
    }
}
