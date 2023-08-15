using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRoot : MonoBehaviour
{
    public GameObject BlockPrefab = null;//만들어낼 블록의 프리팹
    public BlockControl[,] blocks;//그리드
    void Start()
    {
        
    }

    
    void Update()
    {
          
    }

    public void initialSetUp()//블록 생성 후9x9 배치. SceneControl 클래스의 스타트에서 호출될 것
    {
       this.blocks = new BlockControl [Block.BLOCK_NUM_X, Block.BLOCK_NUM_Y];
       int color_index = 0;//블록의 색 번호

        for(int y = 0; y < Block.BLOCK_NUM_Y; y++)//첫 행부터 마지막 행까지
        {
            for(int x = 0; x < Block.BLOCK_NUM_X; x++)//왼쪽부터 오른쪽까지
            {
                //블록프리팹 인스턴스를 씬에 생성
                GameObject game_object = Instantiate(this.BlockPrefab) as GameObject;
                //위에서 만든 블록의 BlockControl클래스를 가져온다.
                BlockControl block = game_object.GetComponent<BlockControl>();
                //블록을 그리드에 저장한다.
                this.blocks[x, y] = block;

                //블록의 위치 정보(그리드 좌표)를 설정
                block.i_pos.x = x;
                block.i_pos.y = y;
                //각 블록컨트롤이 연계할 GameRoot는 자신이라고 설정
                block.block_root = this;

                //그리드 좌표를 실제 위치(씬의 좌표)로 변환
                Vector3 position = BlockRoot.calcBlockPosition(block.i_pos);
                //씬의 블록 위치 이동
                block.transform.position = position;
                //블록 색 변경
                block.setColor((Block.COLOR)color_index);
                //블록 이름 설정
                block.name = "block(" + block.i_pos.x.ToString() + "," + block.i_pos.y.ToString() + ")";//게임 재생 중 일시정지 시 하이어라키에서의 블록 정보 확인을 위함
                //전체 색 중에서 랜덤으로 하나의 색 선택
                color_index = Random.Range(0, (int)Block.COLOR.NORMAL_COLOR_NUM);
            }
        }
    }

    public static Vector3 calcBlockPosition(Block.iPosition i_pos)//지정된 그리드 좌표로 씬에서의 좌표를 구한다
    {
        //배치할 왼쪽 위 구석 위치를 초깃값으로 설정
        Vector3 position = new Vector3(-(Block.BLOCK_NUM_X / 2.0f - 0.5f),-(Block.BLOCK_NUM_Y / 2.0f -0.5f), 0.0f);

        //초깃값 + 그리드좌표 * 블록 크기

        position.x += (float)i_pos.x * Block.COLLISION_SIZE;
        position.y += (float)i_pos.y * Block.COLLISION_SIZE;

        return (position);//씬에서의 좌표를 반환
    }
}
