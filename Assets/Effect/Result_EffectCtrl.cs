using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DuelKingdom.Effect
{
    public class Result_EffectCtrl : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] Light2D m_GlobalSpotLight;
        [SerializeField] Light2D m_Spot;
        [SerializeField] SpriteRenderer m_BackMist;
        [SerializeField] SpriteRenderer m_SimpleBack;
        [SerializeField] ResultEffectData m_PatternVictory;
        [SerializeField] ResultEffectData m_PatternDefeat;
        [SerializeField] ResultEffectData m_PatternDraw;
        [SerializeField] SpriteRenderer m_SimpleBlackBack;
        [SerializeField] Volume m_Volume;

        [SerializeField, Range(0f, 5f)] float m_FastBlackTime = 2.0f;
        [SerializeField, Range(0f, 5f)] float m_EdgeFadeTime = 1.5f;
        [SerializeField, Range(0f, 5f)] float m_EdgeWaitTIme = 1.0f;
        [SerializeField, Range(0f, 5f)] float m_FadeOutTime = 0.5f;

        const string GLOBAL_LIGHT_TAG = "Global Spot Light";

        /// <summary>
        /// ゲームの最終結果に応じたエフェクトを再生
        /// </summary>
        /// <param name="result">0:WIN 1:DEFEAT 2:DRAW </param>
        /// <returns></returns>
        public async UniTask PlayResult(int result)
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

            if (m_GlobalSpotLight == null)
            {
                GameObject light = GameObject.FindGameObjectWithTag(GLOBAL_LIGHT_TAG);
                if (light == null)
                {
                    Debug.LogError("グローバルスポットライトが見つかりませんでした");
                    return;
                }
                if (!light.TryGetComponent<Light2D>(out m_GlobalSpotLight))
                {
                    Debug.LogError("グローバルスポットライトコンポーネントが見つかりませんでした");
                    return;
                }
            }

            ResultEffectData effectData = result switch
            {
                0 => m_PatternVictory,
                1 => m_PatternDefeat,
                2 => m_PatternDraw,
                _ => null,
            };

            m_Volume.profile = effectData != null ? effectData.VolumeProfile : null;
            m_Volume.weight = 0f;
            m_Volume.gameObject.SetActive(effectData != null);
            m_GlobalSpotLight.gameObject.SetActive(false);
            m_Spot.gameObject.SetActive(true);
            m_BackMist.gameObject.SetActive(true);
            m_SimpleBack.gameObject.SetActive(true);
            effectData.NoEffect.gameObject.SetActive(true);
            effectData.Bevel.gameObject.SetActive(true);
            effectData.Edge.gameObject.SetActive(true);
            m_SimpleBlackBack.gameObject.SetActive(true);

            EffectCtrl.PlaySE(EffectSEType.Byun);

            effectData.NoEffect.color =
                new Color(effectData.NoEffect.color.r, effectData.NoEffect.color.g, effectData.NoEffect.color.b, 0f);
            effectData.Bevel.color =
                new Color(effectData.Bevel.color.r, effectData.Bevel.color.g, effectData.Bevel.color.b, 0f);
            effectData.Edge.color =
                new Color(effectData.Edge.color.r, effectData.Edge.color.g, effectData.Edge.color.b, 0f);
            m_SimpleBlackBack.color =
                new Color(m_SimpleBlackBack.color.r, m_SimpleBlackBack.color.g, m_SimpleBlackBack.color.b, 1f);

            await UniTask.Delay(TimeSpan.FromSeconds(m_FastBlackTime), cancellationToken: token);

            EffectCtrl.PlaySE(EffectSEType.AiryWhoosh);

            await effectData.Edge.DOFade(1.0f, m_EdgeFadeTime).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            switch (result)
            {
                case 0:
                    EffectCtrl.PlaySE(EffectSEType.Whoosh);
                    break;
                case 1:
                    EffectCtrl.PlaySE(EffectSEType.PianoPercussion);
                    break;
                case 2:
                    EffectCtrl.PlaySE(EffectSEType.PianoResonance);
                    break;

            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_EdgeWaitTIme), cancellationToken: token);

            Tween fadeNoEffectTween = effectData.NoEffect.DOFade(1.0f, m_FadeOutTime);
            Tween fadeBevelTween = effectData.Bevel.DOFade(1.0f, m_FadeOutTime);
            Tween fadeSimpleBlackBackTween = m_SimpleBlackBack.DOFade(0.0f, m_FadeOutTime);
            if (m_Volume.profile != null && result != 2)
            {
                Tween volumeTween = DOTween.To(
                () => m_Volume.weight,
                x => m_Volume.weight = x,
                1f,
                m_FadeOutTime
                );

                await UniTask.WhenAll(
                    fadeNoEffectTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    fadeBevelTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    fadeSimpleBlackBackTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    volumeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
            }
            else
            {
                await UniTask.WhenAll(
                    fadeNoEffectTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    fadeBevelTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    fadeSimpleBlackBackTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
            }
        }

        public void StopResult()
        {
            m_GlobalSpotLight.gameObject.SetActive(true);
        }

        [Serializable]
        class ResultEffectData
        {
            public SpriteRenderer NoEffect;
            public SpriteRenderer Bevel;
            public SpriteRenderer Edge;
            public VolumeProfile VolumeProfile;
        }
    }
}
