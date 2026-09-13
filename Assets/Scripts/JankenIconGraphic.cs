using UnityEngine;
using UnityEngine.UI;

namespace Janken
{
    /// <summary>
    /// 外部画像を使わず、頂点からグー・チョキ・パーのアイコンを描く教材用Graphic。
    /// </summary>
    public sealed class JankenIconGraphic : Graphic
    {
        public enum Hand { Rock, Scissors, Paper }

        [SerializeField] private Hand hand;
        public Hand Value { get => hand; set { hand = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float size = Mathf.Min(r.width, r.height);
            Vector2 c = r.center;

            switch (hand)
            {
                case Hand.Rock:
                    AddRoundedBlob(vh, c, size * 0.38f, 12);
                    AddRect(vh, c + new Vector2(0, -size * 0.23f), new Vector2(size * 0.48f, size * 0.24f));
                    break;
                case Hand.Scissors:
                    AddThickLine(vh, c + new Vector2(-size * 0.08f, -size * 0.25f), c + new Vector2(-size * 0.23f, size * 0.34f), size * 0.14f);
                    AddThickLine(vh, c + new Vector2(size * 0.04f, -size * 0.25f), c + new Vector2(size * 0.28f, size * 0.28f), size * 0.14f);
                    AddRoundedBlob(vh, c + new Vector2(0, -size * 0.22f), size * 0.27f, 10);
                    break;
                case Hand.Paper:
                    AddRect(vh, c + new Vector2(0, size * 0.02f), new Vector2(size * 0.62f, size * 0.72f));
                    AddRect(vh, c + new Vector2(-size * 0.24f, -size * 0.18f), new Vector2(size * 0.18f, size * 0.50f));
                    break;
            }
        }

        private void AddRoundedBlob(VertexHelper vh, Vector2 center, float radius, int sides)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides;
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                vh.AddVert(p, color, Vector2.zero);
            }
            for (int i = 0; i < sides; i++) vh.AddTriangle(start, start + i + 1, start + i + 2);
        }

        private void AddRect(VertexHelper vh, Vector2 center, Vector2 size)
        {
            int n = vh.currentVertCount;
            Vector2 h = size * 0.5f;
            vh.AddVert(center + new Vector2(-h.x, -h.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(-h.x, h.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(h.x, h.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(h.x, -h.y), color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }

        private void AddThickLine(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
            int n = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
