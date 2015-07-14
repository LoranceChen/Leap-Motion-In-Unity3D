using UnityEngine;
using System.Collections;
using Leap;
using System;
using System.Collections.Generic;

/// <summary>
/// 手势：点击模式，拇指不考虑。食指处于伸直状态，其他三指处于弯曲状态
/// 匹配方案：食指伸直，其他三个手指中有2-3个处于弯曲状态（）
/// </summary>
public class HandFingerPoint : MonoBehaviour 
{
    [SerializeField] HandAndFingersPoint m_HandData;

    //开始弯曲和结束弯曲的触发事件，应该有时间淡入淡出设定，并不完善
    Action[] m_EnterPointFunc = new Action[2];
    Action[] m_ExitPointFunc = new Action[2];

    float[] m_PointingTime = new float[2];//此次指向手势的持续时间
    [SerializeField] bool[] m_IsEnterPointed = new bool[2];//这次伸掌状态打开过

    //匹配阈值设定
    int m_MatchNumber = 2;//手掌弯曲的状态需要几根手指匹配，在观察器
    //const float FingerBendState_Radian = Mathf.PI * 7 / 18;//70度
    //const float FingerStrightState_Radian = Mathf.PI / 12;//15度
    Vector[] m_Dir = new Vector[2];//方向为食指的指向，非弯曲状态设定为Zero
    Vector[] m_Pos = new Vector[2];//食指指尖的位置数据
    public bool[] IsEnterPointed
    {
        get
        {
            return m_IsEnterPointed;
        }
    }

    public float[] PointingTime
    {
        get
        {
            return m_PointingTime;
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
            m_EnterPointFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_EnterPointFunc[1] += rightOpen;
        }
    }

    //手掌打开结束状态，事件注册接口
    public void AddOpenExitMsg(Action leftOpen, Action rightOpen)
    {
        if (leftOpen != null)
        {
            m_ExitPointFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_ExitPointFunc[1] += rightOpen;
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

    /// <summary>
    /// 每帧做手掌弯曲的判断
    /// </summary>
    void Update()
    {
        IndexPointCtrl(m_HandData.FingerDatas);
    }

    /// <summary>
    /// 多个手掌的手指数据是否处于食指指向状态，并对类的属性做出修改
    /// </summary>
    /// <param name="dic"></param>
    void IndexPointCtrl(Dictionary<Finger.FingerType, FingerData>[] dic)
    {
        Vector[] indexPointDir = new Vector[2];
        bool[] isIndexPoint = new bool[2];
        isIndexPoint[0] = IndexPointState(dic[0], out indexPointDir[0]);
        isIndexPoint[1] = IndexPointState(dic[1], out indexPointDir[1]);

        //有几组数据就遍历几次-限1或2次
        int count = dic.Length;
        for (int handIndex = 0; handIndex < count; handIndex++)
        {
            //这个if - else 结构依据m_IsEnterBended-isLeftOpen表示的二维坐标系，而出现的四个象限。
            //此次左手伸掌状态的最开始，触发
            //当不处于m_IsEnterBended状态，并且现在左手是伸掌状态才会触发
            if (!m_IsEnterPointed[handIndex] && isIndexPoint[handIndex])
            {
                if (m_EnterPointFunc[handIndex] != null)
                {
                    m_EnterPointFunc[handIndex]();
                }
                m_PointingTime[handIndex] = 0f;
                m_IsEnterPointed[handIndex] = true;

                m_Dir[handIndex] = indexPointDir[handIndex];
            }

            //持续进行左手的伸掌状态
            //左手已经进入到伸掌状态，而且现在也是伸掌状态
            else if (m_IsEnterPointed[handIndex] && isIndexPoint[handIndex])
            {
                m_PointingTime[handIndex] += Time.deltaTime;//该函数会在Update中调用。
                m_Dir[handIndex] = indexPointDir[handIndex];
            }

            //退出左手伸掌状态，这是此次持续伸掌的最后一瞬间伸掌
            //左手已经进入伸掌状态，并且现在不是伸掌状态
            else if (m_IsEnterPointed[handIndex] && !isIndexPoint[handIndex])
            {
                if (m_ExitPointFunc[handIndex] != null)
                {
                    m_ExitPointFunc[handIndex]();
                }
                m_IsEnterPointed[handIndex] = false;
                m_PointingTime[handIndex] = 0f;

                m_Dir[handIndex] = indexPointDir[handIndex];
            }

            //本身不是处于伸掌状态，而且此次判断也不是伸掌状态
            else
            {
                m_IsEnterPointed[handIndex] = false;
                m_PointingTime[handIndex] = 0f;
                m_Dir[handIndex] = Vector.Zero;
            }
        }
    }

    /// <summary>
    /// 判定一个手掌是否处于食指指向状态。
    /// 手掌中有matchNumber个手指满足即可,实际设定为2个。
    /// </summary>
    /// <param name="dic_FingersData"></param>
    /// <param name="matchNumber"></param>
    /// <param name="dir">【返回值】：如果匹配成功方向为掌心的方向</param>
    /// <returns>是否为食指指向状态。</returns>
    bool IndexPointState(Dictionary<Finger.FingerType, FingerData> dic_FingersData, out Vector dir)
    {
        bool isBend = false;
        dir = Vector.Zero;
        int count = 0;
        Dictionary<Finger.FingerType, FingerData> fingersOutThumbAndIndex = new Dictionary<Finger.FingerType, FingerData>(dic_FingersData);
        fingersOutThumbAndIndex.Remove(Finger.FingerType.TYPE_THUMB);

        //如果不存在食指的信息就不需要继续判断了
        if (fingersOutThumbAndIndex.ContainsKey(Finger.FingerType.TYPE_INDEX))
        {
            FingerData indexFinger = fingersOutThumbAndIndex[Finger.FingerType.TYPE_INDEX];
            //食指处于伸直状态才继续进行判断
            if (FingerMatch.StrightState(indexFinger))
            {
                fingersOutThumbAndIndex.Remove(Finger.FingerType.TYPE_INDEX);

                var values = fingersOutThumbAndIndex.Values;
                //遍历四指，匹配个数满足设定个数认定手掌为弯曲，并且设定弯曲的方向为掌心方向。
                foreach (FingerData fingerData in values)
                {
                    if (FingerMatch.BendState(fingerData))
                    {
                        count++;
                    }
                }

				//log
				//print("FingerPoint bend count:"+count);

                //判定弯曲手指的个数是否符合要求
                //判定食指是否是伸直状态
                if (count >= m_MatchNumber)
                {
                    isBend = true;
                    dir = indexFinger.m_Point.m_Direction;//食指的方向
                }
            }
        }
        return isBend;
    }
}
