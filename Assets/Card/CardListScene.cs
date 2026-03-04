using DG.Tweening;
using DuelKingdom.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// カード一覧の UI コントローラー。
/// - 水平方向のフリック操作（慣性つき）
/// - 中央揃えスナップ
/// - 見た目上の無限ループ表示（ラッピング）
/// - カード位置に応じたアーク状の Z 軸オフセットと拡大縮小、描画順制御
/// </summary>
namespace DuelKingdom.Component.Card
{
    public class CardListScene : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Refs")]
        [SerializeField] RectTransform Viewport;
        [SerializeField] RectTransform Content;
        [SerializeField] GameObject CardPrefab;
        [SerializeField] RectTransform ParentTrans;
        [SerializeField] UnityEngine.UI.Button CloseButton;
        [SerializeField] UnityEngine.UI.Button RuleButton;
        [SerializeField] UnityEngine.UI.Button SelectButton;


        [Header("Scroll")]
        [SerializeField] float Deceleration = 2000f;
        [SerializeField] float StopThreshold = 5f;
        [SerializeField] bool SnapToCards = true;
        [SerializeField] Ease InertiaEase = Ease.OutQuad;
        [SerializeField] Ease SnapEase = Ease.OutQuart;

        [Header("Cards")]
        public List<CardInfo> AllCard = new List<CardInfo>();
        [SerializeField] float CardWidth = 200f;
        [SerializeField] float CardSpacing = 20f;
        [SerializeField] bool AutoGenerateCards = true;

        [Header("Arc / Scale")]
        [SerializeField] float ArcRadiusMultiplier = 1.5f;
        [SerializeField] float MinScale = 0.8f;
        [SerializeField] float MaxScale = 1.15f;

        [Header("Behavior")]
        public bool EnableLooping = true; // true: 一周ループ（ラップ）する、false: ループしない

        // Edge handling when looping is disabled
        public enum EdgeMode { ImmediateClamp, Bounce, SoftClamp }
        [SerializeField] EdgeMode EdgeBehavior = EdgeMode.Bounce;
        [SerializeField] float EdgeBounceDistance = 40f; // overshoot distance for bounce
        [SerializeField] float EdgeBounceDuration = 0.3f; // total duration for bounce (out+in)
        [SerializeField] float EdgeSoftClampDuration = 0.25f; // duration for soft clamp

        // 内部状態
        List<float> cardCenterBase = new List<float>(); // 各カードを中央に持ってくる content.x の基準値
        List<float> baseX = new List<float>(); // 各カードの元の anchoredPosition.x
        // ★修正: RectTransform参照ベースのベースX（SetSiblingIndexによる兄弟インデックス変化の影響を受けない）
        Dictionary<RectTransform, float> rtBaseX = new Dictionary<RectTransform, float>();
        List<CardInfo> _currentCards = new List<CardInfo>();
        Vector2 pointerStartLocal;
        Vector2 contentStart;
        float velocity;
        Tween tween;
        UnityAction<CardType> _action;

        const float ALLCARDLIST_POSX = 188.0f;

        public void HideAllCardList()
        {
            ParentTrans.gameObject.SetActive(false);
        }

        public void ShowAllCardList()
        {
            // 必須参照チェック
            if (Viewport == null || Content == null)
            {
                Debug.LogError("Assign viewport and content");
                enabled = false; return;
            }

            // 閉じるとルール
            var ruleRt = RuleButton.transform as RectTransform;
            ruleRt.anchoredPosition = new Vector2(-ALLCARDLIST_POSX, ruleRt.anchoredPosition.y);
            var closeRt = CloseButton.transform as RectTransform;
            closeRt.anchoredPosition = new Vector2(ALLCARDLIST_POSX, closeRt.anchoredPosition.y);

            RuleButton.gameObject.SetActive(true);
            SelectButton.gameObject.SetActive(false);
            CloseButton.gameObject.SetActive(true);

            // カード一覧画面に、cardListのカードを表示する
            ParentTrans.gameObject.SetActive(true);

            // カードを自動生成する設定なら生成
            if (AutoGenerateCards && CardPrefab != null) GenerateCards(AllCard);
            else _currentCards = AllCard;

            // レイアウト情報を構築して初回の変換を適用
            RecalculateLayout();
            UpdateCardTransforms();
        }

        public void ShowCardList(List<CardInfo> cardList, UnityAction<CardType> action)
        {
            if (Viewport == null || Content == null)
            {
                Debug.LogError("Assign viewport and content");
                enabled = false; return;
            }

            // セレクトだけ
            var selectRt = SelectButton.transform as RectTransform;
            selectRt.anchoredPosition = new Vector2(0, selectRt.anchoredPosition.y);

            RuleButton.gameObject.SetActive(false);
            SelectButton.gameObject.SetActive(true);
            CloseButton.gameObject.SetActive(false);

            // カード一覧画面に、cardListのカードを表示する
            ParentTrans.gameObject.SetActive(true);

            GenerateCards(cardList);

            RecalculateLayout();
            UpdateCardTransforms();

            // UnityActionを保持しておく
            _action = action;
        }

        public void SelectedCard()
        {
            // カードが選択されたときの処理
            _action?.Invoke(CenterCard());
        }

        public CardType CenterCard()
        {
            int idx = FindNearestIndex(Content.anchoredPosition.x);
            if (idx < 0 || idx >= _currentCards.Count) return default;
            return _currentCards[idx].cardType;
        }

        /// <summary>
        /// 指定枚数のカードを Content の子として動的に生成する。
        /// </summary>
        /// <param name="count">生成するカード数</param>
        void GenerateCards(List<CardInfo> cardList)
        {
            _currentCards = cardList;
            //既存の子オブジェクトを削除
            for (int i = Content.childCount - 1; i >= 0; i--) DestroyImmediateSafe(Content.GetChild(i).gameObject);

            // Content の幅を設定し、等間隔に配置
            float totalW = cardList.Count * CardWidth + (cardList.Count - 1) * CardSpacing;
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalW);
            float start = -(totalW * 0.5f) + CardWidth * 0.5f;

            for (int i = 0; i < cardList.Count; i++)
            {
                // プレハブをインスタンス化
                var go = Instantiate(CardPrefab, Content);
                if (go == null) continue;

                if (go.TryGetComponent<Image>(out var image))
                    image.sprite = cardList[i].sprite;
                if (go.transform.GetChild(0).TryGetComponent<TextMeshProUGUI>(out var tmp))
                    tmp.text = $"{cardList[i].value}";
                var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
                rt.SetParent(Content, false);
                rt.pivot = rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(start + i * (CardWidth + CardSpacing), 0f);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CardWidth);

                //クリック時にそのカードを中央へ移動させるハンドラを登録
                int idx = i;
                var trig = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                e.callback.AddListener((d) => OnCardClicked(idx));
                trig.triggers.Add(e);
            }
        }

        /// <summary>
        /// オブジェクトを安全に破棄する（エディタ実行時とプレイモードで適切に扱う）。
        /// </summary>
        /// <param name="go">破棄する GameObject</param>
        void DestroyImmediateSafe(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
#else
        Destroy(go);
#endif
        }

        /// <summary>
        /// レイアウト情報（スナップ基準とベース X）を再計算する。
        /// </summary>
        void RecalculateLayout()
        {
            cardCenterBase.Clear(); baseX.Clear(); rtBaseX.Clear();
            // 各子の anchoredPosition を読み取り基準配列を作る
            for (int i = 0; i < Content.childCount; i++)
            {
                var c = Content.GetChild(i) as RectTransform;
                if (c == null) continue;
                cardCenterBase.Add(-c.anchoredPosition.x); // カードを中央にするための content.x
                baseX.Add(c.anchoredPosition.x); // 基準 X
                rtBaseX[c] = c.anchoredPosition.x; // ★修正: RectTransform参照で登録
            }
        }

        void Update()
        {
            // 視覚的な配置更新（ラップ対応）
            UpdateCardTransforms();
        }

        /// <summary>
        /// Content の X位置を直接設定するユーティリティ。
        /// </summary>
        /// <param name="x">設定する X 値</param>
        void SetContentX(float x) => Content.anchoredPosition = new Vector2(x, Content.anchoredPosition.y);

        /// <summary>
        /// basePos に対して currentXから最も近い単一のラップ候補（幅 totalW 単位でずらした位置）を返す。
        /// ループ無効時は単純に basePos を返す（ラップしない）。
        /// </summary>
        float NearestWrapped(float basePos, float currentX)
        {
            if (!EnableLooping) return basePos;

            float totalW = Mathf.Max(1f, Content.rect.width);
            // currentX と basePos の差を totalWで割って最も近い整数倍を選ぶ
            float n = Mathf.Round((currentX - basePos) / totalW);
            return basePos + n * totalW;
        }

        /// <summary>
        /// 現在位置 currentX に対して最も近いスナップ先（ラップ考慮）を返す。
        /// ループ無効時は単に基準位置の中で最短のものを選びます。
        /// </summary>
        float FindNearestPosition(float currentX)
        {
            if (cardCenterBase.Count == 0) return currentX;
            float best = currentX; float bestDist = float.MaxValue;
            if (EnableLooping)
            {
                //すべての基準位置をラップ候補にして最も近いものを選びる
                foreach (var basePos in cardCenterBase)
                {
                    float cand = NearestWrapped(basePos, currentX);
                    float d = Mathf.Abs(currentX - cand);
                    if (d < bestDist) { bestDist = d; best = cand; }
                }
            }
            else
            {
                // ラップなし：基準位置そのものを比較
                foreach (var basePos in cardCenterBase)
                {
                    float d = Mathf.Abs(currentX - basePos);
                    if (d < bestDist) { bestDist = d; best = basePos; }
                }
            }
            return best;
        }

        /// <summary>
        /// 現在位置 currentX に対して最も近いカードのインデックスを返す（ラップ考慮）。
        /// ループ無効時は単純に基準位置で判定します。
        /// </summary>
        int FindNearestIndex(float currentX)
        {
            if (cardCenterBase.Count == 0) return -1;
            int bestIdx = 0; float bestDist = float.MaxValue;
            if (EnableLooping)
            {
                for (int i = 0; i < cardCenterBase.Count; i++)
                {
                    float cand = NearestWrapped(cardCenterBase[i], currentX);
                    float d = Mathf.Abs(currentX - cand);
                    if (d < bestDist) { bestDist = d; bestIdx = i; }
                }
            }
            else
            {
                for (int i = 0; i < cardCenterBase.Count; i++)
                {
                    float d = Mathf.Abs(currentX - cardCenterBase[i]);
                    if (d < bestDist) { bestDist = d; bestIdx = i; }
                }
            }
            return bestIdx;
        }

        /// <summary>
        /// 現在の Tween を停止して解放する。
        /// </summary>
        Tween KillTween()
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
                tween = null;
            }
            return tween;
        }

        /// <summary>
        /// カードの表示位置をラップさせつつ Z 軸とスケールを更新し、描画順を中央に近い順にする。
        /// ループ無効時はラップせず基準位置をそのまま使います。
        /// </summary>
        void UpdateCardTransforms()
        {
            int n = Content.childCount; if (n == 0) return;

            float totalW = Mathf.Max(1f, Content.rect.width);
            var order = new List<KeyValuePair<int, float>>(n);
            float viewW = Mathf.Max(1f, Viewport.rect.width);

            for (int i = 0; i < n; i++)
            {
                var child = Content.GetChild(i) as RectTransform; if (child == null) continue;

                // ★修正: インデックスではなくRectTransform参照でbaseXを取得（兄弟順変化の影響を受けない）
                float bX = rtBaseX.TryGetValue(child, out var bxVal) ? bxVal : child.anchoredPosition.x;

                float desiredX;
                if (EnableLooping)
                {
                    // Content と合わせて最も近い表示位置へラップする
                    float centerOffset = Content.anchoredPosition.x + bX;
                    float k = Mathf.Round(centerOffset / totalW);
                    desiredX = bX - k * totalW;
                }
                else
                {
                    // ループ無効時は単純に基準位置を使う（ラップしない）
                    desiredX = bX;
                }

                var ap = child.anchoredPosition; ap.x = desiredX; child.anchoredPosition = ap;

                // 視覚的な X（viewport 中心基準）
                float worldX = Content.anchoredPosition.x + desiredX;

                // 円弧方程式を用いて Z を算出（Y は変更しない）
                float radius = Mathf.Max(Mathf.Abs(worldX) * ArcRadiusMultiplier, viewW * 0.6f);
                float z = (Mathf.Abs(worldX) >= radius) ? -radius : -(radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - worldX * worldX)));
                var lp = child.localPosition; lp.z = z; child.localPosition = lp; // set Z, Y unchanged

                // 中央に近いほど大きく表示
                float t = 1f - Mathf.Clamp01(Mathf.Abs(worldX) / (viewW * 0.5f));
                float s = Mathf.Lerp(MinScale, MaxScale, t);
                child.localScale = Vector3.one * s;

                // 描画順用に距離を保存
                order.Add(new KeyValuePair<int, float>(i, Mathf.Abs(worldX)));
            }

            // 中央に近いものほど上にする
            order.Sort((a, b) => b.Value.CompareTo(a.Value));
            // ★修正: SetSiblingIndex 呼び出しでインデックスがずれる前に
            //         RectTransform 参照を先に収集してから割り当てる
            var sortedChildren = new RectTransform[order.Count];
            for (int i = 0; i < order.Count; i++)
                sortedChildren[i] = Content.GetChild(order[i].Key) as RectTransform;
            for (int i = 0; i < sortedChildren.Length; i++)
                if (sortedChildren[i] != null) sortedChildren[i].SetSiblingIndex(i);
        }

        bool _isDraggingInViewport = false; // ドラッグがViewport内で開始されたかどうか

        /// <summary>
        /// ドラッグ開始時の処理。内部状態を初期化する。
        /// </summary>
        /// <param name="ev">ポインターイベントデータ</param>
        public void OnBeginDrag(PointerEventData ev)
        {
            // Viewport 内でのドラッグのみ処理する
            _isDraggingInViewport = RectTransformUtility.RectangleContainsScreenPoint(
                Viewport, ev.position, ev.pressEventCamera);
            if (!_isDraggingInViewport) return;

            KillTween();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport, ev.position, ev.pressEventCamera, out pointerStartLocal);
            contentStart = Content.anchoredPosition; velocity = 0f;
        }

        /// <summary>
        /// ドラッグ中の処理。Content の X を直接移動させる（無限スクロール）。
        /// </summary>
        /// <param name="ev">ポインターイベントデータ</param>
        public void OnDrag(PointerEventData ev)
        {
            if (!_isDraggingInViewport) return; // Viewport 外は無視

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport, ev.position, ev.pressEventCamera, out var local)) return;
            var delta = local - pointerStartLocal;
            SetContentX(contentStart.x + delta.x);
            if (Time.deltaTime > 0) velocity = ev.delta.x / Time.deltaTime;
        }

        /// <summary>
        /// ドラッグ終了時の処理。慣性アニメーションを開始し、完了後にスナップを行う。
        /// </summary>
        /// <param name="ev">ポインターイベントデータ</param>
        public void OnEndDrag(PointerEventData ev)
        {
            if (!_isDraggingInViewport) return; // Viewport 外は無視
            _isDraggingInViewport = false;

            //速度が小さければスナップのみ
            if (Mathf.Abs(velocity) < StopThreshold) { velocity = 0f; if (SnapToCards) StartSnapTween(); return; }

            // 慣性で止まるまでの時間 t を計算し移動距離 s を求める
            float t = Mathf.Clamp(Mathf.Abs(velocity) / Mathf.Max(1e-6f, Deceleration), 0.05f, 1f);
            float s = velocity * t - 0.5f * Mathf.Sign(velocity) * Deceleration * t * t;
            float target = Content.anchoredPosition.x + s; //目標位置

            // ループ無効時、端を超える慣性はモードに応じて処理する
            if (!EnableLooping && cardCenterBase.Count > 0)
            {
                // 基準位置の最小/最大を算出
                float minCenter = float.MaxValue;
                float maxCenter = float.MinValue;
                foreach (var v in cardCenterBase)
                {
                    if (v < minCenter) minCenter = v;
                    if (v > maxCenter) maxCenter = v;
                }

                // 目標が範囲外なら慣性を処理
                if (target < minCenter || target > maxCenter)
                {
                    velocity = 0f; // 慣性はキャンセル

                    float currentX = Content.anchoredPosition.x;
                    float clamped = Mathf.Clamp(currentX, minCenter, maxCenter);

                    // Immediate: ただちにクランプしてスナップ
                    if (EdgeBehavior == EdgeMode.ImmediateClamp)
                    {
                        SetContentX(clamped);
                        if (SnapToCards) StartSnapTween();
                        return;
                    }

                    // Bounce: はみ出し側にオーバーシュートして戻る
                    if (EdgeBehavior == EdgeMode.Bounce)
                    {
                        float boundary = (target < minCenter) ? minCenter : maxCenter;
                        float sign = (target < minCenter) ? 1f : -1f; // if target < min, we moved left, so bounce right (positive)
                        float overshoot = boundary + sign * EdgeBounceDistance;

                        KillTween();
                        float half = Mathf.Max(0.01f, EdgeBounceDuration * 0.5f);
                        // move to overshoot then back to boundary
                        tween = Content.DOAnchorPos(new Vector2(overshoot, Content.anchoredPosition.y), half).SetEase(Ease.OutQuad).SetId(this)
                        .OnComplete(() =>
                        {
                            tween = Content.DOAnchorPos(new Vector2(boundary, Content.anchoredPosition.y), half).SetEase(Ease.InQuad).SetId(this)
                            .OnComplete(() => { tween = null; if (SnapToCards) StartSnapTween(); });
                        });
                        return;
                    }

                    // SoftClamp: ゆっくりとクランプ位置へ移動
                    if (EdgeBehavior == EdgeMode.SoftClamp)
                    {
                        KillTween();
                        tween = Content.DOAnchorPos(new Vector2(clamped, Content.anchoredPosition.y), EdgeSoftClampDuration).SetEase(Ease.OutQuad).SetId(this)
                        .OnComplete(() => { tween = null; if (SnapToCards) StartSnapTween(); });
                        return;
                    }
                }
            }

            KillTween();
            tween = Content.DOAnchorPos(new Vector2(target, Content.anchoredPosition.y), t).SetEase(InertiaEase).SetId(this)
            .OnComplete(() => { tween = null; if (SnapToCards) StartSnapTween(); });
        }

        /// <summary>
        /// 現在位置に最も近いカード中心位置へスナップするアニメーションを開始する。
        /// </summary>
        void StartSnapTween()
        {
            float cur = Content.anchoredPosition.x;
            float dest = FindNearestPosition(cur);
            if (Mathf.Approximately(cur, dest)) return;
            KillTween();
            float d = Mathf.Abs(cur - dest);
            float duration = Mathf.Clamp(0.12f + (d / Mathf.Max(1f, Viewport.rect.width)) * 0.3f, 0.12f, 0.5f);
            tween = Content.DOAnchorPos(new Vector2(dest, Content.anchoredPosition.y), duration).SetEase(SnapEase).SetId(this)
            .OnComplete(() => tween = null);
        }

        /// <summary>
        ///クリックされたカードを中央へアニメーションで移動させる（ラップを考慮）。
        /// </summary>
        /// <param name="index">クリックされたカードのインデックス</param>
        public void OnCardClicked(int index)
        {
            if (index < 0 || index >= cardCenterBase.Count) return;
            float cur = Content.anchoredPosition.x;
            float target = NearestWrapped(cardCenterBase[index], cur);
            KillTween();
            tween = Content.DOAnchorPos(new Vector2(target, Content.anchoredPosition.y), 0.4f).SetEase(Ease.OutCubic).SetId(this)
            .OnComplete(() => tween = null);
        }
    }

}