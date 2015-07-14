using UnityEngine;
using System.Collections;
using Leap;
using System;
using System.Collections.Generic;

/// <summary>
/// 匹配是摊开手的状态,通知相应的操作。在动作匹配中也用到
/// 细分为水平摊开，垂直摊开，一般摊开
/// 数据匹配方式：除大拇指以外的四个手指中有3个手指是伸直状态，并且中指必须是伸直状态
/// </summary>
public class HandOpen : MonoBehaviour
{
	[SerializeField] HandAndFingersPoint m_HandData;

	Action[] m_EnterOpenFunc=new Action[2];
	Action[] m_ExitOpenFunc=new Action[2];
	
    float[] m_OpeningTime =new float[2];
    [SerializeField] bool[] m_IsEnterOpened = new bool[2];//这次伸掌状态时候打开过
    Vector[] m_Dir=new Vector[2];//伸掌的手掌方向用中指指尖方向表示,zero表示非伸掌状态
    Vector[] m_PalmPos = new Vector[2];
    Vector[] m_PalmDir = new Vector[2];
	//设定匹配时手指需要满足的个数
	int m_MatchNumber = 3;//2或3
    
    //中间变量
    Vector[] m_OpenDir = new Vector[2];

	//borderSettings
	// const float FingerStrightState_Radian = Mathf.PI/12;//15度

	public Vector[] Dir 
    {
        get {
            return m_Dir;
        }
    }

    public Vector[] PalmPos
    {
        get
        {
            return m_PalmPos;
        }

    }

    public Vector[] PalmDir
    {
        get
        {
            return m_PalmDir;
        }

    }

    public bool[] IsEnterOpened
    {
        get {
            return m_IsEnterOpened;
        }
    }

    public float[] OpeningTime
    {
        get
        {
            return m_OpeningTime;
        }
    }

	/// <summary>
	/// 只能在下一帧起作用
	/// </summary>
	/// <value>The set match number.</value>
	public int SetMatchNumber
	{
		set
		{
			m_MatchNumber = value;
		}
	}
    //手掌打开状态，事件注册接口
    public void AddOpenEnterMsg(Action leftOpen, Action rightOpen)
    {
        if (leftOpen != null)
        {
            m_EnterOpenFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_EnterOpenFunc[1] += rightOpen;
        }
    }

    //手掌打开结束状态，事件注册接口
    public void AddOpenExitMsg(Action leftOpen, Action rightOpen)
    {
        if (leftOpen != null)
        {
            m_ExitOpenFunc[0] += leftOpen;
        }

        if (rightOpen != null)
        {
            m_ExitOpenFunc[1] += rightOpen;
        }
    }

	void Awake()
	{
        if (m_HandData == null)
            Debug.LogError("It's no ref");
	}

	void Update()
	{
//		print ("m_IsEnterOpened0:"+m_IsEnterOpened[0]+"  1:"+m_IsEnterOpened[1]);

        OpenCtrl( m_HandData.FingerDatas ,m_HandData.PalmDatas);
	}

    /// <summary>
    /// 判断左右手是否处于伸掌状态，触发相应事件和消息，记录相应的值
    /// </summary>
    /// <param name="dic">代表左右手的数据源</param>
    void OpenCtrl(Dictionary<Finger.FingerType,FingerData>[] dic,PointData[] palmData)
    {

        bool[] isOpen = new bool[2];

        isOpen[0] = OpenState(dic[0],palmData[0], out m_OpenDir[0],out m_PalmPos[0],out m_PalmDir[0]);
        isOpen[1] = OpenState(dic[1], palmData[1], out m_OpenDir[1], out m_PalmPos[1], out m_PalmDir[1]);

        //int count = dic.Length;
        for (int handIndex = 0; handIndex < 2; handIndex++)
        {
            //这个if - else 结构依据m_IsEnterOpened-isLeftOpen表示的二维坐标系，而出现的四个象限。
            //此次左手伸掌状态的最开始，触发
            //当不处于m_IsEnterOpened状态，并且现在左手是伸掌状态才会触发
            if (!m_IsEnterOpened[handIndex] && isOpen[handIndex])
            {
                if (m_EnterOpenFunc[handIndex] != null)
                {
                    m_EnterOpenFunc[handIndex]();
                }
                m_OpeningTime[handIndex] = 0f;
                m_IsEnterOpened[handIndex] = true;

				m_Dir[handIndex] = m_OpenDir[handIndex];
               // m_PalmPos[handIndex] = m_PalmPos[handIndex];

            }

            //持续进行左手的伸掌状态
            //左手已经进入到伸掌状态，而且现在也是伸掌状态
            else if (m_IsEnterOpened[handIndex] && isOpen[handIndex])
            {
                m_OpeningTime[handIndex] += Time.deltaTime;//该函数会在Update中调用。
				m_Dir[handIndex] = m_OpenDir[handIndex];
               // m_PalmPos[handIndex] = m_PalmPos[handIndex];
            }

            //退出左手伸掌状态，这是此次持续伸掌的最后一瞬间伸掌
            //左手已经进入伸掌状态，并且现在不是伸掌状态
            else if (m_IsEnterOpened[handIndex] && !isOpen[handIndex])
            {
                if (m_ExitOpenFunc[handIndex] != null)
                {
                    m_ExitOpenFunc[handIndex]();
                }
                m_IsEnterOpened[handIndex] = false;
                m_OpeningTime[handIndex] = 0f;

                m_Dir[handIndex] = m_OpenDir[handIndex];
               // m_PalmPos[handIndex] = m_PalmPos[handIndex];
            }
            //本身不是处于伸掌状态，而且此次判断也不是伸掌状态
            else
            {
                m_IsEnterOpened[handIndex] = false;
                m_OpeningTime[handIndex] = 0f;
                m_Dir[handIndex] = Vector.Zero;
                m_PalmPos[handIndex] = Vector.Zero;
                m_PalmDir[handIndex] = Vector.Zero;
            }
        }
    }

    /// <summary>
    /// 通过给定手指数据，判断是否处于伸手状态
    /// 当三个手指符合不包括拇指，判定为伸掌状态
    /// </summary>
    /// <param name="fingerData"></param>
    /// <param name="?"></param>
    bool OpenState( Dictionary<Finger.FingerType,FingerData> dic_FingersData,PointData palmData,out Vector dir,out Vector palmPos,out Vector palmDir)
    {
        bool isOpen = false;
        dir = Vector.Zero;
        palmPos = Vector.Zero;
        palmDir = Vector.Zero;
        int count = 0;
        Dictionary<Finger.FingerType, FingerData> fingersOutThumb = new Dictionary<Finger.FingerType, FingerData>(dic_FingersData);
        fingersOutThumb.Remove(Finger.FingerType.TYPE_THUMB);

        var values = fingersOutThumb.Values;
            
        foreach( FingerData fingerData in values )
        {
			if (FingerMatch.StrightState(fingerData))
            {
                count++;
            }
        }
//		print ("FingerStright Count："+count);
		if (count >= m_MatchNumber)
        {

            isOpen = true;
        }

        //指定输出的伸手方向为中指指向
        //如果没有识别中指，判定为false
        //如果中指不是伸直状态，判定为false
        if (fingersOutThumb.ContainsKey(Finger.FingerType.TYPE_MIDDLE))
        {
            FingerData middleFingerData = fingersOutThumb[Finger.FingerType.TYPE_MIDDLE];
			if (FingerMatch.StrightState(middleFingerData))
            {
                dir = middleFingerData.m_Point.m_Direction;
                palmPos = palmData.m_Position;
                palmDir = palmData.m_Direction;
            }
            else 
            {
                isOpen = false;
            }
        }
        else 
        {
            isOpen = false;
        }

        return isOpen;
    }

    /// <summary>
    /// 计算当前手掌伸开的个数
    /// 返回个数
    /// </summary>
    /// <param name="index">记录手掌打开的索引值，返回值大于0时有效</param>
    /// <returns>当前处于摊开的手掌个数</returns>
    public int OpenNumber(out int index)
    {
        int number = 0;
        index =0;
        if(m_IsEnterOpened[0]==true)
        {
            if(m_IsEnterOpened[1]==true)
            {
                number = 2;
            }
            else
            {
                number = 1;
                index = 0;
            }
        }
        else if(m_IsEnterOpened[1]==true)
        {
            number = 1;
            index = 1;
        }
        else
        {
            number = 0;
        }
        return number;
    }
}

