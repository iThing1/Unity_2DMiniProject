using GameData;
using System.Collections.Generic;
using UnityEngine;

public class InventoryVisualize : MonoBehaviour
{
    [Header("화물 박스")]
    [SerializeField] private GameObject _boxPrefab;
    [SerializeField] private int _maxBoxCount = 10;

    [Header("물리 설정")]
    [SerializeField] private float _boxSpacing = 1.2f;
    [SerializeField] private float _springFrequency = 2f;
    [SerializeField] private float _springDampingRatio = 0.3f;
    [SerializeField] private float _boxMass = 0.5f;
    [SerializeField] private float _boxLinearDrag = 1.5f;
    [SerializeField] private float _boxAngularDrag = 2f;

    private const int CARGO_PER_BOX = 10;

    private readonly List<GameObject> _boxObjects = new List<GameObject>();
    private readonly List<Rigidbody2D> _boxRigidbodies = new List<Rigidbody2D>();
    private readonly List<SpringJoint2D> _boxJoints = new List<SpringJoint2D>();

    private Rigidbody2D _shipRigidbody;

    private ShipInventory _shipInventory;
    private int _activeBoxCount = 0;
    private int _lastCargoCount = -1;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _shipRigidbody = GetComponent<Rigidbody2D>();
        if (_shipRigidbody == null)
            Debug.LogError("[InventoryVisualize] 우주선에 Rigidbody2D가 없습니다.");

        _shipInventory = GetComponent<ShipInventory>();
        if (_shipInventory == null)
            Debug.LogError("[InventoryVisualize] ShipInventory를 찾지 못했습니다.");

        PreloadPool();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        SetActiveBoxCount(0);
    }

    private void Update()
    {
        if (_shipInventory == null) return;

        int count = _shipInventory.Count;
        if (count == _lastCargoCount) return;

        _lastCargoCount = count;

        int targetBoxes = Mathf.Clamp(
            Mathf.CeilToInt((float)count / CARGO_PER_BOX),
            0, _maxBoxCount
        );
        SetActiveBoxCount(targetBoxes);
    }

    // =========================================================================
    // 풀 생성
    // =========================================================================
    private void PreloadPool()
    {
        if (_boxPrefab == null)
        {
            Debug.LogError("[InventoryVisualize] Box Prefab이 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < _maxBoxCount; i++)
        {
            GameObject box = Instantiate(_boxPrefab, transform);
            box.name = $"CargoBox_{i}";
            box.SetActive(false);

            Rigidbody2D rigidbody = SetupRigidbody(box); 
            SpringJoint2D joint = SetupJoint(box);

            _boxObjects.Add(box);
            _boxRigidbodies.Add(rigidbody);
            _boxJoints.Add(joint);

            _boxObjects.Add(box);
            _boxRigidbodies.Add(rigidbody);
            _boxJoints.Add(joint);
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (prev == GameState.GamePlay)
        {
            _lastCargoCount = -1;
            SetActiveBoxCount(0);
        }
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void SetActiveBoxCount(int targetCount)
    {
        for (int i = _activeBoxCount; i < targetCount; i++)
        {
            _boxObjects[i].SetActive(true);
            ConnectChain(i);
        }

        for (int i = _activeBoxCount - 1; i >= targetCount; i--)
        {
            _boxObjects[i].SetActive(false);
        }

        _activeBoxCount = targetCount;
    }

    private void ConnectChain(int index)
    {
        var joint = _boxJoints[index];
        joint.connectedBody = index == 0 ? _shipRigidbody : _boxRigidbodies[index - 1];

        _boxObjects[index].transform.position = GetChainSpawnPosition(index);
        _boxRigidbodies[index].linearVelocity = Vector2.zero;
        _boxRigidbodies[index].angularVelocity = 0f;
    }

    private Vector3 GetChainSpawnPosition(int index)
    {
        Vector2 shipBack = -transform.up;
        return transform.position + (Vector3)(shipBack * _boxSpacing * (index + 1));
    }

    private Rigidbody2D SetupRigidbody(GameObject box)
    {
        Rigidbody2D rigidbody = box.GetComponent<Rigidbody2D>();
        if (rigidbody == null) rigidbody = box.AddComponent<Rigidbody2D>();

        rigidbody.gravityScale = 0f;
        rigidbody.mass = _boxMass;
        rigidbody.linearDamping = _boxLinearDrag;
        rigidbody.angularDamping = _boxAngularDrag;

        return rigidbody;
    }

    private SpringJoint2D SetupJoint(GameObject box)
    {
        SpringJoint2D joint = box.GetComponent<SpringJoint2D>();
        if (joint == null) joint = box.AddComponent<SpringJoint2D>();

        joint.autoConfigureDistance = false;
        joint.distance = _boxSpacing;
        joint.frequency = _springFrequency;
        joint.dampingRatio = _springDampingRatio;
        joint.enableCollision = false;

        return joint;
    }

}