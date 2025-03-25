using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace QuantumTek.QuantumUI
{
    /// <summary>
    /// QUI_TabWindow is a basic UI element responsible for holding other UI elements in a tab group.
    /// </summary>
    [AddComponentMenu("Quantum Tek/Quantum UI/Tab Window")]
    [DisallowMultipleComponent]
    public class QUI_TabWindow : QUI_Element
    {
        [Header("Tab Window Object References")] [Tooltip("The tab used to open the window.")]
        public QUI_Tab tab;

        public PrefabType prefabType;
        public string resourcePath;
        public Transform listParent;

        public override void SetActive(bool value)
        {
            active = value;

            if (active)
                onActive.Invoke();
            else
                onInactive.Invoke();

            tab.SetActive(value);
        }

#if UNITY_EDITOR
        private IEnumerator LoadAllPreviewsSequentially(List<GameObject> prefabs, List<InvenIcon> icons)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                InvenIcon icon = icons[i];

                int tryCount = 30;
                while (tryCount-- > 0)
                {
                    Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                    if (preview != null)
                    {
                        icon.SetIconImage(preview);
                        
                        var tr = icon.transform as RectTransform;
                        tr.localScale = Vector3.zero;
                        tr.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                        
                        break;
                    }

                    if (!AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                        AssetPreview.GetAssetPreview(prefab);

                    yield return new EditorWaitForSeconds(0.1f);
                }

                yield return new EditorWaitForSeconds(0.05f);
            }
        }
#endif

        public void LoadResourcesWithList(List<GameObject> prefabList, GameObject itemPrefab)
        {
            foreach (Transform child in listParent)
            {
                DestroyImmediate(child.gameObject);
            }
            
            int itemCount = (prefabList.Count / 7) + 1;
            
            List<InvenIcon> iconList = new List<InvenIcon>();

            foreach (var prefab in prefabList)
            {
                GameObject go = Instantiate(itemPrefab, listParent);
                var icon = go.GetComponent<InvenIcon>();
                iconList.Add(icon);
            }
#if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutine(LoadAllPreviewsSequentially(prefabList, iconList), this);
#endif
            float itemHeight = 230f;
            float topPadding = 50f;
            float totalHeight = topPadding + itemHeight * itemCount;

            RectTransform listRect = listParent.GetComponent<RectTransform>();
            if (listRect != null)
            {
                Vector2 size = listRect.sizeDelta;
                size.y = totalHeight;
                listRect.sizeDelta = size;
            }
        }
    }
}