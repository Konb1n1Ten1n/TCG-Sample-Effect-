using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace DuelKingdom.Effect
{
    public class BackGroundCtrl : MonoBehaviour
    {
        [SerializeField] Light2D m_SpotLight;

        private void Start()
        {
            _ = RandomSpotLightFlashing();
        }

        async UniTask RandomSpotLightFlashing()
        {
            float inner = m_SpotLight.pointLightInnerRadius;
            float outer = m_SpotLight.pointLightOuterRadius;

            while (true)
            {
                float calc = Random.Range(-0.05f, 0.05f);
                float targetInner = inner + calc;
                float targetOuter = outer + calc;
                m_SpotLight.pointLightInnerRadius = targetInner;
                m_SpotLight.pointLightOuterRadius = targetOuter;
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
