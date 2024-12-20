using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "IceBlockData", menuName = "Effect/CreateIceBlockData")]
public class IceBlockData : BaseEffectData
{

    MainGameManager GamM;
    FrameManager FraM;

    public override void Init(Enemy enemy)
    {
        StageManager StaM = FindFirstObjectByType<StageManager>();
        BattleManager BatM = StaM.GetCurBattle();
        GamM = BatM.GamM;
        FraM = BatM.FraM;
        this.enemy = enemy;
    }

    public override UniTask Execute()
    {
        //ブロックを置き換えるプログラムに変更するべき

        
        // Vector3Int iceSize = new Vector3Int(2, 2, 0);//一旦
        // List<List<BaseBlock>> boardList = FraM.FrameListList;
        // Vector3Int pos = new Vector3Int(Random.Range(0, FraM.LMovableBorder.width - 1), Random.Range(0, FraM.LMovableBorder.height - 8), 0);

        // for(int y = pos.y; y < pos.y + iceSize.y; y++)
        // for(int x = pos.x; x < pos.x + iceSize.x; x++)
        // {
        //     if(boardList[y][x] == null) continue;
        //     if(boardList[y][x].blockType != BlockType.Mino) continue;
        //     if(boardList[y][x].GetType() != typeof(IceBlock))
        //     {
        //         GamM.BlockConvert<IceBlock>(boardList[y][x]);
        //     }
        // }
        return UniTask.CompletedTask;
    }
}
