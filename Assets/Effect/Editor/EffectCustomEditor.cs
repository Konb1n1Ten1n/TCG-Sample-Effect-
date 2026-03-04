#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DuelKingdom.Effect
{
    [CustomEditor(typeof(EffectController))]
    public class EffectControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var ctrl = (EffectController)target;

            GUILayout.Space(20);
            DrawButton("Play Slash Slave Effect", () => ctrl.PlaySlashSlaveEffect(ctrl.SampleCardArray[0]));
            DrawButton("Play Slash Commoners Effect", () => ctrl.PlaySlashCommonersEffect(ctrl.SampleCardArray[1]));

            DrawButton("Play Start Turn Effect", () => ctrl.PlayStartTurn());
            DrawButton("Play Turn Count Effect", () => ctrl.PlayTurnCount(ctrl.TurnCount));

            DrawButton("Play Turn End Effect", () => ctrl.PlayTurnEndEffect(ctrl.TurnEndResult, ctrl.GetPointNum, ctrl.MyCheck, ctrl.PlayGoldOnEffect));
            DrawButton("Play Mist Effect", () => ctrl.PlayMistEffect(ctrl.SampleCardArray[2]));
            DrawButton("Play Shine Effect", () => ctrl.PlayShineEffect(ctrl.SampleCardArray[3], 9, ctrl.OnWitch));
            DrawButton("Play Change Num Only", () => ctrl.PlayChangeNumOnly(ctrl.SampleCardArray[3], 9, ctrl.OnWitch));
            DrawButton("Play Undo Num", () => ctrl.PlayUndoNumber(ctrl.SampleCardArray[3], ctrl.ShineKnight.SampleUndoNum));

            DrawButton("4 Play Cross Effect", () => ctrl.PlayCrossEffect(ctrl.SampleCardArray[4]));
            DrawButton("4 Stop Cross Effect", () => ctrl.StopCrossEffect(ctrl.SampleCardArray[4]));
            DrawButton("5 Play Cross Effect", () => ctrl.PlayCrossEffect(ctrl.SampleCardArray[5]));
            DrawButton("5 Stop Cross Effect", () => ctrl.StopCrossEffect(ctrl.SampleCardArray[5]));

            DrawButton("All Stop Cross Effect", () => ctrl.StopCrossEffect());

            DrawButton("Play Burst Effect", () => ctrl.PlayBurstEffect(ctrl.SampleCardArray[5], ctrl.BothBurst ? ctrl.SampleCardArray[6] : null));
            DrawButton("Play AbstractShine Effect", () => ctrl.PlayAbstractShineEffect(ctrl.SampleCardArray[7], ctrl.BothAbsShine ? ctrl.SampleCardArray[8] : null));
            DrawButton("Play Result Effect", () => ctrl.PlayResultEffect());
            if (GUILayout.Button("Stop Result Effect")) ctrl.StopResultEffect();
            DrawButton("Play FadeIn Effect", () => ctrl.PlayScreenTransition(true));
            DrawButton("Play FadeOut Effect", () => ctrl.PlayScreenTransition(false));
            DrawButton("Play Selection Effect", () => ctrl.PlaySelectionCardEffect(ctrl.SampleCardArray[9]));
            DrawButton("Stop Selection Effect", () => ctrl.StopSelectionCardEffect());
            DrawButton("Play Score Effect", () => ctrl.PlayScoreEffect(ctrl.GetPointNum, true));
            GUILayout.Space(15);
            if (GUILayout.Button("Delete All Effects")) ctrl.DeleteEffect();
            GUILayout.Space(15);
            if (GUILayout.Button("Play Show All CardList")) ctrl.ShowAllCardList();
            if (GUILayout.Button("Hide All CardList")) ctrl.HideAllCardList();
        }

        private void DrawButton(string label, System.Func<UniTask> action)
        {
            if (GUILayout.Button(label))
                EditorApplication.delayCall += () => _ = action();
        }
    }

    [CustomEditor(typeof(Burst_King))]
    public class BurstKingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var ctrl = (Burst_King)target;

            GUILayout.Space(20);
            if (GUILayout.Button("Set Burst objects"))
            {
                for (int i = 0; i < 2; i++)
                    ctrl.BurstSpriteSets[i].BurstSprites = ctrl.BurstPool[i].transform.GetComponentsInChildren<SpriteRenderer>();
            }
        }
    }
}
#endif