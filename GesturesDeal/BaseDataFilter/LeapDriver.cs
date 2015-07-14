using UnityEngine;
using System.Collections;
using Leap;

/// <summary>
/// 关联LeapMotion驱动,分配内存空间,创建接口
/// </summary>
public sealed class LeapDriver 
{
	private static Controller leapController=null;
	public static Controller GetLeapCtrl()
	{
		if (leapController == null) {
			leapController = new Controller ();
		} 
		return leapController;
	}
}
