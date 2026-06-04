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

    // 단일 Sprite 에셋을 직접 로드할 때
    public void LoadSprite(string address, Action<Sprite> callback) => LoadAsset<Sprite>(address, callback);

    // 스프라이트 시트에서 특정 스프라이트 하나를 로드할 때
    public void LoadSpriteFromSheet(string sheetAddress, string spriteName, Action<Sprite> callback)
    {
        ResolveSpriteFromSheet(sheetAddress, spriteName, callback);
    }

    // 여러 스프라이트 시트를 순서대로 탐색하여 스프라이트를 찾을 때
    public void LoadSpriteFromSheets(string[] sheetAddresses, string spriteName, Action<Sprite> callback)
    {
        TryLoadFromSheet(sheetAddresses, spriteName, 0, callback);
    }

    private void TryLoadFromSheet(string[] sheetAddresses, string spriteName, int index, Action<Sprite> callback)
    {
        if (index >= sheetAddresses.Length)
        {
            Debug.LogWarning($"[ResourceManager] 모든 시트에서 스프라이트를 찾지 못했습니다: {spriteName}");
            callback?.Invoke(null);
            return;
        }

        string sheetAddress = sheetAddresses[index];
        ResolveSpriteFromSheet(sheetAddress, spriteName, sprite =>
        {
            if (sprite != null)
                callback?.Invoke(sprite);
            else
                TryLoadFromSheet(sheetAddresses, spriteName, index + 1, callback);
        });
    }

    private void ResolveSpriteFromSheet(string sheetAddress, string spriteName, Action<Sprite> callback)
    {
        if (_handles.TryGetValue(sheetAddress, out var cached))
        {
            ResolveSheetHandle(cached, spriteName, sheetAddress, callback);
            return;
        }

        var handle = Addressables.LoadAssetAsync<IList<Sprite>>(sheetAddress);
        _handles[sheetAddress] = handle;

        handle.Completed += loadHandle =>
        {
            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[ResourceManager] 스프라이트 시트 로드 실패: {sheetAddress}");
                callback?.Invoke(null);
                return;
            }
            callback?.Invoke(FindSpriteInSheet(loadHandle.Result, spriteName, sheetAddress));
        };
    }

    private void ResolveSheetHandle(AsyncOperationHandle cached, string spriteName, string cacheKey, Action<Sprite> callback)
    {
        if (cached.IsDone)
        {
            callback?.Invoke(FindSpriteInSheet(cached.Result as IList<Sprite>, spriteName, cacheKey));
            return;
        }

        cached.Convert<IList<Sprite>>().Completed += loadHandle =>
        {
            callback?.Invoke(FindSpriteInSheet(loadHandle.Result, spriteName, cacheKey));
        };
    }

    private Sprite FindSpriteInSheet(IList<Sprite> list, string spriteName, string cacheKey)
    {
        if (list == null) return null;

        foreach (var sprite in list)
        {
            if (sprite.name == spriteName) return sprite;
        }

        return null;
    }

    // =========================================================================
    // 범용 에셋 로드
    // =========================================================================

    // Sprite 이외의 에셋(GameObject, AudioClip 등)을 로드할 때
    public void LoadAsset<T>(string address, Action<T> callback) where T : UnityEngine.Object
    {
        if (_handles.TryGetValue(address, out var cached))
        {
            if (cached.IsDone)
            {
                callback?.Invoke(cached.Result as T);
                return;
            }

            cached.Convert<T>().Completed += handle => callback?.Invoke(handle.Result);
            return;
        }

        var newHandle = Addressables.LoadAssetAsync<T>(address);
        _handles[address] = newHandle;

        newHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                callback?.Invoke(handle.Result);
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