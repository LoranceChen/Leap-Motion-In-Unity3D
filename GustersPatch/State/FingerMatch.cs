using UnityEngine;
using System.Collections;
using Leap;
/// <summary>
/// 该方法提供对于单个手指匹配的算法，如伸直，弯曲
/// 以后可能的改变：对于不同的场景可能要求有所不同，这里的阈值也许会随之改变
/// </summary>
public class FingerMatch
{
	//弯曲状态的角度阈值
	static readonly float FingerBendState_Radian = Mathf.PI*4f / 18 ;//40度
	//伸直状态的角度阈值
	static readonly float FingerStrightState_Radian = Mathf.PI/12;//15度

	/// <summary>
	/// 手指伸直的状态,当根骨-指尖的方向和指向的偏差小于阀值时，判定手指为伸直状态。
	/// 注意无效的方向为零向量，先判定是零向量
	/// </summary>
	/// <param name="adjustBorder">对阈值做的微调</param>
	/// <returns></returns>
	public static bool StrightState(FingerData fingerData, float adjustBorder=0f)
	{
		bool isStright =false;
		Vector disalDir = fingerData.m_Point.m_Direction;
		//如果指尖方向为0向量，表示无效的数据
		if (!disalDir.Equals(Vector.Zero)) 
		{
			Vector fingerDir = fingerData.m_Point.m_Position - fingerData.m_Position;//指尖位置减去指根位置，由指根指向指尖的向量	        
			float radian = fingerDir.AngleTo(disalDir);
			
			if (radian < FingerStrightState_Radian + adjustBorder)
			{
				isStright = true;
			}
		}
		return isStright;
	}

	/// <summary>
	/// 判断一根手指是否处于弯曲状态
	/// </summary>
	/// <param name="fingerData">需要判定的手指数据</param>
	/// <param name="bandBorder">弯曲的阈值</param>
	/// <returns></returns>
	public static bool BendState(FingerData fingerData, float adjustBorder=0f)//,out float eulerAugle)
	{
		bool isBend = false;

		//eulerAugle = -1f;
		Vector disalDir = fingerData.m_Point.m_Direction;
		if( !disalDir.Equals(Vector.Zero) )
		{
			Vector fingerDir = fingerData.m_Point.m_Position - fingerData.m_Position;//指尖位置减去指根位置，指跟到指尖的向量

			float radian = fingerDir.AngleTo(disalDir);
			//eulerAugle = radian*180/Mathf.PI;	
			//夹角超过定义的阈值时，认定为弯曲状态
			if (radian > FingerBendState_Radian + adjustBorder)
			{
				isBend = true;
			}
		}

		return isBend;
	}

}
