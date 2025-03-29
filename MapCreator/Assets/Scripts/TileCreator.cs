using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TileCreator : GenericSingleton<TileCreator>
{
    #region Inspector 보이는 변수
    
    [LabelText("맵 크기")]
    [InlineButton("GenerateQuadTileMap", SdfIconType.Map, "맵 생성")]
    [SerializeField] private Vector2 mapSize; // 맵 크기
    
    [TitleGroup("프리팹")]
    [LabelText("베이스 타일 프리팹")]
    [SerializeField] private GameObject tilePrefab;
    
    [TitleGroup("프리팹")]
    [LabelText("바닥 타일")]
    [InlineButton("OpenBasePrefabSelector", SdfIconType.Brush, "베이스 타일 변경")]
    [SerializeField] private GameObject baseTilePrefab;
    
    [TitleGroup("현재 상태")]
    [EnumToggleButtons, HideLabel]
    [OnValueChanged("ChangeBrushCheck")]
    public EditStatus editStatusEnum;
    void ChangeBrushCheck()
    {
        // 오브젝트 설치인 경우 브러시 사이즈 1로 고정
        if (editStatusEnum == EditStatus.SetObject)
        {
            brushSize = 1;
        }
    }
    
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
    
    [Title("맵 데이터")]
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

    private Coroutine stackingCoroutine = null;
    
    private float stackDelay = 0.3f;
    
    private float lastStackTime = 0f;

    private Transform createPos;
    
    private bool isMapCreated = false;

    private int calMapSize;
    
    private float qLastTapTime = -1f;
    private float eLastTapTime = -1f;

    private float qHoldTime = 0f;
    private float eHoldTime = 0f;

    private const float doubleTapThreshold = 0.3f;
    private const float holdThreshold = 0.25f;
    private const float slowRotateSpeed = 90f;
    
    private List<GameObject> lastHighlightedTiles = new List<GameObject>();
    private Color previewTileColor = Color.red;
    private Color defaultTileColor = Color.white;
    
    private bool IsTileChange() => editStatusEnum == EditStatus.ChangeTile;
    private bool IsObjectSet() => editStatusEnum == EditStatus.SetObject;
    private bool IsStackTile() => editStatusEnum == EditStatus.StackTile;
    
    #endregion

    void Start()
    {
        LoadTilePrefabs();
    }
    
    void Update()
    {
        HandleTileEdit();
        UpdatePreviewInstance();
        HandlePreviewRotation();
        HandleKeyboardShortcuts();
        
        if (editStatusEnum != EditStatus.SetObject)
        {
            DestroyPreviewInstance();
        }
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

    void HandleTileEdit()
    {
        if (!Mouse.current.leftButton.isPressed) return;
    
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
    
        Vector3 centerPos = hit.collider.transform.position;
        int range = brushSize;
    
        for (int x = -range + 1; x < range; x++)
        {
            for (int z = -range + 1; z < range; z++)
            {
                Vector3 offsetPos = centerPos + new Vector3(x, 0, z);
    
                if (!quadTiles.TryGetValue(offsetPos, out GameObject selectedTile) || !selectedTile.CompareTag("QuadTile"))
                    continue;
    
                TileScript tile = selectedTile.GetComponent<TileScript>();
                if (tile == null) continue;
    
                switch (editStatusEnum)
                {
                    case EditStatus.EraseToNormal:
                        tile.SetTilePrefab(baseTilePrefab);
                        tile.SetMovable(true);
                        selectedTile.tag = "QuadTile";
                        break;
    
                    case EditStatus.ChangeTile:
                        if (selectedTilePrefab == null)
                        {
                            Debug.Log("변경할 타일 미선택");
                            return;
                        }
                        tile.SetTilePrefab(selectedTilePrefab);
                        tile.SetMovable(false);
                        break;
    
                    case EditStatus.StackTile:
                        if (Time.time - lastStackTime < stackDelay) return;
    
                        tile.SetMovable(false);
                        var stackList = tile.GetStackList() ?? new List<GameObject>();
    
                        if (stackList.Count < 5)
                        {
                            Vector3 stackPos = selectedTile.transform.position + Vector3.up * (1f + stackList.Count);
                            var stackedObj = Instantiate(selectedTilePrefab, stackPos, Quaternion.identity, selectedTile.transform);
                            stackedObj.layer = LayerMask.NameToLayer("Ignore Raycast");
                            stackList.Add(stackedObj);
                            lastStackTime = Time.time;
                        }
                        break;
    
                    case EditStatus.SetObject:
                        if (selectedObjectPrefab == null)
                        {
                            Debug.Log("설치할 오브젝트가 없습니다.");
                            return;
                        }
    
                        if (tile.GetIsStackAble())
                        {
                            var stackObjs = tile.GetStackList() ?? new List<GameObject>();
                            Vector3 basePos;
    
                            if (stackObjs.Count > 0)
                            {
                                basePos = stackObjs[^1].transform.position + Vector3.up * 1f;
                            }
                            else
                            {
                                var baseObj = tile.transform.childCount > 0 ? tile.transform.GetChild(0).gameObject : null;
                                basePos = baseObj != null
                                    ? baseObj.transform.position + Vector3.up * 1f
                                    : tile.transform.position + Vector3.up * 1f;
                            }
    
                            var stackedObj = Instantiate(selectedObjectPrefab, basePos, Quaternion.identity, selectedTile.transform);
                            stackedObj.layer = LayerMask.NameToLayer("Ignore Raycast");
                            stackObjs.Add(stackedObj);
                            tile.SetTileStackAble(false);
                        }
                        break;
                }
            }
        }
    }
    
    void UpdatePreviewInstance()
    {
        if (editStatusEnum != EditStatus.SetObject || selectedObjectPrefab == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            DestroyPreviewInstance();
            return;
        }

        Vector3 targetPos = hit.collider.transform.position;

        if (!quadTiles.ContainsKey(targetPos)) return;

        GameObject tile = quadTiles[targetPos];
        if (tile == lastPreviewTile) return;

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

        previewInstance.transform.position = SnapToTileCenter(targetPos) + offset;

        UpdatePreview();
    }
    
    // 조작 프리뷰 로테이션
    void HandlePreviewRotation()
    {
        if (previewInstance == null) return;

        HandleRotationKey(Keyboard.current.qKey, ref qLastTapTime, ref qHoldTime, -90f, -slowRotateSpeed);
        HandleRotationKey(Keyboard.current.eKey, ref eLastTapTime, ref eHoldTime, 90f, slowRotateSpeed);
    }

    // 조작 => 키보드 q, r키로 프리뷰 각도 수정
    void HandleRotationKey(KeyControl key, ref float lastTapTime, ref float holdTime, float fastAngle, float slowSpeed)
    {
        if (key.wasPressedThisFrame)
        {
            float currentTime = Time.time;
            if (currentTime - lastTapTime <= doubleTapThreshold)
            {
                previewInstance.transform.Rotate(Vector3.up, fastAngle);
                lastTapTime = -1f;
            }
            else
            {
                lastTapTime = currentTime;
            }
            holdTime = 0f;
        }

        if (key.isPressed)
        {
            holdTime += Time.deltaTime;
            if (holdTime > holdThreshold)
            {
                previewInstance.transform.Rotate(Vector3.up, slowSpeed * Time.deltaTime);
            }
        }

        if (key.wasReleasedThisFrame)
        {
            holdTime = 0f;
        }
    }
    
    // 조작 => 키보드 키
    void HandleKeyboardShortcuts()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            GenerateQuadTileMap();
        }
    }
    
    // 맵 생성
    void GenerateQuadTileMap()
    {
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
        
        Dictionary<Vector3, Vector2Int> worldToGridMap = new Dictionary<Vector3, Vector2Int>();
    
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
    
    // 타일 컬러 변경
    void ChangeTileColor(GameObject tile, Color color)
    {
        Transform child = tile.transform.childCount > 0 ? tile.transform.GetChild(0) : null;
        if (child == null) return;
    
        Renderer tileRenderer = child.GetComponent<Renderer>();
        if (tileRenderer == null) return;
    
        Material newMaterial = new Material(tileRenderer.material);
        newMaterial.color = color;
        tileRenderer.material = newMaterial;
    }
    
    // 리소스 로드
    void LoadTilePrefabs()
    {
        tilePrefabs.Clear();

        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);

        tilePrefabs.AddRange(loadedPrefabs);
        
        Debug.Log($"타일 프리팹 {tilePrefabs.Count}개 로드 완료.");
    }
    
    /// <summary>
    /// 프리뷰 관련
    /// </summary>
    /// <param name="obj"></param>
    
    void SetPreviewMode(GameObject obj)
    {
        ApplyTransparentMaterial(obj);
        SetPreviewRigidbody(obj);
        SetPreviewCollider(obj);
    
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void ApplyTransparentMaterial(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.shader = Shader.Find("Transparent/Diffuse");
            Color c = mat.color;
            c.a = 0.5f;
            mat.color = c;
        }
    }

    void SetPreviewRigidbody(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void SetPreviewCollider(GameObject obj)
    {
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
                    Debug.Log("충돌타일검색");
                    coveredTiles.Add(tileScript);
                }
            }
        }

        return coveredTiles;
    }
    
    // 설치 가능 여부
    bool IsPlaceable(List<TileScript> tiles)
    {
        foreach (var tile in tiles)
        {
            if (!tile.GetIsMovable())
                return false;
        }
        return true;
    }

    // 오브젝트 설치
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
        
        foreach (GameObject tile in lastHighlightedTiles)
        {
            ChangeTileColor(tile, defaultTileColor);
        }
        lastHighlightedTiles.Clear();
    }
    
    // 프리뷰 활성화
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
        
        foreach (GameObject tile in lastHighlightedTiles)
        {
            ChangeTileColor(tile, defaultTileColor);
        }
        lastHighlightedTiles.Clear();

// 새로 칠하기
        foreach (TileScript tileScript in coveredTiles)
        {
            GameObject tileObj = tileScript.gameObject;
            ChangeTileColor(tileObj, previewTileColor);
            lastHighlightedTiles.Add(tileObj);
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
    
    // 프리뷰 컬러 변경
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
    
    void SelectPrefab(PrefabType type, Action<GameObject> onSelected)
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            onSelected?.Invoke(prefab);
        }, type);
    }
    
    // 프리뷰 비활성화
    void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
            lastPreviewTile = null;
        }
    }
    
    /// <summary>
    /// 인스펙터 조작용
    /// </summary>
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

            // 기존 프리뷰 제거
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
                lastPreviewTile = null;
            }
        });
    }
}
