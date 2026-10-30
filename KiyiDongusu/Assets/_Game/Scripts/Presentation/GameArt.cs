using UnityEngine;

namespace KiyiDongusu
{
    public sealed class GameArt
    {
        public Sprite Beach { get; }
        public Sprite Pile { get; }
        public Sprite[] Waste { get; } = new Sprite[6];
        public Sprite[] Facilities { get; } = new Sprite[4];
        public GameArt()
        {
            Beach = Whole("Art/BeachClean");
            var pileTexture = Resources.Load<Texture2D>("Art/Pile");
            // Native sprite sub-rectangle trims only inspected transparent margins.
            // Original PNG remains intact; visible alpha bounds are inside this rectangle.
            Pile = pileTexture == null ? null : Cut(pileTexture, new Rect(32, 160, 1384, 700));
            Grid("Art/WasteAtlas", 3, 2, Waste);
            Grid("Art/FacilityAtlas", 2, 2, Facilities);
        }
        static Sprite Cut(Texture2D texture, Rect rect) => Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        static Sprite Whole(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            return texture == null ? null : Cut(texture, new Rect(0, 0, texture.width, texture.height));
        }
        static void Grid(string path, int columns, int rows, Sprite[] destination)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return;
            float width = texture.width / (float)columns, height = texture.height / (float)rows;
            for (int i = 0; i < destination.Length; i++) destination[i] = Cut(texture, new Rect((i % columns) * width, (rows - 1 - i / columns) * height, width, height));
        }
    }
}
