using System;
using System.Collections.Generic;
using System.IO;
using QuantumTek.QuantumUI;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEditor;

public class PresetController : GenericSingleton<PresetController>
{
    [SerializeField] private Transform mainCanvas;
    [SerializeField] private GameObject tileSelectWindow;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject presetSlotPrefab;
    
    [SerializeField] private string tilePrefabPath = "Assets/Resources/Prefabs/TilePrefabs";
    [SerializeField] private string neutralPrefabPath = "Assets/Resources/Prefabs/ObjectPrefabs/Neutral";
    [SerializeField] private string objectPrefabPath = "Assets/Resources/Prefabs/ObjectPrefabs/Props";

    public List<GameObject> tilePrefabs = new List<GameObject>();
    public List<GameObject> neutralPrefabs = new List<GameObject>();
    public List<GameObject> objectPrefabs = new List<GameObject>();

    private List<PresetSlot> slotList = new List<PresetSlot>();
    private GameObject createdInvenPopup;
    private bool isSelectWindowActive;

    public void PresetListON()
    {
        for (int i = 1; i <= 9; i++)
        {
            GameObject slotObj = Instantiate(presetSlotPrefab, slotParent);
            PresetSlot getSlot = slotObj.GetComponent<PresetSlot>();
            getSlot.SetIndex(i);
            slotList.Add(getSlot);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.localScale = Vector3.zero;

            float delay = 0.05f * i;
            rt.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(delay);
        }

        LoadPrefabsAtStart();
    }

    private void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (TileCreator.Instance.GetInitStatus())
            {
                ToggleSelectWindow();
            }
        }
    }

    // 🔹 public으로 뺀 토글 함수
    public void ToggleSelectWindow()
    {
        isSelectWindowActive = !isSelectWindowActive;
        Debug.Log(isSelectWindowActive);

        if (tileSelectWindow == null) return;

        if (isSelectWindowActive)
        {
            createdInvenPopup = Instantiate(tileSelectWindow, mainCanvas);
            
            RectTransform rt = createdInvenPopup.GetComponent<RectTransform>();
            rt.localScale = Vector3.zero;
            rt.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        else
        {
            if (createdInvenPopup != null)
            {
                RectTransform rt = createdInvenPopup.GetComponent<RectTransform>();
                rt.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Destroy(createdInvenPopup);
                    });
            }
        }
    }
    
    private void LoadPrefabsAtStart()
    {
        tilePrefabs.Clear();
        neutralPrefabs.Clear();
        objectPrefabs.Clear();

        string[] tileFiles = Directory.GetFiles(tilePrefabPath, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in tileFiles)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (prefab != null)
                tilePrefabs.Add(prefab);
        }
        
        string[] neutralFiles = Directory.GetFiles(neutralPrefabPath, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in neutralFiles)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (prefab != null)
                neutralPrefabs.Add(prefab);
        }

        string[] objFiles = Directory.GetFiles(objectPrefabPath, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in objFiles)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (prefab != null)
                objectPrefabs.Add(prefab);
        }

        Debug.Log("프리셋 프리팹 로딩 완료");
    }

    public Transform GetMainCanvasTransform()
    {
        return mainCanvas;
    }
}

