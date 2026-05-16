using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // 타입에 관계없이 핸들을 공통 캐시로 관리
    private readonly Dictionary<string, AsyncOperationHandle> _handles
        = new Dictionary<string, AsyncOperationHandle>();

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
        ReleaseAll();
    }

    // =========================================================================
    // Sprite 로드 (단일 / 스프라이트 시트)
    // =========================================================================

    public void LoadSprite(string address, Action<Sprite> callback)
        => LoadAsset<Sprite>(address, callback);

    public void LoadSpriteFromSheet(string sheetAddress, string spriteName, Action<Sprite> callback)
    {
        // 스프라이트 시트 주소와 스프라이트 이름을 합쳐 캐시 키로 사용
        string cacheKey = $"{sheetAddress}/{spriteName}";
        LoadSpriteFromSheetInternal(cacheKey, sheetAddress, spriteName, callback);
    }

    private void LoadSpriteFromSheetInternal(string cacheKey, string sheetAddress, string spriteName, Action<Sprite> callback)
    {
        if (_handles.TryGetValue(cacheKey, out var cached))
        {
            ResolveAtlasHandle(cached, spriteName, cacheKey, callback);
            return;
        }

        var handle = Addressables.LoadAssetAsync<IList<Sprite>>(sheetAddress);
        _handles[cacheKey] = handle;
        handle.Completed += h => OnAtlasLoaded(h, spriteName, cacheKey, callback);
    }

    private void ResolveAtlasHandle(AsyncOperationHandle cached, string spriteName, string cacheKey, Action<Sprite> callback)
    {
        if (cached.IsDone)
        {
            callback?.Invoke(FindSpriteInAtlas(cached.Result as IList<Sprite>, spriteName, cacheKey));
        }
        else
        {
            var typed = cached.Convert<IList<Sprite>>();
            typed.Completed += h => OnAtlasLoaded(h, spriteName, cacheKey, callback);
        }
    }

    private void OnAtlasLoaded(AsyncOperationHandle<IList<Sprite>> handle, string spriteName, string cacheKey, Action<Sprite> callback)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[ResourceManager] 스프라이트 시트 로드 실패: {cacheKey}");
            callback?.Invoke(null);
            return;
        }

        callback?.Invoke(FindSpriteInAtlas(handle.Result, spriteName, cacheKey));
    }

    private Sprite FindSpriteInAtlas(IList<Sprite> list, string spriteName, string cacheKey)
    {
        if (list == null) return null;

        foreach (var sprite in list)
        {
            if (sprite.name == spriteName) return sprite;
        }

        Debug.LogWarning($"[ResourceManager] 스프라이트 시트에서 스프라이트를 찾지 못했습니다: {cacheKey}");
        return null;
    }

    // =========================================================================
    // 범용 에셋 로드
    // =========================================================================
    public void LoadAsset<T>(string address, Action<T> callback) where T : UnityEngine.Object
    {
        if (_handles.TryGetValue(address, out var cached))
        {
            if (cached.IsDone)
                callback?.Invoke(cached.Result as T);
            else
            {
                var typed = cached.Convert<T>();
                typed.Completed += h => callback?.Invoke(h.Result);
            }
            return;
        }

        var handle = Addressables.LoadAssetAsync<T>(address);
        _handles[address] = handle;
        handle.Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded)
                callback?.Invoke(h.Result);
            else
            {
                Debug.LogWarning($"[ResourceManager] 에셋 로드 실패: {address}");
                callback?.Invoke(null);
            }
        };
    }

    // =========================================================================
    // 해제
    // =========================================================================
    public void Release(string address)
    {
        if (_handles.TryGetValue(address, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(address);
            Debug.Log($"[ResourceManager] 리소스 해제: {address}");
        }
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values)
            Addressables.Release(handle);
        _handles.Clear();
    }
}