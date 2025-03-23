using System;
using System.Collections.Generic;
using UnityEngine;

public class PresetController : MonoBehaviour
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject presetSlotPrefab;

    private List<PresetSlot> slotList = new List<PresetSlot>();
    private void Start()
    {
        for (int i = 1; i <= 9; i++)
        {
            GameObject slotObj = Instantiate(presetSlotPrefab, slotParent);

            PresetSlot getSlot = slotObj.GetComponent<PresetSlot>();

            getSlot.SetIndex(i);
            
            slotList.Add(getSlot);
        }
    }
}
