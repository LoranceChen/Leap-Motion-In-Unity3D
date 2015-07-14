using UnityEngine;
using System.Collections;
using Leap;
using System.Collections.Generic;
using System;
/// <summary>
/// 此类为数据的模型，收集Leap中需要的数据，并转换成自己需要的格式
/// 整理手掌和指尖的数据信息
/// [可能的数据结构的优化]：这里用零向量表示无效数据，这个操作在ClearNotExistHandData方法中设定，
/// 	但是，在FingerData和使用PointData中增加额外的字段IsVaild来表示会更直接一些。
/// </summary>
public class HandAndFingersPoint : MonoBehaviour 
{
	const int BUFFER_MAX=5;
	Controller m_LeapCtrl;

    public E_HandInAboveView m_AboveView = E_HandInAboveView.None;
    
	//手指-数据 ,[0]表示左手，[1]表示右手
	private Dictionary<Finger.FingerType,FingerData>[] m_FingerDatas = new Dictionary<Finger.FingerType, FingerData>[2];
	//buffer,[0]表示左手，[1]表示右手,[,n](n属于0,3，表示第n次缓存)
	private Dictionary<Finger.FingerType,FingerData>[,] m_FingerDatasBuffer=new Dictionary<Finger.FingerType, FingerData>[2,BUFFER_MAX];
	private int m_CurBufIndex=0;
	//palm 0：左手 和1：右手
	private PointData[] m_PalmDatas = new PointData[2];
	
	private readonly PointData m_DefaultPointData = new PointData(Vector.Zero, Vector.Zero);
    private readonly FingerData m_DefaultFingerData = new FingerData(Vector.Zero,Vector.Zero,Vector.Zero);

	//中间量：不再使用。不能作为一个中间存储变量，它会被改变。
	//Dictionary<Finger.FingerType,FingerData> m_FingerDataMiddle = new Dictionary<Finger.FingerType, FingerData> ();

    public Dictionary<Finger.FingerType, FingerData>[] FingerDatas
    {
        get 
        {
            return m_FingerDatas;
        }
    }

    public PointData[] PalmDatas 
    {
        get
        {
            return m_PalmDatas;
        }
    }

	void AddDefaultPalmsData()
	{
		m_PalmDatas [0]=m_DefaultPointData;
		m_PalmDatas [1]=m_DefaultPointData;
	}

	/// <summary>
	/// 设定手指的
	/// </summary>
	void AddDefaultFingersData()
	{
		DicAddDefaultData(m_FingerDatas [0]) ;
		DicAddDefaultData(m_FingerDatas [1]) ;
		for(int i=0;i<2;i++)
		{
			for(int j=0;j<BUFFER_MAX;j++)
			{
				DicAddDefaultData(m_FingerDatasBuffer[i,j]);
			}
		}
	}

	/// <summary>
	/// 初始化Dictionary<Finger.FingerType, TheData>的值。
	/// </summary>
	/// <param name="dic">Dic.</param>
	void DicAddDefaultData(Dictionary<Finger.FingerType, FingerData> dic)
	{
		dic.Add (Finger.FingerType.TYPE_INDEX,m_DefaultFingerData);
		dic.Add (Finger.FingerType.TYPE_MIDDLE,m_DefaultFingerData);
		dic.Add (Finger.FingerType.TYPE_PINKY,m_DefaultFingerData);
		dic.Add (Finger.FingerType.TYPE_RING,m_DefaultFingerData);
		dic.Add (Finger.FingerType.TYPE_THUMB,m_DefaultFingerData);
	}

	/// <summary>
	/// 设定Dictionary<Finger.FingerType, TheData>的值为默认值。
	/// </summary>
	/// <param name="dic">Dic.</param>
	void DicUseDefaultData(Dictionary<Finger.FingerType, FingerData> dic)
	{
		dic[Finger.FingerType.TYPE_INDEX]=m_DefaultFingerData;
		dic[Finger.FingerType.TYPE_MIDDLE]=m_DefaultFingerData;
		dic[Finger.FingerType.TYPE_PINKY]=m_DefaultFingerData;
		dic[Finger.FingerType.TYPE_RING]=m_DefaultFingerData;
		dic[Finger.FingerType.TYPE_THUMB]=m_DefaultFingerData;
	}

	void Awake()
	{
        ////分配空间
        m_FingerDatas[0] = new Dictionary<Finger.FingerType, FingerData>();
        m_FingerDatas[1] = new Dictionary<Finger.FingerType, FingerData>();

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < BUFFER_MAX; j++)
            {
                m_FingerDatasBuffer[i, j] = new Dictionary<Finger.FingerType, FingerData>();
            }
        }

		//制作默认数据
		//DicAddDefaultData (m_FingerDataMiddle);

	}

	void Start()
	{
		m_LeapCtrl = LeapDriver.GetLeapCtrl ();	
	}

	void Update()
	{
		//清除（确切说是填充）不存在手掌信息数据。因为手没有被检测到，但这时数据需要更新。所以有这样一个填充判断和控制。
		ClearNotExistHandData ();

		//掌心信息
		SavePalmsFrame ();

		//手指信息
        SaveFingersFrame();


        //记录先进入视野的手,记录到m_AboveView中
        SaveAboveViewHand();
	}

	void FixedUpdate()
	{
		ShowBaxis ();
		//显示
		ShowAllFingersData ();
       // ShowIndexFinger();
		//测试数据的可用性
		//TestFingerData ();
	}

    /// <summary>
    /// 记录第一次进入检测的手是左手还是右手.
    /// 1.枚举为None时
    ///     1.1检测到一只手，指定为该手
    ///     1.2检测到两只手，指定[0]下标
    ///     1.3检测到0只手，指定None
    /// 2.枚举为Right/Legt时
    ///     2.1检测到1只手，指定为该手
    ///     2.2检测到2之手，不变
    ///     2.3检测到0只收，设定为None
    /// </summary>
    void SaveAboveViewHand()
    {
        Frame frame = m_LeapCtrl.Frame();
        HandList hands = frame.Hands;
        int count = hands.Count;

        if(m_AboveView== E_HandInAboveView.None)
        {
            if(count ==1||count==2)
            {
                bool isRight = hands[0].IsRight;
                if(isRight)
                {
                    m_AboveView = E_HandInAboveView.Right;
                }
                else
                {
                    m_AboveView = E_HandInAboveView.Left;
                }
            }
        }
        else
        {
            if(count==1)
            {
                bool isRight = hands[0].IsRight;
                if(isRight)
                {
                    m_AboveView = E_HandInAboveView.Right;
                }
                else
                {
                    m_AboveView = E_HandInAboveView.Left;
                }
            }
            else if(count==0)
            {
                m_AboveView = E_HandInAboveView.None;
            }
        }
    }
	/// <summary>
	/// 保存五个手指的信息。
	/// </summary>
	void SaveFingersFrame()
	{
		Frame frame = m_LeapCtrl.Frame ();

		HandList hands = frame.Hands;

		foreach ( var hand in hands )
        {
            SaveFingerDataWithHandIndex( hand );
        }
		
		m_CurBufIndex=(m_CurBufIndex+1)%(BUFFER_MAX-1);
	}

	/// <summary>
	/// 保存手的信息到指定的缓存中，也保存在当前数据中
	/// </summary>
	/// <param name="handIndex">Hand index.</param>
	/// <param name="hand">Hand.</param>
	/// <param name="curBufIndex">Current buffer index.</param>
	void SaveFingerDataWithHandIndex(Hand hand)                            
	{
		//做空判断好恶心，既然要求传入一个Hand，为什么你要传入一个null呢。
		//这里不做null的判断也不会有问题
        //if (hand != null)
        //{
        bool isLeft = hand.IsLeft;
        int handIndex = isLeft ? 0 : 1;

        foreach (Finger finger in hand.Fingers)
        {
            Finger.FingerType fingerType = finger.Type();

            Vector fingerDir = finger.Direction;

            // Bone bone = finger.Bone(Bone.BoneType.TYPE_DISTAL);
            // Vector distalPos = bone.Center;
            Vector distalPos = finger.TipPosition;
            //记录根骨位置
            Vector metacarpalPos = finger.Bone(Bone.BoneType.TYPE_METACARPAL).Center;

            //如果是拇指，用近端骨指的位置代替
            if (finger.Type()==Finger.FingerType.TYPE_THUMB)
            {
                metacarpalPos = finger.Bone(Bone.BoneType.TYPE_PROXIMAL).Center;
            }

            //将数据保存到m_FingerDatas中，以及buffer中。
            FingerData fingerData = new FingerData(distalPos,fingerDir, metacarpalPos);

            //Vector3 fingerPos = VectorTrans.ToUnityVector3(fingerData.m_Position);
            //s Vector3 fingerDistalPos = VectorTrans.ToUnityVector3(fingerData.m_Point.m_Position);
            SaveFingerData(handIndex, fingerType, fingerData);                                                   
        }
        //}
	}

	/// <summary>
	/// 保存指定的手指到指定的位置中
	/// </summary>
	/// <param name="handNumber">索引号 0表示左手</param>
    /// 
	/// <param name="finger">手指信息</param>
	void SaveFingerData(int handIndex,
                        Finger.FingerType fingerType,
				        FingerData fingerData)
	{
		//将data保存或者覆盖到m_FingerDatas中
		if(m_FingerDatas[handIndex].ContainsKey(fingerType))
		{
            m_FingerDatas[handIndex][fingerType] = fingerData;
		}
		else
		{
            m_FingerDatas[handIndex].Add(fingerType, fingerData);
		}
		
		//保存或者覆盖到buffer中
		if(m_FingerDatasBuffer[handIndex,m_CurBufIndex].ContainsKey(fingerType))
		{
            m_FingerDatasBuffer[handIndex, m_CurBufIndex][fingerType] = fingerData;
		}
		else
		{
            m_FingerDatasBuffer[handIndex, m_CurBufIndex].Add(fingerType, fingerData);
		}
	}

	/// <summary>
	/// 获取掌心的位置和法向量,记录到<c>m_PalmDatas</c>中
	/// </summary>
    void SavePalmsFrame()
	{
		Frame frame = m_LeapCtrl.Frame ();

        //将左手的信息记录到0中，将右手的信息记录到1中
        foreach (var hand in frame.Hands)
        {
            if (hand.IsLeft)
            {
                m_PalmDatas[0].m_Position = hand.PalmPosition;
                m_PalmDatas[0].m_Direction = hand.PalmNormal;
            }
            else if(hand.IsRight)
            {
                m_PalmDatas[1].m_Position = hand.PalmPosition;
                m_PalmDatas[1].m_Direction = hand.PalmNormal;
            }
        }
	}

	/// <summary>
	/// 让不存在手对应的记录，设置为DefaultData
	/// 会在Update中调用，这个方法只是功能性单纯的封装。并没有自己的层次结构
	/// </summary>
	void ClearNotExistHandData()
	{
		Frame frame = m_LeapCtrl.Frame ();
		HandList hands = frame.Hands;
	 	int count = hands.Count;

		//筛选出不存在的手掌,并将这个手掌的信息设定为空
		//一个手掌，或零个手掌
		if (count == 0) 
		{
			//将[0],[1]索引的数据都清空
			for(int i=0;i<2;i++)
			{
				ClearTheHandData(i);
			}
		}
		//1个手掌
		else if(count==1)
		{
			//查找哪个手掌是没有被检测到
			int emptyHandIndex = hands[0].IsLeft?//存在的是左手
								 1://空的是右手，返回1
								 0;//空的是左手，返回0
			ClearTheHandData(emptyHandIndex);
		}
	}
	/// <summary>
	/// 清除一个手掌的信息
	/// 包括：1.手掌信息
	/// 	 2.当前五指信息
	///		 3.缓存五指信息 
	/// 
	/// 参数：0表示左手，1表示右手
	/// </summary>
	void ClearTheHandData(int handIndex)
	{
		//清除手掌
		m_PalmDatas [handIndex].Set (m_DefaultPointData);

		//清除 当前的五指信息。也可遍历所有含有的键值对
		//没有找到很好改变值的方式，如果每次判断isContainsKey，然后在改变值为Default，则需要遍历5次虽然节省了内存，但是增加的Cpu的处理时间.
		//清空后，在需要的时候产生一个新对象。
		m_FingerDatas[handIndex].Clear();

		//清除 当前缓存的信息
		m_FingerDatasBuffer [handIndex, m_CurBufIndex].Clear();

	}

    /// <summary>
    /// 获取手指的缓存数据,可能为空。本来想在运动的时候检测，但考虑到抖动因素，所以决定由动作本身保存缓存数据
    /// </summary>
    /// <returns></returns>
    public Dictionary<Finger.FingerType,FingerData> GetPreviousBufFingerData(int handIndex)
    {
        //Dictionary<Finger.FingerType, FingerData> a;
        int preBufIndex =  (m_CurBufIndex-1) < 0?BUFFER_MAX:m_CurBufIndex-1;//3->2,1->0,0->Max-1
        return m_FingerDatasBuffer[handIndex,preBufIndex];
    }

    //___________________Test_______________
	/// <summary>
	/// test Hand.Direction,
	/// 该方法只有在手掌平摊伸直时有意义。
	/// </summary>
	void HandDirection()
	{
		Frame frame = m_LeapCtrl.Frame ();
		Hand hand = frame.Hands [0];
		Vector3 v1 = VectorTrans.ToUnityVector3 (hand.PalmPosition);
		Vector3 v2 = VectorTrans.ToUnityVector3 (hand.Direction);
		Debug.DrawRay (v1,
		               v2*100,
		               Color.red);
		print (v1+","+v2);
	}

	/// <summary>
	/// 显示Vector的方向和大小
	/// </summary>
	/// <param name="pos">Position.</param>
	/// <param name="dir">Dir.</param>
	void ShowTheDatasPosAndDir(Vector pos,Vector dir)
	{
		UnityEngine.Vector3 unityDir = VectorTrans.ToUnityVector3 (dir);
		UnityEngine.Vector3 unityPos = VectorTrans.ToUnityVector3 (pos);

		Debug.DrawRay (unityPos, unityDir*10, Color.blue);
		print (unityPos+","+unityDir);
	}


	void ShowBaxis()
	{
		Debug.DrawRay (Vector3.zero, Vector3.forward*100, Color.blue);
		Debug.DrawRay (Vector3.zero, Vector3.right*100, Color.red);
		Debug.DrawRay (Vector3.zero, Vector3.up*100, Color.green);
	}

	//显示所有手指缓存数据
	void ShowAllFingersBufData()
	{

	}

	//显示所有手指数据
	void ShowAllFingersData()
	{
		//左右手
		for (int i=0; i<2; i++) 
		{
			//遍历所有包含的手指
			if(m_FingerDatas[i]!=null)
			{
				var fingerDataValues = m_FingerDatas[i].Values;

                foreach (FingerData data in fingerDataValues)
                {
                    ShowFingerData(i,m_PalmDatas[i], data);
                }
			}
		}

	}

	//显示单个手指的数据
	//1.掌心到手指的连线-红色
	//2.手指的方向-蓝色
	void ShowFingerData (int handIndex,PointData palmData,FingerData fingerData)
	{
		Vector3 handPos = VectorTrans.ToUnityVector3 (palmData.m_Position);
        Vector3 handDir = VectorTrans.ToUnityVector3 (palmData.m_Direction);

		Vector3 fingerPos = VectorTrans.ToUnityVector3 (fingerData.m_Position);
        Vector3 fingerDistalPos = VectorTrans.ToUnityVector3 (fingerData.m_Point.m_Position);
		Vector3 fingerDir = VectorTrans.ToUnityVector3 (fingerData.m_Point.m_Direction);

		Debug.DrawLine (handPos,fingerPos,Color.red);
        Debug.DrawLine(fingerPos, fingerDistalPos, Color.red);
        //手心到指尖的连线
        Debug.DrawLine(handPos, fingerDistalPos, Color.yellow);

        float vecLength = (fingerPos - fingerDistalPos).magnitude;
        Debug.DrawRay(fingerDistalPos, fingerDir * vecLength * 0.1f, Color.blue);
        Debug.DrawRay(handPos, handDir * vecLength * 0.1f, Color.blue);
	}

    //显示食指的骨根位置，近端位置，和指尖的位置及其连线
    void ShowIndexFinger()
    {
        Vector3 pos0=Vector3.zero;
        Vector3 pos1=Vector3.zero;
        Vector3 pos2=Vector3.zero;
        HandList hs= m_LeapCtrl.Frame().Hands;
        foreach(var f in hs.Leftmost.Fingers)
        {
            if(f.Type()==Finger.FingerType.TYPE_INDEX)
            {
                pos0 = VectorTrans.ToUnityVector3( f.Bone(Bone.BoneType.TYPE_METACARPAL).Center);
                pos1 = VectorTrans.ToUnityVector3(f.Bone(Bone.BoneType.TYPE_PROXIMAL).Center);
                pos2 = VectorTrans.ToUnityVector3(f.Bone(Bone.BoneType.TYPE_DISTAL).Center);
            }
        }
        Debug.DrawLine(pos0, pos1, Color.red);
        Debug.DrawLine(pos0, pos2, Color.yellow);
        Debug.DrawLine(pos1, pos2, Color.green);
    }

	///适配性检测
	/// 观察手指可用的行为
	void TestVaildData()
	{

	}

	/// <summary>
	/// 检测指定手指的可用性
	/// </summary>
	void TestFingerData()
	{
		//左右手
		for (int i=0; i<2; i++) 
		{
			//遍历所有包含的手指
			if(m_FingerDatas[i]!=null)
			{
				//获取index手指的数据
				//PointData? data = GetDicValue(m_FingerDatas[i],Finger.FingerType.TYPE_INDEX);
				//if(data.HasValue)
				//{
				//	PointData trueData=(PointData)data;

				//}

			}
		}
	}
	
	///获取dic中的数据
	PointData? GetDicValue(Dictionary<Finger.FingerType,PointData> dic,Finger.FingerType fingerType)
	{
		if (dic.ContainsKey (fingerType))
			return dic [fingerType];
		else 
			return null;
	}
}
