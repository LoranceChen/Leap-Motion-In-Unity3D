using UnityEngine;
using System.Collections;
using Leap;

///Log1:手势废弃，Leap API不能正确识别手指的前方，尤其是整根手指从骨根弯曲，不明白Finger.Direction的定义。
///在这里，不能正确匹配手指伸直的判定
/// <summary>
/// 捏手势判定
/// 可用的信息：
///     1.食指和拇指指尖之间的距离
///     2.是否是捏手势
/// 【注意】捏手势的优先级应当设定为很低，因为它匹配成功只需要食指伸直。
/// </summary>
public class HandPinchGroup : MonoBehaviour 
{
    [SerializeField] HandAndFingersPoint m_HandData;
    bool[] m_IsHandPinch = new bool[2];
    float[] m_Distance = new float[2];
    readonly float EnterDisThreshold;//要大于这个阈值才认定进入

    public bool[] IsHandPinch
    {
        get
        {
            return m_IsHandPinch;
        }
    }
    public float[] Distance
    {
        get
        {
            return m_Distance;
        }
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
        for (int i = 0; i < 2; i++)
        {
            //含有中指和食指的数据
            if (m_HandData.FingerDatas[i].ContainsKey(Finger.FingerType.TYPE_INDEX) &&
                m_HandData.FingerDatas[i].ContainsKey(Finger.FingerType.TYPE_THUMB))
            {
                FingerData indexFingerData = m_HandData.FingerDatas[i][Finger.FingerType.TYPE_INDEX];
                Vector indexPos = indexFingerData.m_Point.m_Position;
                Vector thumbPos = m_HandData.FingerDatas[i][Finger.FingerType.TYPE_THUMB].m_Point.m_Position;

                //print("IndexToThumb:" + indexPos.DistanceTo(thumbPos) );
                print((indexFingerData.m_Point.m_Position-indexFingerData.m_Position).AngleTo(indexFingerData.m_Point.m_Direction)*180/Mathf.PI);
                if (FingerMatch.StrightState(indexFingerData))
                {
                    m_IsHandPinch[i] = true;
                    m_Distance[i] = indexPos.DistanceTo(thumbPos);
                   
                }
                else
                {
                    m_IsHandPinch[i] = false;
                    m_Distance[i] = 0f;
                }
            }
            else
            {
                m_IsHandPinch[i] = false;
                m_Distance[i] = 0f;
            }
        }
	}
}
