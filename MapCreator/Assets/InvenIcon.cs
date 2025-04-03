using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvenIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject frameImage;
    [SerializeField] private Image prefabIcon;

    private GameObject prefab;
    private GameObject dragInstance;
    private Canvas canvas;

    public bool dragAble;

    public PrefabType prefabType;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetIconImage(Texture2D iconImage)
    {
        if (iconImage == null || iconImage.width == 0 || iconImage.height == 0) return;

        frameImage.SetActive(true);
        
        prefabIcon.sprite = Sprite.Create(iconImage, new Rect(0, 0, iconImage.width, iconImage.height), new Vector2(0.5f, 0.5f));
        
        dragAble = true;
    }

    public void SetPrefab(GameObject obj) => prefab = obj;
    public GameObject GetPrefab() => prefab;
    public Sprite GetIconSprite() => prefabIcon.sprite;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!dragAble) return;
        
        dragInstance = Instantiate(gameObject, canvas.transform);
        dragInstance.transform.SetAsLastSibling();

        var group = dragInstance.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        
        var dragIcon = dragInstance.GetComponent<InvenIcon>();
        dragIcon.CopyFrom(this);
        
        RectTransform rt = dragInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(100, 100);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragInstance != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 pos);
            dragInstance.transform.localPosition = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragInstance != null)
        {
            Destroy(dragInstance);
        }
    }
    
    public void CopyFrom(InvenIcon original)
    {
        prefab = original.prefab;

        if (original.prefabIcon != null && original.prefabIcon.sprite != null)
        {
            prefabIcon.sprite = original.prefabIcon.sprite;
            prefabIcon.enabled = true;
        }
    }
}