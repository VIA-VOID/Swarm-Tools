using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Newtonsoft.Json;
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
    [LabelText("맵 이름")] [SerializeField]
    private string mapSaveName;
    
    [ButtonGroup("SaveLoad Button Group")]
    [Button("저장", ButtonSizes.Large)]
    private void SaveMapPrefab()
    {
        string path = "Assets/Resources/Prefabs/MapSavePrefabs";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string fileName = string.IsNullOrEmpty(mapSaveName) 
            ? DateTime.Now.ToString("yyyyMMdd_HHmmss") 
            : mapSaveName;

        string fullPath = Path.Combine(path, fileName + ".prefab");
        
        ClearTileHighlights();
        
        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(createPos.gameObject, fullPath, UnityEditor.InteractionMode.AutomatedAction);
        Debug.Log("맵 저장 완료: " + fullPath);
    }

    [ButtonGroup("SaveLoad Button Group")]
    [Button("불러오기", ButtonSizes.Large)]
    private void LoadMapPrefabList()
    {
        GameObject[] savedMaps = Resources.LoadAll<GameObject>("Prefabs/MapSavePrefabs");

        if (savedMaps.Length == 0)
        {
            Debug.LogWarning("저장된 맵이 없습니다.");
            return;
        }

        PrefabSelectorPopup.Show(prefab =>
        {
            if (createPos != null)
            {
                foreach (Transform child in createPos)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            GameObject loadedMap = Instantiate(prefab, createPos);
            loadedMap.name = prefab.name;
            Debug.Log("맵 불러오기 완료: " + prefab.name);
        }, savedMaps); // ← 여기를 처리하려면 오버로드 함수가 필요!
    }
    
    [ButtonGroup("SaveLoad Button Group")]
    [Button("Json 저장", ButtonSizes.Large)]
    private void SaveMapToJson()
    {
        string path = "Assets/Resources/MapSaveData";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string fileName = string.IsNullOrEmpty(mapSaveName) 
            ? DateTime.Now.ToString("yyyyMMdd_HHmmss") 
            : mapSaveName;

        string fullPath = Path.Combine(path, fileName + ".json");

        try
        {
            List<TileData> saveList = new List<TileData>();
            foreach (var tile in tileDatas)
            {
                TileData data = new TileData
                {
                    isMovable = tile.GetIsMovable() ? 1 : 0,
                    tilePoint = tile.GetTilePoint()
                };
                saveList.Add(data);
            }

            var json = JsonConvert.SerializeObject(saveList, Formatting.Indented);
            File.WriteAllText(fullPath, json, Encoding.UTF8);
            Debug.Log("맵 JSON 저장 완료: " + fullPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("맵 저장 실패: " + ex.Message);
        }
    }
    
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
    
    GameObject objectPreviewInstance;
    GameObject stackPreviewInstance;
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
    
    private Transform targetParent;

    private bool IsTileChange()
    {
        DestroyPreviewInstance();
        
        return editStatusEnum == EditStatus.ChangeTile;
    }

    private bool IsObjectSet() => editStatusEnum == EditStatus.SetObject;
    private bool IsStackTile()
    {
        DestroyPreviewInstance();
        return editStatusEnum == EditStatus.StackTile;
    }

    private bool isEditorInit;

    private List<GameObject> highlightedTiles = new List<GameObject>();

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
        HandleBrushPaint();
        HandlePreviewObject();
        HandlePreviewStack();
        HandleRotation();
        HandleShortcuts();
    }

void HandleBrushPaint()
{
    if (!Mouse.current.leftButton.isPressed) return;

    if (!Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit)) return;

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

            TileScript tileScript = selectedTile.GetComponent<TileScript>();
            if (tileScript == null) continue;

            switch (editStatusEnum)
            {
                case EditStatus.EraseToNormal:
                    if (tileScript.CheckObjectPrefab())
                    {
                        GameObject obj = tileScript.GetObjectPrefab();
                        if (obj != null)
                        {
                            Destroy(obj);
                        }

                        List<TileScript> disabledTiles = tileScript.GetObjectDisabledTiles();
                        foreach (TileScript t in disabledTiles)
                        {
                            t.SetMovable(true);
                        }
                        tileScript.SetObjectDisabledTiles(null);
                        tileScript.SetObjectPrefab(null);
                    }

                    List<GameObject> stackList = tileScript.GetStackList();
                    if (stackList != null && stackList.Count > 0)
                    {
                        foreach (GameObject stacked in stackList)
                        {
                            if (stacked != null)
                            {
                                Destroy(stacked);
                            }
                        }
                        stackList.Clear();
                        tileScript.SetMovable(true);
                    }

                    tileScript.SetTilePrefab(baseTilePrefab);
                    tileScript.SetMovable(true);
                    selectedTile.tag = "QuadTile";
                    break;

                case EditStatus.ChangeTile:
                    if (selectedTilePrefab == null)
                    {
                        Debug.Log("변경할 타일 미선택");
                        return;
                    }
                    tileScript.SetTilePrefab(selectedTilePrefab);
                    tileScript.SetMovable(false);
                    break;

                case EditStatus.StackTile:
                    if (Time.time - lastStackTime < stackDelay) return;

                    if (selectedTilePrefab == null) return;
                    
                    tileScript.SetMovable(false);
                    List<GameObject> stackObjList = tileScript.GetStackList() ?? new List<GameObject>();
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
                    if (tileScript.GetIsStackAble())
                    {
                        List<GameObject> stackListObj = tileScript.GetStackList() ?? new List<GameObject>();
                        tileScript.SetTileStackAble(false);
                    }
                    break;
            }
        }
    }
}
    
    void HandlePreviewObject()
    {
        if (!(editStatusEnum == EditStatus.SetObject && selectedObjectPrefab != null)) return;

        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit previewHit))
        {
            DestroyPreviewInstance(objectPreviewInstance);
            return;
        }

        Vector3 targetPos = SnapToTileCenter(previewHit.collider.transform.position);
        if (!quadTiles.ContainsKey(targetPos)) return;

        GameObject tile = quadTiles[targetPos];
        targetParent = tile.transform;

        if (tile != lastPreviewTile || objectPreviewInstance == null)
        {
            lastPreviewTile = tile;

            if (objectPreviewInstance != null)
            {
                Destroy(objectPreviewInstance);
            }

            objectPreviewInstance = Instantiate(selectedObjectPrefab);
            SetPreviewMode(objectPreviewInstance);

            BoxCollider col = objectPreviewInstance.GetComponentInChildren<BoxCollider>();
            Vector3 offset = Vector3.zero;

            if (col != null)
            {
                Vector3 center = col.center;
                Vector3 size = col.size;
                float yOffset = 0.5f + ((size.y * 0.5f) - center.y);
                offset = new Vector3(-center.x, yOffset, -center.z);
            }

            objectPreviewInstance.transform.position = targetPos + offset;
        }

        if (objectPreviewInstance != null)
        {
            UpdatePreview(objectPreviewInstance);
        }
    }

    void DestroyPreviewInstance(GameObject preview)
    {
        if (preview != null)
        {
            Destroy(preview);
            if (preview == objectPreviewInstance) objectPreviewInstance = null;
            if (preview == stackPreviewInstance) stackPreviewInstance = null;
        }
    } 

    
    void HandlePreviewStack()
    {
        if (!(editStatusEnum == EditStatus.StackTile && selectedTilePrefab != null))
        {
            DestroyPreviewInstance(stackPreviewInstance);
            return;
        }

        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit previewHit))
        {
            DestroyPreviewInstance(stackPreviewInstance);
            return;
        }

        Vector3 targetPos = previewHit.collider.transform.position;
        if (!quadTiles.ContainsKey(targetPos)) return;

        GameObject tile = quadTiles[targetPos];
        targetParent = tile.transform;

        TileScript tileScript = tile.GetComponent<TileScript>();
        if (tileScript == null) return;

        List<GameObject> stackList = tileScript.GetStackList() ?? new List<GameObject>();
        float yOffset = 1f + stackList.Count;
        Vector3 previewPos = tile.transform.position + Vector3.up * yOffset;

        bool positionChanged = stackPreviewInstance == null || stackPreviewInstance.transform.position != previewPos;

        if (tile != lastPreviewTile || positionChanged)
        {
            lastPreviewTile = tile;

            if (stackPreviewInstance != null)
            {
                Destroy(stackPreviewInstance);
            }

            stackPreviewInstance = Instantiate(selectedTilePrefab);
            SetPreviewMode(stackPreviewInstance);

            stackPreviewInstance.transform.position = previewPos;
        }

        bool canStack = stackList.Count < 5;
        SetPreviewColor(stackPreviewInstance, canStack);
    }

    
    void HandleRotation()
    {
        if (objectPreviewInstance != null)
        {
            HandleRotationKey(Keyboard.current.qKey, ref qLastTapTime, ref qHoldTime, -90f, -slowRotateSpeed, objectPreviewInstance);
            HandleRotationKey(Keyboard.current.eKey, ref eLastTapTime, ref eHoldTime, 90f, slowRotateSpeed, objectPreviewInstance);
        }
        else if (stackPreviewInstance != null)
        {
            HandleRotationKey(Keyboard.current.qKey, ref qLastTapTime, ref qHoldTime, -90f, -slowRotateSpeed, stackPreviewInstance);
            HandleRotationKey(Keyboard.current.eKey, ref eLastTapTime, ref eHoldTime, 90f, slowRotateSpeed, stackPreviewInstance);
        }
    }


    void HandleShortcuts()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            GenerateQuadTileMap();
        }
    }
    
    void HandleRotationKey(KeyControl key, ref float lastTapTime, ref float holdTime, float fastAngle, float slowSpeed, GameObject previewTarget)
    {
        float currentTime = Time.time;

        if (key.wasPressedThisFrame)
        {
            if (currentTime - lastTapTime <= doubleTapThreshold)
            {
                previewTarget.transform.Rotate(Vector3.up, fastAngle);
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
                previewTarget.transform.Rotate(Vector3.up, slowSpeed * Time.deltaTime);
            }
        }

        if (key.wasReleasedThisFrame)
        {
            holdTime = 0f;
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
        
        PresetController.Instance.PresetListON();
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

    void PlaceObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(prefab, position, rotation);
        obj.transform.SetParent(targetParent);

        targetParent.GetComponent<TileScript>().SetObjectPrefab(obj);

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
    
void UpdatePreview(GameObject preview)
{
    if (preview == null) return;

    List<TileScript> coveredTiles = GetCoveredTilesByCollider(preview);
    bool placeable = IsPlaceable(coveredTiles);

    foreach (var tile in highlightedTiles)
    {
        ChangeTileColor(tile, Color.white);
    }
    highlightedTiles.Clear();

    BoxCollider col = preview.GetComponentInChildren<BoxCollider>();
    if (col != null)
    {
        col.isTrigger = true;

        Vector3 center = col.bounds.center;
        Vector3 halfExtents = col.bounds.extents;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, preview.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (hit.gameObject != preview && hit.CompareTag("Object"))
            {
                placeable = false;
            }

            if (hit.gameObject != preview && hit.CompareTag("QuadTile"))
            {
                GameObject tileChild = hit.gameObject;
                Transform parent = tileChild.transform.parent;

                bool isMovable = true;
                if (parent != null)
                {
                    TileScript tileScript = parent.GetComponent<TileScript>();
                    if (tileScript != null)
                    {
                        isMovable = tileScript.GetIsMovable();
                    }
                }

                ChangeTileColor(tileChild, isMovable ? Color.green : Color.red);
                if (!isMovable) placeable = false;

                highlightedTiles.Add(tileChild);
            }
        }
    }

    SetPreviewColor(preview, placeable);

    if (Mouse.current.leftButton.wasPressedThisFrame && placeable)
    {
        PlaceObject(selectedObjectPrefab, preview.transform.position, preview.transform.rotation);

        List<TileScript> getList = new List<TileScript>();
        foreach (GameObject tileChild in highlightedTiles)
        {
            Transform parent = tileChild.transform.parent;
            if (parent != null)
            {
                TileScript tileScript = parent.GetComponent<TileScript>();
                if (tileScript != null)
                {
                    getList.Add(tileScript);
                    tileScript.SetMovable(false);
                }
            }
            ChangeTileColor(tileChild, Color.white);
        }

        targetParent.GetComponent<TileScript>().SetObjectDisabledTiles(getList);
        highlightedTiles.Clear();
    }
}

    void ChangeTileColor(GameObject tile, Color color)
    {
        Transform child = tile.transform.childCount > 0 ? tile.transform.GetChild(0) : tile.transform;

        MeshRenderer renderer = child.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        
        renderer.material.color = color;
    }
    
    // 높이 변경 없음
    Vector3 SnapToTileCenter(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }
    
    void SetPreviewColor(GameObject obj, bool placeable)
    {
        Color targetColor = placeable ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);

        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.shader = Shader.Find("Transparent/Diffuse");
            mat.color = targetColor;
        }
    }

    void DestroyPreviewInstance()
    {
        if (objectPreviewInstance != null)
        {
            Destroy(objectPreviewInstance);
            objectPreviewInstance = null;
        }
        if (stackPreviewInstance != null)
        {
            Destroy(stackPreviewInstance);
            stackPreviewInstance = null;
        }
        lastPreviewTile = null;
        ClearTileHighlights();
    }
    
    private void ClearTileHighlights()
    {
        foreach (GameObject tile in highlightedTiles)
        {
            ResetTileMaterial(tile);
        }
        highlightedTiles.Clear();
    }
    
    private void ResetTileMaterial(GameObject tile)
    {
        Transform child = tile.transform.childCount > 0 ? tile.transform.GetChild(0) : tile.transform;
        MeshRenderer renderer = child.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        
        renderer.material = renderer.sharedMaterial;
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

    void SelectPrefab(GameObject[] prefabs, Action<GameObject> onSelected)
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            onSelected?.Invoke(prefab);
        }, prefabs);
    }
    
    void OpenTileBrushPrefabSelector()
    {
#if UNITY_EDITOR
        GameObject[] tilePrefabs = PresetController.Instance.tilePrefabs.ToArray();
        SelectPrefab(tilePrefabs, prefab => selectedTilePrefab = prefab);
#endif
    }

    void OpenBasePrefabSelector()
    {
#if UNITY_EDITOR
        GameObject[] tilePrefabs = PresetController.Instance.tilePrefabs.ToArray();
        SelectPrefab(tilePrefabs, prefab => baseTilePrefab = prefab);
#endif
    }

    void OpenObjectPrefabSelector()
    {
#if UNITY_EDITOR
        GameObject[] objectPrefabs = PresetController.Instance.objectPrefabs.ToArray();
        SelectPrefab(objectPrefabs, prefab =>
        {
            selectedObjectPrefab = prefab;
            DestroyPreviewInstance();
        });
#endif
    }

    #endregion
}
