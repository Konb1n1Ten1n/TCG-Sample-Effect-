using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DuelKingdom.Component.Card;

#if UNITY_EDITOR
using Stopwatch = System.Diagnostics.Stopwatch;
#endif

namespace DuelKingdom.Effect
{
    public class EffectController : MonoBehaviour
    {
        #region private
        [SerializeField] SpriteRenderer m_BackGroundSp;
        [SerializeField] Camera m_MainC;
        [SerializeField] Volume m_GlobalVolume;
        [SerializeField] Transform m_EffectObject;
        [SerializeField] Transform[] m_SampleCardArray;

        [SerializeField, Range(0f, 2f)] float m_TimeScale = 1f;
        [SerializeField] AudioSource m_SESource;

        [Header("Sound Management")]
        [SerializeField] EffectSoundData m_EffectSoundData;

        Dictionary<EffectSEType, EffectSoundData.SoundEntry> m_SoundDictionary;

        [Header("Effect Prefabs")]
        [SerializeField] Slash_Slave m_Slash_Slave;
        [SerializeField] Slash_Commoners m_Slash_Commoners;

        // ターン開始3~2秒 ゲーム開始5~4秒
        [SerializeField] int m_TurnCount;
        [SerializeField] StartTurn m_StartTurn;

        [SerializeField] TurnEnd_Result m_TurnEnd_Result;
        
        [SerializeField] int m_TurnEndResult = 0;
        [SerializeField] int m_GetPointNum = 0;
        [SerializeField] bool m_MyCheck = false;
        [SerializeField] bool m_PlayGoldOnEffect = false;

        [SerializeField] Mist_Witch m_Mist_Witch;

        [SerializeField] bool m_OnWitch;
        [SerializeField, ColorUsage(true, true)] Color m_BasicShineGlowColor;
        [SerializeField] Color m_BasicShineLightColor;
        [SerializeField, ColorUsage(true, true)] Color m_WitchShineGlowColor;
        [SerializeField] Color m_WitchShineLightColor;
        [SerializeField] Shine m_Shine_Knight;

        [SerializeField] Cross_Pope m_Cross_Pope;

        [SerializeField] bool m_BothBurst = false;
        [SerializeField] Burst_King m_Burst_King;

        [SerializeField] bool m_BothAbsShine = false;
        [SerializeField] AbstractShine_Queen m_AbstractShine_Queen;

        [SerializeField] int m_isResult = 0;
        [SerializeField] Result_EffectCtrl m_Result_EffectCtrl;

        [SerializeField] SelectionCardEffect m_SelectionCardEffect;
        [SerializeField] ScreenTransition m_Screen_Transition;

        [SerializeField] int m_ScoreNum = 0;
        [SerializeField] Image[] m_MyScoreChecks;
        [SerializeField] Image[] m_EnemyScoreChecks;

        int m_MyDisplayedScore = 0;
        int m_EnemyDisplayedScore = 0;

        [SerializeField] CardListScene m_CardListScene;

        [SerializeField] Light2D m_BackGroundInLight;

        [SerializeField] ShaderVariantCollection m_ShaderVariant;
        #endregion

        #region 参照用
        // Play/Stop ペアで使用する持続アクティブインスタンス
        List<(Cross_Pope Instance, Transform Target)> m_ActiveCrossInstances = new List<(Cross_Pope, Transform)>();
        List<(SelectionCardEffect Instance, Transform Target)> m_ActiveSelectionInstances = new List<(SelectionCardEffect, Transform)>();
        Dictionary<Transform, CancellationTokenSource> m_SelRestoreCts = new Dictionary<Transform, CancellationTokenSource>();
        ScreenTransition m_ActiveScreenTransitionInstance;
        Result_EffectCtrl m_ActiveResultEffectCtrlInstance;

        CancellationTokenSource m_Cts = new CancellationTokenSource();

        VolumeProfile m_InitialGlobalVolumeProfile;
        float m_InitialGlobalVolumeWeight;
        float m_InitialBackGroundPowerLerp;
        Vector3 m_InitialCameraLocalPosition;

        public SpriteRenderer BackGroundSp => m_BackGroundSp;
        public Camera MainCam => m_MainC;
        public Volume GlobalVolume => m_GlobalVolume;
        public Transform[] SampleCardArray => m_SampleCardArray;

        public Slash_Slave SlashSlave => m_Slash_Slave;
        public Slash_Commoners SlashCommoners => m_Slash_Commoners;

        public int TurnCount { get => m_TurnCount; set => m_TurnCount = value; }
        public StartTurn StartTurn => m_StartTurn;

        public TurnEnd_Result TurnEnd_Result => m_TurnEnd_Result;

        public int TurnEndResult { get => m_TurnEndResult; set => m_TurnEndResult = value; }
        public int GetPointNum { get => m_GetPointNum; set => m_GetPointNum = value; }
        public bool MyCheck { get => m_MyCheck; set => m_MyCheck = value; }
        public bool PlayGoldOnEffect { get => m_PlayGoldOnEffect; set => m_PlayGoldOnEffect = value; }

        public int IsResult { get => m_isResult; set => m_isResult = value; }
        public Mist_Witch MistWitch => m_Mist_Witch;

        public bool OnWitch { get => m_OnWitch; set => m_OnWitch = value; }
        public Color BasicShineGlowColor { get => m_BasicShineGlowColor; set => m_BasicShineGlowColor = value; }
        public Color BasicShineLightColor { get => m_BasicShineLightColor; set => m_BasicShineLightColor = value; }
        public Color WitchShineGlowColor { get => m_WitchShineGlowColor; set => m_WitchShineGlowColor = value; }
        public Color WitchShineLightColor { get => m_WitchShineLightColor; set => m_WitchShineLightColor = value; }
        public Shine ShineKnight => m_Shine_Knight;

        public Cross_Pope CrossPope => m_Cross_Pope;
        public bool BothBurst { get => m_BothBurst; set => m_BothBurst = value; }
        public bool BothAbsShine { get => m_BothAbsShine; set => m_BothAbsShine = value; }
        public Burst_King BurstKing => m_Burst_King;
        public AbstractShine_Queen AbstractShineQueen => m_AbstractShine_Queen;
        public Result_EffectCtrl ResultEffectCtrl => m_Result_EffectCtrl;
        public SelectionCardEffect SelectionCardEffect => m_SelectionCardEffect;
        public ScreenTransition ScreenTransition => m_Screen_Transition;
        public int ScoreNum { get => m_ScoreNum; set => m_ScoreNum = value; }
        public Image[] MyScoreChecks => m_MyScoreChecks;
        public Image[] EnemyScoreChecks => m_EnemyScoreChecks;

        public CardListScene CardListScene => m_CardListScene;

        public CancellationToken EffectCancellationToken => m_Cts.Token;
        public VolumeProfile InitialGlobalVolumeProfile => m_InitialGlobalVolumeProfile;
        public float InitialGlobalVolumeWeight => m_InitialGlobalVolumeWeight;
        public float InitialBackGroundPowerLerp => m_InitialBackGroundPowerLerp;

        public const int RESULT_WIN = 0;
        public const int RESULT_DEFEAT = 1;
        public const int RESULT_DRAW = 2;
        #endregion

        private void Start()
        {
            m_ShaderVariant.WarmUp();
            InitializeSoundDictionary();
            m_InitialGlobalVolumeProfile = m_GlobalVolume.profile;
            m_InitialGlobalVolumeWeight = m_GlobalVolume.weight;
            m_InitialBackGroundPowerLerp = m_BackGroundSp.material.GetFloat("_Power_Lerp");
            m_InitialCameraLocalPosition = m_MainC.transform.localPosition;

            // スコアチェックをインスタンス化
            for (int i = 0; i < m_MyScoreChecks.Length; i++)
            {
                Material mayMat = Instantiate(m_MyScoreChecks[i].material);
                m_MyScoreChecks[i].material = mayMat;

                Material enemyMat = Instantiate(m_EnemyScoreChecks[i].material);
                m_EnemyScoreChecks[i].material = enemyMat;
            }
        }

        private void OnDestroy()
        {
            m_Cts.Cancel();
            m_Cts.Dispose();
        }

#if UNITY_EDITOR
        private void Update() => Time.timeScale = m_TimeScale;
#endif
        /// <summary>
        /// サウンド辞書の初期化
        /// </summary>
        private void InitializeSoundDictionary()
        {
            if (m_EffectSoundData == null) return;

            m_SoundDictionary = new Dictionary<EffectSEType, EffectSoundData.SoundEntry>();
            foreach (var sound in m_EffectSoundData.sounds)
            {
                if (sound.seType != EffectSEType.None && sound.clip != null)
                {
                    m_SoundDictionary[sound.seType] = sound;
                }
            }
        }

        /// <summary>
        /// 効果音を再生
        /// </summary>
        /// <param name="seType">SE種類</param>
        public void PlaySE(EffectSEType seType)
        {
            if (seType == EffectSEType.None) return;

            if (m_SoundDictionary != null && m_SoundDictionary.TryGetValue(seType, out var sound))
            {
                m_SESource.PlayOneShot(sound.clip, sound.volume);
            }
        }

        /// <summary>
        /// 効果音を音量指定して再生
        /// </summary>
        /// <param name="seType">SE種類</param>
        /// <param name="volume">音量（0-1）</param>
        public void PlaySE(EffectSEType seType, float volume)
        {
            if (seType == EffectSEType.None) return;

            if (m_SoundDictionary != null && m_SoundDictionary.TryGetValue(seType, out var sound))
            {
                m_SESource.PlayOneShot(sound.clip, volume);
            }
            else
            {
                Debug.LogWarning($"Sound '{seType}' not found in EffectSoundData");
            }
        }

        /// <summary>
        /// プレハブからインスタンスを生成する共通ヘルパー
        /// </summary>
        private async UniTask<T> InstantiateEffectAsync<T>(T prefab) where T : MonoBehaviour
        {
            var op = InstantiateAsync(prefab, m_EffectObject);
            await op.ToUniTask().AttachExternalCancellation(m_Cts.Token);
            return op.Result[0];
        }

        public async UniTask PlaySlashSlaveEffect(Transform target)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Slash_Slave);
            instance.EffectCtrl = this;
            await instance.PlaySlashEffect(target);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlaySlashSlaveEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlaySlashCommonersEffect(Transform target)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Slash_Commoners);
            instance.EffectCtrl = this;
            await instance.PlaySlashEffect(target);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlaySlashCommonersEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayStartTurn()
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_StartTurn);
            instance.EffectCtrl = this;
            await instance.DisplayStartTurnText();
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayStartTurn: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayTurnCount(int turnCount)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_StartTurn);
            instance.EffectCtrl = this;
            await instance.DisplayTurnCountText(turnCount);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayTurnCount: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }



        public async UniTask PlayTurnEndEffect(int turnEndResult, int getPointNum, bool MyCheck, bool onGoldEffect)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_TurnEnd_Result);
            instance.EffectCtrl = this;
            await instance.PlayTurnEndEffect(turnEndResult, getPointNum, MyCheck, onGoldEffect);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayTurnEndEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayMistEffect(Transform target)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Mist_Witch);
            instance.EffectCtrl = this;
            await instance.ExamplePlayMist(target);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayMistEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayShineEffect(Transform target, int shineCount, bool? onWitch = false)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Shine_Knight);
            instance.EffectCtrl = this;
            await instance.PlayShineEffect(target, shineCount, onWitch);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayShineEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayChangeNumOnly(Transform target, int changeNum, bool? onWitch = false)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Shine_Knight);
            instance.EffectCtrl = this;
            await instance.ChangeNumber(target, changeNum, onWitch, false);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayChangeNumOnly: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayUndoNumber(Transform target, int changeNum)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Shine_Knight);
            instance.EffectCtrl = this;
            await instance.UndoNumber(target, changeNum);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayUndoNumber: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        private async UniTask StopAndDestroyCrossAsync(Cross_Pope instance)
        {
            if (instance == null) return;
            await instance.StopCrossEffect();
            if (instance != null) Destroy(instance.gameObject);
        }

        /// <summary>
        /// 再生後 StopCrossEffect() を呼ぶまでインスタンスを保持します
        /// </summary>
        public async UniTask PlayCrossEffect(Transform target)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            // 同じターゲットの既存インスタンスを並行停止・置き換え
            for (int i = m_ActiveCrossInstances.Count - 1; i >= 0; i--)
            {
                if (m_ActiveCrossInstances[i].Target == target)
                {
                    var old = m_ActiveCrossInstances[i].Instance;
                    m_ActiveCrossInstances.RemoveAt(i);
                    StopAndDestroyCrossAsync(old).Forget();
                }
            }

            var instance = await InstantiateEffectAsync(m_Cross_Pope);
            instance.EffectCtrl = this;
            m_ActiveCrossInstances.Add((instance, target));
            await instance.PlayCrossEffect(target);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayCrossEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        /// <summary>指定ターゲットのクロスエフェクトのみ停止します</summary>
        public async UniTask StopCrossEffect(Transform target)
        {
            if (target == null) return;
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            for (int i = m_ActiveCrossInstances.Count - 1; i >= 0; i--)
            {
                if (m_ActiveCrossInstances[i].Target != target) continue;
                var inst = m_ActiveCrossInstances[i].Instance;
                m_ActiveCrossInstances.RemoveAt(i);
                await StopAndDestroyCrossAsync(inst);
                break;
            }
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] StopCrossEffect(target): {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        /// <summary>全てのクロスエフェクトを停止します</summary>
        public async UniTask StopCrossEffect()
        {
            if (m_ActiveCrossInstances.Count == 0) return;
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var stale = new List<(Cross_Pope Instance, Transform Target)>(m_ActiveCrossInstances);
            m_ActiveCrossInstances.Clear();

            var tasks = new UniTask[stale.Count];
            for (int i = 0; i < stale.Count; i++)
                tasks[i] = StopAndDestroyCrossAsync(stale[i].Instance);
            await UniTask.WhenAll(tasks);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] StopCrossEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayBurstEffect(Transform card, Transform antherCard)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_Burst_King);
            instance.EffectCtrl = this;
            await instance.PlayBurstEffect(card, antherCard);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayBurstEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayAbstractShineEffect(Transform card, Transform anotherCard)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var instance = await InstantiateEffectAsync(m_AbstractShine_Queen);
            instance.EffectCtrl = this;
            await instance.PlayAbsShineEffect(card, anotherCard);
            if (instance != null) Destroy(instance.gameObject);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayAbstractShineEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        /// <summary>
        /// 再生後 StopResultEffect() を呼ぶまでインスタンスを保持します
        /// </summary>
        public async UniTask PlayResultEffect()
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            m_ActiveResultEffectCtrlInstance = await InstantiateEffectAsync(m_Result_EffectCtrl);
            m_ActiveResultEffectCtrlInstance.EffectCtrl = this;
            await m_ActiveResultEffectCtrlInstance.PlayResult(m_isResult);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayResultEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public void StopResultEffect()
        {
            if (m_ActiveResultEffectCtrlInstance == null) return;
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            m_ActiveResultEffectCtrlInstance.StopResult();
            Destroy(m_ActiveResultEffectCtrlInstance.gameObject);
            m_ActiveResultEffectCtrlInstance = null;
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] StopResultEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        private async UniTask StopAndDestroySelectionAsync(SelectionCardEffect instance, Transform target, CancellationToken restoreToken = default)
        {
            if (instance == null) return;
            await instance.offSelectionEffect(target, restoreToken);
            if (instance != null) Destroy(instance.gameObject);
        }

        /// <summary>
        /// 再生後 StopSelectionCardEffect() を呼ぶまでインスタンスを保持します。
        /// 既に再生中のインスタンスがある場合は停止アニメーションを並行実行します。
        /// </summary>
        public async UniTask PlaySelectionCardEffect(Transform target)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            // 同じターゲットが再選択された場合、進行中のレイヤー復元をキャンセル
            if (m_SelRestoreCts.TryGetValue(target, out var pendingCts))
            {
                pendingCts.Cancel();
                pendingCts.Dispose();
                m_SelRestoreCts.Remove(target);
            }

            if (m_ActiveSelectionInstances.Count > 0)
            {
                var stale = new List<(SelectionCardEffect Instance, Transform Target)>(m_ActiveSelectionInstances);
                m_ActiveSelectionInstances.Clear();
                foreach (var (inst, tgt) in stale)
                {
                    // 再選択対象のカードはレイヤー復元をしない
                    if (tgt == target)
                    {
                        StopAndDestroySelectionAsync(inst, tgt, new CancellationToken(true)).Forget();
                        continue;
                    }
                    if (m_SelRestoreCts.TryGetValue(tgt, out var old)) { old.Cancel(); old.Dispose(); }
                    var restoreCts = new CancellationTokenSource();
                    m_SelRestoreCts[tgt] = restoreCts;
                    StopAndDestroySelectionAsync(inst, tgt, restoreCts.Token).Forget();
                }
            }

            var newInstance = await InstantiateEffectAsync(m_SelectionCardEffect);
            newInstance.EffectCtrl = this;
            m_ActiveSelectionInstances.Add((newInstance, target));
            await newInstance.onSelectionEffect(target);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlaySelectionCardEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask StopSelectionCardEffect()
        {
            if (m_ActiveSelectionInstances.Count == 0) return;
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var stale = new List<(SelectionCardEffect Instance, Transform Target)>(m_ActiveSelectionInstances);
            m_ActiveSelectionInstances.Clear();

            var tasks = new UniTask[stale.Count];
            for (int i = 0; i < stale.Count; i++)
            {
                var tgt = stale[i].Target;
                // 明示的な停止なのでレイヤー復元する。進行中の復元CTSがあればキャンセル
                if (m_SelRestoreCts.TryGetValue(tgt, out var cts)) { cts.Cancel(); cts.Dispose(); m_SelRestoreCts.Remove(tgt); }
                tasks[i] = StopAndDestroySelectionAsync(stale[i].Instance, tgt);
            }
            await UniTask.WhenAll(tasks);
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] StopSelectionCardEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        public async UniTask PlayScoreEffect(int addScore, bool MyCheck)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            if (MyCheck)
                m_MyDisplayedScore = Mathf.Clamp(m_MyDisplayedScore + addScore, 0, m_MyScoreChecks.Length);
            else
                m_EnemyDisplayedScore = Mathf.Clamp(m_EnemyDisplayedScore + addScore, 0, m_EnemyScoreChecks.Length);

            int newScore = MyCheck ? m_MyDisplayedScore : m_EnemyDisplayedScore;

            Sequence seq = DOTween.Sequence();

            int i = 0;
            foreach (Image source in MyCheck ? m_MyScoreChecks : m_EnemyScoreChecks)
            {
                i++;
                if (i <= newScore && source.material.GetFloat("_NoiseStep") >= 1)
                {
                    source.gameObject.SetActive(true);
                    Material mat = source.material;
                    Tween matTw = DOTween.To(() => mat.GetFloat("_NoiseStep"),
                        x => mat.SetFloat("_NoiseStep", x), 0, 1.0f);
                    seq.Join(matTw);
                    continue;
                }
                if (i > newScore && source.gameObject.activeSelf && source.material.GetFloat("_NoiseStep") <= 0)
                {
                    Material mat = source.material;
                    Tween matTw = DOTween.To(() => mat.GetFloat("_NoiseStep"),
                        x => mat.SetFloat("_NoiseStep", x), 1, 1.0f)
                        .OnComplete(() => source.gameObject.SetActive(false));
                    seq.Join(matTw);
                }
            }

            await seq.AsyncWaitForCompletion().AsUniTask();
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayScoreEffect: {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        /// <summary>画面遷移アニメーション</summary>
        /// <param name="fadeIn">true：遷移開始（インスタンス生成）。false：遷移終了（インスタンス破棄）。</param>
        /// <param name="duration">速度</param>
        public async UniTask PlayScreenTransition(bool fadeIn, float duration = 0.5f)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            if (fadeIn)
            {
                m_ActiveScreenTransitionInstance = await InstantiateEffectAsync(m_Screen_Transition);
                m_ActiveScreenTransitionInstance.EffectCtrl = this;
                await m_ActiveScreenTransitionInstance.PlayScreenTransition(fadeIn, duration);
            }
            else
            {
                if (m_ActiveScreenTransitionInstance == null)
                {
#if UNITY_EDITOR
                    Debug.Log("[EffectTimer] PlayScreenTransition(fadeIn=false): skipped (no active instance)");
#endif
                    return;
                }
                await m_ActiveScreenTransitionInstance.PlayScreenTransition(fadeIn, duration);
                Destroy(m_ActiveScreenTransitionInstance.gameObject);
                m_ActiveScreenTransitionInstance = null;
            }
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"[EffectTimer] PlayScreenTransition(fadeIn={fadeIn}): {sw.Elapsed.TotalSeconds:F2}s");
#endif
        }

        /// <summary>
        /// 実行されたGlobal Volume以外全ての子オブジェクトを破棄し、実行中のUniTask処理をキャンセルする
        /// </summary>
        public void DeleteEffect()
        {
            // UniTask操作をキャンセルして再生成
            m_Cts.Cancel();
            m_Cts.Dispose();
            m_Cts = new CancellationTokenSource();

            // Tween待ちのUniTaskを全停止
            DOTween.KillAll();

            // 外部状態を復元
            if (m_GlobalVolume != null)
            {
                m_GlobalVolume.profile = m_InitialGlobalVolumeProfile;
                m_GlobalVolume.weight = m_InitialGlobalVolumeWeight;
            }
            if (m_BackGroundSp != null && m_BackGroundSp.material != null)
                m_BackGroundSp.material.SetFloat("_Power_Lerp", m_InitialBackGroundPowerLerp);
            if (m_MainC != null)
                m_MainC.transform.localPosition = m_InitialCameraLocalPosition;
            if (m_BackGroundInLight != null)
                m_BackGroundInLight.gameObject.SetActive(true);

            // 持続インスタンスの参照をクリア
            m_ActiveCrossInstances.Clear();
            m_ActiveSelectionInstances.Clear();
            foreach (var cts in m_SelRestoreCts.Values) { cts.Cancel(); cts.Dispose(); }
            m_SelRestoreCts.Clear();
            m_ActiveScreenTransitionInstance = null;
            m_ActiveResultEffectCtrlInstance = null;

            // GlobalVolume以外の子オブジェクトを全て破棄
            for (int i = 0; i < m_EffectObject.childCount; i++)
            {
                Transform child = m_EffectObject.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        public void ShowAllCardList() => m_CardListScene.ShowAllCardList();
        public void HideAllCardList() => m_CardListScene.HideAllCardList();

        class ScoreImageState
        {
            public Image Image;
            public Transform Parent;
            public int SiblingIndex;
            public bool Active;
            public Material MaterialInstance;
            public float NoiseStep;
            public Color Color;
        }
    }

    public interface IEffectComponent
    {
        EffectController EffectCtrl { get; set; }
    }
}
