using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DuelKingdom.Effect
{
    public class TurnEnd_Result : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        bool m_IsEffectStarted;
        VolumeProfile m_OriginalVolumeProfile;
        float m_OriginalVolumeWeight;

        [SerializeField] VolumeProfile m_GoldVolumeProfile;
        [SerializeField] Light2D m_GlobalLight;
        [SerializeField] ParticleSystem m_MainPS;
        [SerializeField] ParticleSystem m_LeftPS;
        [SerializeField] ParticleSystem m_RightUpPS;
        [SerializeField] TextMeshPro m_ResultText;
        [SerializeField] BGgroup m_PatternWin;
        [SerializeField] BGgroup m_PatternLose;
        [SerializeField] BGgroup m_PatternDraw;

        const string RESULT_VICTORY_TEXT = "WIN";
        const string RESULT_DEFEAT_TEXT = "LOSE";
        const string RESULT_DRAW_TEXT = "DRAW";

        private void OnDestroy()
        {
            if (!m_IsEffectStarted || EffectCtrl == null) return;
            EffectCtrl.GlobalVolume.profile = m_OriginalVolumeProfile;
            EffectCtrl.GlobalVolume.weight = m_OriginalVolumeWeight;
        }

        /// <summary>
        /// ターン終了時のエフェクトを再生
        /// </summary>
        /// <param name="turnEndResult">0:引き分け 1:勝ち 2:負け</param>
        /// <param name="getPointNum"></param>
        /// <param name="onGoldEffect"></param>
        /// <returns></returns>
        public async UniTask PlayTurnEndEffect(int turnEndResult, int getPointNum, bool MyCheck, bool onGoldEffect)
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

            m_OriginalVolumeProfile = EffectCtrl.GlobalVolume.profile;
            m_OriginalVolumeWeight = EffectCtrl.GlobalVolume.weight;
            m_IsEffectStarted = true;

            if (onGoldEffect)
            {
                EffectCtrl.GlobalVolume.profile = m_GoldVolumeProfile;
                EffectCtrl.GlobalVolume.weight = 1;
                _ = DelayPS();
            }

            SpriteRenderer backGround = null;
            SpriteRenderer simpleBackGround = null;
            SpriteRenderer simpleBackGround2 = null;
            string pointText = string.Empty;

            switch (turnEndResult)
            {
                case EffectController.RESULT_WIN:
                    backGround = m_PatternWin.BackGround;
                    simpleBackGround = m_PatternWin.SimpleBackGround;
                    simpleBackGround2 = m_PatternWin.SimpleBackGround2;
                    simpleBackGround2.color = new Color(simpleBackGround2.color.r, simpleBackGround2.color.g, simpleBackGround2.color.b, 0f);
                    simpleBackGround2.gameObject.SetActive(true);
                    if (getPointNum > 0) pointText = "\n<size=40>+" + getPointNum.ToString() + " Point</size>";
                    m_ResultText.text = RESULT_VICTORY_TEXT + pointText;
                    break;
                case EffectController.RESULT_DEFEAT:
                    backGround = m_PatternLose.BackGround;
                    simpleBackGround = m_PatternLose.SimpleBackGround;
                    simpleBackGround2 = m_PatternLose.SimpleBackGround2;
                    simpleBackGround2.color = new Color(simpleBackGround2.color.r, simpleBackGround2.color.g, simpleBackGround2.color.b, 0f);
                    simpleBackGround2.gameObject.SetActive(true);
                    m_ResultText.text = RESULT_DEFEAT_TEXT;
                    break;
                case EffectController.RESULT_DRAW:
                    backGround = m_PatternDraw.BackGround;
                    simpleBackGround = m_PatternDraw.SimpleBackGround;
                    simpleBackGround2 = null;
                    m_ResultText.text = RESULT_DRAW_TEXT;
                    break;
            }

            simpleBackGround.color = new Color(simpleBackGround.color.r, simpleBackGround.color.g, simpleBackGround.color.b, 0f);
            backGround.color = new Color(backGround.color.r, backGround.color.g, backGround.color.b, 0f);
            simpleBackGround.gameObject.SetActive(true);
            backGround.gameObject.SetActive(true);

            m_ResultText.color = new Color(m_ResultText.color.r, m_ResultText.color.g, m_ResultText.color.b, 0f);
            m_ResultText.rectTransform.localScale = Vector2.one * 0.25f;
            m_ResultText.gameObject.SetActive(true);

            Sequence seSeq = DOTween.Sequence();
            if (onGoldEffect)
            {
                seSeq.AppendInterval(1.1f)
                    .AppendCallback(() => EffectCtrl.PlaySE(EffectSEType.CinematicBuoon));

                await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: token);
            }
            else
            {
                EffectCtrl.PlaySE(EffectSEType.CinematicBuoon);

                await UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: token);
            }

            _ = EffectCtrl.PlayScoreEffect(getPointNum, MyCheck);

            {
                if (turnEndResult == 2)
                {
                    Tween simpleBackGTeen = simpleBackGround.DOFade(0.65f, 0.2f).SetEase(Ease.InSine);
                    Tween backGTeen = backGround.DOFade(1.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textFadeTeen = m_ResultText.DOFade(1.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textScaleTeen = m_ResultText.rectTransform.DOScale(0.09f, 0.2f).SetEase(Ease.InSine);

                    await UniTask.WhenAll(
                    simpleBackGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    backGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textFadeTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textScaleTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
                }
                else
                {
                    Tween simpleBackGTeen = simpleBackGround.DOFade(0.65f, 0.2f).SetEase(Ease.InSine);
                    Tween simpleBackG2Teen = simpleBackGround2.DOFade(0.72f, 0.2f).SetEase(Ease.InSine);
                    Tween backGTeen = backGround.DOFade(1.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textFadeTeen = m_ResultText.DOFade(1.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textScaleTeen = m_ResultText.rectTransform.DOScale(0.09f, 0.2f).SetEase(Ease.InSine);

                    await UniTask.WhenAll(
                    simpleBackGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    simpleBackG2Teen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    backGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textFadeTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textScaleTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
                }
            }

            await m_ResultText.rectTransform.DOScale(0.08f, 1.5f).SetEase(Ease.InSine).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            {
                if (turnEndResult == 2)
                {
                    Tween simpleBackGTeen = simpleBackGround.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween backGTeen = backGround.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textFadeTeen = m_ResultText.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textScaleTeen = m_ResultText.rectTransform.DOScale(0.05f, 0.2f).SetEase(Ease.InSine);

                    await UniTask.WhenAll(
                    simpleBackGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    backGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textFadeTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textScaleTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
                }
                else
                {
                    Tween simpleBackGTeen = simpleBackGround.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween simpleBackG2Teen = simpleBackGround2.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween backGTeen = backGround.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textFadeTeen = m_ResultText.DOFade(0.0f, 0.2f).SetEase(Ease.InSine);
                    Tween textScaleTeen = m_ResultText.rectTransform.DOScale(0.05f, 0.2f).SetEase(Ease.InSine);

                    await UniTask.WhenAll(
                    simpleBackGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    simpleBackG2Teen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    backGTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textFadeTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    textScaleTeen.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
                }
            }

            m_ResultText.gameObject.SetActive(false);
            simpleBackGround.gameObject.SetActive(false);
            if (turnEndResult != 2) simpleBackGround2.gameObject.SetActive(false);
            backGround.gameObject.SetActive(false);

            await UniTask.WaitUntil(() => NoneParticle(), cancellationToken: token);

            m_IsEffectStarted = false;
            EffectCtrl.GlobalVolume.profile = m_OriginalVolumeProfile;
            EffectCtrl.GlobalVolume.weight = m_OriginalVolumeWeight;

            async UniTask DelayPS()
            {
                EffectCtrl.PlaySE(EffectSEType.Gold);
                m_MainPS.Play();
                m_LeftPS.Play();
                m_RightUpPS.Play();

                await UniTask.Delay(System.TimeSpan.FromSeconds(3.5f), cancellationToken: token);

                m_MainPS.Stop();
                m_LeftPS.Stop();
                m_RightUpPS.Stop();
            }

            bool NoneParticle()
            {
                if (m_LeftPS.particleCount > 0) return false;
                if (m_RightUpPS.particleCount > 0) return false;
                if (m_MainPS.particleCount > 0) return false;
                return true;
            }
        }

        [Serializable]
        class BGgroup
        {
            public SpriteRenderer BackGround;
            public SpriteRenderer SimpleBackGround;
            public SpriteRenderer SimpleBackGround2;
        }
    }
}
