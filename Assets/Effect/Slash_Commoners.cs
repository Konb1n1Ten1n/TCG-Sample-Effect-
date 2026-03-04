using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 平民の斬撃エフェクト
/// </summary>
namespace DuelKingdom.Effect
{
    public class Slash_Commoners : MonoBehaviour
    {
        public Transform SampleTargetTransform;

        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] ParticleSystemRenderer m_PSRenderer;
        [SerializeField] ParticleSystem m_PS;
        [SerializeField] SpriteRenderer m_SlashSp;
        [SerializeField] SpriteRenderer m_BloodSp;
        [SerializeField] float m_TargetSplashAlpha = 0.2f;

        [Header("Time")]
        [SerializeField, Range(0, 1)] float m_ParticleDelay = 0.07f;
        [SerializeField, Range(0, 1)] float m_BloodFadeDelay = 0.65f;
        [SerializeField, Range(0, 1)] float m_BloodFadeSpeed = 0.5f;
        [SerializeField, Range(0, 1)] float[] m_SlashAnimSpeed = new float[2] { 0.15f, 0.2f };
        [SerializeField, Range(0, 1)] float m_SlashAnimDelay = 0.07f;

        [Header("Loop")]
        [SerializeField] bool m_LoopPlay;

        const string SLASH_MAT_PROPERTY_0 = "_First";
        const string SLASH_MAT_PROPERTY_1 = "_Second";
        const float SLASH_MAT_VALUE_0_MIN = 0f;
        const float SLASH_MAT_VALUE_0_MAX = 1f;
        const float SLASH_MAT_VALUE_1_MIN = 0f;
        const float SLASH_MAT_VALUE_1_MAX = 1f;

        /// <summary>
        /// 斬撃エフェクトの再生
        /// </summary>
        /// <returns></returns>
        public async UniTask PlaySlashEffect(Transform target)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("この関数はプレイ中にのみ有効です。");
                return;
            }
#endif
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            this.transform.position = target.position;

            m_BloodSp.gameObject.SetActive(false);
            m_BloodSp.color = new Color(m_BloodSp.color.r, m_BloodSp.color.g, m_BloodSp.color.b, 0);
            // シェーダーの初期値を設定（両プロパティを最小値に）
            m_SlashSp.material.SetFloat(SLASH_MAT_PROPERTY_0, SLASH_MAT_VALUE_0_MIN);
            m_SlashSp.material.SetFloat(SLASH_MAT_PROPERTY_1, SLASH_MAT_VALUE_1_MIN);

            Tween mat0 = DOTween.To(() => m_SlashSp.material.GetFloat(SLASH_MAT_PROPERTY_0),
                    x => m_SlashSp.material.SetFloat(SLASH_MAT_PROPERTY_0, x),
                    SLASH_MAT_VALUE_0_MAX, m_SlashAnimSpeed[0]).SetEase(Ease.OutSine);
            Tween mat1 = DOTween.To(() => m_SlashSp.material.GetFloat(SLASH_MAT_PROPERTY_1),
                    x => m_SlashSp.material.SetFloat(SLASH_MAT_PROPERTY_1, x),
                    SLASH_MAT_VALUE_1_MAX, m_SlashAnimSpeed[1]).SetEase(Ease.OutSine);
            Tween playPs = DOVirtual.DelayedCall(m_ParticleDelay, () => m_PS.Play());
            Tween playBlood = DOVirtual.DelayedCall(m_BloodFadeDelay, async () => await PlayBlood().SuppressCancellationThrow());

            Sequence seq = DOTween.Sequence();
            seq.Append(mat0)
                .JoinCallback(() => EffectCtrl.PlaySE(EffectSEType.Slash))
                .Join(playPs)
                .Join(playBlood)
                .Insert(m_SlashAnimDelay, mat1);

            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            await m_BloodSp.DOFade(0f, m_BloodFadeSpeed).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
            m_BloodSp.gameObject.SetActive(false);

            if (m_LoopPlay)
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(200), cancellationToken: token);
                await PlaySlashEffect(SampleTargetTransform);
            }

            async UniTask PlayBlood()
            {
                float startScale = m_BloodSp.transform.localScale.x;
                m_BloodSp.transform.localScale = new Vector3(startScale * 1.2f, startScale * 1.2f, m_BloodSp.transform.localScale.z);
                m_BloodSp.gameObject.SetActive(true);
                Tween onBloodFade = m_BloodSp.DOFade(m_TargetSplashAlpha, 0.05f).SetEase(Ease.OutSine);
                Tween bloodSclae = m_BloodSp.transform.DOScale(startScale, 0.05f).SetEase(Ease.OutSine);

                Sequence bloodSeq = DOTween.Sequence();
                bloodSeq.Append(onBloodFade).Join(bloodSclae);

                await bloodSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
            }
        }

        /*
        [CustomEditor(typeof(Slash_Commoners))]
        public class Slash_CommonersEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                Slash_Commoners script = (Slash_Commoners)target;
                if (GUILayout.Button("Play Slash Effect"))
                {
                    var t = script.SampleTargetTransform; 
                    EditorApplication.delayCall += () => _ = script.PlaySlashEffect(t);
                }
            }
        }
        */
    }
}