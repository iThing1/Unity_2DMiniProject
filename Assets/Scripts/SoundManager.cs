using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    // SFX: 한 번 로드한 클립은 재사용 (성능 최적화)
    private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _sfxHandles
        = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

    // BGM: key => Addressable 주소 매핑 (외부에서 RegisterBGM으로 등록)
    private readonly Dictionary<string, string> _bgmAddressMap
        = new Dictionary<string, string>();

    // 현재 재생 중인 BGM 상태
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

    private void OnDestroy()
    {
        foreach (var handle in _sfxHandles.Values)
            Addressables.Release(handle);
        _sfxHandles.Clear();

        ReleaseBgmHandle();
    }

    public void RegisterBGM(string key, string address)
    {
        _bgmAddressMap[key] = address;
    }

    public void RegisterAllBGM(Dictionary<string, string> bgmMap)
    {
        foreach (var kvp in bgmMap)
            _bgmAddressMap[kvp.Key] = kvp.Value;
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

    // Addressable 주소로 BGM을 직접 재생 (키 등록 없이 사용할 때)
    public void PlayBGMByAddress(string address)
    {
        const string directKey = "__direct__";
        _bgmAddressMap[directKey] = address;
        _currentBgmKey = null; // 강제 재생
        PlayBGM(directKey);
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