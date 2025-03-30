using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabSelectorPopup : EditorWindow
{
    private Vector2 scrollPos;
    
    public System.Action<GameObject> OnPrefabSelected; // 선택 콜백
    
    private Dictionary<GameObject, Texture2D> previewTextures = new Dictionary<GameObject, Texture2D>();

    private List<GameObject> currentPrefabs = new List<GameObject>();
    private PrefabType currentType;
    
    public static void Show(System.Action<GameObject> onPrefabSelected, GameObject[] prefabs)
    {
        if (!TileCreator.Instance.GetInitStatus()) return;

        PrefabSelectorPopup window = GetWindow<PrefabSelectorPopup>("프리팹 선택");
        window.OnPrefabSelected = onPrefabSelected;

        // GameObject 배열 직접 할당
        window.currentPrefabs = new List<GameObject>(prefabs);
        window.previewTextures.Clear();

        foreach (var prefab in prefabs)
        {
            window.previewTextures[prefab] = AssetPreview.GetAssetPreview(prefab);
        }

        EditorApplication.delayCall += () => window.Repaint();
        window.Show();
    }

    private void LoadPrefabs()
    {
        previewTextures.Clear();
        currentPrefabs.Clear();

        // PresetController에서 필요한 프리팹 리스트 받아오기
        switch (currentType)
        {
            case PrefabType.TilePrefab:
                currentPrefabs = PresetController.Instance.tilePrefabs;
                break;
            case PrefabType.NeutralPrefab:
                currentPrefabs = PresetController.Instance.neutralPrefabs;
                break;
            case PrefabType.ObjectPrefab:
                currentPrefabs = PresetController.Instance.objectPrefabs;
                break;
        }

        foreach (var prefab in currentPrefabs)
        {
            previewTextures[prefab] = AssetPreview.GetAssetPreview(prefab);
        }

        EditorApplication.delayCall += () => Repaint();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (GameObject prefab in currentPrefabs)
        {
            EditorGUILayout.BeginHorizontal();

            Texture2D preview = previewTextures.ContainsKey(prefab) ? previewTextures[prefab] : null;
            if (preview == null && AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
            {
                Repaint();
            }

            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            else
                GUILayout.Label("🔄 미리보기 로드 중...", GUILayout.Width(64), GUILayout.Height(64));

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