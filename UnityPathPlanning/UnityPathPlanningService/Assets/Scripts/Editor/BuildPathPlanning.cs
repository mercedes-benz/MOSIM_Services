using UnityEngine;
using UnityEditor;

public class BuildPathPlanning{

    public static void CreateServerBuild ()
    {
        Debug.Log("Building Path Planning Service Server Build"); 
        string[] scenes = new string[] {"Assets/Scenes/pathPlanningService.unity"};
        BuildPipeline.BuildPlayer(scenes,"./build/UnityPathPlanningService.exe", BuildTarget.StandaloneWindows, BuildOptions.EnableHeadlessMode);
    }
}