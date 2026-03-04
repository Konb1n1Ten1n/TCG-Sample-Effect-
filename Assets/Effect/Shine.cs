using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Color = UnityEngine.Color;

namespace DuelKingdom.Effect
{
    public class Shine : MonoBehaviour
    {
        public int SampleUndoNum;

        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] Light2D m_TextLight;
        [SerializeField] RectTransform m_TargetRisePos;
        [SerializeField] RectTransform m_OriginalPos;
        [SerializeField] TextMeshPro m_TempTMpro;
        [SerializeField, ColorUsage(true, true)] Color m_ChangeColor;

        [Header("Parameter")]
        [SerializeField] float m_TextLightIntensity = 1.0f;

        Transform m_OriginalParent;

        const float LIGHT_UP_TIME = 0.25f;
        const float NUM_RISE_TIME = 1.0f;
        const float NUM_CHANGE_TIME = 0.5f;

        public async UniTask PlayShineEffect(Transform target, int changeNum, bool? onWitch = false)
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

            if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var numTMpro))
            {
                Debug.LogError("ターゲットにTextMeshProコンポーネントが見つかりませんでした。");
                return;
            }

            // 初期化
            Color lightColor = onWitch.Value ? m_EffectCtrl.WitchShineLightColor : m_EffectCtrl.BasicShineLightColor;
            Color glowColor = onWitch.Value ? m_EffectCtrl.WitchShineGlowColor : m_EffectCtrl.BasicShineGlowColor;

            m_TextLight.color = lightColor;
            m_TextLight.intensity = 0.0f;
            m_TextLight.transform.localPosition = m_OriginalPos.localPosition;
            this.transform.position = numTMpro.transform.position;

            // 数字が光る
            EffectCtrl.PlaySE(EffectSEType.Shine);
            DOTween.To(() => m_TextLight.intensity, x => m_TextLight.intensity = x, m_TextLightIntensity, NUM_RISE_TIME).SetEase(Ease.OutSine);

            await UniTask.Delay(TimeSpan.FromSeconds(NUM_RISE_TIME), cancellationToken: token);

            await ChangeNumber(numTMpro, changeNum, onWitch, true);

            Tween textLightDownTween =
                DOTween.To(() => m_TextLight.intensity, x => m_TextLight.intensity = x, 0, NUM_RISE_TIME)
                .SetEase(Ease.OutSine);

            await UniTask.WhenAll(
                textLightDownTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                );

            await UniTask.Delay(TimeSpan.FromSeconds(LIGHT_UP_TIME), cancellationToken: token);
        }

        /// <summary>
        /// 数字を変えるエフェクトのみを再生する関数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="targetObj"></param>
        /// <param name="changeNum"></param>
        /// <param name="onJointly">この関数単体で再生する場合はfalseかnull</param>
        /// <returns></returns>
        public async UniTask ChangeNumber<T>(T targetObj, int changeNum, bool ? onWitch = false, bool ? onJointly = false) where T : UnityEngine.Component
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            TextMeshPro numTMpro = null;

            if (targetObj is Transform t)
            {
                if (!t.GetChild(0).TryGetComponent<TextMeshPro>(out numTMpro))
                {
                    Debug.LogError("ターゲットにTextMeshProコンポーネントが見つかりませんでした。");
                    return;
                }
            }
            else if (targetObj is TextMeshPro tmp)
            {
                numTMpro = tmp;
            }

            if (!onJointly.Value)
            {
                this.transform.position = numTMpro.transform.position;
            }

            Color glowColor = onWitch.Value ? m_EffectCtrl.WitchShineGlowColor : m_EffectCtrl.BasicShineGlowColor;
            float firstPosY = numTMpro.rectTransform.anchoredPosition.y;

            m_OriginalParent = numTMpro.rectTransform.parent;
            numTMpro.rectTransform.SetParent(this.transform);

            m_TempTMpro.rectTransform.localScale = numTMpro.rectTransform.localScale;

            m_TempTMpro.color = new Color(m_TempTMpro.color.r, m_TempTMpro.color.g, m_TempTMpro.color.b, 0f);
            m_TempTMpro.fontMaterial.SetColor("_GlowColor", glowColor);
            m_TempTMpro.fontMaterial.EnableKeyword("GLOW_ON");

            // 数字を変える
            m_TempTMpro.gameObject.SetActive(true);
            m_TempTMpro.text = changeNum.ToString();
            Tween tempFadeTween =
                m_TempTMpro.DOFade(1f, NUM_CHANGE_TIME);
            Tween numFadeTween =
                numTMpro.DOFade(0f, NUM_CHANGE_TIME);

            numTMpro.fontMaterial.SetColor("_GlowColor", glowColor);
            numTMpro.fontMaterial.EnableKeyword("GLOW_ON");

            await UniTask.WhenAll(tempFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token), numFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token));

            m_TempTMpro.gameObject.SetActive(false);
            numTMpro.text = changeNum.ToString();
            numTMpro.alpha = 1f;
            await UniTask.Delay(TimeSpan.FromSeconds(NUM_CHANGE_TIME), cancellationToken: token);

            // 元に戻す
            numTMpro.rectTransform.SetParent(m_OriginalParent, true);
            numTMpro.rectTransform.anchoredPosition = new Vector2(numTMpro.rectTransform.anchoredPosition.x, firstPosY);
        }

        public async UniTask UndoNumber<T>(T targetObj, int changeNum) where T : UnityEngine.Component
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            TextMeshPro numTMpro = null;

            if (targetObj is Transform t)
            {
                if (!t.GetChild(0).TryGetComponent<TextMeshPro>(out numTMpro))
                {
                    Debug.LogError("ターゲットにTextMeshProコンポーネントが見つかりませんでした。");
                    return;
                }
            }
            else if (targetObj is TextMeshPro tmp)
            {
                numTMpro = tmp;
            }

            float firstPosY = numTMpro.rectTransform.anchoredPosition.y;

            m_OriginalParent = numTMpro.rectTransform.parent;
            numTMpro.rectTransform.SetParent(this.transform);

            m_TempTMpro.rectTransform.anchoredPosition = numTMpro.rectTransform.anchoredPosition;
            m_TempTMpro.rectTransform.localScale = numTMpro.rectTransform.localScale;

            m_TempTMpro.color = new Color(m_TempTMpro.color.r, m_TempTMpro.color.g, m_TempTMpro.color.b, 0f);
            m_TempTMpro.fontMaterial.DisableKeyword("GLOW_ON");

            // 数字を変える
            m_TempTMpro.gameObject.SetActive(true);
            m_TempTMpro.text = changeNum.ToString();
            Tween tempFadeTween =
                m_TempTMpro.DOFade(1f, NUM_CHANGE_TIME);
            Tween numFadeTween =
                numTMpro.DOFade(0f, NUM_CHANGE_TIME);

            numTMpro.fontMaterial.DisableKeyword("GLOW_ON");

            await UniTask.WhenAll(tempFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token), numFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token));

            m_TempTMpro.gameObject.SetActive(false);
            numTMpro.text = changeNum.ToString();
            numTMpro.alpha = 1f;
            await UniTask.Delay(TimeSpan.FromSeconds(NUM_CHANGE_TIME), cancellationToken: token);

            // 元に戻す
            numTMpro.rectTransform.SetParent(m_OriginalParent, true);
            numTMpro.rectTransform.anchoredPosition = new Vector2(numTMpro.rectTransform.anchoredPosition.x, firstPosY);
        }
    }
}