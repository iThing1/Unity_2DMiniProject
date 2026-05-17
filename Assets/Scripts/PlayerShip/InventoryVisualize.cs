using UnityEngine;

public class InventoryVisualize : MonoBehaviour
{
    [Header("화물 박스 오브젝트")]
    [SerializeField] private GameObject[] _boxObjects;

    private const int CARGO_PER_BOX = 10;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnCargoChanged += HandleCargoChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnCargoChanged -= HandleCargoChanged;
    }

    private void Start()
    {
        // 초기 상태: 전부 비활성
        SetActiveBoxCount(0);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleCargoChanged(int count, int capacity)
    {
        int activeBoxes = Mathf.CeilToInt((float)count / CARGO_PER_BOX);
        SetActiveBoxCount(activeBoxes);
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void SetActiveBoxCount(int activeCount)
    {
        if (_boxObjects == null) return;

        for (int i = 0; i < _boxObjects.Length; i++)
        {
            if (_boxObjects[i] != null)
                _boxObjects[i].SetActive(i < activeCount);
        }
    }
}
