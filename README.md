# TCG-Sample-Effect-
TCGのカード演出・効果処理UIを中心にしたUnityサンプル集

## 概要
本リポジトリは、デジタルTCG（トレーディングカードゲーム）にありがちな **カード演出** と **効果処理UI** を、Unity上で試せるようにまとめたサンプル集です。  
演出（見た目）と処理（状態遷移）を切り分けて実装する際の参考になることを目的としています。

- 主な言語: C#
- Unity プロジェクト形式（`Assets/`, `Packages/`, `ProjectSettings/` を含みます）

## 想定している用途
- TCG風のUI/演出の試作
- 演出とロジックの依存を薄くする実装方針のサンプル置き場

## 使い方（起動手順）
1. このリポジトリを clone / download します
2. Unity Hub で本フォルダを **Add** してプロジェクトとして開きます
3. `Assets/` 配下のサンプルシーン（Scene）を開いて再生（Play）してください

## ディレクトリ構成（目安）
- `Assets/`  
  実装本体（スクリプト、シーン、プレハブ、素材など）
- `Packages/`  
  PackageManagerの依存関係
- `ProjectSettings/`  
  Unityプロジェクト設定
- `Effect Sample.slnx`  
  IDE用のソリューション関連ファイル

## サンプル一覧
- Play Slash Slave Effect	ctrl.PlaySlashSlaveEffect(ctrl.SampleCardArray[0])	サンプルカード0を使ったSlash（Slave）演出
- Play Slash Commoners Effect	ctrl.PlaySlashCommonersEffect(ctrl.SampleCardArray[1])	サンプルカード1を使ったSlash（Commoners）演出
- Play Start Turn Effect	ctrl.PlayStartTurn()	ターン開始演出
- Play Turn Count Effect	ctrl.PlayTurnCount(ctrl.TurnCount)	ターン数（TurnCount）表示/更新演出
- Play Turn End Effect	ctrl.PlayTurnEndEffect(ctrl.TurnEndResult, ctrl.GetPointNum, ctrl.MyCheck, ctrl.PlayGoldOnEffect)	ターン終了結果系演出（結果/点数/チェック/GoldOnフラグ(?)を渡す）
- Play Mist Effect	ctrl.PlayMistEffect(ctrl.SampleCardArray[2])	サンプルカード2を使ったMist演出
- Play Shine Effect	ctrl.PlayShineEffect(ctrl.SampleCardArray[3], 9, ctrl.OnWitch)	サンプルカード3＋数値9＋OnWitchフラグでShine演出
- Play Change Num Only	ctrl.PlayChangeNumOnly(ctrl.SampleCardArray[3], 9, ctrl.OnWitch)	Shineとは別に「数値変更のみ」演出（同引数）
- Play Undo Num	ctrl.PlayUndoNumber(ctrl.SampleCardArray[3], ctrl.ShineKnight.SampleUndoNum)	数値Undo演出（Undo値は ShineKnight.SampleUndoNum）
- 4 Play Cross Effect	ctrl.PlayCrossEffect(ctrl.SampleCardArray[4])	サンプルカード4でCross演出開始
- 4 Stop Cross Effect	ctrl.StopCrossEffect(ctrl.SampleCardArray[4])	サンプルカード4のCross演出停止
- 5 Play Cross Effect	ctrl.PlayCrossEffect(ctrl.SampleCardArray[5])	サンプルカード5でCross演出開始
- 5 Stop Cross Effect	ctrl.StopCrossEffect(ctrl.SampleCardArray[5])	サンプルカード5のCross演出停止
- All Stop Cross Effect	ctrl.StopCrossEffect()	Cross演出を全停止（引数なしオーバーロード）
- Play Burst Effect	ctrl.PlayBurstEffect(ctrl.SampleCardArray[5], ctrl.BothBurst ? ctrl.SampleCardArray[6] : null)	Burst演出（第2対象は BothBurst がtrueならカード6、falseならnull）
- Play AbstractShine Effect	ctrl.PlayAbstractShineEffect(ctrl.SampleCardArray[7], ctrl.BothAbsShine ? ctrl.SampleCardArray[8] : null)	AbstractShine演出（第2対象は BothAbsShine によりカード8 or null）
- Play Result Effect	ctrl.PlayResultEffect()	リザルト表示演出開始
- Stop Result Effect	ctrl.StopResultEffect()	リザルト表示演出停止（※これだけ DrawButton ではなく同期ボタン呼び出し）
- Play FadeIn Effect	ctrl.PlayScreenTransition(true)	画面遷移：FadeIn（true）
- Play FadeOut Effect	ctrl.PlayScreenTransition(false)	画面遷移：FadeOut（false）
- Play Selection Effect	ctrl.PlaySelectionCardEffect(ctrl.SampleCardArray[9])	カード選択演出開始（サンプルカード9）
- Stop Selection Effect	ctrl.StopSelectionCardEffect()	カード選択演出停止
- Play Score Effect	ctrl.PlayScoreEffect(ctrl.GetPointNum, true)	スコア演出（点数=GetPointNum、第2引数true固定）
- Delete All Effects	ctrl.DeleteEffect()	生成/表示中の演出オブジェクトを全削除（演出そのもの���はなく掃除コマンド）
- Play Show All CardList	ctrl.ShowAllCardList()	カード一覧を全表示
- Hide All CardList	ctrl.HideAllCardList()	カード一覧を全非表示

## 開発メモ（方針）
- 「演出（View）」と「処理（Model/State）」の責務を分離する
- 効果処理はテストしやすい形で組む
