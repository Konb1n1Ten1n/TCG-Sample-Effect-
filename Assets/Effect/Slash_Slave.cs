using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 奴隷の斬撃エフェクト
/// </summary>
namespace DuelKingdom.Effect
{
    public class Slash_Slave : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] ParticleSystem m_PS;
        [SerializeField] ParticleSystem m_PS2;
        [SerializeField] SpriteRenderer m_SlashSp;
        [SerializeField] SpriteRenderer m_SlashSp2;
        [SerializeField] SpriteRenderer m_BloodSp;
        [SerializeField] SpriteRenderer[] m_ChainSps;
        [SerializeField] SpriteRenderer m_BlackSp;

        [Header("Change Sprite & Material Color")]
        [SerializeField] Color m_SplasColor;

        [SerializeField] float m_BlackSpeed = 0.5f;

        const string SLASH_MAT_PROPERTY_0 = "_First";
        const string SLASH_MAT_PROPERTY_1 = "_Second";
        const float SLASH_MAT_VALUE_0_MIN = 0f;
        const float SLASH_MAT_VALUE_0_MAX = 1f;
        const float SLASH_MAT_VALUE_1_MIN = 0f;
        const float SLASH_MAT_VALUE_1_MAX = 1f;
        const float SLASH_ANIM_DURATION_0 = 0.15f;
        const float SLASH_ANIM_DURATION_1 = 0.2f;

        private void OnDestroy()
        {
            if (m_SlashSp != null)
            {
                m_SlashSp.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_0, SLASH_MAT_VALUE_0_MAX);
                m_SlashSp.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_1, SLASH_MAT_VALUE_1_MAX);
            }
            if (m_SlashSp2 != null)
            {
                m_SlashSp2.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_0, SLASH_MAT_VALUE_0_MAX);
                m_SlashSp2.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_1, SLASH_MAT_VALUE_1_MAX);
            }
        }

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

            m_BlackSp.material.SetFloat("_CircleRadius", 0f);
            m_BlackSp.gameObject.SetActive(true);

            Sequence chainSeq = DOTween.Sequence();
            Tween blackTw = DOTween.To(() => m_BlackSp.material.GetFloat("_CircleRadius"),
                                        x => m_BlackSp.material.SetFloat("_CircleRadius", x),
                                        0.4f, m_BlackSpeed);
            chainSeq.Append(blackTw)
                .JoinCallback(() => EffectCtrl.PlaySE(EffectSEType.Dark))
                .JoinCallback(() => EffectCtrl.PlaySE(EffectSEType.Chain));

            for (int i = 0; i < m_ChainSps.Length; i++)
            {
                int index = i;

                Tween fadeTw = DOTween.To(() => m_ChainSps[index].material.GetFloat("_Alpha"),
                                            x => m_ChainSps[index].material.SetFloat("_Alpha", x),
                                            1.0f, m_BlackSpeed).SetEase(Ease.OutSine);

                chainSeq.Join(fadeTw);

                m_ChainSps[index].gameObject.SetActive(true);
            }

            await chainSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            m_BloodSp.gameObject.SetActive(false);
            Color bloodmatColor = m_BloodSp.material.GetColor("_Color");
            Color setColor = new Color(bloodmatColor.r, bloodmatColor.g, bloodmatColor.b, 0);
            m_BloodSp.material.SetColor("_Color", setColor);
            // シェーダーの初期値を設定（両プロパティを最小値に）
            m_SlashSp.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_0, SLASH_MAT_VALUE_0_MAX);
            m_SlashSp.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_1, SLASH_MAT_VALUE_1_MAX);
            m_SlashSp2.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_0, SLASH_MAT_VALUE_0_MAX);
            m_SlashSp2.sharedMaterial.SetFloat(SLASH_MAT_PROPERTY_1, SLASH_MAT_VALUE_1_MAX);

            // パーティクルは遅延再生するため、別タスクで開始処理を走らせる
            _ = PlaySlashPS(m_PS);

            EffectCtrl.PlaySE(EffectSEType.Slash);
            _ = SlashEffect(SLASH_MAT_VALUE_0_MAX, SLASH_MAT_VALUE_0_MIN, SLASH_MAT_PROPERTY_0, SLASH_ANIM_DURATION_0, m_SlashSp);
            await UniTask.Delay(TimeSpan.FromMilliseconds(150), cancellationToken: token);
            await SlashEffect(SLASH_MAT_VALUE_1_MAX, SLASH_MAT_VALUE_1_MIN, SLASH_MAT_PROPERTY_1, SLASH_ANIM_DURATION_1, m_SlashSp);

            m_PS.Stop();

            _ = PlaySlashPS(m_PS2);
            _ = PlayBlood();

            EffectCtrl.PlaySE(EffectSEType.SlashHeavy);
            _ = SlashEffect(SLASH_MAT_VALUE_0_MAX, SLASH_MAT_VALUE_0_MIN, SLASH_MAT_PROPERTY_0, SLASH_ANIM_DURATION_0, m_SlashSp2);
            await UniTask.Delay(TimeSpan.FromMilliseconds(150), cancellationToken: token);
            await SlashEffect(SLASH_MAT_VALUE_1_MAX, SLASH_MAT_VALUE_1_MIN, SLASH_MAT_PROPERTY_1, SLASH_ANIM_DURATION_1, m_SlashSp2);
            m_PS2.Stop();

            await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: token);

            Sequence endSeq = DOTween.Sequence();
            Tween blackEndTw = DOTween.To(() => m_BlackSp.material.GetFloat("_CircleRadius"),
                                        x => m_BlackSp.material.SetFloat("_CircleRadius", x),
                                        0.0f, m_BlackSpeed);
            endSeq.Append(blackEndTw);

            for (int i = 0; i < m_ChainSps.Length; i++)
            {
                int index = i;

                Tween fadeTw = DOTween.To(() => m_ChainSps[index].material.GetFloat("_Alpha"),
                                            x => m_ChainSps[index].material.SetFloat("_Alpha", x),
                                            0.0f, m_BlackSpeed).SetEase(Ease.OutSine);

                endSeq.Join(fadeTw);

                m_ChainSps[index].gameObject.SetActive(true);
            }

            setColor = new Color(setColor.r, setColor.g, setColor.b, 0);
            endSeq.Join(m_BloodSp.material.DOColor(setColor, 0.5f));

            await endSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            m_BloodSp.gameObject.SetActive(false);

            // パーティクルを遅延して再生するローカル関数
            async UniTask PlaySlashPS(ParticleSystem ps)
            {
                // SLASH_ANIM_DURATION_0 / 2秒待ってからパーティクルを再生
                await UniTask.Delay(TimeSpan.FromSeconds(SLASH_ANIM_DURATION_0 / 2), cancellationToken: token);
                ps.Play();
            }

            async UniTask PlayBlood()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(SLASH_ANIM_DURATION_0 / 1), cancellationToken: token);

                float startScale = m_BloodSp.transform.localScale.x;
                m_BloodSp.transform.localScale = new Vector3(startScale * 1.2f, startScale * 1.2f, m_BloodSp.transform.localScale.z);
                m_BloodSp.gameObject.SetActive(true);
                setColor = new Color(setColor.r, setColor.g, setColor.b, m_SplasColor.a);
                m_BloodSp.material.DOColor(setColor, 0.05f).SetEase(Ease.OutSine);
                m_BloodSp.transform.DOScale(startScale, 0.05f).SetEase(Ease.OutSine);

            }

            // 指定プロパティを時間経過で線形補間してアニメーションするローカル関数
            async UniTask SlashEffect(float a, float b, string propertyName, float duration, SpriteRenderer sp)
            {
                float elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    float progress = Mathf.Lerp(a, b, elapsedTime / duration);
                    sp.sharedMaterial.SetFloat(propertyName, progress);

                    elapsedTime += Time.deltaTime;
                    await UniTask.Yield(cancellationToken: token);
                }

                sp.sharedMaterial.SetFloat(propertyName, b);
                elapsedTime = 0;
            }
        }
    }
}