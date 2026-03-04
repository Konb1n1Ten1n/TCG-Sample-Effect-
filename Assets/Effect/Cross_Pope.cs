using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Transform = UnityEngine.Transform;

namespace DuelKingdom.Effect
{
    public class Cross_Pope : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }
        public Transform SampleCard;

        [SerializeField] ParticleSystem m_PS;
        [SerializeField] Volume m_GlobalVolume;
        [SerializeField] VolumeProfile m_CrossVolumeProfile;
        [SerializeField] SpriteRenderer m_CrossSp;
        [SerializeField] SpriteRenderer m_BackSp;
        [Header("Parameter")]
        [SerializeField] float m_EffectDura = 0.5f;
        [SerializeField] float m_FirstScale = 1.2f;
        [SerializeField] float m_BackScale = 1.1f;

        Tween m_CrossTween;
        Tween m_BackTween;
        CancellationTokenSource m_PlayCts;

        public async UniTask PlayCrossEffect(Transform trans, bool? onCross = true)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("この関数はプレイ中にのみ有効です。");
                return;
            }
#endif
            m_PlayCts?.Cancel();
            m_PlayCts = new CancellationTokenSource();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken, m_PlayCts.Token);
            CancellationToken token = linkedCts.Token;

            if (m_CrossTween != null && m_CrossTween.active) m_CrossTween.Kill();
            if (m_BackTween != null && m_BackTween.active) m_BackTween.Kill();

            //m_GlobalVolume.profile = m_CrossVolumeProfile;
            //m_GlobalVolume.weight = 1.0f;

            this.transform.position = trans.position;

            EffectCtrl.PlaySE(EffectSEType.Cross);
            m_CrossSp.color = new Color(m_CrossSp.color.r, m_CrossSp.color.g, m_CrossSp.color.b, 0f);
            m_BackSp.color = new Color(m_BackSp.color.r, m_BackSp.color.g, m_BackSp.color.b, 0f);
            m_CrossTween = m_CrossSp.DOFade(1.0f, m_EffectDura);
            m_BackTween = m_BackSp.DOFade(1.0f, m_EffectDura);

            Vector3 targetCrossScale = m_CrossSp.transform.lossyScale;
            Vector3 targetBackScale = m_BackSp.transform.lossyScale * m_BackScale;
            m_CrossSp.transform.localScale = targetCrossScale * m_FirstScale;
            m_BackSp.transform.localScale = targetBackScale * m_FirstScale;
            Tween crossScaleTw = m_CrossSp.transform.DOScale(targetCrossScale, m_EffectDura).SetEase(Ease.OutSine);
            Tween backScaleTw = m_BackSp.transform.DOScale(targetBackScale, m_EffectDura).SetEase(Ease.OutSine);

            Sequence seq = DOTween.Sequence();

            seq.Join(m_BackTween)
                .Join(crossScaleTw)
                .Join(backScaleTw);

            if (onCross.Value) seq.Join(m_CrossTween);

            m_PS.Play();

            try
            {
                await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                seq.Kill();
                return;
            }

            m_CrossTween.Kill();
            m_BackTween.Kill();
        }

        public async UniTask StopCrossEffect()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("この関数はプレイ中にのみ有効です。");
                return;
            }
#endif
            m_PlayCts?.Cancel();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            if (m_CrossTween != null && m_CrossTween.active) m_CrossTween.Kill();
            if (m_BackTween != null && m_BackTween.active) m_BackTween.Kill();

            m_CrossTween = m_CrossSp.DOFade(0.0f, m_EffectDura);
            m_BackTween = m_BackSp.DOFade(0.0f, m_EffectDura);

            m_PS.Stop();

            try
            {
                await UniTask.WhenAll(
                    m_CrossTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    m_BackTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
            }
            catch (OperationCanceledException)
            {
                m_CrossTween.Kill();
                m_BackTween.Kill();
                return;
            }

            //m_GlobalVolume.profile = null;
            //m_GlobalVolume.weight = 0f;

            m_CrossTween.Kill();
            m_BackTween.Kill();
        }
    }

}
