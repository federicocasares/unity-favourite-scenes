using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneQuickAccessWindow : EditorWindow {

    private Vector2 scrollPosition;
    private List<string> scenePaths = new List<string>();
    private string newScenePath = "";
    private Dictionary<string, Color> sceneColors = new Dictionary<string, Color>();

    [MenuItem("Window/Favourite Scenes")]
    public static void ShowWindow() {
        GetWindow<SceneQuickAccessWindow>("Favourite Scenes");
    }

    private void OnEnable() {
        // Load saved scene paths from the editor preferences
        string savedPaths = EditorPrefs.GetString("SceneQuickAccess_Paths", "");
        if (!string.IsNullOrEmpty(savedPaths)) {
            // Hopefully separating by ; is good enough and won't break!
            scenePaths = new List<string>(savedPaths.Split(';'));
        }
        UpdateSceneColors();
    }

    private void UpdateSceneColors() {
        sceneColors.Clear();
        foreach (var path in scenePaths) {
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (!sceneColors.ContainsKey(sceneName)) {
                sceneColors[sceneName] = GenerateColorFromString(sceneName);
            }
        }
    }

    private Color GenerateColorFromString(string input) {
        // Simple hash function to generate a consistent number from the string
        int hash = input.GetHashCode();

        // Use the hash to compute a hue value
        float hue = Mathf.Abs(hash % 360) / 360f; // Normalize to 0-1
        float saturation = 0.5f;
        float value = 0.95f; // Bright colours work best with the Unity UI

        Color color = Color.HSVToRGB(hue, saturation, value);
        return color;
    }

    private void OnGUI() {
        // Start scene list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        var sortedScenePaths = scenePaths.OrderBy(path => Path.GetFileNameWithoutExtension(path)).ToList();

        // Calculate number of rows needed
        int rows = Mathf.CeilToInt(sortedScenePaths.Count / 2f);

        for (int row = 0; row < rows; row++) {
            EditorGUILayout.BeginHorizontal();

            // First column
            int firstIndex = row * 2;
            if (firstIndex < sortedScenePaths.Count) {
                DrawSceneButton(sortedScenePaths[firstIndex]);
            }

            // Second column
            int secondIndex = row * 2 + 1;
            if (secondIndex < sortedScenePaths.Count) {
                DrawSceneButton(sortedScenePaths[secondIndex]);
            }

            EditorGUILayout.EndHorizontal();
        }

        // Add new scene path
        EditorGUILayout.BeginHorizontal();
        newScenePath = EditorGUILayout.TextField("Scene Path:", newScenePath);
        if (GUILayout.Button("Add", GUILayout.Width(45))) {
            if (!string.IsNullOrEmpty(newScenePath) && !scenePaths.Contains(newScenePath)) {
                scenePaths.Add(VerifyAndNormalizeScenePath(newScenePath));
                SaveScenePaths();
                newScenePath = "";
            }
        }

        // Help button
        if (GUILayout.Button("?", GUILayout.Width(20))) {
            EditorUtility.DisplayDialog("Valid Scene Path Formats", GetHelpText(), "Got it!");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSceneButton(string scenePath) {
        EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width / 2 - 10));

        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        Color buttonColor = sceneColors[sceneName];

        Color originalColor = GUI.color;
        GUI.color = buttonColor;

        if (GUILayout.Button(sceneName, EditorStyles.miniButton)) {
            if (!EditorApplication.isPlaying) {
                OpenScene(scenePath);
            }
        }

        GUI.color = originalColor;

        // Remove button
        if (GUILayout.Button("X", GUILayout.Width(20))) {
            scenePaths.Remove(scenePath);
            SaveScenePaths();
            UpdateSceneColors();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OpenScene(string scenePath) {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private void SaveScenePaths() {
        // Remove any invalid scenes and duplicates before saving
        scenePaths = scenePaths.Where(path => VerifyAndNormalizeScenePath(path) != "").Distinct().ToList();
        EditorPrefs.SetString("SceneQuickAccess_Paths", string.Join(";", scenePaths));
        UpdateSceneColors();
    }

    private string VerifyAndNormalizeScenePath(string path) {
        if (string.IsNullOrEmpty(path)) {
            return "";
        }

        string normalizedPath = path;

        // Add Assets/ prefix if missing
        if (!normalizedPath.StartsWith("Assets/")) {
            normalizedPath = "Assets/" + normalizedPath;
        }

        // Add .unity extension if missing
        if (!normalizedPath.EndsWith(".unity")) {
            normalizedPath += ".unity";
        }

        // Check if the scene exists
        if (!File.Exists(normalizedPath)) {
            Debug.LogWarning($"Scene not found: {normalizedPath}");
            return "";
        }
        return normalizedPath;
    }

    private string GetHelpText() {
        return
            "The following formats are valid:\n\n" +
            "• Assets/Scenes/MyScene.unity\n" +
            "• Assets/Scenes/MyScene\n" +
            "• Scenes/MyScene.unity\n" +
            "• Scenes/MyScene\n\n" +
            "The system will automatically add missing 'Assets/' prefix and '.unity' extension if needed.";
    }
}
