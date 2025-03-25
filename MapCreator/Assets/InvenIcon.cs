using UnityEngine;
using UnityEngine.UI;

public class InvenIcon : MonoBehaviour
{
    [SerializeField] private Image prefabIcon;

    private GameObject setPrefab;

    public void SetIconImage(Texture2D iconImage)
    {
        if (iconImage == null || iconImage.width == 0 || iconImage.height == 0)
        {
            return;
        }
        
        prefabIcon.sprite = Sprite.Create(
            iconImage,
            new Rect(0, 0, iconImage.width, iconImage.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}
