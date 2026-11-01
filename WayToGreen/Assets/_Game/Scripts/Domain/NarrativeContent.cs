using System;
using UnityEngine;
namespace KiyiDongusu
{
    [Serializable] public sealed class NarrativePage{public string en,tr,fact;public int pose;}
    [Serializable] public sealed class NarrativeNode{public int id;public NarrativePage[] pages;}
    [Serializable] public sealed class ShoreQuest{public string en,tr,descriptionEn,descriptionTr,fact;}
    [Serializable] public sealed class ShoreFact{public string id,en,tr,source,url;}
    [Serializable] public sealed class NarrativeContent
    {
        public NarrativeNode[] nodes;public ShoreQuest[] quests;public ShoreFact[] facts;
        static NarrativeContent cached;
        public static NarrativeContent Load()=>cached??(cached=JsonUtility.FromJson<NarrativeContent>(Resources.Load<TextAsset>("Narrative").text));
        public ShoreFact Fact(string id)=>Array.Find(facts,f=>f.id==id);
        public static string Local(string en,string tr)=>GameText.T(en,tr);
        public static string Resolve(string text,GameSession game)
        {
            if(game.State.materials.Length>5)text=text.Replace("{capacity}",game.Processing.Capacity(5).ToString()).Replace("{seconds}",(game.Processing.Capacity(5)/game.Processing.Rate(5)).ToString("0.#",GameText.Culture));
            return text.Replace("{total}",game.TotalUnits.ToString()).Replace("{processed}",game.Processed.ToString());
        }
    }
}
