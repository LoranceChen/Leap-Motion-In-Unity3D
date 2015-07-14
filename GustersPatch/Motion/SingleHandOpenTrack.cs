using UnityEngine;
using System.Collections;
using Leap;
using System;
/// <summary>
/// 当检测到一直手后，进入追踪状态，记录单手运动的速度，掌心方向。
/// 检测到HandOpen之后，延迟0.5s进入追踪状态。
/// 关于速度：慢速[0,10],快速[25,40],建议以25为上限
/// </summary>
public class SingleHandOpenTrack : MonoBehaviour {
    [SerializeField] HandOpen m_HandOpen;
    Controller leap;
    //Update设定
    readonly float TrackStepTime = 0.1f;
    float m_CurTrackTime;

    //初始状态
    [SerializeField]
    bool m_IsSingleHandOpened;
    readonly float EnterDelayTime = 0.15f;//进入0.5s后触发
    float m_CurEnterDelayTime;
    Action m_OnEnterFunc;

    //运动状态
	[SerializeField] float m_Speed;//手掌运动速度，前后两个位置做差
    Vector m_PreviousPos;//前次的位置
    Vector m_Dir=Vector.Zero;//手掌运动的方向，CurPos-PrePos

    //结束状态
    Action m_OnEndFunc;
    public bool IsSingleHandOpened
    {
        get
        {
            return m_IsSingleHandOpened;
        }
    }
    public Vector Dir
    {
        get
        {
            return m_Dir;
        }
    }

    public float Speed
    {
        get
        {
            return m_Speed;
        }
    }

    public void RegisterFunc(Action m_OnEnter,Action m_OnEnd)
    {
        if(m_OnEnter!=null)
        {
            m_OnEndFunc += m_OnEnter;
        }
        if(m_OnEnd!=null)
        {
            m_OnEndFunc += m_OnEnd;
        }
    }

    public void CancelFunc(Action m_OnEnter, Action m_OnEnd)
    {
        if (m_OnEnter != null)
        {
            m_OnEndFunc -= m_OnEnter;
        }
        if (m_OnEnd != null)
        {
            m_OnEndFunc -= m_OnEnd;
        }
    }
    void Awake()
    {
        leap = LeapDriver.GetLeapCtrl();
    }
	// Update is called once per frame
	void Update () 
    {
	    m_CurTrackTime+=Time.deltaTime;
        m_CurEnterDelayTime += Time.deltaTime;
        
        if(m_CurTrackTime>TrackStepTime)
        {
            m_CurTrackTime = 0f;
            //print(leap.Frame().Hands.Count);
            if (leap.Frame().Hands.Count == 1)
            {
                int index;
                int number = m_HandOpen.OpenNumber(out index);
                //只有一只手被检测到
            
                //如果只有一只手处于摊开状态
                if (number == 1)
                {
                    //第一次伸掌
                    if (!m_IsSingleHandOpened)
                    {
                        //满足延迟时间
                        if (m_CurEnterDelayTime > EnterDelayTime)
                        {
                            m_CurEnterDelayTime = 0f;

                            m_IsSingleHandOpened = true;
                            m_PreviousPos = m_HandOpen.PalmPos[index];
                            if (m_OnEndFunc != null)
                            {
                                m_OnEndFunc();
                            }
                        }
                    }
                    //已经处于伸掌状态
                    else
                    {
                        Vector curDir = m_HandOpen.PalmPos[index];
                        m_Dir = curDir - m_PreviousPos;
                        m_Speed = curDir.DistanceTo(m_PreviousPos);
                        m_PreviousPos = curDir;
                    }
                }
                //不是伸掌状态
                else
                {
                    //上一次是伸掌状态，说明这是一次手势的结束，触发结束事件
                    if (m_IsSingleHandOpened)
                    {
                        if (m_OnEndFunc != null)
                        {
                            m_OnEndFunc();
                        }
                    }
                    //初始化数据
                    Reset();
                }
            }
            //不是伸掌状态
            else
            {
                //上一次是伸掌状态，说明这是一次手势的结束，触发结束事件
                if (m_IsSingleHandOpened)
                {
                    if (m_OnEndFunc != null)
                    {
                        m_OnEndFunc();
                    }
                }
                //初始化数据
                Reset();
            }
        }
       
	}
    /// <summary>
    /// 重置数据，如果判定失败时调用
    /// </summary>
    void Reset()
    {
        m_Speed = 0f;
        m_PreviousPos = Vector.Zero;
        m_Dir = Vector.Zero;
        m_IsSingleHandOpened = false;
//        m_CurTrackTime = 0f;
        m_CurEnterDelayTime = 0f;
    }
}
