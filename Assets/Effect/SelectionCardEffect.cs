using Cysharp.Threading.Tasks;
using DG.Tweening;
using DuelKingdom.Effect;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SelectionCardEffect : MonoBehaviour
{
    EffectController m_EffectCtrl;
    CancellationToken m_ExternalToken;
    [HideInInspector] public EffectController EffectCtrl
    {
        get => m_EffectCtrl;
        set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
    }

    [SerializeField] ParticleSystem m_SelectionPS;
    [SerializeField] Light2D m_SlectionLight;

    [Header("Time")]
    [SerializeField] float m_LightUpDura = 1f;
    [Header("Parametor")]
    [SerializeField] float m_ScaleValue = 1.15f;
    [Header("Card Order in Layer")]
    [SerializeField] int m_TempLayer = 0;
    [SerializeField] int m_TargetLayer = -1;

    Vector3 m_InitialCardScale;

    const string NOLIGHT_SORTINGLAYER = "NoLight";
    const string DEFAULT_SORTINGLAYER = "Default";

    public async UniTask onSelectionEffect(Transform target)
    {
        if (!target.TryGetComponent<SpriteRenderer>(out var targetSp))
        {
            Debug.LogError("ターゲットにSpriteRendererコンポーネントが見つかりませんでした。");
            return;
        }
        if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var targetNumTxt))
        {
            Debug.LogError("ターゲットの子オブジェクトにTextMeshProコンポーネントが見つかりませんでした。");
            return;
        }

        this.transform.position = targetSp.transform.position;
        DOTween.Kill($"offSelection_{target.GetInstanceID()}", true);
        m_InitialCardScale = targetSp.transform.localScale;

        targetSp.sortingLayerID = SortingLayer.NameToID(NOLIGHT_SORTINGLAYER);
        // targetSp.sortingOrder = m_TargetLayer;
        targetNumTxt.sortingLayerID = SortingLayer.NameToID(NOLIGHT_SORTINGLAYER);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
        CancellationToken token = linkedCts.Token;

        m_SelectionPS.Play();

        Tween lightUpTween = DOTween.To(() => m_SlectionLight.intensity,
                    x => m_SlectionLight.intensity = x,
                    1, m_LightUpDura);
        Tween scaleTween = targetSp.transform.DOScale(m_ScaleValue, m_LightUpDura);

        Sequence seq = DOTween.Sequence();
        seq.Append(lightUpTween)
              .Join(scaleTween);

        try 
        { 
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }
        catch (OperationCanceledException)
        {
            seq.Kill();
        }
    }

    public async UniTask offSelectionEffect(Transform target, CancellationToken restoreToken = default, bool? setScale = true)
    {
        if (!target.TryGetComponent<SpriteRenderer>(out var targetSp))
        {
            Debug.LogError("ターゲットにSpriteRendererコンポーネントが見つかりませんでした。");
            return;
        }
        if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var targetNumTxt))
        {
            Debug.LogError("ターゲットの子オブジェクトにTextMeshProコンポーネントが見つかりませんでした。");
            return;
        }

        this.transform.position = targetSp.transform.position;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
        CancellationToken token = linkedCts.Token;

        m_SelectionPS.Stop();

        Tween lightTween = DOTween.To(() => m_SlectionLight.intensity,
                    x => m_SlectionLight.intensity = x,
                    0, m_LightUpDura);
        Tween scaleTween = targetSp.transform.DOScale(m_InitialCardScale, m_LightUpDura);

        Sequence seq = DOTween.Sequence();
        seq.SetId($"offSelection_{target.GetInstanceID()}");
        seq.Append(lightTween);
        if (setScale.Value) seq.Join(scaleTween);

        try
        {
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
            if (!restoreToken.IsCancellationRequested)
            {
                targetSp.sortingLayerID = SortingLayer.NameToID(DEFAULT_SORTINGLAYER);
                // targetSp.sortingOrder = m_TempLayer;
                targetNumTxt.sortingLayerID = SortingLayer.NameToID(DEFAULT_SORTINGLAYER);
            }
        }
        catch (OperationCanceledException)
        {
            seq.Kill();
        }
    }

    private void OnDestroy()
    {
    }
}
