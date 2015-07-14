using UnityEngine;
using System.Collections;
using Leap;
public class VectorCompare 
{
	static readonly float VerToXAxisDirThreshold = Mathf.PI * 7 / 18;//垂直x轴的方向阈值[70,180-70]
	static readonly float VerToYAxisDirThreshold = Mathf.PI * 7 / 18;//垂直y轴方向的阈值[70,180-70]
	static readonly float VerToZAxisDirThreshold = Mathf.PI * 7 / 18;//垂直y轴方向的阈值[70,180-70]


	/// <summary> 
	/// 一个方向是否垂直+x轴
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="threshold">阈值</param>
	/// <returns>垂直true</returns>
	public static bool VerDirToXAxis(Vector dir,float thresholdAdjust = 0f)
	{
		bool isVer=false;
		float radian = dir.AngleTo(Vector.XAxis);
		
		if( radian > VerToXAxisDirThreshold+thresholdAdjust && 
		   radian < (Mathf.PI-(VerToXAxisDirThreshold+thresholdAdjust)))
		{
			isVer = true;
		}
		return isVer;
	}

	
	public static bool VerDirToYAxis(Vector dir, float thresholdAdjust = 0f)
	{
		bool isVer = false;

		float radian = dir.AngleTo(Vector.YAxis);
		
		if( radian > VerToYAxisDirThreshold+thresholdAdjust &&
		   radian < ( Mathf.PI -(VerToYAxisDirThreshold+ thresholdAdjust) ))
		{
			isVer = true;
		}
		return isVer;
	}
	public static bool VerDirToZAxis(Vector dir, float thresholdAdjust = 0f)
	{
		bool isVer = false;
		
		float radian = dir.AngleTo(Vector.YAxis);
		
		if( radian > VerToZAxisDirThreshold+thresholdAdjust &&
		   radian < ( Mathf.PI -(VerToYAxisDirThreshold+ thresholdAdjust) ))
		{
			isVer = true;
		}
		return isVer;
	}

	//public static float RadianTo
}
