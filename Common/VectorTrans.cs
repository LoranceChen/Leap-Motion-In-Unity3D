using UnityEngine;
using System.Collections;
using Leap;
public class VectorTrans
{
	public static Vector3 ToUnityVector3(Vector leapVector)
	{
		Vector3 unityVector3 = new Vector3( leapVector.x,leapVector.y,-leapVector.z);
		return unityVector3;
	}
}
