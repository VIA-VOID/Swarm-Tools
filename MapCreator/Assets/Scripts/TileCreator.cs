using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.WSA;

public class TileCreator : GenericSingleton<TileCreator>
{
    #region Inspector 보이는 변수
    
    [LabelText("맵 크기")]
    [InlineButton("GenerateQuadTileMap", SdfIconType.Map, "맵 생성")]
    [SerializeField] private Vector2 mapSize; // 맵 크기
    
    [TitleGroup("프리팹")]
    [LabelText("타일 프리팹")]
    [SerializeField] private GameObject tilePrefab;
    
    [TitleGroup("프리팹")]
    [LabelText("바닥 타일")]
    [InlineButton("OpenBasePrefabSelector", SdfIconType.Brush, "베이스 타일 변경")]
    [SerializeField] private GameObject baseTilePrefab;
    
    [TitleGroup("현재 상태")]
    [EnumToggleButtons, HideLabel]
    [OnValueChanged("ChangeBrushCheck")]
    public EditStatus editStatusEnum;

    [LabelText("타일 브러시")]
    [SerializeField]
    [ShowIf("@IsTileChange() || IsStackTile()")]
    [InlineButton("OpenTileBrushPrefabSelector", SdfIconType.Brush, "브러시 변경")]
    private GameObject selectedTilePrefab;

    [LabelText("브러시 사이즈")]
    [SerializeField]
    [ShowIf(nameof(IsTileChange))]
    private int brushSize;
    
    [LabelText("오브젝트 브러시"), InlineEditor]
    [SerializeField]
    [ShowIf(nameof(IsObjectSet))]
    [InlineButton("OpenObjectPrefabSelector", SdfIconType.Brush, "브러시 변경")]
    private GameObject selectedObjectPrefab;
    
    [Title("맵 데이터 관련")]
    [LabelText("맵 데이터 리스트")]
    [SerializeField]private List<TileScript> tileDatas;
    
    #endregion
    
    #region private 변수

    // 타일 관리 딕셔너리
    private Dictionary<Vector3, GameObject> quadTiles = new Dictionary<Vector3, GameObject>();
    // 타일 좌표 딕셔너리
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();
    
    // 시작 타일 오브젝트
    private GameObject startTileObj;
    // 종료 타일 오브젝트
    private GameObject endTileObj;
    // 움직임 코루틴
    private Coroutine moveCoroutine = null;
    
    private const string prefabPath = "Prefabs/TilePrefabs";
    
    private List<GameObject> tilePrefabs = new List<GameObject>();
    
    private GameObject previewInstance;
    private GameObject lastPreviewTile;

    private Transform createPos;

    private Coroutine stackingCoroutine = null;
    
    private float stackDelay = 0.3f;
    
    private float lastStackTime = 0f;
    
    private bool isMapCreated = false;

    private int calMapSize;
    
    private float qLastTapTime = -1f;
    private float eLastTapTime = -1f;

    private float qHoldTime = 0f;
    private float eHoldTime = 0f;

    private const float doubleTapThreshold = 0.3f;
    private const float holdThreshold = 0.25f;
    private const float slowRotateSpeed = 90f;
    
    private bool IsTileChange() => editStatusEnum == EditStatus.ChangeTile;
    private bool IsObjectSet() => editStatusEnum == EditStatus.SetObject;
    private bool IsStackTile() => editStatusEnum == EditStatus.StackTile;
    
    private bool isEditorInit;
    
    #endregion
    void Start()
    {
        LoadTilePrefabs();
    }

    #region Public Functions

    public int GetMapSize()
    {
        return calMapSize;
    }

    public bool GetInitStatus()
    {
        return isMapCreated;
    }
    
    #endregion
    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
    
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 centerPos = hit.collider.transform.position;
                int range = brushSize;
    
                for (int x = -range + 1; x < range; x++)
                {
                    for (int z = -range + 1; z < range; z++)
                    {
                        Vector3 offsetPos = centerPos + new Vector3(x, 0, z);
    
                        if (!quadTiles.ContainsKey(offsetPos)) continue;
    
                        GameObject selectedTile = quadTiles[offsetPos];
                        if (!selectedTile.CompareTag("QuadTile")) continue;
    
                        TileScript getTileScript = selectedTile.GetComponent<TileScript>();
                        if (getTileScript == null) continue;
    
                        switch (editStatusEnum)
                        {
                            case EditStatus.EraseToNormal:
                                getTileScript.SetTilePrefab(baseTilePrefab);
                                getTileScript.SetMovable(true);
                                selectedTile.tag = "QuadTile";
                                break;
    
                            case EditStatus.ChangeTile:
                                if (selectedTilePrefab == null)
                                {
                                    Debug.Log("변경할 타일 미선택");
                                    return;
                                }
                                getTileScript.SetTilePrefab(selectedTilePrefab);
                                getTileScript.SetMovable(false);
                                break;
    
                            case EditStatus.StackTile:
                                if (Time.time - lastStackTime < stackDelay) return;
    
                                getTileScript.SetMovable(false);
    
                                List<GameObject> stackObjList = getTileScript.GetStackList();
                                if (stackObjList == null)
                                    stackObjList = new List<GameObject>();
    
                                if (stackObjList.Count < 5)
                                {
                                    Vector3 stackPos = selectedTile.transform.position + Vector3.up * (1f + stackObjList.Count);
                                    GameObject stackedObj = Instantiate(selectedTilePrefab, stackPos, Quaternion.identity, selectedTile.transform);
                                    stackedObj.layer = LayerMask.NameToLayer("Ignore Raycast");
                                    stackObjList.Add(stackedObj);
                                    lastStackTime = Time.time;
                                }
                                break;
    
                            case EditStatus.SetObject:
                                if (selectedObjectPrefab == null)
                                {
                                    Debug.Log("설치할 오브젝트가 없습니다.");
                                    return;
                                }
    
                                if (getTileScript.GetIsStackAble())
                                {
                                    List<GameObject> stackList = getTileScript.GetStackList();
                                    if (stackList == null)
                                        stackList = new List<GameObject>();
    
                                    Vector3 basePos;
    
                                    if (stackList.Count > 0)
                                    {
                                        // 가장 마지막 스택된 오브젝트 위에 설치
                                        GameObject topObj = stackList[stackList.Count - 1];
                                        basePos = topObj.transform.position + Vector3.up * 1f;
                                    }
                                    else
                                    {
                                        // 기존 objectObj 위에 설치
                                        Transform baseTransform = getTileScript.transform;
                                        GameObject baseObject = baseTransform.childCount > 0 ? baseTransform.GetChild(0).gameObject : null;
                                    }
                                    
                                    getTileScript.SetTileStackAble(false); // 한 번만 스택 가능
                                }
                                break;
                        }
                    }
                }
            }
        }

        // R 키로 맵 초기화
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            GenerateQuadTileMap();
        }
    
        if (editStatusEnum == EditStatus.SetObject && selectedObjectPrefab != null)
        {
            // 마우스 위치에 따라 프리뷰 갱신
            Ray previewRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(previewRay, out RaycastHit previewHit))
            {
                Vector3 targetPos = previewHit.collider.transform.position;
    
                if (quadTiles.ContainsKey(targetPos))
                {
                    GameObject tile = quadTiles[targetPos];
                    if (tile != lastPreviewTile)
                    {
                        lastPreviewTile = tile;
    
                        if (previewInstance == null)
                        {
                            previewInstance = Instantiate(selectedObjectPrefab);
                            SetPreviewMode(previewInstance);
                        }
    
                        BoxCollider col = previewInstance.GetComponentInChildren<BoxCollider>();
                        Vector3 offset = Vector3.zero;
    
                        if (col != null)
                        {
                            Vector3 center = col.center;
                            Vector3 size = col.size;
    
                            float yOffset = 0.5f + ((size.y * 0.5f) - center.y);
                            offset = new Vector3(-center.x, yOffset, -center.z);
                        }
    
                        Vector3 snapped = SnapToTileCenter(targetPos);
                        previewInstance.transform.position = snapped + offset;
                    }
                    
                    if (previewInstance != null)
                    {
                        UpdatePreview();
                    }
                }
            }
            else
            {
                DestroyPreviewInstance();
            }
        }
    
        if (previewInstance != null)
        {
            // Q 회전 처리
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                float currentTime = Time.time;
                if (currentTime - qLastTapTime <= doubleTapThreshold)
                {
                    // 더블탭 감지
                    previewInstance.transform.Rotate(Vector3.up, -90f);
                    qLastTapTime = -1f; // 초기화
                }
                else
                {
                    qLastTapTime = currentTime;
                }
    
                qHoldTime = 0f; // 길게 누르기 타이머 리셋
            }
    
            if (Keyboard.current.qKey.isPressed)
            {
                qHoldTime += Time.deltaTime;
                if (qHoldTime > holdThreshold)
                {
                    previewInstance.transform.Rotate(Vector3.up, -slowRotateSpeed * Time.deltaTime);
                }
            }
    
            if (Keyboard.current.qKey.wasReleasedThisFrame)
            {
                qHoldTime = 0f;
            }
    
            // E 회전 처리
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                float currentTime = Time.time;
                if (currentTime - eLastTapTime <= doubleTapThreshold)
                {
                    previewInstance.transform.Rotate(Vector3.up, 90f);
                    eLastTapTime = -1f;
                }
                else
                {
                    eLastTapTime = currentTime;
                }
    
                eHoldTime = 0f;
            }
    
            if (Keyboard.current.eKey.isPressed)
            {
                eHoldTime += Time.deltaTime;
                if (eHoldTime > holdThreshold)
                {
                    previewInstance.transform.Rotate(Vector3.up, slowRotateSpeed * Time.deltaTime);
                }
            }
    
            if (Keyboard.current.eKey.wasReleasedThisFrame)
            {
                eHoldTime = 0f;
            }
        }
    }
 
    void GenerateQuadTileMap()
    {
        if (!isEditorInit) return;
        
        int sizeX = (int)mapSize.x;
        int sizeY = (int)mapSize.y;
    
        tileDatas.Clear();
        quadTiles.Clear();
    
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        if (createPos == null)
        {
            GameObject createdObj;

            createdObj = new GameObject("TileMapParent");

            createPos = createdObj.transform;
        }
    
        else
        {
            foreach (Transform child in createPos)
            {
                Destroy(child.gameObject);
            }
        }

        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                Vector3 worldPos = new Vector3(x, 0, sizeY - 1 - y);
                GameObject tile = Instantiate(tilePrefab, createPos.position + worldPos, Quaternion.identity, createPos);

                TileScript getTile = tile.GetComponent<TileScript>();
                getTile.SetTilePrefab(baseTilePrefab);
                getTile.SetMovable(true);
                getTile.SetTileStackAble(true);
                getTile.SetTilePoint(x, y);
                tile.tag = "QuadTile"; // 태그 설정
                quadTiles.Add(transform.position + worldPos, tile);
                tileDatas.Add(tile.GetComponent<TileScript>());
                tileMap[new Vector2Int(x, y)] = getTile; 
            }
        }
    
        // 맵이 초기화될 때, 시작점과 도착점 초기화
        startTileObj = null;
        endTileObj = null;

        float cameraPos = (mapSize.y - 1) / 2f;

        Camera.main.transform.position = new Vector3(cameraPos, mapSize.y, cameraPos);

        isMapCreated = true;
    }

    void LoadTilePrefabs()
    {
        tilePrefabs.Clear();

        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);

        tilePrefabs.AddRange(loadedPrefabs);
        
        Debug.Log($"타일 프리팹 {tilePrefabs.Count}개 로드 완료.");

        isEditorInit = true;
    }

    private void SetPreviewMode(GameObject obj)
    {
        // 반투명 처리
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.shader = Shader.Find("Transparent/Diffuse");
            Color c = mat.color;
            c.a = 0.5f;
            mat.color = c;
        }

        // 레이어 설정
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Rigidbody 설정
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Collider를 Trigger로 바꾸기
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
            col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
    }
    
    List<TileScript> GetCoveredTilesByCollider(GameObject obj)
    {
        Bounds bounds = obj.GetComponent<Collider>().bounds;

        List<TileScript> coveredTiles = new List<TileScript>();

        foreach (var kvp in quadTiles)
        {
            Vector3 tilePos = kvp.Key;
            GameObject tile = kvp.Value;

            // 중심 위치가 콜라이더 안에 들어가는지 검사
            if (bounds.Contains(tilePos))
            {
                TileScript tileScript = tile.GetComponent<TileScript>();
                if (tileScript != null)
                {
                    coveredTiles.Add(tileScript);
                }
            }
        }

        return coveredTiles;
    }
    
    bool IsPlaceable(List<TileScript> tiles)
    {
        foreach (var tile in tiles)
        {
            if (!tile.GetIsMovable())
                return false;
        }
        return true;
    }

    void PlaceObject(GameObject prefab, Vector3 position)
    {
        Quaternion rotation = previewInstance != null ? previewInstance.transform.rotation : Quaternion.identity;

        GameObject obj = Instantiate(prefab, position, rotation);

        Collider col = obj.GetComponent<Collider>();
        if (col == null)
            col = obj.AddComponent<BoxCollider>();
        col.isTrigger = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        List<TileScript> coveredTiles = GetCoveredTilesByCollider(obj);
        foreach (var tile in coveredTiles)
        {
            tile.SetMovable(false);
        }
    }
    
    void UpdatePreview()
    {
        if (previewInstance == null) return;

        List<TileScript> coveredTiles = GetCoveredTilesByCollider(previewInstance);
        bool placeable = IsPlaceable(coveredTiles);
        
        BoxCollider col = previewInstance.GetComponentInChildren<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            
            Vector3 center = col.bounds.center;
            Vector3 halfExtents = col.bounds.extents;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, previewInstance.transform.rotation, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.gameObject != previewInstance && hit.CompareTag("Object"))
                {
                    placeable = false;
                }
            }
        }
        
        SetPreviewColor(previewInstance, placeable);

        if (Mouse.current.leftButton.wasPressedThisFrame && placeable)
        {
            PlaceObject(selectedObjectPrefab, previewInstance.transform.position);
        }
    }
    
    // 높이 변경 없음
    Vector3 SnapToTileCenter(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }
    
    void SetPreviewColor(GameObject obj, bool placeable)
    {
        Color targetColor = placeable ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.shader = Shader.Find("Transparent/Diffuse");
            mat.color = targetColor;
        }
    }

    void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
            lastPreviewTile = null;
        }
    }
    
    #region 인스펙터용 코드

    void ChangeBrushCheck()
    {
        // 오브젝트 설치인 경우 브러시 사이즈 1로 고정
        if (editStatusEnum == EditStatus.SetObject)
        {
            brushSize = 1;
        }
    }

    void SelectPrefab(PrefabType type, Action<GameObject> onSelected)
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            onSelected?.Invoke(prefab);
        }, type);
    }
    
    void OpenTileBrushPrefabSelector()
    {
        SelectPrefab(PrefabType.TilePrefab, prefab => selectedTilePrefab = prefab);
    }
    
    void OpenBasePrefabSelector()
    {
        SelectPrefab(PrefabType.TilePrefab, prefab => baseTilePrefab = prefab);
    }
    void OpenObjectPrefabSelector()
    {
        SelectPrefab(PrefabType.ObjectPrefab, prefab =>
        {
            selectedObjectPrefab = prefab;

            DestroyPreviewInstance();
        });
    }
    

    #endregion
}
