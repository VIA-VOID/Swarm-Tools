using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Title("카메라 컨트롤")]
    [LabelText("카메라 거리")]
    [OnValueChanged("ChangeCameraHeight")]
    public int cameraHeight;
    [OnValueChanged("ChangeCameraAngle")]
    [LabelText("카메라 각도 슬라이더")]
    [SerializeField] private Slider cameraAngleSlider;
    [LabelText("카메라 이동 속도")]
    [SerializeField] private float cameraMoveSpeed = 5f;
    [LabelText("카메라 줌 속도")]
    [SerializeField] private float zoomSpeed = 10f;
    
    [LabelText("각도 컨트롤 활성화 패널")]
    [SerializeField] private GameObject controlPanel;
    [LabelText("각도 컨트롤 비활성화 패널")]
    [SerializeField] private GameObject defaultPanel;

    [LabelText("패널 비활성화 버튼")]
    [SerializeField] private GameObject panelOffButton;

    private Camera mainCamera;
    private float moveDuration = 0.5f;
    private float cameraAngleX;

    private void Start()
    {
        mainCamera = Camera.main;

        Vector3 originPos = defaultPanel.GetComponent<RectTransform>().position;

        defaultPanel.GetComponent<RectTransform>().position = new Vector3(0, originPos.y, originPos.z);

        SetDefault();
    }

    private void Update()
    {
        HandleCameraMovement();

        HandleCameraZoom();
    }

    void SetDefault()
    {
        cameraMoveSpeed = 5f;
        zoomSpeed = 10f;
    }
    
    public void MovePanelToZero(bool isOn)
    {
        RectTransform onPanelRect = controlPanel.GetComponent<RectTransform>();
        RectTransform offPanelRect = defaultPanel.GetComponent<RectTransform>();

        if (isOn)
        {
            offPanelRect.DOAnchorPosX(-100, moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    defaultPanel.SetActive(!isOn);
            
                    controlPanel.SetActive(isOn);
                    onPanelRect.DOAnchorPosX(0, moveDuration).SetEase(Ease.OutCubic);
                    
                    panelOffButton.SetActive(true);
                });
        }
        else
        {
            panelOffButton.SetActive(false);
            
            onPanelRect.DOAnchorPosX(-100, moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    controlPanel.SetActive(isOn);
                    
                    defaultPanel.SetActive(!isOn);
                    offPanelRect.DOAnchorPosX(0, moveDuration).SetEase(Ease.OutCubic);
                });
        }
    }
    
    public void ChangeCameraHeight()
    {
        Vector3 originPos = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(originPos.x, cameraHeight, originPos.z);
    }
    
    void ChangeCameraAngle()
    {
        if (mainCamera == null) return;

        // 슬라이더 값 (0 ~ 1) → 각도 60 ~ 90도로 매핑
        cameraAngleX = Mathf.Lerp(60f, 90f, cameraAngleSlider.value);
        
        Vector3 currentRotation = mainCamera.transform.rotation.eulerAngles;
        mainCamera.transform.rotation = Quaternion.Euler(cameraAngleX, currentRotation.y, currentRotation.z);
    }
    
    void HandleCameraMovement()
    {
        if (mainCamera == null) return;

        Vector3 moveDirection = Vector3.zero;

        // 키 입력 감지
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            moveDirection += Vector3.forward;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            moveDirection += Vector3.back;
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveDirection += Vector3.left;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveDirection += Vector3.right;
        }

        // 이동 실행
        if (moveDirection != Vector3.zero)
        {
            mainCamera.transform.position += moveDirection.normalized * cameraMoveSpeed * Time.deltaTime;
        }
    }

    void HandleCameraZoom()
    {
        TileCreator tileCreator = TileCreator.Instance;
        
        if (mainCamera == null) return;

        // 마우스 휠 값 가져오기 (위로 돌리면 양수, 아래로 돌리면 음수)
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0)
        {
            // 현재 카메라 위치
            Vector3 cameraPos = mainCamera.transform.position;

            // 목표 Y값 계산 (스크롤 방향에 따라 증가/감소)
            float targetY = cameraPos.y - scrollInput * zoomSpeed * Time.deltaTime;

            Vector2 mapSize = tileCreator.GetMapSize();
            
            // 최소/최대 Y값 제한 (맵 크기 ~ 맵 크기의 3배)
            float minY = mapSize.y;
            float maxY = mapSize.y * 3;
            targetY = Mathf.Clamp(targetY, minY, maxY);

            // 카메라 Y값 적용
            mainCamera.transform.position = new Vector3(cameraPos.x, targetY, cameraPos.z);
        }
    }

}
