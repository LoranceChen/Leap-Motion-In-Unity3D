using UnityEngine;
using System.Collections;
using System;
using Leap;
/// <summary>
/// 定位：水平前向
/// 从伸手到握拳姿势
/// 匹配：进入初始状态后，经过一个固定时间，判定手处于弯曲状态。表示成立。
/// 之所以不用手指的弯曲程度计算，是因为不准
/// </summary>
public class HandOpenToFist : MonoBehaviour 
{
	[SerializeField] HandBend m_HandBend;
	[SerializeField] HandOpen m_HandOpen;
	//Update
	readonly float CheckStepTime = 0.2f;
	float m_CurCheckStepTime;

	//enter
	float m_CurEnterDelayTime;
	readonly float EnterDelayTimeThrehold=0.3f;
	Action m_OnEnterFunc;
	[SerializeField] bool m_IsEnteredOpen;

	//中间过程
	readonly float WaitBendTimeThreshold=2f;//进入初始状态1s后，若为弯曲状态，则说明动作成立。

	//end
	Action m_OnEndFunc;

	//record
	float m_PreviousRadian;

	//threshold
	readonly float ForwardThreshold=Mathf.PI /9;//指尖方向到-z轴的阈值
	readonly float DownThreshold=Mathf.PI /9;//指尖方向到-z轴的阈值

	public void RegisterFunc(Action onEnter,Action onEnd)
	{
		if(onEnd!=null)
		{
			m_OnEndFunc+=onEnd;
		}
		if(onEnter!=null)
		{
			m_OnEnterFunc+=onEnter;
		}
	}

	public void CancelFunc(Action onEnter,Action onEnd)
	{
		if(onEnd!=null)
		{
			m_OnEndFunc-=onEnd;
		}
		if(onEnter!=null)
		{
			m_OnEnterFunc-=onEnter;
		}
	}


	// Update is called once per frame
	void Update () {
		m_CurCheckStepTime += Time.deltaTime;
		m_CurEnterDelayTime += Time.deltaTime;

		//每隔一定时间做一次检测
		if(m_CurCheckStepTime>CheckStepTime)
		{
			m_CurCheckStepTime=0f;
			//没有进入判定状态
			if(!m_IsEnteredOpen)
			{
				int handIndex;
				//是初始状态
				if(EnterOpenState(out handIndex))
				{
					//满足了延迟时间
					if(m_CurEnterDelayTime>EnterDelayTimeThrehold)
					{
						m_CurEnterDelayTime=0f;

						//设定初始状态
						m_IsEnteredOpen=true;
						if(m_OnEnterFunc!=null)
						{
							m_OnEnterFunc();
						}
						//等待一个弯曲判定
						StartCoroutine(WaitForBendState(WaitBendTimeThreshold,handIndex));
					}
				}
				//不是初始状态
				else
				{
					m_CurEnterDelayTime=0f;
				}
			}
		}
	}

	/// <summary>
	/// 判断是否进入伸掌状态
	/// 没有考虑多只手的检测，影响是，右手有最终判定权。
	/// </summary>
	/// <param name="handIndex">选定的手的索引,返回true时有效</param>
	/// <returns><c>true</c>, if open state was entered, <c>false</c> otherwise.</returns>
	bool EnterOpenState(out int handIndex)
	{
		bool isEnter = false;
		handIndex = -1;
		for(int i=0;i<2;i++)
		{
			if(m_HandOpen.IsEnterOpened [i])
			{
				Vector palmDir = m_HandOpen.PalmDir[i];
				Vector fingerDir = m_HandOpen.Dir[i];

				//是水平向前的方向
				if(IsHorAndForwardOpenHand(palmDir,fingerDir))
				{
					isEnter =true;
					handIndex = i;
				}
			}
		}

		return isEnter;
	}

	/// <summary>
	/// 经过一段时间后，若指定的手处于弯曲状态，则触发握拳的完成事件，初始化成员变量
	/// </summary>
	/// <returns>The for bend state.</returns>
	/// <param name="delayTime">经过多长时间后触发</param>
	/// <param name="handIndex">对哪只手进行弯曲状态的匹配</param>
	IEnumerator WaitForBendState(float delayTime,int handIndex)
	{
		float curTime = 0f;
		
		while (curTime<delayTime) 
		{
			curTime+=Time.deltaTime;
			yield return null;
		}
		
		//目前是BendHand状态
		if (m_HandBend.IsEnterBended [handIndex]) 
		{
			//本该进行一次水平握拳的判定，但判定不准
			//if(m_HandBend.Dir[handIndex].AngleTo(-Vector.YAxis)<DownThreshold)

			//触发完成事件
			if(m_OnEndFunc!=null)
			{
				m_OnEndFunc();
			}
		}
		//reset
		m_IsEnteredOpen = false;
		m_CurCheckStepTime = 0f;
		m_CurEnterDelayTime = 0f;
	}

	/// <summary>
	/// 判定一个摊开手掌的状态是否是水平向前
	/// </summary>
	/// <returns><c>true</c>, if and forward open hand was hored, <c>false</c> otherwise.</returns>
	bool IsHorAndForwardOpenHand(Vector palmDir,Vector fingerDir )
	{
		bool isHorAndForward = false;
		if( fingerDir.AngleTo(-Vector.ZAxis) < ForwardThreshold && 
		    palmDir.AngleTo(-Vector.YAxis) < DownThreshold )
		{
			isHorAndForward=true;
		}
		return isHorAndForward;
	}
}
