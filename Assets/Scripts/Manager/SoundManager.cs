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

    public bool IsBGMOn { get; private set; } = true;

    public float BGMVolume => _bgmSource != null ? _bgmSource.volume : 1f;
    public float SFXVolume => _sfxSource != null ? _sfxSource.volume : 1f;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void OnDestroy()
    {
        foreach (var handle in _sfxHandles.Values)
            Addressables.Release(handle);
        _sfxHandles.Clear();

        ReleaseBgmHandle();
    }

    // =========================================================================
    // [프로젝트 종속] 초기화 
    // =========================================================================
    public void SetUp()
    {
        RegisterBGMFromData();
        PreloadSFXFromData();
        ApplyVolumeSettings();
    }

    public void RegisterBGMFromData()
    {
        _bgmAddressMap.Clear();

        foreach (var sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM)
                _bgmAddressMap[sound.SoundPath] = sound.SoundPath;
        }
    }

    public void PreloadSFXFromData()
    {
        foreach (var sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type != SoundType.SFX) continue;
            if (_sfxHandles.ContainsKey(sound.SoundPath)) continue;

            var handle = Addressables.LoadAssetAsync<AudioClip>(sound.SoundPath);
            _sfxHandles[sound.SoundPath] = handle;
        }
    }

    private void ApplyVolumeSettings()
    {
        var bgmVol = GameDataManager.Instance.Get<GameSettingData>("SOUND_BACKGROUND_VOLUME");
        if (bgmVol != null) SetBGMVolume(bgmVol.DefaultValue / 100f);

        var sfxVol = GameDataManager.Instance.Get<GameSettingData>("SOUND_EFFECT_VOLUME");
        if (sfxVol != null) SetSFXVolume(sfxVol.DefaultValue / 100f);
    }

    // =========================================================================
    //  [프로젝트 종속] 이벤트 핸들러 - GameState 기반 BGM 전환
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (!IsBGMOn) return;
        PlayBGMForState(next);
    }
    
    private void PlayBGMForState(GameState state)
    {
        string bindState = GameUtility.StateToString(state);
        if (bindState == null) return;

        List<string> candidates = new List<string>();
        foreach (SoundData sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM && sound.BindState == bindState)
                candidates.Add(sound.SoundPath);
        }

        if (candidates.Count > 0)
        {
            string selected = candidates[Random.Range(0, candidates.Count)];
            PlayBGM(selected);
        }
        else Debug.LogWarning($"[SoundManager] BGM 후보 없음: {bindState}");
    }
    // =========================================================================
    // BGM On/Off
    // =========================================================================
    public void ToggleBGM(GameState currentState)
    {
        IsBGMOn = !IsBGMOn;

        if (IsBGMOn)
            PlayBGMForState(currentState);
        else
            StopBGM();
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
    public void PlaySFX(string address)
    {
        if (_sfxHandles.TryGetValue(address, out var cached))
        {
            if (cached.IsDone)
            {
                PlaySFXClip(cached.Result);
                return;
            }

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