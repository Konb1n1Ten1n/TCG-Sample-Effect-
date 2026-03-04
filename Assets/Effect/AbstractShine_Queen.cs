using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Threading;

namespace DuelKingdom.Effect
{
    public class AbstractShine_Queen : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] Light2D[] m_HaloLight; // [0]: targetSp用, [1]: anotherSp用
        [SerializeField] Light2D m_SunLight;
        [SerializeField] Light2D[] m_AmbientLights;
        [SerializeField] SpriteRenderer m_DrawBackGround;
        [SerializeField] VolumeProfile m_QueenVolumeProfile;
        [SerializeField] SpriteRenderer m_CircleEmission;
        [SerializeField] ParticleSystem[] m_FeatherPS; // [0]: targetSp用, [1]: anotherSp用
        [SerializeField] Transform m_TargetMovePos;       // 1枚時の移動先
        [SerializeField] Vector2 m_TargetMovePosTwo;      // 2枚時の targetSp の移動先
        [SerializeField] Vector2 m_AnotherMovePosTwo;     // 2枚時の anotherSp の移動先

        [SerializeField] float m_TargetAmbientRayLightIntensity = 5f;
        [SerializeField] float m_TargetSunLightIntensity = 0.84f;
        [SerializeField] float m_TargetHaleIntensity = 8.6f;
        [SerializeField] float m_MidScale = 1.7f;
        [SerializeField] float m_MidScaleUpDura = 0.25f;
        [SerializeField] float m_LastScaleUpDura = 0.1f;
        [SerializeField] float m_MidRotateDura = 6f;
        [SerializeField] float m_TargetAlpha = 0.3f;
        [SerializeField] float m_LightVolumeDura = 3f;
        [SerializeField] float m_AmbientLightDura = 0.5f;
        [SerializeField] int m_SortingOrderOffset = 5;

        Sequence m_DiverseSeq;

        public async UniTask PlayAbsShineEffect(Transform target, Transform another = null)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("この関数はプレイ中にのみ有効です。");
                return;
            }
#endif
            this.transform.rotation = target.transform.rotation;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            KillAndClear(ref m_DiverseSeq);

            if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var targetNumTxt))
            {
                Debug.LogError("target の 子オブジェクト に TextMeshPro コンポーネントが見つかりませんでした。");
                return;
            }
            if (!target.TryGetComponent<SpriteRenderer>(out var targetSp))
            {
                Debug.LogError("target に SpriteRenderer コンポーネントが見つかりませんでした。");
            }

            TextMeshPro anotherNumTxt = null;
            SpriteRenderer anotherSp = null;
            if (another != null && !another.GetChild(0).TryGetComponent<TextMeshPro>(out anotherNumTxt))
            {
                Debug.LogError("anotherSp の 子オブジェクト に TextMeshPro コンポーネントが見つかりませんでした。");
                return;
            }
            if (another != null && !another.TryGetComponent<SpriteRenderer>(out anotherSp))
            {
                Debug.LogError("anotherSp に SpriteRenderer コンポーネントが見つかりませんでした。");
            }

            EffectCtrl.GlobalVolume.profile = m_QueenVolumeProfile;
            EffectCtrl.GlobalVolume.weight = 0f;
            m_DrawBackGround.color =
                new Color(m_DrawBackGround.color.r, m_DrawBackGround.color.g, m_DrawBackGround.color.b, 0f);
            m_DrawBackGround.gameObject.SetActive(true);
            m_DiverseSeq = DOTween.Sequence();

            targetSp.sortingOrder += m_SortingOrderOffset; // エフェクトより前に表示
            targetSp.sortingLayerID = SortingLayer.NameToID("NoLight"); // 光より前に表示
            targetNumTxt.sortingOrder += m_SortingOrderOffset;
            targetNumTxt.sortingLayerID = SortingLayer.NameToID("NoLight");
            if (anotherSp != null)
            {
                anotherSp.sortingOrder += m_SortingOrderOffset;
                anotherSp.sortingLayerID = SortingLayer.NameToID("NoLight");
                anotherNumTxt.sortingOrder += m_SortingOrderOffset;
                anotherNumTxt.sortingLayerID = SortingLayer.NameToID("NoLight");
            }

            m_CircleEmission.material.SetFloat("Rotate", 0);
            m_CircleEmission.gameObject.SetActive(true);

            // 2枚時は Vector2 変数、1枚時は Transform の位置を使用
            Vector3 targetDestPos = anotherSp != null
                ? new Vector3(m_TargetMovePosTwo.x, m_TargetMovePosTwo.y, targetSp.transform.position.z)
                : m_TargetMovePos.position;
            Vector3 anotherDestPos = anotherSp != null
                ? new Vector3(m_AnotherMovePosTwo.x, m_AnotherMovePosTwo.y, anotherSp.transform.position.z)
                : Vector3.zero;

            // 初期状態を保存
            Vector2 initialCardPos = targetSp.transform.position;
            Vector3 initialCardScale = targetSp.transform.localScale;
            Vector3 initialCardRotate = targetSp.transform.eulerAngles;
            Vector2 initialHalePos = m_HaloLight[0].transform.position;

            Vector2 initialAnotherCardPos = anotherSp != null ? (Vector2)anotherSp.transform.position : Vector2.zero;
            Vector3 initialAnotherCardScale = anotherSp != null ? anotherSp.transform.localScale : Vector3.one;
            Vector2 initialAnotherHalePos = anotherSp != null ? (Vector2)m_HaloLight[1].transform.position : Vector2.zero;

            DOTween.To(() => m_SunLight.intensity,
                            x => m_SunLight.intensity = x, m_TargetSunLightIntensity, m_AmbientLightDura);
            foreach (var light in m_AmbientLights)
            {
                DOTween.To(() => light.intensity,
                            x => light.intensity = x, m_TargetAmbientRayLightIntensity, m_AmbientLightDura);
            }

            DOTween.To(() => m_CircleEmission.material.GetFloat("Rotate"),
                            x => m_CircleEmission.material.SetFloat("Rotate", x),
                            1, m_AmbientLightDura);

            DOTween.To(() => m_HaloLight[0].intensity,
                        x => m_HaloLight[0].intensity = x, m_TargetHaleIntensity, m_LightVolumeDura);
            if (anotherSp != null)
            {
                DOTween.To(() => m_HaloLight[1].intensity,
                            x => m_HaloLight[1].intensity = x, m_TargetHaleIntensity, m_LightVolumeDura);
            }
            DOTween.To(() => EffectCtrl.GlobalVolume.weight,
                        x => EffectCtrl.GlobalVolume.weight = x, 1f, m_LightVolumeDura);
            m_DrawBackGround.DOFade(m_TargetAlpha, m_LightVolumeDura);

            // カード移動・スケールアップシーケンス
            m_DiverseSeq
                .Append(targetSp.transform.DOMove(targetDestPos, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                .Join(m_HaloLight[0].transform.DOMove(targetDestPos, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                .Join(targetSp.transform.DOScale(m_MidScale, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                .Join(targetSp.transform.DORotate(
                    new Vector3(targetSp.transform.eulerAngles.x,
                    360,
                    anotherSp != null ? 13 : targetSp.transform.eulerAngles.z + 13),
                    m_MidScaleUpDura, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

            if (anotherSp != null)
            {
                m_DiverseSeq
                    .Join(anotherSp.transform.DOMove(anotherDestPos, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                    .Join(m_HaloLight[1].transform.DOMove(anotherDestPos, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                    .Join(anotherSp.transform.DOScale(m_MidScale, m_MidScaleUpDura).SetEase(Ease.OutQuad))
                    .Join(anotherSp.transform.DORotate(new Vector3(
                        anotherSp.transform.eulerAngles.x, 
                        360, 
                        15), 
                        m_MidScaleUpDura, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            }

            EffectCtrl.PlaySE(EffectSEType.SingleMotionSwish);
            EffectCtrl.PlaySE(EffectSEType.Angel);
            await m_DiverseSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            EffectCtrl.PlaySE(EffectSEType.Bird);

            // FeatherPS を移動先の位置にセットして再生
            m_FeatherPS[0].transform.position = targetDestPos;
            m_FeatherPS[0].Play();
            if (anotherSp != null)
            {
                m_FeatherPS[1].transform.position = anotherDestPos;
                m_FeatherPS[1].Play();
            }

            // ホバーロテーション（両カード同時）
            Tween rotTween = targetSp.transform.DORotate(
                                new Vector3(
                                    targetSp.transform.eulerAngles.x,
                                    targetSp.transform.eulerAngles.y,
                                    anotherSp != null ? 13 : targetSp.transform.eulerAngles.z + 13),
                                m_MidRotateDura).SetEase(Ease.OutSine);
            if (anotherSp != null)
            {
                Tween anotherRotTween = anotherSp.transform.DORotate(
                                    new Vector3(
                                        anotherSp.transform.eulerAngles.x,
                                        anotherSp.transform.eulerAngles.y, 13), m_MidRotateDura).SetEase(Ease.OutSine);
                await UniTask.WhenAll(
                    rotTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    anotherRotTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                );
            }
            else
            {
                await UniTask.WhenAll(rotTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token));
            }

            // 元に戻す処理
            m_DiverseSeq = DOTween.Sequence();
            Tween circleTween = DOTween.To(() => m_CircleEmission.material.GetFloat("Rotate"),
                            x => m_CircleEmission.material.SetFloat("Rotate", x),
                            0, m_LastScaleUpDura);
            Tween haloTween = DOTween.To(() => m_HaloLight[0].intensity,
                            x => m_HaloLight[0].intensity = x, 0, m_LastScaleUpDura);
            Tween sunTween = DOTween.To(() => m_SunLight.intensity,
                            x => m_SunLight.intensity = x, 0, m_LastScaleUpDura);
            Tween volumeTween = DOTween.To(() => EffectCtrl.GlobalVolume.weight,
                            x => EffectCtrl.GlobalVolume.weight = x, 0f, m_LastScaleUpDura);
            rotTween = targetSp.transform.DORotate(
                                initialCardRotate, m_LastScaleUpDura, RotateMode.FastBeyond360).SetEase(Ease.OutSine);

            m_DiverseSeq
                .Append(circleTween)
                .Join(haloTween)
                .Join(targetSp.transform.DOMove(initialCardPos, m_LastScaleUpDura).SetEase(Ease.OutQuad))
                .Join(m_HaloLight[0].transform.DOMove(initialHalePos, m_LastScaleUpDura).SetEase(Ease.OutQuad))
                .Join(targetSp.transform.DOScale(initialCardScale, m_LastScaleUpDura).SetEase(Ease.OutQuad))
                .Join(m_DrawBackGround.DOFade(0, m_LastScaleUpDura))
                .Join(sunTween)
                .Join(volumeTween);

            if (anotherSp != null)
            {
                Tween haloTween2 = DOTween.To(() => m_HaloLight[1].intensity,
                                x => m_HaloLight[1].intensity = x, 0, m_LastScaleUpDura);
                Tween anotherRotTween = anotherSp.transform.DORotate(
                                    new Vector3(
                                        anotherSp.transform.eulerAngles.x,
                                        0, 0), m_LastScaleUpDura, RotateMode.FastBeyond360).SetEase(Ease.OutSine);
                m_DiverseSeq
                    .Join(haloTween2)
                    .Join(anotherSp.transform.DOMove(initialAnotherCardPos, m_LastScaleUpDura).SetEase(Ease.OutQuad))
                    .Join(m_HaloLight[1].transform.DOMove(initialAnotherHalePos, m_LastScaleUpDura).SetEase(Ease.OutQuad))
                    .Join(anotherSp.transform.DOScale(initialAnotherCardScale, m_LastScaleUpDura).SetEase(Ease.OutQuad));
            }

            foreach (var light in m_AmbientLights)
            {
                Tween lightTween = DOTween.To(() => light.intensity,
                                x => light.intensity = x, 0, m_LastScaleUpDura);
                m_DiverseSeq.Join(lightTween);
            }

            await m_DiverseSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            // targetSp の復元
            targetSp.sortingLayerID = SortingLayer.NameToID("Default");
            targetSp.sortingOrder -= m_SortingOrderOffset;
            targetNumTxt.sortingLayerID = SortingLayer.NameToID("Default");
            targetNumTxt.sortingOrder -= m_SortingOrderOffset;

            // anotherSp の復元
            if (anotherSp != null)
            {
                anotherSp.sortingLayerID = SortingLayer.NameToID("Default");
                anotherSp.sortingOrder -= m_SortingOrderOffset;
                anotherNumTxt.sortingLayerID = SortingLayer.NameToID("Default");
                anotherNumTxt.sortingOrder -= m_SortingOrderOffset;
            }

            m_CircleEmission.gameObject.SetActive(false);
        }

        void KillAndClear(ref Sequence seq, bool complete = false)
        {
            if (seq == null) return;
            // active チェックをしてから Kill する（既に自動破棄されている場合の防御）
            if (seq.active) seq.Kill(complete);
            seq = null;
        }
    }
}
