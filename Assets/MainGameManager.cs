using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using DG.Tweening;
using System.Linq;
using UnityEngine.UIElements;
using TMPro;

public class MainGameManager : MonoBehaviour
{
    BattleManager BatM;
    FrameManager FraM;
    AttackManager AttM;
    AudioManager AudM;

    private List<int> bagList; //袋リスト

    public RootBlock playerBlock { get; private set; } //プレイヤーブロック
    RootBlock holdBlock; //ホールドブロック

    List<RootBlock> nextRBlockList; //ネクストブロックリスト
    int generationNum; //ブロックの世代数


    public MainStateType mainState { get; private set; }

    public void Init(BattleManager BatM)
    {
        this.BatM = BatM;
        FraM = BatM.FraM;
        AttM = BatM.AttM;
        AudM = BatM.AudM;

        if(!BatM || !FraM || !AttM || !AudM)
        {
            Debug.Log("any manager is not found");
            return;
        }
        
        nextRBlockList = new List<RootBlock>();
        bagList = new List<int>();
        generationNum = 0;
        mainState = MainStateType.idle;
    }

    /// <summary>
    /// 次のターンを開始
    /// </summary>
    public void TurnStart()
    {
        mainState = MainStateType.running;

        playerBlock = GetNextBlock();
        playerBlock.transform.localPosition = BatM.battleData.blockSpawnPos;


        foreach(BaseBlock baseBlock in playerBlock.BlockList)
        baseBlock.frameIndex = BatM.battleData.blockSpawnPos + baseBlock.shapeIndex;

        if(!FraM.SetRBlock(playerBlock)) 
        {
            Debug.Log("ゲームオーバー");
            return;
        }
    }

    /// <summary>
    /// プレイヤーブロックの操作を終了し、ターン終了する
    /// </summary>
    public void TurnEnd()
    {
        if(playerBlock) playerBlock.DestroyGhostBlock();
        playerBlock = null;
        bool isLined = CheckLine(); //playerBlockは列の判定対象外のため、nullにしてからでないと、列が揃っているか判定できない

        if(!isLined) AttM.DoAttack(); //攻撃

        TurnStart();
    }

    /// <summary>
    /// プレイヤーブロックを生成できるだけ生成し、ネクストブロックを取得
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public RootBlock GetNextBlock(int index = 0) //ネクストブロックを取得
    {
        if(BatM.battleData.nextBlockPosList.Count < index) return null; //index個目が配置できるnextBlockを超えた場合、nullを返す
        RootBlock nextRBlock;
        if(nextRBlockList.Count <= index) //ブロックの数が足りない場合、新たに生成
        {
            nextRBlockList.Add(GenerateRBlock(BatM.blockShapeData.blockDataList[GetRandomInt()]));
            nextRBlockList.Last().transform.localPosition = BatM.battleData.nextBlockPosList.Last() + new Vector3(0, 0, -3);
        }
        if(nextRBlockList[index] != null) nextRBlock = nextRBlockList[index]; //ほしい奴
        else nextRBlock = GetNextBlock(index + 1); //ほしい奴がない場合、次の奴を取得

        RootBlock nextNextRBlock = GetNextBlock(index + 1); //次の奴
        if(nextNextRBlock != null)
        {
            if(index < BatM.battleData.nextBlockPosList.Count) nextNextRBlock.gameObject.SetActive(true);
            nextNextRBlock.transform.DOKill();
            nextNextRBlock.transform.DOLocalJump(BatM.battleData.nextBlockPosList[index], 0.5f, 1, 0.3f);
            nextRBlockList[index] = nextNextRBlock;
        }
        nextRBlockList.Remove(nextRBlock);
        nextRBlock.transform.DOKill();
        return nextRBlock;
    }

    public void SetNextBlock(RootBlock rootBlock, int index)
    {
        if(index > BatM.battleData.nextBlockPosList.Count)
        {
            for(int i = BatM.battleData.nextBlockPosList.Count; i < index; i++)
            {
                nextRBlockList.Add(GenerateRBlock(BatM.blockShapeData.blockDataList[GetRandomInt()]));
                nextRBlockList[i].transform.localPosition = BatM.battleData.nextBlockPosList.Last() + new Vector3(0, 0, -3);
                nextRBlockList[i].gameObject.SetActive(false);
            }
        }

        nextRBlockList.Insert(index, rootBlock);
        for(int i = index; i < nextRBlockList.Count; i++)
        {
            if(i < BatM.battleData.nextBlockPosList.Count)
            {
                nextRBlockList[i].transform.DOKill();
                nextRBlockList[i].transform.DOLocalJump(BatM.battleData.nextBlockPosList[i], 0.5f, 1, 0.3f);
            }
            else
            {
                nextRBlockList[i].transform.localPosition = BatM.battleData.nextBlockPosList.Last() + new Vector3(0, 0, -3);
                nextRBlockList[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ルートブロックを生成
    /// いつかblockDataがnullの場合はランダムに生成にしたい
    /// </summary>
    /// <param name="blockData"></param>
    /// <returns></returns>
    public RootBlock GenerateRBlock(RootBlockData blockData = null)
    {
        RootBlock rootBlock = BlockPool.Instance.rootPool.Get();
        rootBlock.name = "RootBlock";
        rootBlock.blockData = blockData;
        rootBlock.transform.parent = this.transform;
        rootBlock.transform.rotation = BatM.transform.rotation;
        rootBlock.generationNum = generationNum++;
        rootBlock.Init(this, FraM);

        if(blockData == null) return rootBlock;

        rootBlock.pivot.transform.localPosition = blockData.pivotPos;
        foreach(Vector3Int shapeIndex in blockData.blockPosList)
        {
            BaseBlock block = GenerateBlock(blockData.blockType, blockData.colorType, BatM.GetTexture(blockData.colorType));
            rootBlock.AddBlock(block, shapeIndex);
        }
        return rootBlock;
    }

    /// <summary>
    /// ベースブロックを生成
    /// </summary>
    /// <param name="blockType"></param>
    /// <param name="colorType"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public BaseBlock GenerateBlock(BlockType blockType, ColorType colorType, Texture texture)
    {
        BaseBlock block = BlockPool.Instance.blockPool.Get();
        block.name = "BaseBlock";
        block.blockType = blockType;
        block.colorType = colorType;
        block.mainRenderer.material.mainTexture = texture;
        block.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 180) + BatM.transform.rotation.eulerAngles);
        return block;
    }

    int GetRandomInt() //袋からランダムに数を返す
    {
        if(bagList.Count == 0) {
            bagList.Clear();
            for(int i = 0; i < BatM.blockShapeData.blockDataList.Count; i++) bagList.Add(i);
        }

        int random = Random.Range(0, bagList.Count);
        int num = bagList[random];
        bagList.RemoveAt(random);
        return num;
    }

    public void SetHoldBlock()
    {
        if(holdBlock == null)
        {
            FraM.DeleteRBlock(playerBlock);
            holdBlock = playerBlock;
            holdBlock.transform.position = BatM.transform.position + BatM.battleData.holdBlockPos;
            holdBlock.pivot.transform.rotation = Quaternion.identity;
            TurnEnd();
        }
        else
        {
            FraM.DeleteRBlock(playerBlock);
            playerBlock.DestroyGhostBlock();
            RootBlock tempBlock = holdBlock;
            holdBlock = playerBlock;
            playerBlock = tempBlock;

            foreach(BaseBlock baseBlock in playerBlock.BlockList)
            if(baseBlock != null) baseBlock.frameIndex = FraM.LFrameBorder.lowerLeft + Vector3Int.RoundToInt(holdBlock.transform.position) + baseBlock.shapeIndex;

            playerBlock.transform.position = holdBlock.transform.position;
            holdBlock.transform.position = BatM.transform.position + BatM.battleData.holdBlockPos;
            holdBlock.pivot.transform.rotation = Quaternion.identity;

            FraM.SetRBlock(playerBlock);
        }
    }

    public bool CheckLine() //ラインが揃っているかチェック
    {
        bool isLine = false;
        if(mainState != MainStateType.running) return isLine;
        mainState = MainStateType.checkLine;
        List<int> lineList = new List<int>();
        bool canDelete;

        for(int y = FraM.LFrameBorder.lowerLeft.y; y <= FraM.LFrameBorder.upperRight.y; y++)
        {
            canDelete = false;

            for(int x = FraM.LFrameBorder.lowerLeft.x; x <= FraM.LFrameBorder.upperRight.x; x++)
            {
                BaseBlock baseBlock = FraM.GetBlock(new Vector3Int(x, y, 0));
                if(baseBlock == null || baseBlock.RootBlock == playerBlock) 
                {
                    if(baseBlock == null || playerBlock != null) //応急措置　<- これをしないと、turnEnd時にCheckLineがtrueを返してくれない
                    {
                        canDelete = false;
                        break;
                    }
                }
                if(baseBlock.blockType == BlockType.Mino) canDelete = true;
            }
            if(canDelete) lineList.Add(y);
        }
        if(lineList.Count > 0) 
        {
            isLine = true;
            DeleteLine(lineList);
        }
        if(mainState == MainStateType.checkLine) mainState = MainStateType.running;
        return isLine;
    }

    void DeleteLine(List<int> lineList) //ラインを消す
    {
        mainState = MainStateType.deleting;
        List<BaseBlock> deleteBlockList = new List<BaseBlock>(); //削除するブロックリスト
        ColorType colorType; //変更する色

        //最大の世代数を持つルートブロックを取得
        BaseBlock maxGenBlock = null;
        int maxGenerationNum = -1;
        List<BaseBlock> blockList = new List<BaseBlock>(); //ブロックリスト
        foreach(int y in lineList) blockList.AddRange(FraM.GetBlockLine(y));

        foreach(BaseBlock block in blockList)
        {
            if(block.blockType != BlockType.Mino) continue;
            if(block.RootBlock.generationNum > maxGenerationNum) 
            {
                maxGenBlock = block;
                maxGenerationNum = block.RootBlock.generationNum;
            }
        }

        colorType = maxGenBlock.colorType;

        AudM.PlayNormalSound(NormalSound.Lined);
        foreach(BaseBlock baseBlock in blockList)
        {
            BaseBlock deleteBlock = baseBlock.OnDelete(); //削除
            if(deleteBlock != null)
            {
                deleteBlock.transform.parent = this.transform;
                FraM.DeleteBlock(deleteBlock);
                deleteBlock.SetColor(colorType, BatM.GetTexture(colorType)); // 色を変更
                deleteBlockList.Add(deleteBlock);
            }
        }

        if(mainState != MainStateType.idle) 
        {
            AttM.AddAttackQueue(deleteBlockList, lineList.Count); //攻撃ブロックを生成
        }
        RowDown(lineList);
    }

    void RowDown(List<int> lineList) //ラインを下にずらす
    {
        lineList.Sort();
		lineList.Reverse();
        foreach(int y in lineList)
        {
            List<RootBlock> rootBlockList = FraM.GetRBlocks(new Vector3Int(0, y + 1, 0), FraM.LFrameBorder.max);
            foreach(RootBlock rootBlock in rootBlockList) FraM.DeleteRBlock(rootBlock);
            foreach(RootBlock rootBlock in rootBlockList) 
            {
                rootBlock.Transform(Vector3Int.down);
                FraM.SetRBlock(rootBlock); //下に落ちれなかった場合、SetBlockされないため、応急措置
            }
        }
        if(mainState != MainStateType.idle) mainState = MainStateType.running;
        if(playerBlock != null) playerBlock.GenerateGhostBlock();
    }

    public void ResetBlock() //コントロールブロックを破棄
    {
        mainState = MainStateType.idle;
        
        if(playerBlock != null) 
        {
            playerBlock.DestroyGhostBlock();
            BlockPool.ReleaseNotRootBlock(playerBlock);
        }
        if(holdBlock != null) BlockPool.ReleaseNotRootBlock(holdBlock);
        playerBlock = null;
        holdBlock = null;
        foreach(RootBlock rootBlock in nextRBlockList)
        if(rootBlock != null) BlockPool.ReleaseNotRootBlock(rootBlock);
        nextRBlockList.Clear();
    }

    public T BlockConvert<T>(BaseBlock oldBlock) where T : BaseBlock
    {
        BaseBlock newBlock = oldBlock.AddComponent<T>();
        newBlock.blockType = oldBlock.blockType;
        newBlock.frameIndex = oldBlock.frameIndex;
        oldBlock.RootBlock.AddBlock(newBlock, oldBlock.shapeIndex, false);

        FraM.DeleteBlock(oldBlock);
        DestroyImmediate(oldBlock);
        FraM.SetBlock(newBlock);

        return newBlock as T;
    }

    public T RootConvert<T>(RootBlock oldRootBlock) where T : RootBlock
    {
        T newRootBlock = oldRootBlock.gameObject.AddComponent<T>();
        newRootBlock.Init(this, FraM);
        newRootBlock.pivot = oldRootBlock.pivot;

        foreach(BaseBlock baseBlock in oldRootBlock.BlockList) newRootBlock.AddBlock(baseBlock, baseBlock.shapeIndex, false);

        DestroyImmediate(oldRootBlock);
        return newRootBlock;
    }

    public void SetEditorMode()
    {
        mainState = MainStateType.running;
    }
}

public enum MainStateType
{
    idle,
    running,
    checkLine,
    deleting,
}