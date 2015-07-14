using UnityEngine;
using System.Collections;
using Leap;

//一个手指的数据包含一个指尖点数据和手指根骨的位置数据
public struct FingerData
{
    public PointData m_Point;//指尖的位置和指向
    public Vector m_Position;//手指根骨的位置,对于拇指来说是Proximal phalanges近端指骨的位置

    public FingerData(PointData pointData, Vector pos)
    {
        m_Point = pointData;
        m_Position = pos;
    }

    public FingerData(Vector pointPos, Vector pointDir, Vector pos)
    {
        m_Point.m_Position = pointPos;
        m_Point.m_Direction = pointDir;
        m_Position = pos;
    }

	public void Set(FingerData fd)
	{
		m_Point = fd.m_Point;
		m_Position = fd.m_Position;
	}
}
//一个点的数据，包括方向和位置
public struct PointData
{
    public Vector m_Position;//位置
    public Vector m_Direction;//方向

	public PointData(Vector pos,Vector dir)
	{
		m_Position = pos;
		m_Direction = dir;
	}

	public void Set(PointData pd)
	{
		m_Position = pd.m_Position;
		m_Direction = pd.m_Direction;
	}

	public void Set(Vector pos,Vector dir)
	{
		m_Position = pos;
		m_Direction = dir;
	}
}

//先被看到的手
public enum E_HandInAboveView
{
    None,
    Left,
    Right
}