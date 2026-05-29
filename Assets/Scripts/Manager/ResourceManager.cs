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
        LoadSpriteFromSheetInternal(sheetAddress, sheetAddress, spriteName, callback);
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
        AtlasLoadContext ctx = new AtlasLoadContext(spriteName, cacheKey, callback, this);
        handle.Completed += ctx.OnAtlasLoadedFromContext;
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
            AtlasLoadContext ctx = new AtlasLoadContext(spriteName, cacheKey, callback, this);
            typed.Completed += ctx.OnAtlasLoadedFromContext;
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
                AssetLoadContext<T> ctx = new AssetLoadContext<T>(address, callback);
                typed.Completed += ctx.OnCachedLoaded;
            }
            return;
        }

        var handle = Addressables.LoadAssetAsync<T>(address);
        _handles[address] = handle;
        AssetLoadContext<T> newCtx = new AssetLoadContext<T>(address, callback);
        handle.Completed += newCtx.OnNewLoaded;
    }

    private class AssetLoadContext<T> where T : UnityEngine.Object
    {
        private readonly string _address;
        private readonly Action<T> _callback;

        public AssetLoadContext(string address, Action<T> callback)
        {
            _address = address;
            _callback = callback;
        }

        public void OnCachedLoaded(AsyncOperationHandle<T> handle)
        {
            _callback?.Invoke(handle.Result);
        }

        public void OnNewLoaded(AsyncOperationHandle<T> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                _callback?.Invoke(handle.Result);
            else
            {
                Debug.LogWarning($"[ResourceManager] 에셋 로드 실패: {_address}");
                _callback?.Invoke(null);
            }
        }
    }

    private class AtlasLoadContext
    {
        private readonly string _spriteName;
        private readonly string _cacheKey;
        private readonly Action<Sprite> _callback;
        private readonly ResourceManager _owner;

        public AtlasLoadContext(string spriteName, string cacheKey, Action<Sprite> callback, ResourceManager owner)
        {
            _spriteName = spriteName;
            _cacheKey = cacheKey;
            _callback = callback;
            _owner = owner;
        }

        public void OnAtlasLoadedFromContext(AsyncOperationHandle<IList<Sprite>> handle)
        {
            _owner.OnAtlasLoaded(handle, _spriteName, _cacheKey, _callback);
        }
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