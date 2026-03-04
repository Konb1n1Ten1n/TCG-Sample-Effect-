using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DuelKingdom.Effect
{
    public class Burst_King : MonoBehaviour
    {
        EffectController m_EffectCtrl;
        CancellationToken m_ExternalToken;
        [HideInInspector] public EffectController EffectCtrl
        {
            get => m_EffectCtrl;
            set { m_EffectCtrl = value; m_ExternalToken = value != null ? value.EffectCancellationToken : default; }
        }

        [SerializeField] VolumeProfile m_KingVolumeProfile;
        [SerializeField] SpriteRenderer m_BlackSp;
        [SerializeField] SpriteRenderer[] m_PileVerticalSp;
        [SerializeField] SpriteRenderer[] m_PileHorizontalSp;
        [SerializeField] ParticleSystem m_BackGroundPS;
        [SerializeField] ParticleSystem[] m_FlamePS; // 2枚対応のため配列化
        public BoxCollider2D[] BurstPool; // 2枚対応のため配列化
        public BurstSpriteSet[] BurstSpriteSets; // バーストスプライトを複数セット用意してランダムに選ぶための配列

        [SerializeField] PileDropSetting m_PileVerticalSetting;
        [SerializeField] PileDropSetting m_PileHorizontalSetting;
        [SerializeField] float m_ShakeDuration = 0.5f;
        [SerializeField] Vector3 m_ShakeVerStrength;
        [SerializeField] Vector3 m_ShakeHorStrength;
        [SerializeField, Range(1, 50)] int m_BurstCount = 6;
        [SerializeField, Range(0.0f, 3.0f)] float m_BurstInterval = 0.05f;
        [SerializeField, Range(0.0f, 3.0f)] float m_StartBGFadeDelay = 1;
        [SerializeField, Range(0.0f, 3.0f)] float m_EndBGFadeDelay = 0.5f;

        [Serializable]
        public class BurstSpriteSet
        {
            public SpriteRenderer[] BurstSprites;
        }

        [Serializable]
        struct PileDropSetting
        {
            public float angleDeg;
            public float startDistance;
            public float duration;
            public float rotateToZ;
            public Ease moveEase;
            public Ease rotateEase;
        }

        public async UniTask PlayBurstEffect(Transform target, Transform another = null)
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

            if (!target.GetChild(0).TryGetComponent<TextMeshPro>(out var targetTxt))
            {
                Debug.LogError("ターゲットの子オブジェクトにTextMeshProコンポーネントが見つかりませんでした。");
                return;
            }
            if (!target.TryGetComponent<SpriteRenderer>(out var targetSp))
            {
                Debug.LogError("ターゲットにSpriteRendererコンポーネントが見つかりませんでした。");
                return;
            }

            TextMeshPro anotherTxt = null;
            SpriteRenderer anotherSp = null;
            if (another != null && !another.GetChild(0).TryGetComponent<TextMeshPro>(out anotherTxt))
            {
                Debug.LogError("another の子オブジェクトにTextMeshProコンポーネントが見つかりませんでした。");
                return;
            }
            if (another != null && !another.TryGetComponent<SpriteRenderer>(out anotherSp))
            {
                Debug.LogError("another にSpriteRendererコンポーネントが見つかりませんでした。");
                return;
            }

            VolumeProfile originalVolume = EffectCtrl.GlobalVolume.profile;
            float originalWeight = EffectCtrl.GlobalVolume.weight;
            EffectCtrl.GlobalVolume.weight = 0.4f;
            EffectCtrl.GlobalVolume.profile = m_KingVolumeProfile;

            m_FlamePS[0].transform.position = new Vector3(targetSp.transform.position.x, targetSp.transform.position.y - 1f, targetSp.transform.position.z);
            if (anotherSp != null)
                m_FlamePS[1].transform.position = new Vector3(anotherSp.transform.position.x, anotherSp.transform.position.y - 1f, anotherSp.transform.position.z);
            targetSp.sortingOrder += 2; // エフェクトより前に表示
            targetTxt.sortingOrder += 3; // エフェクトより前に表示
            if (anotherSp != null)
            {
                anotherSp.sortingOrder += 2;
                anotherTxt.sortingOrder += 3;
            }
            m_BlackSp.color = new Color(m_BlackSp.color.r, m_BlackSp.color.g, m_BlackSp.color.b, 0f);
            m_BlackSp.gameObject.SetActive(true);

            m_PileVerticalSp[0].transform.eulerAngles = Vector3.zero;
            m_PileVerticalSp[0].gameObject.SetActive(true);

            Vector3 targetVerPos = new Vector3(target.position.x, target.position.y - 0.5f, target.position.z);

            if (anotherSp != null)
            {
                m_PileVerticalSp[1].transform.eulerAngles = Vector3.zero;
                m_PileVerticalSp[1].gameObject.SetActive(true);
                Vector3 anotherVerPos = new Vector3(another.position.x, another.position.y - 1f, another.position.z);
                await UniTask.WhenAll(
                    DropPileDiagonalTo(m_PileVerticalSp[0], targetVerPos, angleDeg: m_PileVerticalSetting.angleDeg, startDistance: m_PileVerticalSetting.startDistance, duration: m_PileVerticalSetting.duration, rotateToZ: m_PileVerticalSetting.rotateToZ, moveEase: m_PileVerticalSetting.moveEase, rotateEase: m_PileVerticalSetting.rotateEase),
                    DropPileDiagonalTo(m_PileVerticalSp[1], anotherVerPos, angleDeg: m_PileVerticalSetting.angleDeg, startDistance: m_PileVerticalSetting.startDistance, duration: m_PileVerticalSetting.duration, rotateToZ: m_PileVerticalSetting.rotateToZ, moveEase: m_PileVerticalSetting.moveEase, rotateEase: m_PileVerticalSetting.rotateEase)
                );
            }
            else
            {
                await DropPileDiagonalTo(
                    m_PileVerticalSp[0],
                    targetVerPos,
                    angleDeg: m_PileVerticalSetting.angleDeg,
                    startDistance: m_PileVerticalSetting.startDistance,
                    duration: m_PileVerticalSetting.duration,
                    rotateToZ: m_PileVerticalSetting.rotateToZ,
                    moveEase: m_PileVerticalSetting.moveEase,
                    rotateEase: m_PileVerticalSetting.rotateEase
                );
            }
            _ = PlayBlackFade();
            await EffectCtrl.MainCam.transform.DOShakePosition(
                m_ShakeDuration,
                strength: m_ShakeVerStrength,
                randomness: 90,
                snapping: false,
                fadeOut: true
                ).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            m_PileHorizontalSp[0].transform.eulerAngles = Vector3.zero;
            m_PileHorizontalSp[0].gameObject.SetActive(true);
            Vector3 targetHorPos = new Vector3(targetSp.transform.position.x, targetSp.transform.position.y - 1.26f, targetSp.transform.position.z);
            if (anotherSp != null)
            {
                m_PileHorizontalSp[1].transform.eulerAngles = Vector3.zero;
                m_PileHorizontalSp[1].gameObject.SetActive(true);
                Vector3 anotherHorPos = new Vector3(another.position.x, another.position.y - 1.26f, another.position.z);
                await UniTask.WhenAll(
                    DropPileDiagonalTo(m_PileHorizontalSp[0], targetHorPos, angleDeg: m_PileHorizontalSetting.angleDeg, startDistance: m_PileHorizontalSetting.startDistance, duration: m_PileHorizontalSetting.duration, rotateToZ: m_PileHorizontalSetting.rotateToZ, moveEase: m_PileHorizontalSetting.moveEase, rotateEase: m_PileHorizontalSetting.rotateEase),
                    DropPileDiagonalTo(m_PileHorizontalSp[1], anotherHorPos, angleDeg: m_PileHorizontalSetting.angleDeg, startDistance: m_PileHorizontalSetting.startDistance, duration: m_PileHorizontalSetting.duration, rotateToZ: m_PileHorizontalSetting.rotateToZ, moveEase: m_PileHorizontalSetting.moveEase, rotateEase: m_PileHorizontalSetting.rotateEase)
                );
            }
            else
            {
                await DropPileDiagonalTo(
                    m_PileHorizontalSp[0],
                    targetHorPos,
                    angleDeg: m_PileHorizontalSetting.angleDeg,
                    startDistance: m_PileHorizontalSetting.startDistance,
                    duration: m_PileHorizontalSetting.duration,
                    rotateToZ: m_PileHorizontalSetting.rotateToZ,
                    moveEase: m_PileHorizontalSetting.moveEase,
                    rotateEase: m_PileHorizontalSetting.rotateEase
                );
            }
            _ = PlayBlackFade();
            await EffectCtrl.MainCam.transform.DOShakePosition(
                m_ShakeDuration,
                strength: m_ShakeHorStrength,
                randomness: 90,
                snapping: false,
                fadeOut: true
                ).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            _ = PlayBackgroundFade();

            _ = PlayFade();

            EffectCtrl.PlaySE(EffectSEType.Meramera);
            BurstPool[0].transform.position = target.position;
            if (another != null) BurstPool[1].transform.position = another.position;
            for (int i = 0; i < m_BurstCount; i++)
            {
                int a = i - BurstSpriteSets[0].BurstSprites.Length * (i / BurstSpriteSets[0].BurstSprites.Length);
                _ = PlayBurst(0, a, BurstPool[0], targetSp);
                if (anotherSp != null)
                {
                    // BurstSpriteSets がカードごとに独立しているため競合なし
                    int b = i - BurstSpriteSets[1].BurstSprites.Length * (i / BurstSpriteSets[1].BurstSprites.Length);
                    _ = PlayBurst(1, b, BurstPool[1], anotherSp);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(m_BurstInterval), cancellationToken: token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_EndBGFadeDelay), cancellationToken: token);

            // 背景を戻す
            m_FlamePS[0].Stop();
            if (anotherSp != null) m_FlamePS[1].Stop();
            if (m_BackGroundPS != null && m_BackGroundPS.particleCount > 0)
            {
                _ = FadeOutExistingParticles(m_BackGroundPS, 0f, 2f, stopEmission: true, clearAfter: false);
            }
            DOTween.To(() => EffectCtrl.BackGroundSp.material.GetFloat("_Power_Lerp"),
                        x => EffectCtrl.BackGroundSp.material.SetFloat("_Power_Lerp", x), 1, 2f)
                        .SetEase(Ease.OutSine);
            Tween verFade = DOTween.To(() => m_PileVerticalSp[0].material.GetFloat("_Alpha"),
                        x => m_PileVerticalSp[0].material.SetFloat("_Alpha", x), 0, 2f)
                        .SetEase(Ease.OutSine);
            Tween horFade = DOTween.To(() => m_PileHorizontalSp[0].material.GetFloat("_Alpha"),
                        x => m_PileHorizontalSp[0].material.SetFloat("_Alpha", x), 0, 2f)
                        .SetEase(Ease.OutSine);

            if (anotherSp != null)
            {
                Tween verFade2 = DOTween.To(() => m_PileVerticalSp[1].material.GetFloat("_Alpha"),
                            x => m_PileVerticalSp[1].material.SetFloat("_Alpha", x), 0, 2f)
                            .SetEase(Ease.OutSine);
                Tween horFade2 = DOTween.To(() => m_PileHorizontalSp[1].material.GetFloat("_Alpha"),
                            x => m_PileHorizontalSp[1].material.SetFloat("_Alpha", x), 0, 2f)
                            .SetEase(Ease.OutSine);
                await UniTask.WhenAll(
                    verFade.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    horFade.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    verFade2.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    horFade2.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                );
                m_PileVerticalSp[1].gameObject.SetActive(false);
                m_PileHorizontalSp[1].gameObject.SetActive(false);
            }
            else
            {
                await UniTask.WhenAll(
                    verFade.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                    horFade.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                );
            }

            m_PileVerticalSp[0].gameObject.SetActive(false);
            m_PileHorizontalSp[0].gameObject.SetActive(false);
            m_BlackSp.gameObject.SetActive(false);
            targetSp.sortingOrder -= 2;
            targetTxt.sortingOrder -= 3;
            if (anotherSp != null)
            {
                anotherSp.sortingOrder -= 2;
                anotherTxt.sortingOrder -= 3;
            }

            await DOTween.To(() => EffectCtrl.GlobalVolume.weight,
                        x => EffectCtrl.GlobalVolume.weight = x, originalWeight, 1f).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            EffectCtrl.GlobalVolume.profile = originalVolume;

            async UniTask PlayFade()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.75f), cancellationToken: token);
                // カード燃えてなくなる演出
                Tween textFadeTween = targetTxt.DOFade(0, 0.5f);
                Tween spFadeTween = DOTween.To(() => targetSp.material.GetFloat("_FlameIntencity"),
                        x => targetSp.material.SetFloat("_FlameIntencity", x), 1, 1.5f)
                        .SetEase(Ease.OutSine);

                if (anotherSp != null)
                {
                    await UniTask.WhenAll(
                        textFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                        spFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                        anotherTxt.DOFade(0, 0.5f).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token),
                        DOTween.To(() => anotherSp.material.GetFloat("_FlameIntencity"),
                                x => anotherSp.material.SetFloat("_FlameIntencity", x), 1, 1.5f)
                                .SetEase(Ease.OutSine).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token)
                    );
                }
                else
                {
                    await UniTask.WhenAll(textFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token), spFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token));
                }
            }

            async UniTask PlayBurst(int poolNum, int i, BoxCollider2D pool, SpriteRenderer cardSp)
            {
                if (i % 3 == 0) EffectCtrl.PlaySE(EffectSEType.Basi);
                BurstSpriteSets[poolNum].BurstSprites[i].sortingOrder = cardSp.sortingOrder + 1;
                BurstSpriteSets[poolNum].BurstSprites[i].transform.position = GetRandomPointInBox(pool);
                BurstSpriteSets[poolNum].BurstSprites[i].color = new Color(BurstSpriteSets[poolNum].BurstSprites[i].color.r, BurstSpriteSets[poolNum].BurstSprites[i].color.g, BurstSpriteSets[poolNum].BurstSprites[i].color.b, 0f);
                BurstSpriteSets[poolNum].BurstSprites[i].gameObject.SetActive(true);
                await BurstSpriteSets[poolNum].BurstSprites[i].DOFade(1f, 0.2f).SetEase(Ease.OutSine).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
                await BurstSpriteSets[poolNum].BurstSprites[i].DOFade(0f, 0.2f).SetEase(Ease.OutSine).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
                BurstSpriteSets[poolNum].BurstSprites[i].gameObject.SetActive(false);
            }

            async UniTask PlayBackgroundFade()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(m_StartBGFadeDelay), cancellationToken: token);

                m_BackGroundPS.Play();

                await UniTask.Delay(TimeSpan.FromSeconds(0.7f), cancellationToken: token);

                m_FlamePS[0].Play();
                if (anotherSp != null) m_FlamePS[1].Play();
            }

            async UniTask PlayBlackFade()
            {
                await m_BlackSp.DOFade(1.0f, 0.1f).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

                float lerp = EffectCtrl.BackGroundSp.material.GetFloat("_Power_Lerp");
                EffectCtrl.BackGroundSp.material.SetFloat("_Power_Lerp", lerp - 0.5f);

                await m_BlackSp.DOFade(0.0f, 0.1f).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
            }
        }

        Vector3 GetRandomPointInBox(BoxCollider2D box)
        {
            // ローカル空間でランダムな点（BoxCollider2D.size はローカルサイズ）
            Vector2 localPoint = box.offset + new Vector2(
                UnityEngine.Random.Range(-box.size.x * 0.5f, box.size.x * 0.5f),
                UnityEngine.Random.Range(-box.size.y * 0.5f, box.size.y * 0.5f)
            );

            // TransformPoint でワールド座標に変換（回転・スケールを考慮）
            Vector3 worldPoint = box.transform.TransformPoint(localPoint);

            // Z は現在の transform の Z を尊重（2D シーンで Z を維持したい場合）
            worldPoint.z = box.transform.position.z;
            return worldPoint;
        }

        async UniTask FadeOutExistingParticles(ParticleSystem ps, float targetSize, float duration, bool stopEmission = true, bool clearAfter = true)
        {
            if (ps == null) return;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            // 発行を止める（既存パーティクルは残す）
            if (stopEmission) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

            var main = ps.main;
            int max = Mathf.Max(64, main.maxParticles);
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[max];

            // 最初に現在のパーティクルを取得して初期サイズを保存
            int initialCount = ps.GetParticles(particles);
            if (initialCount <= 0)
            {
                if (clearAfter) ps.Clear();
                return;
            }

            float[] originalSizes = new float[initialCount];
            for (int i = 0; i < initialCount; i++)
            {
                originalSizes[i] = particles[i].startSize;
            }

            float p = 0f;
            Tweener tw = DOTween.To(() => p, v =>
            {
                p = v;
                // 毎フレーム最新のパーティクル情報を取得してサイズだけ書き換える
                int curCount = ps.GetParticles(particles);
                if (curCount > particles.Length)
                {
                    particles = new ParticleSystem.Particle[curCount];
                    curCount = ps.GetParticles(particles);
                }

                for (int i = 0; i < curCount; i++)
                {
                    float baseSize = (i < originalSizes.Length) ? originalSizes[i] : particles[i].startSize;
                    float newSize = Mathf.Lerp(baseSize, targetSize, p);
                    particles[i].startSize = newSize;

                    // 3D サイズを使っている場合は以下を使ってください（コメント解除）:
                    // particles[i].startSize3D = new Vector3(newSize, newSize, newSize);
                }
                ps.SetParticles(particles, curCount);
            }, 1f, duration).SetEase(Ease.Linear);

            await tw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            if (clearAfter)
            {
                ps.Clear();
            }
        }

        async UniTask DropPileDiagonalTo(SpriteRenderer sp, Vector3 targetWorldPos, float angleDeg, float startDistance, float duration, float rotateToZ = 0f, Ease moveEase = Ease.InSine, Ease rotateEase = Ease.OutSine)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken, destroyCancellationToken);
            CancellationToken token = linkedCts.Token;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 rotatedUp = Quaternion.Euler(0, 0, angleDeg) * Vector3.up; // 回転を考慮した上方向ベクトル
            Vector3 startPos = targetWorldPos + rotatedUp * startDistance;

            // 初期位置・角度をセット
            sp.transform.position = startPos;
            sp.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

            // Sequence で移動と回転を同時に行う（必要なら落下に重力感をつけるイージングを選択）
            EffectCtrl.PlaySE(EffectSEType.MotionAgility);
            Sequence seq = DOTween.Sequence();
            seq.Append(sp.transform.DOMove(targetWorldPos, duration).SetEase(moveEase)).OnComplete(() =>
            {
                EffectCtrl.PlaySE(EffectSEType.HardPunch);
            });
            seq.Join(sp.transform.DORotate(new Vector3(0, 0, rotateToZ), duration).SetEase(rotateEase));
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }
    }
}
