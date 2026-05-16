using UnityEngine;
using GameData;

public class DataLoadTest : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
    }

    private void HandleDataInitialized()
    {
        Debug.Log("<color=yellow>====== DataLoadTest 시작 ======</color>");

        TestPlanet();
        TestStage();
        TestUpgrade();
        TestSound();
        TestUI();
        TestGameSetting();
        TestGameConstant();

        Debug.Log("<color=yellow>====== DataLoadTest 완료 ======</color>");
    }

    private void TestPlanet()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<PlanetData>())
        {
            Debug.Log($"[Planet] {d.Id} | {d.Name} | Size:{d.Size} | Grade:{d.Grade} | Pop:{d.BasePop} | Sprite:{d.PlanetSprite}");
            count++;
        }
        PrintSummary("Planet", count);
    }

    private void TestStage()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<StageData>())
        {
            Debug.Log($"[Stage] {d.Id} | {d.Name} | MaxPlanet:{d.MaxPlanet} | ReqGold:{d.ReqGold} | Planets:[{d.PlanetList}]");
            count++;
        }
        PrintSummary("Stage", count);
    }

    private void TestUpgrade()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<UpgradeData>())
        {
            Debug.Log($"[Upgrade] {d.Id} | {d.Name} | Target:{d.Target} | MaxLv:{d.MaxLevel} | Calc:{d.CalcType} | Base:{d.BaseStats} | Val:{d.UpgradeValue}");
            count++;
        }
        PrintSummary("Upgrade", count);
    }

    private void TestSound()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<SoundData>())
        {
            Debug.Log($"[Sound] {d.Id} | {d.Name} | Type:{d.Type} | BindState:{d.BindState} | Path:{d.SoundPath}");
            count++;
        }
        PrintSummary("Sound", count);
    }

    private void TestUI()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<UIData>())
        {
            Debug.Log($"[UI] {d.Id} | {d.Name} | Type:{d.Type} | BindState:{d.BindState} | Path:{d.PrefabPath}");
            count++;
        }
        PrintSummary("UI", count);
    }

    private void TestGameSetting()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<GameSettingData>())
        {
            Debug.Log($"[GameSetting] {d.Id} | Value:{d.Value} | {d.Desc}");
            count++;
        }
        PrintSummary("GameSetting", count);
    }

    private void TestGameConstant()
    {
        int count = 0;
        foreach (var d in GameDataManager.Instance.GetAll<GameConstantData>())
        {
            Debug.Log($"[GameConstant] {d.Id} | Value:{d.Value} | {d.Desc}");
            count++;
        }
        PrintSummary("GameConstant", count);
    }

    private void PrintSummary(string label, int count)
    {
        var color = count > 0 ? "lime" : "red";
        var status = count > 0 ? $"{count}개 로드 성공" : "로드된 항목 없음. 주소/JSON 확인 필요";
        Debug.Log($"<color={color}>[{label}] {status}</color>");
    }
}