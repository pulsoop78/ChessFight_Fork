using UnityEditor;
using UnityEditor.SceneManagement;
namespace ChessFight.RagdollLab.Editor
{
    public static class ArmoryTrialMenu
    {
        [MenuItem("ChessFight/Prototype/Armory/Open Grayscale Trial")]
        public static void Open()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Scenes/LowerPalaceArmoryTrial.unity");
        }
    }
}
