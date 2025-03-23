using System;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

[Serializable]
public class PresetSlot : MonoBehaviour
{
    [SerializeField] private Image prefabImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private TMP_Text indexText;

    private int index;

    public int GetIndex()
    {
        return index;
    }

    public void SetIndex(int getIndex)
    {
        index = getIndex;
        
        indexText.text = index.ToString();
    }
}
