using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PresetSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image prefabImage;
    [SerializeField] private TMP_Text indexText;

    [SerializeField]private GameObject assignedPrefab;

    public PrefabType presetType;

    public void SetIndex(int getIndex)
    {
        indexText.text = getIndex.ToString();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var icon = eventData.pointerDrag?.GetComponent<InvenIcon>();
        if (icon == null)
        {
            Debug.LogWarning("드래그된 아이콘에서 InvenIcon 스크립트를 찾을 수 없습니다.");
            return;
        }

        var prefab = icon.GetPrefab();
        
        if (prefab == null)
        {
            Debug.Log("프리팹 감지 실패");
            return;
        }
        else
        {
            Debug.Log("감지" + prefab.name);

            presetType = icon.prefabType;
            prefabImage.sprite = icon.GetIconSprite();
            prefabImage.enabled = true;
            
            assignedPrefab = icon.GetPrefab(); 
        }
    }
    
    public GameObject GetAssignedPrefab()
    {
        return assignedPrefab;
    }
}