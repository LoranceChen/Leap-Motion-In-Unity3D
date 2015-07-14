using UnityEngine;
using System.Collections;
using Leap;
using System.Collections.Generic;
using System;
/// <summary>
/// 手势：除拇指以外的四指弯曲
/// 匹配：2-3根手指（越接近垂直越难分辨），提供接口，默认识别3个手指，但是如果垂直方向的位置，指定2个手指即可
/// 冲突：该状态与单指模式冲突，解决方案：同一场景下设定优先级
/// </summary>
public class HandBend : MonoBehaviour
{
    [SerializeField] HandAndFingersPoint m_HandData;

    //开始弯曲和结束弯曲的触发事件，应该有时间淡入淡出设定，并不完善
    Action[] m_EnterBendFunc = new Action[2];
    Action[] m_ExitBendFunc = new Action[2];

    float[] m_BendingTime = new float[2];//此次弯曲的持续时间
    [SerializeField] bool[] m_IsEnterBended = new bool[2];//这次伸掌状态打开过

    int m_MatchNumber = 2;//手掌弯曲的状态需要几根手指匹配
   //const float FingerBendState_Radian = Mathf.PI*5.5f / 18 ;//55度

	Vector[] m_Dir = new Vector[2];//方向为掌心的指向，非弯曲状态设定为Zero
	float[] m_BendRadian = new float[2];//弯曲程度，用中指的弯曲度代替

    public bool[] IsEnterBended
    {
        get
        {
            return m_IsEnterBended;
        }
    }

    public float[] BendingTime
    {
        get
        {
            return m_BendingTime;
        }
    }

    public Vector[] Dir
    {
        get
        {
            return m_Dir;
        }
    }
    //手掌打开状态，事件注册接口
    public void AddOpenEnterMsg(Action leftOpen, Action rightOpen)
    {
        if (leftOpen != null)
        {
            m_EnterBendFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_EnterBendFunc[1] += rightOpen;
        }
    }

    //手掌打开结束状态，事件注册接口
    public void AddOpenExitMsg(Action leftOpen, Action rightOpen)
    {
        if (leftOpen != null)
        {
            m_ExitBendFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_ExitBendFunc[1] += rightOpen;
        }
    }

    /// <summary>
    /// set设定的时候限制在2,3两种取值
    /// </summary>
    public int MatchNumber
    {
        get
        {
            return m_MatchNumber;
        }
        set
        {
            if (value < 2)
                m_MatchNumber = 2;
            else if (value > 3)
                m_MatchNumber = 3;
        }
    }
	// Use this for initialization
	void Start () {
	
	}
	
	/// <summary>
	/// 每帧做手掌弯曲的判断
	/// </summary>
	void Update () 
    {
        BendCtrl(m_HandData.FingerDatas,m_HandData.PalmDatas);
	}

    void BendCtrl(Dictionary<Finger.FingerType, FingerData>[] dic,PointData[] palmDatas)
    {
        Vector[] bendDir = new Vector[2];
        bool[] isBend = new bool[2];
        isBend[0] = BendState(dic[0], palmDatas[0], m_MatchNumber, out bendDir[0]);
        isBend[1] = BendState(dic[1], palmDatas[1], m_MatchNumber, out bendDir[1]);

        //有几组数据就遍历几次-限1或2次
        int count= dic.Length;
        for (int handIndex = 0; handIndex < count; handIndex++)
        {
            //这个if - else 结构依据m_IsEnterBended-isLeftOpen表示的二维坐标系，而出现的四个象限。
            //此次左手伸掌状态的最开始，触发
            //当不处于m_IsEnterBended状态，并且现在左手是伸掌状态才会触发
            if (!m_IsEnterBended[handIndex] && isBend[handIndex])
            {
                if (m_EnterBendFunc[handIndex] != null)
                {
                    m_EnterBendFunc[handIndex]();
                }
                m_BendingTime[handIndex] = 0f;
                m_IsEnterBended[handIndex] = true;

                m_Dir[handIndex] = bendDir[handIndex];
            }

            //持续进行左手的伸掌状态
            //左手已经进入到伸掌状态，而且现在也是伸掌状态
            else if (m_IsEnterBended[handIndex] && isBend[handIndex])
            {
                m_BendingTime[handIndex] += Time.deltaTime;//该函数会在Update中调用。
                m_Dir[handIndex] = bendDir[handIndex];
            }

            //退出左手伸掌状态，这是此次持续伸掌的最后一瞬间伸掌
            //左手已经进入伸掌状态，并且现在不是伸掌状态
            else if (m_IsEnterBended[handIndex] && !isBend[handIndex])
            {
                if (m_ExitBendFunc[handIndex] != null)
                {
                    m_ExitBendFunc[handIndex]();
                }
                m_IsEnterBended[handIndex] = false;
                m_BendingTime[handIndex] = 0f;

                m_Dir[handIndex] = bendDir[handIndex];
            }

            //本身不是处于伸掌状态，而且此次判断也不是伸掌状态
            else
            {
                m_IsEnterBended[handIndex] = false;
                m_BendingTime[handIndex] = 0f;
                m_Dir[handIndex] = Vector.Zero;
            }
        }
    }
    /// <summary>
    /// 判定一个手掌是否处于弯曲状态。
    /// 手掌中有matchNumber个手指满足即可,实际设定为2-3个
    /// </summary>
    /// <param name="dic_FingersData"></param>
    /// <param name="matchNumber"></param>
    /// <param name="dir">【返回值】：如果匹配成功方向为掌心的方向</param>
    /// <returns>是否为弯曲状态</returns>
    bool BendState( Dictionary<Finger.FingerType, FingerData> dic_FingersData,PointData palmData, int matchNumber,out Vector dir )
    {
        bool isBend = false;
        dir = Vector.Zero;
        int count = 0;
        Dictionary<Finger.FingerType, FingerData> fingersOutThumb = new Dictionary<Finger.FingerType, FingerData>(dic_FingersData);
        fingersOutThumb.Remove(Finger.FingerType.TYPE_THUMB);

        var values = fingersOutThumb.Values;

		//float eulerAngule = 0f;
        //遍历四指，匹配个数满足设定个数认定手掌为弯曲，并且设定弯曲的方向为掌心方向。
        foreach (FingerData fingerData in values)
        {
			if (FingerMatch.BendState(fingerData))//,out eulerAngule))
            {
                count++;
            }
        }
		//print ("Bend Count："+count+" BendEuler:"+eulerAngule);
        if (count >= matchNumber)
        {
            isBend = true;
            dir = palmData.m_Direction;
        }
		
        return isBend;
    }
}
