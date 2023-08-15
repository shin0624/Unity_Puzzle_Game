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

    public static int BLOCK_NUM_X = 9;//블록을 배치할 수 있는 X방향 최대 수
    public static int BLOCK_NUM_Y = 9;//블록을 배치할 수 있는 Y방향 최대 수
}

public class BlockControl : MonoBehaviour
{
    public Block.COLOR color = (Block.COLOR)0;//블록 색 초기 ->핑크
    public BlockRoot block_root = null;//블록루트 클래스의 변수.블록을 만들어내거나 교체
    public Block.iPosition i_pos;//블록 좌표

    
    void Start()
    {
        this.setColor(this.color);//색칠
    }

    
    void Update()
    {
        
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
}
