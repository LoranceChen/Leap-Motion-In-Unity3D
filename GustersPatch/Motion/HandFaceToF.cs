using UnityEngine;
using System.Collections;
using Leap;
using System;

/// <summary>
/// 判定两只手处于垂直向前状态
/// 记录两只手之间的距离，速度。
/// </summary>
public class HandFaceToF : MonoBehaviour
{
    [SerializeField] HandOpen m_HandOpen;

    //Update参数
    readonly float CheckStepTime = 0.1f;
    float m_CurCheckStepTime;
        
    //初始状态
    [SerializeField] bool m_IsEnteredFTF;
    Action m_OnEnterFunc;
    //运动状态
    float m_PreviousDis;
    [SerializeField] float m_Speed;//cur-pre.大于0表示放大
    //终结状态
    Action m_OnEndFunc;
    //延迟验证
    readonly float EnterDelayTime = 0.2f;
    float m_CurEnterDelayTime;
	
    //手势匹配
	readonly float VerToXAxisDirAdjust = -Mathf.PI/18;//垂直x轴的方向调节阈值[60,180-60]
	readonly float VerToYAxisDirAdjust = -Mathf.PI/18;//垂直y轴方向的调节阈值[60,180-60]

    public bool IsEntered
    {
        get
        {
            return m_IsEnteredFTF;
        }
    }
    public float Speed
    {
        get
        {
            return m_Speed;
        }
    }
    public void RegisterFunc(Action m_OnEnter, Action m_OnEnd)
    {
        if (m_OnEnter != null)
        {
            m_OnEndFunc += m_OnEnter;
        }
        if (m_OnEnd != null)
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

	// Update is called once per frame
	void Update () 
    {
	    m_CurCheckStepTime+=Time.deltaTime;
        m_CurEnterDelayTime += Time.deltaTime;
        if(m_CurCheckStepTime>CheckStepTime)
        {
            m_CurCheckStepTime = 0f;
            //满足FaceToFace
            if(FTFState())
            {
                //当前不是进入状态
                if(!m_IsEnteredFTF)
                {
                    if(m_CurEnterDelayTime > EnterDelayTime)
                    {
                        m_CurEnterDelayTime=0f;

                        m_IsEnteredFTF=true;
                        m_PreviousDis = m_HandOpen.PalmPos[0].DistanceTo(m_HandOpen.PalmPos[1]);    
                        if(m_OnEnterFunc!=null)
                        {
                            m_OnEnterFunc();
                        }
                    }
                }
                //已经进入FTF状态
                else
                {
                    //记录数据
                    float curDis = m_HandOpen.PalmPos[0].DistanceTo(m_HandOpen.PalmPos[1]);
                    m_Speed = curDis-m_PreviousDis;
                    m_PreviousDis = curDis;
                }
            }
            //不是FTF
            else
            {
                //这是终结状态
                if (m_IsEnteredFTF)
                {
                    if(m_OnEndFunc!=null)
                    {
                        m_OnEndFunc();
                    }
                }
                Reset();
            }
        }
	}

    void Reset()
    {
        m_IsEnteredFTF=false;

        //运动状态
        m_PreviousDis=0f;
        m_Speed=0f;//cur-pre.大于0表示放大
        m_CurCheckStepTime = 0f;
        m_CurEnterDelayTime=0f;    
    }

    bool FTFState()
    {
        bool isFTF=false;
        if( m_HandOpen.IsEnterOpened[0] && m_HandOpen.IsEnterOpened[1] )
        {
			if(VectorCompare.VerDirToXAxis(m_HandOpen.Dir[0],VerToXAxisDirAdjust) &&
			   VectorCompare.VerDirToYAxis(m_HandOpen.PalmDir[0], VerToYAxisDirAdjust) &&
			   VectorCompare.VerDirToXAxis(m_HandOpen.Dir[1],VerToXAxisDirAdjust) &&
			   VectorCompare.VerDirToYAxis(m_HandOpen.PalmDir[1], VerToYAxisDirAdjust))
			{
                isFTF = true;
            }
        }
        return isFTF;
    }

    /// <summary>
    /// 一个方向是否垂直+x轴
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="threshold">阈值</param>
    /// <returns>垂直true</returns>
    bool VerDirToXAxis(Vector dir,float threshold)
    {
        bool isVer=false;
        float radian = dir.AngleTo(Vector.XAxis);

        if( radian > threshold && radian < (Mathf.PI-threshold))
        {
            isVer = true;
        }
        return isVer;
    }

    bool VerDirToYAxis(Vector dir, float threshold)
    {
        bool isVer = false;

        float radian = dir.AngleTo(Vector.YAxis);

        if(radian>threshold && radian<(Mathf.PI - threshold))
        {
            isVer = true;
        }
        return isVer;
    }
}
