using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DuelKingdom.Effect
{
    public class Mist_Witch : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] ParticleSystem m_PS;
        [SerializeField] Light2D m_SpotLight;

        public async UniTask PlayMistEffect(int sourceNum, TextMeshProUGUI sourceTMpro, int changeNum, TextMeshProUGUI changeTMpro)
        {
            m_PS.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(2.0f));

            sourceTMpro.text = sourceNum.ToString();
            changeTMpro.text = changeNum.ToString();

            await UniTask.WaitUntil(() => m_PS.particleCount <= 0);
        }

        public async UniTask ExamplePlayMist(Transform target)
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

            if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var tmp))
            {
                Debug.LogError("ターゲットの子オブジェクトにTextMeshProUGUIコンポーネントが見つかりませんでした。");
                return;
            }

            this.transform.position = tmp.transform.position;

            EffectCtrl.PlaySE(EffectSEType.Mist);
            _ = OnLightUp();
            m_PS.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: token);

            await UniTask.WaitUntil(() => m_PS.particleCount <= 0, cancellationToken: token);

            async UniTask OnLightUp()
            {
                float firstOuterRadius = 0.58f;
                m_SpotLight.pointLightOuterRadius = 0.0f;
                m_SpotLight.intensity = 1.0f;

                DOTween.To(
                    () => m_SpotLight.pointLightOuterRadius,
                    (value) => m_SpotLight.pointLightOuterRadius = value,
                    firstOuterRadius,
                    1.8f
                ).SetEase(Ease.OutQuad);
                await UniTask.Delay(TimeSpan.FromSeconds(3.5f), cancellationToken: token);
                DOTween.To(
                    () => m_SpotLight.intensity,
                    (value) => m_SpotLight.intensity = value,
                    0,
                    1.75f
                ).SetEase(Ease.OutQuad);
                await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: token);
                m_SpotLight.pointLightOuterRadius = 0.0f;
            }
        }
    }
}