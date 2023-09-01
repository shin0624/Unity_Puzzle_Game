using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRoot : MonoBehaviour//각 블록에 준비된 기능을 사용하여 브블록 전체의 움직임 제어. 
{
    public GameObject BlockPrefab = null;//만들어낼 블록의 프리팹
    public BlockControl[,] blocks;//그리드

    private GameObject main_camera = null;//메인 카메라
    private BlockControl grabbed_block = null;//잡은 블록

    void Start()
    {
        //카메라로부터 마우스 커서를 통과하는 광선을 쏘기 위해 메인 카메라 확보-->나열된 블록 표면에 빛을 쏘아 어디를 가리키는지 계산하기 위해 카메라 오브젝트를 가져온다
        this.main_camera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    
    void Update()
    {
        //9x9 블록 배열 내 모든 블록에 대하여, 마우스 좌표와 겹치는지 체크, 잡을 수 있는 상태의 블록을 잡도록 처리
        Vector3 mouse_position;//마우스 위치
        this.unprojectMousePosition(out mouse_position, Input.mousePosition);//마우스 위치를 가져온다
        //가져온 마우스 위치를 하나의 Vector2로 모은다.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y);
        if(this.grabbed_block == null)//잡은 블록이 비었으면
        {
            //if(!this.is_has_falling_block()){
            if (Input.GetMouseButtonDown(0))
            {
                foreach(BlockControl block in this.blocks)//마우스 버튼이 눌렸다면 blocks 배열의 모든 요소를 차례로 처리
                {
                    if(!block.isGrabbable())
                    {
                        continue;//블록을 잡을 수 없다면 루프의 처음으로 점프
                    }
                    if (!block.isContainedPosition(mouse_position_xy))
                    {
                        continue;//마우스 위치가 블록 영역 내부가 아니라면 루프의 처음으로 점프
                    }
                    this.grabbed_block = block;//처리중인 블록을 grabbed_block에 등록
                    this.grabbed_block.beginGrab();//잡았을 때의 처리 실행
                    break;
                }
            }
            //}
        }
        else
        {
            do//블록 교체 코드 추가.-->블록을 잡고 상하좌우 중 한 쪽으로 블록 크기의 반 이상 마우스 이동 시 블록이 교체되도록.
            {
                //슬라이드할 곳의 블록을 가져옴
                BlockControl swap_target = this.getNextBlock(grabbed_block, grabbed_block.slide_dir);

                if (swap_target == null)//슬라이드 할 곳 블록이 비어있을 경우
                {
                    break;//루프 탈출
                }

                if (!swap_target.isGrabbable())//슬라이드 할 곳의 블록이 잡을 수 있는 상태가 아닐 경우
                {
                    break;//루프 탈출
                }

                float offset = this.grabbed_block.calcDirOffset(mouse_position_xy, this.grabbed_block.slide_dir);//현재 위치 ~ 슬라이드 위치 까지의 거리

                if (offset < Block.COLLISION_SIZE / 2.0f)//수리 거리가 블록 크기의 절반 이하일 때
                {
                    break;//루프 탈출
                }
                this.swapBlock(grabbed_block, grabbed_block.slide_dir, swap_target);//블록 교체

                this.grabbed_block = null;//지금은 블록을 잡고 있지 않음.
            } while (false);

            //블록을 잡았을 때
            if(!Input.GetMouseButton(0))
            {
                this.grabbed_block.endGrab();//마우스 버튼이 눌려져 있지 않으면 블록을 놓았을 때의 처리를 실행 후 grabbed_block을 비우도록 한다.
                this.grabbed_block = null;
            }
        }
    }

    public bool unprojectMousePosition(out Vector3 world_position, Vector3 mouse_position)//마우스가 지금 어느 블록 표면을 가리키는 지 계산.
     //9x9 블록 표면에 가상의 판을 두고, 카메라에서 마우스 좌표를 향해 빛을 통과시켜 빛의 도달 여부에 따라 마우스가 가리키는 현재 3차원 공간의 위치를 알 수 있다.
    {// *  out = 초기화 되지 않은 변수의 참조전달을 위해 지정(ref는 초기화 된 변수만 전달.)
        bool ret;
        Plane plane = new Plane(Vector3.back, new Vector3(0.0f, 0.0f, -Block.COLLISION_SIZE / 2.0f));//카메라에 대해서 뒤를 향하고, 블록의 절반 크기만큼 앞에 위치한 판.

        Ray ray = this.main_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);//카메라와 마우스를 통과하는 빛을 만든다.
        float depth;//광선이 판에 닿았을 때 정보가 기록되는 변수

        if(plane.Raycast(ray, out depth))//광선이 판에 닿았을 때
        {
            world_position = ray.origin + ray.direction * depth;//인수 world_position을 마우스 위치로.
            ret = true;
        }
        else//닿지 않았다면 월드포지션을 0인 벡터로.
        {
            world_position = Vector3.zero;
            ret= false;
        }
        return (ret);
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

    public BlockControl getNextBlock(BlockControl block, Block.DIR4 dir)//BlockControl 스크립트 매개변수 block = 현재 잡고 있는 블록. / Block 클래스의 DIR4 열거체 변수 dir = 슬라이드 방향
        //슬라이드 할 곳에 어떤 블록이 있는지 반환. 슬라이드할 곳의 블록을 blocks 배열 내에서 선택하여 그 블록을 반환. 만약 슬라이드할 곳이 9*9 그리드 바깥일 경우 블록 존재 x(null)
    {
        BlockControl next_block = null;//슬라이드할 곳의 블록을 여기에 저장

        switch (dir)
        {
            case Block.DIR4.RIGHT:
                if(block.i_pos.x < Block.BLOCK_NUM_X - 1)//그리드 내
                {
                    next_block = this.blocks[block.i_pos.x + 1, block.i_pos.y];
                }
                break;

            case Block.DIR4.LEFT:
                if (block.i_pos.x > 0)//그리드 내
                {
                    next_block = this.blocks[block.i_pos.x-1, block.i_pos.y];
                }
                break;

             case Block.DIR4.UP:
                if (block.i_pos.y < Block.BLOCK_NUM_Y - 1)//그리드 내
                {
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y+1];
                }
                break;

            case Block.DIR4.DOWN:
                if (block.i_pos.y > 0)//그리드 내
                {
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y - 1];
                }
                break;
        }
        return (next_block);

    }

    public static Vector3 getDirVector(Block.DIR4 dir)//인수 dir로 받은 방향으로 블록 하나만큼 이동할 경우의 이동량을 벡터3형으로 반환.(인수 방향이 RIGHT이면 현재 블록의 오른쪽 좌표로 이동하는 양을 반환.)
    {
        Vector3 v = Vector3.zero;

        switch (dir)
        {
            case Block.DIR4.RIGHT: v = Vector3.right;break;//오른쪽으로 1단위 이동
            case Block.DIR4.LEFT: v = Vector3.left; break;//왼쪽으로 1단위 이동
            case Block.DIR4.UP: v = Vector3.up; break;//위로 1단위 이동
            case Block.DIR4.DOWN: v = Vector3.down; break;//아래로 1단위 이동
        }
        v *= Block.COLLISION_SIZE;// 블록의 크기를 곱한다.
        return (v);
    }

    public static Block.DIR4 getOppositDir(Block.DIR4 dir)//인수 dir로 받은 방향의 역방향을 반환. 잡은 블록이 오른쪽으로 이동할 경우, 이동할 곳에 있는 블록은 반대인 왼쪽으로 이동하게끔.
    {
        Block.DIR4 opposit = dir;
        switch (dir)
        {
            case Block.DIR4.RIGHT: opposit = Block.DIR4.LEFT;break;
            case Block.DIR4.LEFT: opposit = Block.DIR4.RIGHT; break;
            case Block.DIR4.UP: opposit = Block.DIR4.DOWN; break;
            case Block.DIR4.DOWN: opposit = Block.DIR4.UP; break;
        }
        return (opposit);
    }

    public void swapBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)//블록 교체 작업 수행. block0 = 잡고있는 블록/ dir = 이동 방향 / block1 = 이동할 곳의 블록
    {
        //각각의 블록 색 
        Block.COLOR color0 = block0.color;
        Block.COLOR color1 = block1.color;

        //각각의 블록의 확대율
        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;

        //각각의 블록의 사라지는 시간
        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;

        //각각의 블록의 이동할 곳을 구한다.
        Vector3 offset0 = BlockRoot.getDirVector(dir); 
        Vector3 offset1 = BlockRoot.getDirVector(BlockRoot.getOppositDir(dir));

        //색 교체
        block0.setColor(color1);
        block1.setColor(color0);

        //확대율 교체
        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;

        //사라지는 시간 교체
        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;

        block0.beginSlide(offset0);//원래 블록 이동 시작
        block1.beginSlide(offset1);//이동할 위치의 블록 이동 시작
    }

}
