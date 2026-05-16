using GameData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _sfxHandles
        = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

    private readonly Dictionary<string, string> _bgmAddressMap
        = new Dictionary<string, string>();

    private AsyncOperationHandle<AudioClip>? _bgmHandle;
    private string _currentBgmKey;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnSFXPlayRequested += PlaySFX;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnSFXPlayRequested -= PlaySFX;
    }

    private void OnDestroy()
    {
        foreach (var handle in _sfxHandles.Values)
            Addressables.Release(handle);
        _sfxHandles.Clear();

        ReleaseBgmHandle();
    }

    private void HandleDataInitialized()
    {
        RegisterBGMFromData();
        ApplyVolumeSettings();
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        // GamePlay 종료 시 BGM 정지
        if (prev == GameState.GamePlay)
            StopBGM();

        // 새 상태에 바인딩된 BGM 재생
        string bindState = next switch
        {
            GameState.Lobby => "Lobby",
            GameState.GamePlay => "GamePlay",
            _ => null
        };

        if (bindState == null) return;

        var candidates = new List<string>();
        foreach (var sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM && sound.BindState == bindState)
                candidates.Add(sound.Id);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[SoundManager] '{bindState}'에 바인딩된 BGM 없음");
            return;
        }

        PlayBGM(candidates[Random.Range(0, candidates.Count)]);
    }

    private void RegisterBGMFromData()
    {
        _bgmAddressMap.Clear();

        foreach (var sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM)
                _bgmAddressMap[sound.Id] = sound.SoundPath;
        }

        Debug.Log($"[SoundManager] BGM {_bgmAddressMap.Count}개 등록 완료");
    }

    private void ApplyVolumeSettings()
    {
        var bgmVol = GameDataManager.Instance.Get<GameSettingData>("SOUND_BACKGROUND_VOLUME");
        if (bgmVol != null) SetBGMVolume(bgmVol.Value / 100f);

        var sfxVol = GameDataManager.Instance.Get<GameSettingData>("SOUND_EFFECT_VOLUME");
        if (sfxVol != null) SetSFXVolume(sfxVol.Value / 100f);
    }

    // =========================================================================
    // BGM
    // =========================================================================

    public void PlayBGM(string key)
    {
        if (_currentBgmKey == key) return;

        if (!_bgmAddressMap.TryGetValue(key, out string address))
        {
            Debug.LogWarning($"[SoundManager] 등록되지 않은 BGM 키: {key}");
            return;
        }

        ReleaseBgmHandle();
        _currentBgmKey = key;

        var handle = Addressables.LoadAssetAsync<AudioClip>(address);
        _bgmHandle = handle;
        handle.Completed += OnBgmLoaded;
    }

    public void ResumeBGM()
    {
        if (_currentBgmKey != null && !_bgmSource.isPlaying)
            _bgmSource.Play();
    }

    public void PauseBGM() => _bgmSource.Pause();

    public void StopBGM()
    {
        _bgmSource.Stop();
        ReleaseBgmHandle();
        _currentBgmKey = null;
    }

    public void SetBGMVolume(float volume) => _bgmSource.volume = Mathf.Clamp01(volume);

    // =========================================================================
    // SFX
    // =========================================================================

    private void PlaySFX(string address)
    {
        if (_sfxHandles.TryGetValue(address, out var cached))
        {
            if (cached.IsDone)
                PlaySFXClip(cached.Result);
            else
                cached.Completed += handle => PlaySFXClip(handle.Result);
            return;
        }

        var newHandle = Addressables.LoadAssetAsync<AudioClip>(address);
        _sfxHandles[address] = newHandle;
        newHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                PlaySFXClip(handle.Result);
            else
                Debug.LogWarning($"[SoundManager] SFX 로드 실패: {address}");
        };
    }

    public void ReleaseSFX(string address)
    {
        if (_sfxHandles.TryGetValue(address, out var handle))
        {
            Addressables.Release(handle);
            _sfxHandles.Remove(address);
        }
    }

    public void ReleaseAllSFX()
    {
        foreach (var handle in _sfxHandles.Values)
            Addressables.Release(handle);
        _sfxHandles.Clear();
    }

    public void SetSFXVolume(float volume) => _sfxSource.volume = Mathf.Clamp01(volume);

    // =========================================================================
    // 내부 구현
    // =========================================================================
    private void PlaySFXClip(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    private void OnBgmLoaded(AsyncOperationHandle<AudioClip> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[SoundManager] BGM 로드 실패: {_currentBgmKey}");
            return;
        }

        if (_bgmSource == null) return;

        _bgmSource.clip = handle.Result;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    private void ReleaseBgmHandle()
    {
        if (_bgmHandle.HasValue)
        {
            Addressables.Release(_bgmHandle.Value);
            _bgmHandle = null;
        }
    }
}