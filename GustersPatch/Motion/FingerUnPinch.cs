using UnityEngine;
using System.Collections;
using System;

///Log1:手势废弃，Leap API不能正确识别手指的前方，尤其是整根手指从骨根弯曲，不明白Finger.Direction的定义。
/// <summary>
/// 匹配手指捏住的动作
/// 两种方案：
/// 	1.当匹配捏的初始状态后，等待一段时间后，若此刻是捏的终止状态，则说明完成了捏动作  X：决定第二种
/// 	2.当匹配初始后，每帧会追踪两手指尖的距离，当距离一直在减小，并最终处于捏的终止状态时，说明完成了捏动作
/// 	比较：方式2优点：可以记录每帧的变化时差，以此作为捏的快慢程度量。
/// 		 方式2缺点：复杂，需要考虑手指的细微抖动所造成的位置比较————解决方案：每隔0.2秒做一次判定，不用协同程序，因为时间太短。
/// 匹配只会涉及一只手，即如果检测到一只手处于捏动作中，那么不会再检测其他手，除非这只手退出了捏状态。
/// 手势匹配细节：
///     1.捏动作的初始状态：
///         食指伸直，食指指尖与拇指指尖相距一定距离（初始距离阈值）
///         涉及变量:Action m_OnEnterPinch;bool m_IsEnteredPinch;const float EnterDisThreshold;
///     2.捏动作的运动过程
///         每隔0.2s检测食指和拇指指尖的距离，并记录。以前后两次距离的差值来表示速度
///         涉及变量：speed:float;m_PreviousDistance:float;const CheckTimeStep:float
///     3.捏动作结束状态：
///         食指伸直，食指指尖与拇指指尖相距一定距离（终结距离阈值）
///         涉及变量：Action m_OnPinched;const float PinchedDisThreshold;
/// </summary>
public class FingerUnPinch : MonoBehaviour
{
    //[SerializeField] HandAndFingersPoint m_HandData;
    [SerializeField]
    HandPinchGroup m_HandPinch;
    //Enter Pinch
    Action m_OnEnterUnPinch;
    readonly float EnterDelayTime = 0.3f;
    [SerializeField] bool m_IsEnteredUnPinch;//是否进入了捏动作的匹配状态，即是否检测到捏的初始状态，并且到现在为止动作也是正常匹配中。
    readonly float EnterDisThreshold = 10f;//小于10mm才算进入UnPinch状态

    //On Pinching
    [SerializeField] float m_Speed;//当前的两指尖距离-m_PreviousDistance，代表速度。
    [SerializeField] float m_PreviousDistance;
    float m_CurCheckTime;
    readonly float CheckTimeStep = 0.2f;//状态判定的时间间隔————当匹配捏的初始状态后每隔N秒做一次数据处理

    //Pinched
    readonly float UnPinchedDisThreshold = 100f;//Leap单位，100mm
    Action m_OnUnPinched;//完成捏动作匹配后，触发事件链。


    [SerializeField] int m_HandIndex;//当前捏状态所识别的手索引

    // Update is called once per frame
    void Update()
    {
        //没有进入初始状态时判定是否进入了初始状态
        if (!m_IsEnteredUnPinch)
        {
            //符合
            if (EnterUnPinchState(out m_HandIndex))
            {
                m_IsEnteredUnPinch = true;
                m_PreviousDistance = m_HandPinch.Distance[m_HandIndex];
                StartCoroutine(EnterUnPinchDelay());
            }
        }

        //当前处于捏状态中
        if (m_IsEnteredUnPinch)
        {
            //时间判定
            m_CurCheckTime += Time.deltaTime;
            if (m_CurCheckTime > CheckTimeStep)
            {
                m_CurCheckTime = 0f;

                float curDis = m_HandPinch.Distance[m_HandIndex];
                //依然处于捏动作过程中
                if (curDis > m_PreviousDistance)
                {
                    //处于结束状态
                    if (curDis > UnPinchedDisThreshold)
                    {
                        //触发相应操作
                        ResetState();
                        if (m_OnUnPinched != null)
                        {
                            m_OnUnPinched();
                        }
                    }
                    //不是结束状态
                    else
                    {
                        //记录数据
                        m_Speed = curDis - m_PreviousDistance;
                        m_PreviousDistance = curDis;
                    }
                }
                //不匹配捏动作
                else
                {
                    //退出捏动作
                    ResetState();
                }
            }
        }
    }

    void ResetState()
    {
        m_CurCheckTime = 0f;
        m_Speed = 0f;
        m_PreviousDistance = 0f;
        m_IsEnteredUnPinch = false;
        m_HandIndex = -1;
    }

    /// <summary>
    /// 延迟发送缩放广播
    /// </summary>
    /// <returns></returns>
    IEnumerator EnterUnPinchDelay()
    {
        float curDelayTime = 0f;
        while (curDelayTime < EnterDelayTime)
        {
            curDelayTime += Time.deltaTime;
            yield return null;
        }
        //进入了点击状态
        if (m_IsEnteredUnPinch)
        {
            //发送广播
            if (m_OnEnterUnPinch != null)
            {
                m_OnEnterUnPinch();
            }
        }
    }

    /// <summary>
    /// 初始动作的匹配判定
    /// </summary>
    bool EnterUnPinchState(out int handIndex)
    {
        bool isEnterUnPinch = false;
        handIndex = -1;
        for (int i = 0; i < 2; i++)
        {
            if (m_HandPinch.IsHandPinch[i])
            {
                //距离小于阈值
                if (m_HandPinch.Distance[i] < EnterDisThreshold)
                {
                    isEnterUnPinch = true;
                    handIndex = i;
                }
            }
        }
        return isEnterUnPinch;
    }
}
