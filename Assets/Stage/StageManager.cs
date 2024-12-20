using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    List<BattleManager> battleList;
    [HideInInspector]
    public CameraManager CamM;
    [HideInInspector]
    public ControllerManager ConM;
    [HideInInspector]
    public AudioManager AudM;
    [HideInInspector]
    public GameUIManager UIM;

    int battleIndex = 0;


    public StageData stageData;

    [Header("jsonで呼び出すステージ名")]
    public string stageName;

    public void Awake()
    {
        ConM = GetComponent<ControllerManager>();
        AudM = GetComponent<AudioManager>();
        // UIM = GetComponent<GameUIManager>();
        CamM = FindFirstObjectByType<CameraManager>();

        ConM.Init();
        AudM.Init();
        CamM.Init();
        // UIM.Init();

        DG.Tweening.DOTween.SetTweensCapacity(tweenersCapacity:20000, sequencesCapacity:200);
    }
    // Start is called before the first frame update
    void Start()
    {
        if(stageData == null) 
        {
            // if(stageName == "") return;
            // else stageData = ReadWrite.Read<StageData>(stageName);

            stageData = SendStage.stageData;
            if(stageData == null) return;
        }

        battleList = new List<BattleManager>();
        this.name = stageData.name;

        int i = 0;
        foreach(BattleData battleData in stageData.battleDataList_easy)
        {
            BattleManager BatM = new GameObject("BattleManager").AddComponent<BattleManager>();
            BatM.name = "Battle" + battleData.name;
            BatM.transform.SetParent(this.transform);
            battleList.Add(BatM);
            battleIndex = i;

            if(i != 0) BatM.transform.position = battleList[i - 1].transform.position + new Vector3(battleData.offset.x, battleData.offset.y, 0);

            BatM.Init(this);
            if(battleData) BatM.SetData(battleData);
            i++;
        }
        battleIndex = 0;
        
        battleList[battleIndex].PlayBattle();
        AudM.SetBGM();
    }

    public void PlayNextBattle()
    {
        battleIndex++;
        if(battleIndex < battleList.Count) battleList[battleIndex].PlayBattle();
        else ClearStage();
    }

    public void ClearStage()
    {
        Debug.Log("Stage" + stageData.name + "Clear");
        //SceneManager.LoadScene("TitleScene");
        return;
    }

    public BattleManager GetCurBattle()
    {
        return battleList[battleIndex];
    }
}
