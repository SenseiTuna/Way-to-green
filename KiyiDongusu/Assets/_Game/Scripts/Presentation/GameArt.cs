using UnityEngine;

namespace KiyiDongusu
{
    public sealed class GameArt
    {
        public Sprite Beach { get; }
        public Sprite Pile { get; }
        public Sprite[] Waste { get; } = new Sprite[6];
        public Sprite[] Facilities { get; } = new Sprite[4];
        public Sprite Net { get; }
        public Sprite HandOpen { get; }
        public Sprite HandPinch { get; }
        readonly Sprite[] stages=new Sprite[3];
        public Sprite PileAt(float fraction)=>fraction>.75f?Pile:fraction>.5f?stages[0]??Pile:fraction>.25f?stages[1]??Pile:stages[2]??Pile;
        public GameArt()
        {
            Beach = Whole("Art/BeachClean");
            var pileTexture = Resources.Load<Texture2D>("Art/Pile");
            // Native sprite sub-rectangle trims only inspected transparent margins.
            // Original PNG remains intact; visible alpha bounds are inside this rectangle.
            Pile = pileTexture == null ? null : Cut(pileTexture, new Rect(32, 160, 1384, 700));
            Grid("Art/WasteAtlas", 3, 2, Waste);
            Grid("Art/FacilityAtlas", 2, 2, Facilities);
            var parts=Resources.Load<Texture2D>("Art/InteractionAtlas");
            if(parts!=null) Net=Cut(parts,new Rect(0,0,parts.width*.39f,parts.height));
            var hands=Resources.Load<Texture2D>("Art/HandAtlas");
            if(hands!=null){HandOpen=Cut(hands,new Rect(0,0,hands.width/2f,hands.height));HandPinch=Cut(hands,new Rect(hands.width/2f,0,hands.width/2f,hands.height));}
            var phases=Resources.Load<Texture2D>("Art/PileStages");
            if(phases!=null)
            {
                // Inspected transparent gutters separate the three generated groups.
                float[] bounds={0,.39f,.72f,1};
                for(int i=0;i<3;i++)stages[i]=Cut(phases,new Rect(bounds[i]*phases.width,.10f*phases.height,(bounds[i+1]-bounds[i])*phases.width,.63f*phases.height));
            }
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
