using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Examples;
using UnityEngine;

// 점수 계산 테이블 TScoreToClams
[Serializable]
public class TilePrefab : MonoBehaviour
{
    [PreviewField(Height = 20)]
    [TableColumnWidth(30, Resizable = false)]
    public Texture2D icon;

    [TableColumnWidth(60)]
    public int index;
    public string prefabName;

    [OnInspectorInit]
    private void CreateData()
    {
        icon = ExampleHelper.GetTexture();
    }
}
