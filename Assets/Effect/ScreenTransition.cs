using Cysharp.Threading.Tasks;
using DG.Tweening;
using DuelKingdom.Effect;
using System.Threading;
using UnityEngine;

public class ScreenTransition : MonoBehaviour
{
    EffectController m_EffectCtrl;
    CancellationToken m_ExternalToken;
    [HideInInspector] public EffectController EffectCtrl
    {
        get => m_EffectCtrl;
        set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
    }

    [SerializeField] SpriteRenderer m_TransitionSp;

    const string TRANSITION_KEY = "_Transition";

    public async UniTask PlayScreenTransition(bool fadeIn, float duration = 0.5f)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
        CancellationToken token = linkedCts.Token;

        if (fadeIn) m_TransitionSp.gameObject.SetActive(true);

        Tween fadeTween = DOTween.To(() => m_TransitionSp.material.GetFloat(TRANSITION_KEY),
                    x => m_TransitionSp.material.SetFloat(TRANSITION_KEY, x),
                    fadeIn ? 0 : 1, duration);
        await fadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

        if (!fadeIn) m_TransitionSp.gameObject.SetActive(false);
    }
}
