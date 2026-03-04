using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DuelKingdom.Effect
{
    public class Bubble_Doctor : MonoBehaviour
    {
        public TextMeshPro SampleTargetTMpro;
        [HideInInspector] public EffectController EffectCtrl;

        [SerializeField] ParticleSystem m_Ps;
        [SerializeField] ParticleSystemRenderer m_PsRenderer;
        [SerializeField] Light2D m_SpotLight;

        [Header("Parameter")]
        [SerializeField] int m_IncreaseValue = 2;
        [SerializeField] float m_TargetLightOuterRadius = 1.0f;
        [SerializeField, ColorUsage(true, true)] Color m_ChangeColor;
        [Header("Time")]
        [SerializeField] float m_PsStartDelay = 2.5f;
        [SerializeField] float m_LightUpTime = 0.7f;

        public async UniTask PlayBubbleEffect(TextMeshPro targetTMpro)
        {
            // 初期化
            this.transform.position = targetTMpro.transform.position;
            m_SpotLight.pointLightOuterRadius = 0;
            Transform originalParent = targetTMpro.rectTransform.parent;
            targetTMpro.rectTransform.SetParent(this.transform);
            float firstPosY = targetTMpro.rectTransform.anchoredPosition.y;
            m_PsRenderer.material.SetFloat("_Alpha", 1);

            EffectCtrl.PlaySE(EffectSEType.Bubble);
            m_Ps.Play();

            await UniTask.Delay(System.TimeSpan.FromSeconds(m_PsStartDelay));

            // 上昇＆ライトアップ
            EffectCtrl.PlaySE(EffectSEType.Angel_Faa);
            await DOTween.To(() => m_SpotLight.pointLightOuterRadius,
                    x => m_SpotLight.pointLightOuterRadius = x, m_TargetLightOuterRadius, m_LightUpTime)
                    .SetEase(Ease.OutSine).AsyncWaitForCompletion();

            // 数字変更
            int value = int.Parse(targetTMpro.text) + m_IncreaseValue;
            targetTMpro.fontMaterial.SetColor("_GlowColor", m_ChangeColor);
            targetTMpro.fontMaterial.EnableKeyword("GLOW_ON");

            await targetTMpro.DOFade(0f, 0.3f).AsyncWaitForCompletion();
            DOTween.To(() => m_PsRenderer.material.GetFloat("_Alpha"),
                    x => m_PsRenderer.material.SetFloat("_Alpha", x), 0, 2f)
                    .SetEase(Ease.OutSine).OnComplete(() => m_Ps.Clear());

            targetTMpro.text = value.ToString();

            targetTMpro.color = new Color(targetTMpro.color.r, targetTMpro.color.g, targetTMpro.color.b, 1f);

            // 下降＆ライトダウン
            await DOTween.To(() => m_SpotLight.pointLightOuterRadius,
                    x => m_SpotLight.pointLightOuterRadius = x, 0, m_LightUpTime)
                    .SetEase(Ease.OutSine).AsyncWaitForCompletion();

            m_Ps.Stop();
            targetTMpro.rectTransform.SetParent(originalParent);
        }
    }
}