using Cysharp.Threading.Tasks;
using DG.Tweening;
using DuelKingdom.Effect;
using System.Threading;
using UnityEngine;

public class StartTurn : MonoBehaviour
{
    [SerializeField] Sprite[] m_TurnCounts;
    [SerializeField] SpriteRenderer m_TextSp;
    [SerializeField] SpriteRenderer m_TurnCountSp;
    [SerializeField] Ease m_FirstEase;
    [SerializeField] Ease m_LastEase;
    [SerializeField] float m_TextGeneralTime = 5.0f;
    [SerializeField] float m_CountGeneralTime = 3.0f;
    [SerializeField, Range(0, 1)] float m_FirstDura = 0.2f;
    [SerializeField, Range(0, 1)] float m_MidDura = 0.6f;
    [SerializeField, Range(0, 1)] float m_LastDura = 0.2f;
    [SerializeField] float m_FirstTargetScale = 1.2f;
    [SerializeField] float m_MidTargetScale = 0.8f;
    [SerializeField] float m_EndTargetScale = 0.3f;
    EffectController m_EffectCtrl;
    CancellationToken m_ExternalToken;

    [HideInInspector]
    public EffectController EffectCtrl
    {
        get => m_EffectCtrl;
        set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
    }

    public async UniTask DisplayStartTurnText()
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
        CancellationToken token = linkedCts.Token;

        Vector3 prevTextScale = m_TextSp.transform.localScale;
        m_TextSp.color = new Color(m_TextSp.color.r, m_TextSp.color.g, m_TextSp.color.b, 0.0f);
        m_TextSp.transform.localScale = prevTextScale * m_FirstTargetScale;
        m_TextSp.gameObject.SetActive(true);

        m_EffectCtrl.PlaySE(EffectSEType.CinematicBuoon);

        Tween firstFadeTw = m_TextSp.DOFade(1.0f, m_TextGeneralTime * m_FirstDura).SetEase(m_FirstEase);
        Tween firstScTw = m_TextSp.transform.DOScale(prevTextScale, m_TextGeneralTime * m_FirstDura).SetEase(m_FirstEase);
        await UniTask.WhenAll(
            firstFadeTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
            firstFadeTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
            );

        Tween midScTw = m_TextSp.transform.DOScale(prevTextScale * m_MidTargetScale, m_TextGeneralTime * m_MidDura).SetEase(m_FirstEase);
        await midScTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

        Tween lastFadeTw = m_TextSp.DOFade(0.0f, m_TextGeneralTime * m_LastDura).SetEase(m_LastEase);
        Tween lastScTw = m_TextSp.transform.DOScale(prevTextScale * m_EndTargetScale, m_TextGeneralTime * m_LastDura).SetEase(m_LastEase);
        await UniTask.WhenAll(
            lastFadeTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
            lastScTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
            );
    }

    public async UniTask DisplayTurnCountText(int turnCount, bool? playSE = false)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
        CancellationToken token = linkedCts.Token;

        Vector3 prevCountScale = m_TurnCountSp.transform.localScale;
        m_TurnCountSp.color = new Color(m_TurnCountSp.color.r, m_TurnCountSp.color.g, m_TurnCountSp.color.b, 0.0f);
        m_TurnCountSp.transform.localScale = prevCountScale * m_FirstTargetScale;
        m_TurnCountSp.gameObject.SetActive(true);

        m_TurnCountSp.sprite = m_TurnCounts[Mathf.Clamp(turnCount - 1, 0, m_TurnCounts.Length - 1)];

        if (playSE.Value) m_EffectCtrl.PlaySE(EffectSEType.CinematicBuoon);

        Tween firstCountFadeTw = m_TurnCountSp.DOFade(1.0f, m_TextGeneralTime * m_FirstDura).SetEase(m_FirstEase);
        Tween firstCountScTw = m_TurnCountSp.transform.DOScale(prevCountScale, m_TextGeneralTime * m_FirstDura).SetEase(m_FirstEase);
        await UniTask.WhenAll(
            firstCountFadeTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
            firstCountScTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
            );

        Tween midCountScTw = m_TurnCountSp.transform.DOScale(prevCountScale * m_MidTargetScale, m_CountGeneralTime * m_MidDura).SetEase(m_FirstEase);
        await midCountScTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

        Tween lastCountFadeTw = m_TurnCountSp.DOFade(0.0f, m_TextGeneralTime * m_LastDura).SetEase(m_LastEase);
        Tween lastCountScTw = m_TurnCountSp.transform.DOScale(prevCountScale * m_EndTargetScale, m_TextGeneralTime * m_LastDura).SetEase(m_LastEase);
        await UniTask.WhenAll(
            lastCountFadeTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
            lastCountScTw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
            );
    }
}
