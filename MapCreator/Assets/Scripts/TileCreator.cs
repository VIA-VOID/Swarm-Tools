using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.WSA;

public class TileCreator : MonoBehaviour
{
    [LabelText("맵 크기")]
    [SerializeField] private Vector2 mapSize; // 맵 크기
    
    [TitleGroup("프리팹")]
    [LabelText("타일 프리팹")]
    [SerializeField] private GameObject tilePrefab;
    
    [TitleGroup("프리팹")]
    [Button("베이스 타일 변경")]
    private void OpenBasePrefabSelector()
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            baseTilePrefab = prefab;
        },PrefabType.TilePrefab);
    }
    [LabelText("베이스 타일")]
    [SerializeField] private GameObject baseTilePrefab;

    [Title("카메라 컨트롤")]
    [LabelText("카메라 거리")]
    [OnValueChanged("ChangeCameraHeight")]
    [SerializeField] private int cameraHeight;
    [LabelText("카메라 각도")]
    //[OnValueChanged("")]
    [SerializeField] private float cameraAngle;
    
    [Title("맵 데이터 관련")]
    [LabelText("맵 데이터 리스트")]
    [SerializeField]private List<TileScript> tileDatas;

    [FormerlySerializedAs("createStatus")]
    [TitleGroup("현재 상태")]
    [EnumToggleButtons, HideLabel]
    public EditStatus pathFinderEnum;
    
    // 타일 관리 딕셔너리
    private Dictionary<Vector3, GameObject> quadTiles = new Dictionary<Vector3, GameObject>();
    // 타일 좌표 딕셔너리
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();
    
    // 생성된 캐릭터
    private GameObject spawnedCharacter;
    // 사용중인 메인 카메라
    private Camera mainCamera;
    // 시작 타일 오브젝트
    private GameObject startTileObj;
    // 종료 타일 오브젝트
    private GameObject endTileObj;
    // 움직임 코루틴
    private Coroutine moveCoroutine = null;
    
    private const string prefabPath = "Prefabs/TilePrefabs";
    
    private List<GameObject> tilePrefabs = new List<GameObject>();
    public List<TileScript> testList;
    
    [FormerlySerializedAs("selectedPrefab")]
    [LabelText("타일 브러시"), InlineEditor]
    [SerializeField, ReadOnly] private GameObject selectedTilePrefab;

    [TitleGroup("현재 상태")]
    [Button("타일 브러시 변경")]
    private void OpenTileBrushPrefabSelector()
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            selectedTilePrefab = prefab;
        },PrefabType.TilePrefab);
    }
    
    [LabelText("오브젝트 브러시"), InlineEditor]
    [SerializeField, ReadOnly] private GameObject selectedObjectPrefab;
    
    [Button("설치 오브젝트 변경")]
    private void OpenObjectPrefabSelector()
    {
        PrefabSelectorPopup.Show(prefab =>
        {
            selectedObjectPrefab = prefab;
        }, PrefabType.ObjectPrefab);
    }
    
    private void Start()
    {
        mainCamera = Camera.main;

        LoadTilePrefabs();
    }

    #region Public Functions

    #endregion

    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 pos = hit.collider.transform.position;
                
                if (quadTiles.ContainsKey(pos) && quadTiles[pos].CompareTag("QuadTile"))
                {
                    GameObject selectedTile = quadTiles[pos];

                    if (pathFinderEnum == EditStatus.EraseToNormal)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();
                        
                        getTileScript.SetTilePrefab(baseTilePrefab);
                        
                        getTileScript.SetMovable(true);
                        
                        selectedTile.tag = "QuadTile";

                        if (getTileScript != null)
                        {
                            getTileScript.SetMovable(true); // 이동 가능 블록으로 재변경
                        }
                    }

                    if (pathFinderEnum == EditStatus.ChangeTile)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();
                        
                        getTileScript.SetTilePrefab(selectedTilePrefab);
                        
                        getTileScript.SetMovable(false);
                        
                        if (getTileScript != null)
                        {
                            getTileScript.SetMovable(false);
                        }
                    }

                    if (pathFinderEnum == EditStatus.SetObject)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();
                                         
                        getTileScript.SetObjectPrefab(selectedTilePrefab);
                        
                        getTileScript.SetMovable(false);
                        
                        if (getTileScript != null)
                        {
                            getTileScript.SetMovable(false);
                        }
                    }
                }
            }
        }
        
        // R 키를 누르면 맵을 초기화
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            GenerateQuadTileMap();
        }
    }

    void ChangeCameraHeight()
    {
        Vector3 originPos = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(originPos.x, cameraHeight, originPos.z);
    }

    void ChangeCameraAngle()
    {
        
    }
    
    [Title("제어 버튼")]
    [Button("맵 생성")]
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
    
        if (spawnedCharacter != null)
        {
            Destroy(spawnedCharacter);
        }
    
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    
        Dictionary<Vector3, Vector2Int> worldToGridMap = new Dictionary<Vector3, Vector2Int>();
    
        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                Vector3 worldPos = new Vector3(x, 0, sizeY - 1 - y);
                GameObject tile = Instantiate(tilePrefab, transform.position + worldPos, Quaternion.identity, transform);

                TileScript getTile = tile.GetComponent<TileScript>();
                getTile.SetTilePrefab(baseTilePrefab);
                getTile.SetMovable(true);
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

        cameraHeight = (int)mapSize.y;
        
        ChangeCameraHeight();
    }
    
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

    void LoadTilePrefabs()
    {
        tilePrefabs.Clear();

        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);

        tilePrefabs.AddRange(loadedPrefabs);
        
        Debug.Log($"타일 프리팹 {tilePrefabs.Count}개 로드 완료.");
    }
}
