using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabSelectorPopup : EditorWindow
{
    private string tilePrefabPath = "Assets/Resources/Prefabs/TilePrefabs"; // 프리팹 경로
    private string objectPrefabPath = "Assets/Resources/Prefabs/ObjectPrefabs";
    private List<GameObject> tilePrefabs = new List<GameObject>();
    private List<GameObject> objectPrefabs = new List<GameObject>();
    
    private Vector2 scrollPos;
    
    public System.Action<GameObject> OnPrefabSelected; // 선택 콜백
    
    private Dictionary<GameObject, Texture2D> previewTextures = new Dictionary<GameObject, Texture2D>();

    public static void Show(System.Action<GameObject> onPrefabSelected, PrefabType type)
    {
        PrefabSelectorPopup window = GetWindow<PrefabSelectorPopup>("프리팹 선택");
        window.OnPrefabSelected = onPrefabSelected;

        if (type == PrefabType.TilePrefab)
        {
            
        }
        window.LoadPrefabs();
        window.Show();
    }

    private void LoadPrefabs()
    {
        tilePrefabs.Clear();
        objectPrefabs.Clear();
        previewTextures.Clear();
    
        string[] files = Directory.GetFiles(tilePrefabPath, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (prefab != null)
            {
                tilePrefabs.Add(prefab);
                previewTextures[prefab] = AssetPreview.GetAssetPreview(prefab);
            }
        }

        // 한 프레임 후 미리보기 갱신 시도
        EditorApplication.delayCall += () => Repaint();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (GameObject prefab in tilePrefabs)
        {
            EditorGUILayout.BeginHorizontal();
        
            // 🔹 프리뷰 로딩 중이면 다시 요청
            Texture2D preview = previewTextures.ContainsKey(prefab) ? previewTextures[prefab] : null;
            if (preview == null && AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
            {
                Repaint(); // 계속 갱신해서 미리보기가 나오게 함.
            }

            // 🔹 미리보기 표시
            if (preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            }
            else
            {
                GUILayout.Label("🔄 미리보기 로드 중...", GUILayout.Width(64), GUILayout.Height(64));
            }

            EditorGUILayout.LabelField(prefab.name, GUILayout.Width(150));

            if (GUILayout.Button("선택", GUILayout.Width(80)))
            {
                OnPrefabSelected?.Invoke(prefab);
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
    }

}