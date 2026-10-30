using System;
using System.IO;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KiyiDongusu.Editor
{
    public static class Workspace
    {
        public static void FullAndChecks()
        {
            DomainChecks.Run();
            BuildGame.Full();
        }
        [MenuItem("Kiyi Dongusu/Open Game and Connect MCP")]
        public static async void Open()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
                bool connected = await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
                Debug.Log("KIYI_MCP_CONNECTED=" + connected);
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
