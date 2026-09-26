using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PlayerHUDPanelBase : MonoBehaviour, IHUDPanel
{
    protected PlayerContext Context { get; private set; }
    protected bool IsInitialized => Context != null;

    private bool isShown;

    public void Initialize(PlayerContext context)
    {
        if (context == null)
        {
            return;
        }

        if (Context == context) return;

        Release();
        Context = context;
        OnInitialize();
    }

    public void Show()
    {
        if (!IsInitialized)
            return;

        if (!isShown)
        {
            SubscribeEvents();
            isShown = true;
        }

        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (!isShown)
            return;

        UnsubscribeEvents();
        isShown = false;
        gameObject.SetActive(false);
    }

    public void Release()
    {
        Hide();

        if (!IsInitialized)
            return;

        OnRelease();
        Context = null;
    }

    

    protected virtual void OnInitialize() { }
    protected virtual void SubscribeEvents() { }
    protected virtual void UnsubscribeEvents() { }
    protected virtual void OnRelease() { }
    protected virtual void Refresh() { }
}
