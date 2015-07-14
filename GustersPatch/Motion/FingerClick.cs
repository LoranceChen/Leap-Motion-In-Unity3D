using UnityEngine;
using System.Collections;
using System;
using Leap;

/// <summary>
/// 1.当判定手指为FingerPoint姿势并且手指放置在相应的碰撞层上（按钮上）时，进入初始状态。
/// 2.每隔0.2s判定手指-z轴的偏移量。
/// 3.当达到前进偏移阈值（深度阈值）时，进入等待反弹状态。
/// 4.反弹状态的检测根据z轴的偏移量增加而决定
/// 5.检测到反弹后，继续检测反弹阈值，如果超过反弹阈值，则触发点击完成事件m_OnClicked.
/// </summary>
public class FingerClick : MonoBehaviour 
{
    [SerializeField] HandFingerPoint m_FingerPoint;
    [SerializeField] HandAndFingersPoint m_HandData;
    int m_CollisionLayer;//碰撞的层
    Action m_OnClicked;//完成点击事件
    
    Action m_OnEnterClick;//进入点击模式
    readonly float EnterDelayTime=0.2f;//进入点击模式后延迟多长多长事件触发

    //Motion
    readonly float CheckTimeStep = 0.1f;//运动状态检测的时间差。
    Vector m_PreviousPos = Vector.Zero;//上一次检测的位置
    Vector m_Off = Vector.Zero;
    //threshold
    readonly float DeepThreshold=25f;//向前偏移的深度阈值
    readonly float BackThreshold=16f;//返回时的反弹阈值
    readonly float ForwordPointThreshold=Mathf.PI *5 /18;//向前指向的阈值

    //点击的持续时间，点击的速度
    [SerializeField] float m_DeepClickingTime;//进入点击状态后，手指向前移动持续的时间
    [SerializeField]
    float m_BackClickingTime;//进入反弹状态后，手指返回移动持续的时间
    [SerializeField]
    float m_ClickSpeed;//点击时候的速度

    //手势识别中的几个状态节点
    [SerializeField]
    bool m_IsEnteredClick;//进入了识别状态
    [SerializeField]
    bool m_IsEnteredBackClick;//进入了反弹状态
    Action m_OnBackFunc;

    Vector m_EnterPos;//初始状态指尖的位置
    Vector m_EnterBackPos;//反弹状态的初始位置

	//点击状态点
	readonly float DeepClickRadianThreshold = Mathf.PI*30/180;//点击时每帧的方向偏离-z轴的夹角

    public bool IsEnteredClick
    {
        get
        {
            return m_IsEnteredClick;
        }
    }
    public bool IsEnteredBackClick
    {
        get
        {
            return m_IsEnteredBackClick;
        }
    }

    public Vector Off
    {
        get
        {
            return m_Off;
        }
    }
    //FinishClickAction Msg
    //手掌打开状态，事件注册接口
    public void RegisterClickedMsg(Action onClicked)
    {
        if (onClicked != null)
        {
            m_OnClicked += onClicked;
        }

    }
    public void CancelClickedMsg(Action onClicked)
    {
        if(onClicked!=null)
        {
            m_OnClicked -= onClicked;
        }
    }

    /// <summary>
    /// 进入点击状态事件的注册和注销操作
    /// </summary>
    /// <param name="onEnterClick"></param>
    public void RegisterEnterClickMsg(Action onEnterClick)
    {
        if (onEnterClick != null)
        {
            m_OnClicked += onEnterClick;
        }
    }
    public void CancelEnterClickMsg(Action onEnterClick)
    {
        if (onEnterClick != null)
        {
            m_OnClicked -= onEnterClick;
        }
    }

    public void RegisterBackClickMsg(Action onBack)
    {
        if (onBack != null)
        {
            m_OnBackFunc += onBack;
        }
    }
    public void CancelBackClickMsg(Action onBack)
    {
        if (onBack != null)
        {
            m_OnBackFunc -= onBack;
        }
    }
	
	// Update is called once per frame
	void Update () 
    {     
        int clickIndex;
        //这里的判定本该分成预备状态、运动状态、终结状态。但是对于点击来讲，这三个状态的手势判定条件都相同（手势相同、阈值相同）
        //所以现在都用同一个控制器来表示
        
        if (EnterClickState(out clickIndex))
        {
            //初始状态
            if(!m_IsEnteredClick)
            {
                //标记进入点击状态，记录初始位置
                m_IsEnteredClick = true;
                m_EnterPos = m_HandData.FingerDatas[clickIndex][Finger.FingerType.TYPE_INDEX].m_Point.m_Position;
                m_PreviousPos = m_EnterPos;

                //延迟触发初始事件
                StartCoroutine(EnterClickDelay());
            }
            //非初始状态
            else
            //不是反弹状态
            if(!m_IsEnteredBackClick)
            {
                m_DeepClickingTime+=Time.deltaTime;
                //是否进入一次位置判定
                if (m_DeepClickingTime > CheckTimeStep)
                {
                    m_DeepClickingTime = 0f;
                    //收集当前手指索引的食指指尖位置数据
                    Vector indexFingerTipPos = m_HandData.FingerDatas[clickIndex][Finger.FingerType.TYPE_INDEX].m_Point.m_Position;
                    m_Off = indexFingerTipPos - m_PreviousPos;
                    
                    //是向前点击状态，偏移向量与-z轴（Leap坐标系）的夹角小于某个阈值
					if(m_Off.AngleTo(-Vector.ZAxis)<DeepClickRadianThreshold)
                    //if (indexFingerTipPos.z < m_PreviousPos.z)
                    {
                        float deep = m_EnterPos.z - indexFingerTipPos.z;
                        //是反弹状态
                        if( deep > DeepThreshold )
                        {
                            //标记进入了反弹状态，记录反弹状态的位置
                            m_IsEnteredBackClick = true;
                            m_EnterBackPos = indexFingerTipPos;
                            if(m_OnBackFunc != null)
                            {
                               // print("aaa");
                                m_OnBackFunc();
                            }
                        }
                    }
                    //不是前进状态
                    else
                    {
                        //手势匹配失败，重置变量
                        ResetClickState();
                    }
                    //更新m_PreviousPos的位置
                    m_PreviousPos = indexFingerTipPos;
                }
            }
            else //进入了返回状态
            {
                m_BackClickingTime+=Time.deltaTime;
                //进入位置判定
                if ( m_BackClickingTime > CheckTimeStep )
                {
                    m_BackClickingTime = 0f;
                    Vector indexFingerTipPos = m_HandData.FingerDatas[clickIndex][Finger.FingerType.TYPE_INDEX].m_Point.m_Position;
                    //是后退状态
                    if (m_PreviousPos.z < indexFingerTipPos.z)
                    {
                        float backDeep = indexFingerTipPos.z - m_EnterBackPos.z;
                        //满足了反弹阈值
                        if( backDeep > BackThreshold )
                        {
                            print("Clicked");
                            //触发事件，重置状态
                            if (m_OnClicked!=null)
                            {
                                print("Clicked()");
                                m_OnClicked();
                            }
                            ResetClickState();
                        }
                    }
                }
            }
        }
        //不是手指点击状态
        else
        {
            ResetClickState();
            m_IsEnteredClick = false;
        }
	}

    /// <summary>
    /// 进入点击状态后延迟触发一个进入事件
    /// </summary>
    /// <returns></returns>
    IEnumerator EnterClickDelay()
    {
        float curDelayTime=0f;
        while(curDelayTime<EnterDelayTime)
        {
            curDelayTime += Time.deltaTime;
            yield return null;
        }
        //进入了点击状态
        if (m_IsEnteredClick)
        {
            //发送广播
            if (m_OnEnterClick != null)
            {
                m_OnEnterClick();
            }
        }
    }

    /// <summary>
    /// 在判定过程中，因为匹配失败或者完成匹配。都会重置点击状态
    /// </summary>
    void ResetClickState()
    {
        m_DeepClickingTime = 0f;
        m_ClickSpeed = 0f;
        m_BackClickingTime = 0f;

        m_IsEnteredBackClick = false;
    }

    /// <summary>
    /// 进入预备/运动/终结状态
    /// 检测到大致前向方向
    /// </summary>
    /// <param name="dir">选择哪个手进入的预备状态，只有返回true才有效</param>
    /// <returns></returns>
    bool EnterClickState(out int index)
    {
        bool isEnter = false;
        index = -1;

        for (int i = 0; i < 2; i++)
        {
            //是指向状态
            if (m_FingerPoint.IsEnterPointed[i])
            {
                Vector dir = m_FingerPoint.Dir[i];
                //是点击手势
                if (IsPointAsClickForwardDir(dir, ForwordPointThreshold))
                {
                    isEnter = true;
                    index = i;
                }
            }
        }
        return isEnter;
    }

    /// <summary>
    /// 判定一个手指指向是否是点击前方
    /// 这里阈值的判定与其说是+x轴，倒不如说是距离yz面的夹角程度更直接，但实现方式上选择了+x轴。
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="threshold">表示距离+x的夹角，标准是90度</param>
    bool IsPointAsClickForwardDir(Vector dir,float threshold)
    {
        bool IsPointAsClick = false;
        float radian = dir.AngleTo(Vector.Right);

        if (radian > threshold && radian < Mathf.PI - threshold)
        {
            IsPointAsClick = true;
        }
        return IsPointAsClick;
    }
}
