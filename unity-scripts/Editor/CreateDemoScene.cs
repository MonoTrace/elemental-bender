#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CreateDemoScene {
    [MenuItem("Tools/Elemental Bender/Create Demo Scene")]
    public static void Create() {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // Reset scene and managed state on each Play for a reproducible teaching demo.
        EditorSettings.enterPlayModeOptionsEnabled=false;
        if(!System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) {
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_Text).Assembly);
            var essential=System.IO.Path.Combine(package.resolvedPath,"Package Resources","TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(essential,false);
            AssetDatabase.Refresh();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var demo=new GameObject("Elemental Bender",typeof(ElementalReceiver),typeof(ElementalArena));
        var arena=demo.GetComponent<ElementalArena>();
        arena.playerModelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ElementalBender/Characters/YBot.prefab");
        arena.enemyModelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ElementalBender/Characters/XBot.prefab");
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/ElementalDemo.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/ElementalDemo.unity",true)};
    }
    // Also usable by batch-mode verification without changing user scenes.
    public static void CreateBatch() { Create(); }
}
#endif
