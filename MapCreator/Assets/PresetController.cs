using System;
using System.Collections.Generic;
using QuantumTek.QuantumUI;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PresetController : GenericSingleton<PresetController>
{
    [SerializeField] private Transform mainCanvas;
    [SerializeField] private GameObject tileSelectWindow;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject presetSlotPrefab;
    
    private List<PresetSlot> slotList = new List<PresetSlot>();
    private GameObject createdInvenPopup;
    private bool isSelectWindowActive;

    void Start()
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
    }

    private void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleSelectWindow();
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
}
