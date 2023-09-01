using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Block;

public class Block
{//블록에 관한 정보를 다루는 클래스.
    public static float COLLISION_SIZE = 1.0f;//블록의 충돌 크기
    public static float VANISH_TIME = 3.0f;//불이 붙고 사라질 때까지의 시간

    

    public struct iPosition 
    {//그리드에서의 좌표를 나타내는 구조체
        public int x;
        public int y;
    }
    public enum COLOR
    {
        //블록색상 열거
        NONE = -1,//색지정 X
        PINK = 0,
        BLUE,
        YELLOW,
        GREEN,
        MAGENTA,
        ORANGE,
        GRAY,
        NUM,//컬러 종류 = 7
        FIRST = PINK,//초기 컬러
        LAST = ORANGE,//최종 컬러
        NORMAL_COLOR_NUM = GRAY,//보통 컬러(회색 이외의 색)의 수
    };

    public enum DIR4
    {//상하좌우 네 방향
        NONE = -1,//방향지정X
        RIGHT,
        LEFT,
        UP,
        DOWN,
        NUM,//방향 종류  =4
    };

    public enum STEP
    {//블록의 상태 표시
        NONE = -1,//상태정보 없음
        IDLE = 0,//대기중
        GRABBED,//잡혀있음
        RELEASED,//떨어진 순간
        SLIDE,//슬라이드 중
        VACANT,//소멸 중
        RESPAWN,//재생성 중
        FALL,//낙하 중
        LONG_SLIDE,//크게 슬라이드 중
        NUM,//상태가 몇종류인지 표시
    };


    public static int BLOCK_NUM_X = 9;//블록을 배치할 수 있는 X방향 최대 수
    public static int BLOCK_NUM_Y = 9;//블록을 배치할 수 있는 Y방향 최대 수
}

public class BlockControl : MonoBehaviour
{
    public Block.COLOR color = (Block.COLOR)0;//블록 색 초기 ->핑크
    public BlockRoot block_root = null;//블록루트 클래스의 변수.블록을 만들어내거나 교체
    public Block.iPosition i_pos;//블록 좌표

    public Block.STEP step = Block.STEP.NONE;//지금 상태
    public Block.STEP next_step = Block.STEP.NONE;//다음 상태
    private Vector3 position_offset_initial = Vector3.zero;// 교체 전 위치
    public Vector3 position_offset = Vector3.zero;//교체 후 위치

    public float vanish_timer = -1.0f;//블록이 사라질 때 까지의 시간
    public Block.DIR4 slide_dir = Block.DIR4.NONE;//슬라이드 된 방향.
    public float step_timer = 0.0f;//블록이 교체된 때의 이동시간 

    public Material opague_material;//불투명 마테리얼
    public Material transparent_material;//반투명 마테리얼

    public Block.DIR4 calcSlideDir(Vector2 mouse_position)//인수 마우스 위치를 바탕으로 어느쪽으로 슬라이드되었는지 판단 후 Block.DIR4형 값으로 반환-->블록 교체 여부를 판단.
    {
        Block.DIR4 dir = Block.DIR4.NONE;
        Vector2 v = mouse_position - new Vector2(this.transform.position.x, this.transform.position.y);//지정된 마우스 위치와 현재 위치의 차

        if (v.magnitude > 0.1f)//magnitude = 벡터 원점과 끝점 사이. 즉 벡터의 크기
        {
            //벡터 크기 0.1 이하이면 슬라이드하지 않은 것으로 정의
            if (v.y > v.x)//v가 0.1 이상이면 방향을 구하고 DIR4형 값 반환
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.UP;
                }
                else
                {
                    dir = Block.DIR4.LEFT;
                }
            }
            else
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.RIGHT;
                }
                else
                {
                    dir = Block.DIR4.DOWN;
                }
            }
        }
        return (dir);
    }

    public float calcDirOffset(Vector2 position, Block.DIR4 dir)
    {
        //인수(위치, 방향)을 근거로, 현재 위치와 슬라이드 할 곳의 거리가 어느정도인지 반환
        float offset = 0.0f;
        Vector2 v = position - new Vector2(this.transform.position.x, this.transform.position.y);//지정된 위치와 블록의 현재 위치의 차
        switch (dir)
        {
            case Block.DIR4.RIGHT: offset = v.x; break;
            case Block.DIR4.LEFT: offset = -v.x; break;
            case Block.DIR4.UP: offset = v.y; break;
            case Block.DIR4.DOWN: offset = -v.y; break;
        }
        return (offset);
    }

    public void beginSlide(Vector3 offset)
    {
        this.position_offset_initial = offset;
        this.position_offset = this.position_offset_initial;
        this.next_step = Block.STEP.SLIDE;//상태를 SLIDE로 변경
    }

    void Start()
    {
        this.setColor(this.color);//색칠
        this.next_step = Block.STEP.IDLE;//다음 블록을 대기중으로
    }

    
    void Update()
    {
        Vector3 mouse_position;//마우스 위치
        this.block_root.unprojectMousePosition(out mouse_position, Input.mousePosition);//마우스 위치 획득. unprojectMousePosition = BlockRoot클래스의 메서드에서, 마우스가 지금 어느 블록의 표면을 가리키는지 계산

        //획득한 마우스 위치를 x와 y만으로 한다.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y);

        //블록 중복 시 발화 및 소멸 이벤트 발생 단계 추가
        if (this.vanish_timer >= 0.0f)//타이머가 0 이상이면
        {
            this.vanish_timer -=Time.deltaTime;//타이머 값 감소
            if(this.vanish_timer <0.0f)//타이머가 0 미만이면
            {
                if(this.step != Block.STEP.SLIDE)//슬라이드 중이 아닐 때
                {
                    this.vanish_timer = -1.0f;
                    this.next_step = Block.STEP.VACANT;//상태를 소멸 중으로 변경
                }
                else
                {
                    this.vanish_timer = 0.0f;
                }
            }

        }


        this.step_timer += Time.deltaTime;
        float slide_time = 0.2f;

        if(this.next_step== Block.STEP.NONE)//상태정보 없음의 경우
        {
            switch (this.step) 
            {
                case Block.STEP.SLIDE: if (this.step_timer >= slide_time)
                    {
                        if (this.vanish_timer == 0.0f)
                        {
                            this.next_step = Block.STEP.VACANT;//슬라이드 중인 블록 소멸 시 VACANT(사라진)상태로 이행
                        }
                        else
                        {
                            this.next_step = Block.STEP.IDLE;
                        }
                    }
                    break;
            }

        }


        //다음 블록 상태가 "정보 없음" 이외인 동안 = 즉 , 다음 블록 상태가 변경된 경우
        while (this.next_step != Block.STEP.NONE)
        {
            this.step = this.next_step;
            this.next_step = Block.STEP.NONE;

            switch(this.step)
            {
                case Block.STEP.IDLE://대기 상태
                    this.position_offset = Vector3.zero;//블록 표시 크기를 보통 크기로
                    this.transform.localScale = Vector3.one * 1.0f;
                    break;
                case Block.STEP.GRABBED://잡힌 상태
                    this.transform.localScale = Vector3.one * 1.2f;//블록 표시 크기를 크게
                    break;
                case Block.STEP.RELEASED://떨어져 있는 상태
                    this.position_offset = Vector3.zero;
                    this.transform.localScale = Vector3.one * 1.0f;//블록 표시 크기를 보통 사이즈로
                    break;
                case Block.STEP.VACANT://사라진 상태
                    this.position_offset= Vector3.zero;
                    this.setVisible(false);//블록 발화 시 사라짐 상태 이행 처리 추가.-->현재 블록을 표시하지 않도록
                    break;
            }
            this.step_timer = 0.0f;
        }
        switch (this.step)
        {
            case Block.STEP.GRABBED://잡힌 상태일 때. 항상 슬라이드 방향을 체크하도록
                this.slide_dir = this.calcSlideDir(mouse_position_xy); break;
            case Block.STEP.SLIDE://슬라이드 중일 때.
                float rate = this.step_timer / slide_time;//블록을 서서히 이동하는 거리.
                rate = Mathf.Min(rate, 1.0f);
                rate = Mathf.Sin(rate * Mathf.PI / 2.0f);
                this.position_offset = Vector3.Lerp(this.position_offset_initial, Vector3.zero, rate);break;
                //Vector3.Lerp = 시작벡터 ~ 목표벡터 사이의 시간에 따른 위치를 구할 때 사용.Lerp(start, finish, 0.0~1.0값)
        }

        //그리드 좌표를 실제 좌표(씬의 좌표)로 변환하고 position_offset 추가
        Vector3 position = BlockRoot.calcBlockPosition(this.i_pos) + this.position_offset;

        //실제 위치를 새로운 위치로 변경
        this.transform.position = position;

        this.setColor(this.color);//블록의 색을 서서히 바꾸는 처리 추가

        if (this.vanish_timer >= 0.0f)
        {
            Color color0 = Color.Lerp(this.GetComponent<Renderer>().material.color, Color.white, 0.5f);//현재 색과 흰색의 중간 색
            Color color1 = Color.Lerp(this.GetComponent<Renderer>().material.color, Color.black, 0.5f);//현재 색과 검은색의 중간 색

            //발화 연출 시간이 절반을 지났다면
            if(this.vanish_timer < Block.VANISH_TIME / 2.0f)
            {
                color0.a = this.vanish_timer / (Block.VANISH_TIME / 2.0f);//투명도(a) 설정
                color1.a = color0.a;
                this.GetComponent<Renderer>().material = this.transparent_material;//반투명 마테리얼 적용
            }
            float rate = 1.0f - this.vanish_timer / Block.VANISH_TIME;//vanish_timer가 줄어들수록 1에 가까워진다.
            this.GetComponent<Renderer>().material.color = Color.Lerp(color0, color1, rate);//서서히 색을 바꿀 수 있도록.
            //Lerp(색A, 색B, 비율)로 사용-->color0에서 color1로 변화되는 과정을 rate% 진행한 만큼의 색을 반환. 즉 color1과 color2의 중간 색을 구해서 현재 블록의 마테리얼 색상으로 설정
        }

    }
    public void beginGrab()
    {
        this.next_step = Block.STEP.GRABBED;
    }

    public void endGrab()
    {
        this.next_step = Block.STEP.IDLE;
    }
    public bool isGrabbable()
    {
        bool is_grabbable = false;
        switch (this.step) 
        {
            case Block.STEP.IDLE:is_grabbable = true;break;//대기 상태일 때에만 true(잡을 수 있다)를 반환
        }
        return(is_grabbable);
    }

    public bool isContainedPosition(Vector2 position)
    {
        bool ret = false;
        Vector3 center = this.transform.position;
        float h = Block.COLLISION_SIZE / 2.0f;
        /*do
        {
            //x좌표가 자신과 겹치지 않으면 break로 루프 탈출
            if (position.x < center.x - h || center.x + h < position.x)
            {
                break;
            }
            //y좌표가 자신과 겹치지 않으면 break로 루프 탈출
            if (position.y < center.y - h || center.y + h < position.y)
            {
                break;
            }
            //x좌표, y좌표 모두 겹쳐있으면 true(겹쳐 있다)를 반환
            ret = true;
        } while (false);*/
        if (position.x >= center.x - h && position.x <= center.x + h && position.y >= center.y - h && position.y <= center.y + h)
        {//블록의 중심과 마우스 좌표 간의 거리를 계산하여 해당 거리가 블록의 크기 반만큼 이내인 경우에만 클릭 가능한 영역으로 판단.
         //-->블록의 중심에서 얼마나 떨어져 있는지에 관계 없이 정확한 영역을 판단가능
            ret = true;
        }
        return (ret);
    }



    public void setColor(Block.COLOR color)
    {//인수 color의 색으로 블록을 칠한다.
        this.color = color;//지정된 색을 멤버 변수에 보관
        Color color_value;//Color클래스는 색을 나타냄.

        switch (this.color)//칠할 색에 따라 케이스 구분
        {
            default:
            case Block.COLOR.PINK:
                color_value = new Color(1.0f, 0.5f, 0.5f);
                break;

            case Block.COLOR.BLUE:
                color_value = Color.blue;
                break;

            case Block.COLOR.YELLOW:
                color_value = Color.yellow;
                break;
            case Block.COLOR.GREEN:
                color_value = Color.green;
                break;
            case Block.COLOR.MAGENTA:
                color_value = Color.magenta;
                break;
            case Block.COLOR.ORANGE:
                color_value = new Color(1.0f, 0.46f, 0.0f);
                break;
        }
        //이 게임 오브젝트의 마테리얼 색상 변경
        this.GetComponent<Renderer>().material.color = color_value;
    }

    public void toVanishing()//블록을 지우기 시작할 때
    {
        this.vanish_timer = Block.VANISH_TIME;//소멸 때 까지 걸리는 시간을 규정값으로 리셋
    }

    public bool isVanshing()//블록이 지워지는 중이면 true. 즉 vanish_timer가 0 이상이면 true
    {
        bool is_vanishing = (this.vanish_timer > 0.0f);
        return (is_vanishing);
    }

    public void rewindVanishTimer()//사라질 때까지 걸리는 시간을 리셋
    {
        this.vanish_timer = Block.VANISH_TIME;//소멸 때 까지 걸리는 시간을 규정값으로 리셋
    }

    public bool isVisible()
    {
        bool is_visible = this.GetComponent<Renderer>().enabled;
        return (is_visible);//그리기 가능 상태일 때. 즉 블록이 표시되고 있을 때 true반환
    }

    public void setVisible(bool is_visible)                                           
    {
        this.GetComponent<Renderer>().enabled = is_visible;//그리기 가능 설정에 bool값을 대입. 인수에 true를 지정하면 블록이 표시되고, false를 지정하면 블록이 표시되지 않는다.
    }

    public bool isIdle()
    {
        bool is_idle = false;
        if(this.step==Block.STEP.IDLE && this.next_step == Block.STEP.NONE)//현재 블록 상태가 대기중, 다음 블록 상태가 없음 일 경우 true 반환
        {
            is_idle = true;
        }
        return(is_idle);
    }
}
