using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;
namespace KiyiDongusu.Editor
{
    public static class CoastTypography
    {
        const string Folder="Assets/_Game/Resources/Typography/";
        [MenuItem("Kiyi Dongusu/Prepare SDF Typography")]
        public static void Prepare()
        {
            const string settingsPath="Assets/_Game/Resources/TMP Settings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<TMP_Settings>();
                var serialized=new SerializedObject(settings);
                serialized.FindProperty("assetVersion").stringValue="2";
                serialized.FindProperty("m_defaultFontSize").floatValue=24;
                serialized.FindProperty("m_defaultAutoSizeMinRatio").floatValue=.8f;
                serialized.FindProperty("m_defaultAutoSizeMaxRatio").floatValue=1.2f;
                serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue=false;
                serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(settings,settingsPath);
            }
            var regular=Create("Lato-Regular");Create("Lato-Bold");
            var so=new SerializedObject(settings);so.FindProperty("m_defaultFontAsset").objectReferenceValue=regular;so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("KIYI_TYPOGRAPHY Prepared Lato SDF regular/bold with English/Turkish glyphs");
        }
        static TMP_FontAsset Create(string name)
        {
            string path=Folder+name+" SDF.asset";var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null){PruneUnusedPairs(existing);return existing;}
            var source=AssetDatabase.LoadAssetAtPath<Font>(Folder+name+".ttf");
            if(source==null)throw new InvalidOperationException("Missing source font: "+name);
            var font=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);
            font.name=name+" SDF";
            string characters="";for(int i=32;i<127;i++)characters+=(char)i;
            characters+="ÇçĞğİıÖöŞşÜüÂâÎîÛûéÉ×–—…‘’“”";
            if(!font.TryAddCharacters(characters,out string missing,true))throw new InvalidOperationException("Missing glyphs: "+missing);
            PruneUnusedPairs(font);
            AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);return font;
        }
        static void PruneUnusedPairs(TMP_FontAsset font)
        {
            var used=new HashSet<uint>();foreach(var glyph in font.glyphTable)used.Add(glyph.index);
            font.fontFeatureTable.glyphPairAdjustmentRecords.RemoveAll(p=>!used.Contains(p.firstAdjustmentRecord.glyphIndex)||!used.Contains(p.secondAdjustmentRecord.glyphIndex));
            EditorUtility.SetDirty(font);
        }
    }
}
