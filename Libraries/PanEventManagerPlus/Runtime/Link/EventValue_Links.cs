using Pan.Event;
using Pan.Util.IOB;
using Sirenix.OdinInspector;
using UnityEngine;



/// <summary>
/// Link과 연계되는 옵저버 매니저
/// </summary>
public class Observer_LinkManager : BaseEventValue_Observer<Observer_LinkManager, Observer_LinkManager.IS>
{
    public interface IS : IObserver
    {
        public interface IAlarm_ControlLinks : IS { void Alarm_ControlLinks(MonoBehaviour link); }
    }



    /// <summary>
    /// Link를 지정/지정해제 했을때 알람
    /// <para><see cref="MonoBehaviour"/>가 null일수도 있으며, 이때는 제거로 지정해제여 코드를 작성해야한다</para>
    /// </summary>
    public Subscribe_Alarm<IS, IS.IAlarm_ControlLinks, MonoBehaviour> WhenControlLinks_Alarm
    {
        get
        {
            whenControlLinks_Alarm ??= new Subscribe_Alarm<IS, IS.IAlarm_ControlLinks, MonoBehaviour>(this, (link, ob) => ob.Alarm_ControlLinks(link));
            return whenControlLinks_Alarm;
        }
    }
    private Subscribe_Alarm<IS, IS.IAlarm_ControlLinks, MonoBehaviour> whenControlLinks_Alarm;
}



public abstract class BaseEventValue_Link<TEventValue, TLink> : PanBaseEventValue.EventAbles<TEventValue>
    where TEventValue : BaseEventValue_Link<TEventValue, TLink>, new()
    where TLink : MonoBehaviour
{
    ///======================================================================================================================================================



    public TLink CurrentLink => currentLink;
    [BoxGroup("박스", false)]
    [LabelText("링크")]
    [SerializeField, ReadOnly]
    private TLink currentLink;



    public bool HasLink => currentLink != null;



    /// <summary>
    /// Link 변경 알림 도중 observer가 다시 SetLink/RemoveLink를 호출해 상태 전이가 중첩되는 것을 막습니다.
    /// </summary>
    private bool isControllingLink;
    private bool linkLifecycleInvalidated;



    ///<summary>
    /// <see cref="CurrentLink"/> 지정하기
    ///</summary>
    public bool SetLink(TLink link)
    {
        //! null, 중복 확인
        if (link == null || currentLink == link || isControllingLink) { return false; }

        isControllingLink = true;
        linkLifecycleInvalidated = false;
        try
        {
            if (currentLink != null)
            {
                //! 제거 알림 동안에는 observer가 기존 CurrentLink를 확인할 수 있어야 한다.
                Alarm_WhenControlLink(null);
                if (linkLifecycleInvalidated) { return false; }
            }

            currentLink = link;
            Alarm_WhenControlLink(currentLink);
        }
        finally
        {
            isControllingLink = false;
            linkLifecycleInvalidated = false;
        }

        return true;
    }



    ///<summary>
    /// <see cref="CurrentLink"/> 지정해제하기
    ///</summary>
    public bool RemoveLink()
    {
        if (currentLink == null || isControllingLink) { return false; }

        isControllingLink = true;
        linkLifecycleInvalidated = false;
        try
        {
            //! 제거 알림 동안에는 observer가 기존 CurrentLink를 확인할 수 있어야 한다.
            Alarm_WhenControlLink(null);
            if (linkLifecycleInvalidated) { return false; }
            currentLink = null;
        }
        finally
        {
            isControllingLink = false;
            linkLifecycleInvalidated = false;
        }

        return true;
    }



    /// <summary>
    /// <see cref="CurrentLink"/> 확인하기
    /// </summary>
    /// <param name="link"></param>
    /// <returns></returns>
    public bool TryGetLink(out TLink link)
    {
        if (HasLink)
        {
            link = CurrentLink;
            return true;
        }

        link = null;
        return false;
    }



    ///======================================================================================================================================================



    ///<summary>
    /// 이벤트밸류가 비활성화 될때 <see cref="CurrentLink"/>도 함께 비활성화 할지 여부
    /// </summary>
    [BoxGroup("박스", false)]
    [LabelText("이벤트밸류 비활성화시 링크도 비활성화")]
    [ShowInInspector, ReadOnly]
    protected virtual bool DisableCurrentLinkWhenDisable => true;



    ///======================================================================================================================================================



    protected sealed override void Disable()
    {
        TLink linkToDisable = currentLink;
        bool ownsLinkControl = !isControllingLink;

        if (ownsLinkControl)
        {
            isControllingLink = true;
        }
        else
        {
            linkLifecycleInvalidated = true;
        }

        try
        {
            if (ownsLinkControl && linkToDisable != null)
            {
                //! RemoveLink와 동일하게 제거 알림 중에는 기존 CurrentLink를 유지한다.
                Alarm_WhenControlLink(null);
            }
        }
        finally
        {
            currentLink = null;

            if (ownsLinkControl)
            {
                isControllingLink = false;
            }

            try
            {
                if (DisableCurrentLinkWhenDisable && linkToDisable != null)
                {
                    linkToDisable.gameObject.SetActive(false);
                }
            }
            finally
            {
                DisableCurrent();
            }
        }
    }



    /// <summary>링크 이벤트 밸류 전용 Disable</summary>
    protected virtual void DisableCurrent() { }



    ///======================================================================================================================================================



    private void Alarm_WhenControlLink(MonoBehaviour link)
    {
        if (Observer_LinkManager.TryPeek(Current, out var ob_Link))
        {
            ob_Link.WhenControlLinks_Alarm.Alarm(link);
        }
    }



    ///======================================================================================================================================================
}
