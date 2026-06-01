using GameData;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task SetUp()
    {
        RegisterBGMFromData();
        PreloadSFXFromData();
        ApplyVolumeSettings();

        await Task.CompletedTask;
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
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        Debug.Log($"[SoundManager] 게임 상태 변경: {prev} -> {next}");
        string bindState = GameUtility.StateToString(next);
        if (bindState == null) return;

        List<string> candidate = new List<string>();
        foreach(SoundData sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM && sound.BindState == bindState)
                candidate.Add(sound.SoundPath);
        }

        if (candidate.Count > 0)
        {
            string selected = candidate[Random.Range(0, candidate.Count)];
            PlayBGM(selected);
        }
        else Debug.LogWarning($"[SoundManager] BGM 후보 없음: {bindState}");
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
                PlaySFXClip(cached.Result);
            else
            {
                SfxLoadContext ctx = new SfxLoadContext(address, this);
                cached.Completed += ctx.OnSfxCachedLoaded;
            }
            return;
        }

        var newHandle = Addressables.LoadAssetAsync<AudioClip>(address);
        _sfxHandles[address] = newHandle;
        SfxLoadContext newCtx = new SfxLoadContext(address, this);
        newHandle.Completed += newCtx.OnSfxNewLoaded;
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

    private class SfxLoadContext
    {
        private readonly string _address;
        private readonly SoundManager _owner;

        public SfxLoadContext(string address, SoundManager owner)
        {
            _address = address;
            _owner = owner;
        }

        public void OnSfxCachedLoaded(AsyncOperationHandle<AudioClip> handle)
        {
            _owner.PlaySFXClip(handle.Result);
        }

        public void OnSfxNewLoaded(AsyncOperationHandle<AudioClip> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                _owner.PlaySFXClip(handle.Result);
            else
                Debug.LogWarning($"[SoundManager] SFX 로드 실패: {_address}");
        }
    }
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