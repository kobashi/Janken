using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Janken
{
    /// <summary>
    /// 日本式じゃんけんの全進行と、教材向けのコード生成UIを担当する。
    /// Sceneには何も置かず、起動時にこのクラスが画面を組み立てる。
    /// </summary>
    public sealed class JankenGame : MonoBehaviour
    {
        private const float CpuFinalChoiceAt = 4f;
        private static readonly float[] LateChoiceDurations = { .5f, 1f, 1.5f };
        private static readonly string[] DifficultyNames = { "達人", "侍（普通）", "若武者" };
        private sealed class ChoiceTile
        {
            public Button Button;
            public RectTransform Rect;
            public JankenIconGraphic.Hand Hand;
        }

        private static readonly Color Navy = Hex("10162F");
        private static readonly Color Navy2 = Hex("191F42");
        private static readonly Color Cream = Hex("FFF6DF");
        private static readonly Color Red = Hex("FF4D5E");
        private static readonly Color Yellow = Hex("FFD84D");
        private static readonly Color Cyan = Hex("35D5E5");
        private static readonly Color Muted = Hex("AAB0CC");

        private Font font;
        private RectTransform root;
        private GameObject openingScreen;
        private GameObject choiceScreen;
        private GameObject battleScreen;
        private readonly List<ChoiceTile> choiceTiles = new();
        private readonly List<Button> difficultyButtons = new();
        private readonly List<JankenIconGraphic.Hand> playerHandHistory = new();
        private readonly List<JankenIconGraphic.Hand> cpuHandHistory = new();
        private Text scoreText;
        private Text choiceTimerText;
        private Text choicePromptText;
        private Text choiceHintText;
        private Text difficultyStatusText;
        private Text choiceFeedbackText;
        private Text choiceLockedText;
        private Text cpuChoiceText;
        private Image choiceTimerFill;
        private Text callText;
        private Text resultText;
        private Text detailText;
        private Text playerHistoryText;
        private Text cpuHistoryText;
        private GameObject judgeOverlay;
        private Text judgeText;
        private Image judgeBarFill;
        private JankenIconGraphic playerIcon;
        private JankenIconGraphic cpuIcon;
        private RectTransform playerCard;
        private RectTransform cpuCard;
        private Button againButton;
        private SynthSound sound;
        private int wins;
        private int losses;
        private int draws;
        private bool busy;
        private bool choiceActive;
        private bool hasLockedHand;
        private JankenIconGraphic.Hand lockedHand;
        private int tapCount;
        private float choiceStartedAt;
        private int difficultyIndex = 1;
        private float LateChoiceDuration => LateChoiceDurations[difficultyIndex];
        private float ChoiceDuration => CpuFinalChoiceAt + LateChoiceDuration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<JankenGame>() == null)
                new GameObject("JankenGame").AddComponent<JankenGame>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            // WebGLではOSフォントへフォールバックできないため、OFLの日本語フォントを同梱する。
            font = Resources.Load<Font>("Fonts/NotoSansCJKjp-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sound = gameObject.AddComponent<SynthSound>();
            sound.Initialize();
            BuildEventSystem();
            BuildInterface();
            ShowOnly(openingScreen);
            StartCoroutine(OpeningEntrance());
        }

        private void BuildEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject go = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private void BuildInterface()
        {
            GameObject canvasGo = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasGo.GetComponent<RectTransform>();

            Image background = UI<Image>("Background", root);
            Stretch(background.rectTransform);
            background.color = Navy;
            background.raycastTarget = false;
            MakeDecorations();

            scoreText = MakeText("Score", root, "0 勝　0 敗　0 分", 22, FontStyle.Bold, Muted);
            Anchor(scoreText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(480, 42));
            scoreText.alignment = TextAnchor.MiddleCenter;

            openingScreen = BuildOpening();
            choiceScreen = BuildChoice();
            battleScreen = BuildBattle();
        }

        private void MakeDecorations()
        {
            for (int i = 0; i < 14; i++)
            {
                Image dot = UI<Image>("Dot", root);
                float size = 8 + (i % 4) * 5;
                Anchor(dot.rectTransform, new Vector2((i * 0.173f) % 1f, (i * 0.317f) % 1f), new Vector2((i * 0.173f) % 1f, (i * 0.317f) % 1f), Vector2.zero, new Vector2(size, size));
                dot.color = new Color((i % 2 == 0 ? Cyan : Yellow).r, (i % 2 == 0 ? Cyan : Yellow).g, (i % 2 == 0 ? Cyan : Yellow).b, 0.15f);
                dot.raycastTarget = false;
                dot.transform.rotation = Quaternion.Euler(0, 0, i * 17f);
            }
        }

        private GameObject BuildOpening()
        {
            GameObject panel = Panel("Opening");
            Text eyebrow = MakeText("Eyebrow", panel.transform, "JAPANESE ROCK • PAPER • SCISSORS", 17, FontStyle.Bold, Cyan);
            Anchor(eyebrow.rectTransform, new Vector2(.5f, .72f), new Vector2(.5f, .72f), Vector2.zero, new Vector2(700, 42));
            eyebrow.alignment = TextAnchor.MiddleCenter;

            Text title = MakeText("Title", panel.transform, "じゃんけん！", 82, FontStyle.Bold, Cream);
            Anchor(title.rectTransform, new Vector2(.5f, .62f), new Vector2(.5f, .62f), Vector2.zero, new Vector2(900, 120));
            title.alignment = TextAnchor.MiddleCenter;

            Text sub = MakeText("Subtitle", panel.transform, "最初はグー。タイミングよく手を選ぼう。", 24, FontStyle.Normal, Muted);
            Anchor(sub.rectTransform, new Vector2(.5f, .49f), new Vector2(.5f, .49f), Vector2.zero, new Vector2(800, 60));
            sub.alignment = TextAnchor.MiddleCenter;

            difficultyStatusText = MakeText("DifficultyStatus", panel.transform, "難易度：侍（普通）　後出し 1.0 秒", 20, FontStyle.Bold, Yellow);
            Anchor(difficultyStatusText.rectTransform, new Vector2(.5f, .385f), new Vector2(.5f, .385f), Vector2.zero, new Vector2(820, 40));
            difficultyStatusText.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < DifficultyNames.Length; i++)
            {
                int selected = i;
                Button difficulty = MakeButton("Difficulty_" + i, panel.transform,
                    $"{DifficultyNames[i]}  {LateChoiceDurations[i]:0.0}秒", Navy2, new Vector2(230, 54));
                Anchor(difficulty.GetComponent<RectTransform>(), new Vector2(.5f, .30f), new Vector2(.5f, .30f), new Vector2((i - 1) * 250, 0), new Vector2(230, 54));
                difficulty.onClick.AddListener(() => SetDifficulty(selected));
                difficultyButtons.Add(difficulty);
            }

            Button start = MakeButton("StartButton", panel.transform, "はじめる", Red, new Vector2(300, 78));
            Anchor(start.GetComponent<RectTransform>(), new Vector2(.5f, .16f), new Vector2(.5f, .16f), Vector2.zero, new Vector2(300, 78));
            start.onClick.AddListener(BeginGame);
            SetDifficulty(1);
            return panel;
        }

        private void SetDifficulty(int index)
        {
            bool changed = index != difficultyIndex;
            difficultyIndex = Mathf.Clamp(index, 0, LateChoiceDurations.Length - 1);
            if (difficultyStatusText != null)
                difficultyStatusText.text = $"難易度：{DifficultyNames[difficultyIndex]}　後出し {LateChoiceDuration:0.0} 秒";
            for (int i = 0; i < difficultyButtons.Count; i++)
            {
                Color baseColor = i == difficultyIndex ? Yellow : Navy2;
                Button button = difficultyButtons[i];
                button.targetGraphic.color = baseColor;
                ColorBlock colors = button.colors;
                colors.normalColor = baseColor;
                colors.highlightedColor = Color.Lerp(baseColor, Color.white, .18f);
                colors.pressedColor = Color.Lerp(baseColor, Color.black, .18f);
                colors.selectedColor = baseColor;
                button.colors = colors;
                Text label = button.transform.Find("Text").GetComponent<Text>();
                label.color = i == difficultyIndex ? Navy : Cream;
            }
            if (changed) sound?.Play("choose", .55f);
        }

        private GameObject BuildChoice()
        {
            GameObject panel = Panel("Choice");
            choicePromptText = MakeText("Prompt", panel.transform, "5.0秒間、連打で手を決めろ！", 43, FontStyle.Bold, Cream);
            Anchor(choicePromptText.rectTransform, new Vector2(.5f, .84f), new Vector2(.5f, .84f), Vector2.zero, new Vector2(900, 64));
            choicePromptText.alignment = TextAnchor.MiddleCenter;
            choiceHintText = MakeText("Hint", panel.transform, "CPUの最後の発音後も1.0秒間入力可能。後出しで決めろ！", 18, FontStyle.Normal, Muted);
            Anchor(choiceHintText.rectTransform, new Vector2(.5f, .775f), new Vector2(.5f, .775f), Vector2.zero, new Vector2(900, 36));
            choiceHintText.alignment = TextAnchor.MiddleCenter;

            Image timerBack = UI<Image>("TimerBack", panel.transform);
            Anchor(timerBack.rectTransform, new Vector2(.5f, .71f), new Vector2(.5f, .71f), Vector2.zero, new Vector2(620, 22));
            timerBack.color = new Color(1, 1, 1, .10f);
            choiceTimerFill = UI<Image>("TimerFill", timerBack.transform);
            Stretch(choiceTimerFill.rectTransform);
            choiceTimerFill.color = Cyan;
            choiceTimerFill.raycastTarget = false;
            choiceTimerText = MakeText("TimerText", panel.transform, "残り 5.0 秒", 19, FontStyle.Bold, Cream);
            Anchor(choiceTimerText.rectTransform, new Vector2(.5f, .665f), new Vector2(.5f, .665f), Vector2.zero, new Vector2(400, 34));
            choiceTimerText.alignment = TextAnchor.MiddleCenter;
            cpuChoiceText = MakeText("CpuChoice", panel.transform, "CPUも選択中… 0/3", 17, FontStyle.Bold, Muted);
            Anchor(cpuChoiceText.rectTransform, new Vector2(.5f, .62f), new Vector2(.5f, .62f), Vector2.zero, new Vector2(500, 30));
            cpuChoiceText.alignment = TextAnchor.MiddleCenter;

            CreateChoiceTile(panel.transform, JankenIconGraphic.Hand.Rock, "グー", Red, new Vector2(-330, -25));
            CreateChoiceTile(panel.transform, JankenIconGraphic.Hand.Scissors, "チョキ", Yellow, new Vector2(0, -25));
            CreateChoiceTile(panel.transform, JankenIconGraphic.Hand.Paper, "パー", Cyan, new Vector2(330, -25));

            choiceFeedbackText = MakeText("Feedback", panel.transform, "連打スタート！", 25, FontStyle.Bold, Yellow);
            Anchor(choiceFeedbackText.rectTransform, new Vector2(.5f, .105f), new Vector2(.5f, .105f), Vector2.zero, new Vector2(650, 34));
            choiceFeedbackText.alignment = TextAnchor.MiddleCenter;
            choiceLockedText = MakeText("Locked", panel.transform, "選択：まだなし", 18, FontStyle.Normal, Muted);
            Anchor(choiceLockedText.rectTransform, new Vector2(.5f, .055f), new Vector2(.5f, .055f), Vector2.zero, new Vector2(650, 28));
            choiceLockedText.alignment = TextAnchor.MiddleCenter;
            return panel;
        }

        private void CreateChoiceTile(Transform parent, JankenIconGraphic.Hand hand, string label, Color accent, Vector2 position)
        {
            Button button = MakeButton("Choice_" + hand, parent, "", Navy2, new Vector2(210, 190));
            RectTransform rect = button.GetComponent<RectTransform>();
            Anchor(rect, new Vector2(.5f, .43f), new Vector2(.5f, .43f), position, new Vector2(210, 190));
            ColorBlock cb = button.colors;
            cb.normalColor = Navy2;
            cb.highlightedColor = new Color(accent.r * .42f, accent.g * .42f, accent.b * .42f, 1);
            cb.pressedColor = accent;
            cb.selectedColor = Navy2;
            cb.fadeDuration = .12f;
            button.colors = cb;

            JankenIconGraphic icon = UI<JankenIconGraphic>("Icon", button.transform);
            Anchor(icon.rectTransform, new Vector2(.5f, .60f), new Vector2(.5f, .60f), Vector2.zero, new Vector2(110, 110));
            icon.Value = hand;
            icon.color = accent;
            icon.raycastTarget = false;
            Text text = MakeText("Label", button.transform, label, 29, FontStyle.Bold, Cream);
            Anchor(text.rectTransform, new Vector2(.5f, .16f), new Vector2(.5f, .16f), Vector2.zero, new Vector2(180, 40));
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            ChoiceTile tile = new() { Button = button, Rect = rect, Hand = hand };
            choiceTiles.Add(tile);
            button.onClick.AddListener(() => TapChoice(tile));
        }

        private GameObject BuildBattle()
        {
            GameObject panel = Panel("Battle");
            callText = MakeText("Call", panel.transform, "", 56, FontStyle.Bold, Yellow);
            Anchor(callText.rectTransform, new Vector2(.5f, .82f), new Vector2(.5f, .82f), Vector2.zero, new Vector2(900, 90));
            callText.alignment = TextAnchor.MiddleCenter;

            playerCard = MakeCard(panel.transform, "あなた", new Vector2(.30f, .49f), out playerIcon, Cyan);
            cpuCard = MakeCard(panel.transform, "あいて", new Vector2(.70f, .49f), out cpuIcon, Red);
            Text versus = MakeText("Versus", panel.transform, "VS", 38, FontStyle.Bold, Muted);
            Anchor(versus.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(110, 80));
            versus.alignment = TextAnchor.MiddleCenter;

            resultText = MakeText("Result", panel.transform, "", 54, FontStyle.Bold, Cream);
            Anchor(resultText.rectTransform, new Vector2(.5f, .19f), new Vector2(.5f, .19f), Vector2.zero, new Vector2(700, 70));
            resultText.alignment = TextAnchor.MiddleCenter;
            detailText = MakeText("Detail", panel.transform, "", 20, FontStyle.Normal, Muted);
            Anchor(detailText.rectTransform, new Vector2(.5f, .12f), new Vector2(.5f, .12f), Vector2.zero, new Vector2(700, 40));
            detailText.alignment = TextAnchor.MiddleCenter;
            playerHistoryText = MakeText("PlayerHistory", panel.transform, "", 17, FontStyle.Bold, Cyan);
            Anchor(playerHistoryText.rectTransform, new Vector2(.30f, .285f), new Vector2(.30f, .285f), Vector2.zero, new Vector2(430, 42));
            playerHistoryText.alignment = TextAnchor.MiddleCenter;
            cpuHistoryText = MakeText("CpuHistory", panel.transform, "", 17, FontStyle.Bold, Red);
            Anchor(cpuHistoryText.rectTransform, new Vector2(.70f, .285f), new Vector2(.70f, .285f), Vector2.zero, new Vector2(430, 42));
            cpuHistoryText.alignment = TextAnchor.MiddleCenter;
            againButton = MakeButton("Again", panel.transform, "もう一回", Cyan, new Vector2(240, 64));
            Anchor(againButton.GetComponent<RectTransform>(), new Vector2(.5f, .055f), new Vector2(.5f, .055f), Vector2.zero, new Vector2(240, 64));
            againButton.onClick.AddListener(BackToChoice);

            Image overlay = UI<Image>("JudgeOverlay", panel.transform);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(Navy.r, Navy.g, Navy.b, .97f);
            judgeOverlay = overlay.gameObject;
            judgeText = MakeText("JudgeText", overlay.transform, "判定中…", 62, FontStyle.Bold, Cream);
            Anchor(judgeText.rectTransform, new Vector2(.5f, .58f), new Vector2(.5f, .58f), Vector2.zero, new Vector2(900, 100));
            judgeText.alignment = TextAnchor.MiddleCenter;
            Text judgeHint = MakeText("JudgeHint", overlay.transform, "勝負の行方は――", 21, FontStyle.Normal, Muted);
            Anchor(judgeHint.rectTransform, new Vector2(.5f, .46f), new Vector2(.5f, .46f), Vector2.zero, new Vector2(700, 44));
            judgeHint.alignment = TextAnchor.MiddleCenter;
            Image judgeBarBack = UI<Image>("JudgeBarBack", overlay.transform);
            Anchor(judgeBarBack.rectTransform, new Vector2(.5f, .36f), new Vector2(.5f, .36f), Vector2.zero, new Vector2(640, 28));
            judgeBarBack.color = new Color(1, 1, 1, .10f);
            judgeBarFill = UI<Image>("JudgeBarFill", judgeBarBack.transform);
            Stretch(judgeBarFill.rectTransform);
            judgeBarFill.color = Red;
            judgeBarFill.raycastTarget = false;
            judgeOverlay.SetActive(false);
            return panel;
        }

        private RectTransform MakeCard(Transform parent, string caption, Vector2 anchor, out JankenIconGraphic icon, Color accent)
        {
            Image card = UI<Image>(caption + "Card", parent);
            Anchor(card.rectTransform, anchor, anchor, Vector2.zero, new Vector2(330, 280));
            card.color = Navy2;
            Text tag = MakeText("Caption", card.transform, caption, 20, FontStyle.Bold, accent);
            Anchor(tag.rectTransform, new Vector2(.5f, .88f), new Vector2(.5f, .88f), Vector2.zero, new Vector2(240, 40));
            tag.alignment = TextAnchor.MiddleCenter;
            icon = UI<JankenIconGraphic>("Hand", card.transform);
            Anchor(icon.rectTransform, new Vector2(.5f, .43f), new Vector2(.5f, .43f), Vector2.zero, new Vector2(180, 180));
            icon.color = accent;
            icon.raycastTarget = false;
            return card.rectTransform;
        }

        private IEnumerator OpeningEntrance()
        {
            RectTransform title = openingScreen.transform.Find("Title").GetComponent<RectTransform>();
            yield return ScaleBounce(title, .58f);
        }

        private void BeginGame()
        {
            if (busy) return;
            sound.Play("start");
            StartCoroutine(EnterChoiceScreen(openingScreen));
        }

        private IEnumerator EnterChoiceScreen(GameObject from)
        {
            yield return Transition(from, choiceScreen);
            BeginChoicePhase();
        }

        private void BeginChoicePhase()
        {
            choiceActive = true;
            hasLockedHand = false;
            tapCount = 0;
            choiceStartedAt = Time.unscaledTime;
            playerHandHistory.Clear();
            cpuHandHistory.Clear();
            choicePromptText.text = $"{ChoiceDuration:0.0}秒間、連打で手を決めろ！";
            choiceHintText.text = $"{DifficultyNames[difficultyIndex]}：CPUの最後の発音後も {LateChoiceDuration:0.0} 秒間入力可能";
            choiceFeedbackText.text = "グー・チョキ・パーを何度でも選べ！";
            choiceFeedbackText.color = Yellow;
            choiceLockedText.text = "現在の手：まだなし　｜　0タップ";
            foreach (ChoiceTile tile in choiceTiles) tile.Button.interactable = true;
            StartCoroutine(ChoiceTimer());
            StartCoroutine(CpuChoiceCountdown());
        }

        private void TapChoice(ChoiceTile tile)
        {
            if (!choiceActive) return;

            tapCount++;
            sound.Play("tap");
            sound.PlayHand((int)tile.Hand, .86f);
            StartCoroutine(ScaleBounce(tile.Rect, .16f));
            lockedHand = tile.Hand;
            hasLockedHand = true;
            playerHandHistory.Add(tile.Hand);
            choiceFeedbackText.text = HandName(tile.Hand) + "を選択！";
            choiceFeedbackText.color = Cyan;
            choiceLockedText.text = $"現在の手：{HandName(lockedHand)}　｜　{tapCount}タップ";
        }

        private IEnumerator CpuChoiceCountdown()
        {
            float[] beatTimes = { 1f, 2.5f, CpuFinalChoiceAt };
            string[] beats = { "ジャン", "ケン", "ポン" };
            for (int i = 0; i < 3; i++)
            {
                while (choiceActive && Time.unscaledTime - choiceStartedAt < beatTimes[i])
                    yield return null;
                if (!choiceActive) yield break;
                JankenIconGraphic.Hand hand = (JankenIconGraphic.Hand)Random.Range(0, 3);
                cpuHandHistory.Add(hand);
                cpuChoiceText.text = $"CPU「{beats[i]}」 {i + 1}/3　手を選択（秘密）";
                cpuChoiceText.color = i == 2 ? Yellow : Muted;
                sound.Play(i == 2 ? "pon" : "count", .48f);
                sound.PlayHand((int)hand, .82f);
                yield return ScaleBounce(cpuChoiceText.rectTransform, .16f);
            }
            cpuChoiceText.text = $"後出し受付中！ 発音後 {LateChoiceDuration:0.0} 秒　CPUの手は秘密";
            cpuChoiceText.color = Red;
        }

        private IEnumerator ChoiceTimer()
        {
            while (choiceActive)
            {
                float elapsed = Time.unscaledTime - choiceStartedAt;
                float remaining = Mathf.Max(0f, ChoiceDuration - elapsed);
                choiceTimerText.text = elapsed >= CpuFinalChoiceAt
                    ? $"後出し可能　残り {remaining:0.0} 秒"
                    : $"残り {remaining:0.0} 秒";
                float ratio = remaining / ChoiceDuration;
                RectTransform fill = choiceTimerFill.rectTransform;
                fill.anchorMax = new Vector2(ratio, 1);
                fill.offsetMax = Vector2.zero;
                choiceTimerFill.color = elapsed >= CpuFinalChoiceAt ? Red : ratio < .55f ? Yellow : Cyan;
                if (remaining <= 0f) break;
                yield return null;
            }

            choiceActive = false;
            foreach (ChoiceTile tile in choiceTiles) tile.Button.interactable = false;
            while (cpuHandHistory.Count < 3)
            {
                JankenIconGraphic.Hand fallback = (JankenIconGraphic.Hand)Random.Range(0, 3);
                cpuHandHistory.Add(fallback);
            }
            choiceTimerText.text = "TIME UP!";
            choiceFeedbackText.text = hasLockedHand ? "入力を確定！" : "未選択のため敗北！";
            choiceFeedbackText.color = hasLockedHand ? Yellow : Red;
            choiceLockedText.text = "あなたの最終手：？？？　｜　CPUの最終手：？？？";
            sound.Play("timeup");
            yield return ScaleBounce(choiceTimerText.rectTransform, .32f);
            yield return new WaitForSecondsRealtime(.25f);
            JankenIconGraphic.Hand? player = hasLockedHand ? lockedHand : null;
            StartCoroutine(PlayRound(player));
        }

        private IEnumerator PlayRound(JankenIconGraphic.Hand? player)
        {
            busy = true;
            yield return Transition(choiceScreen, battleScreen);
            JankenIconGraphic.Hand cpu = cpuHandHistory[cpuHandHistory.Count - 1];
            if (player.HasValue) playerIcon.Value = player.Value;
            cpuIcon.Value = cpu;
            playerIcon.canvasRenderer.SetAlpha(0f);
            cpuIcon.canvasRenderer.SetAlpha(0f);
            resultText.text = "";
            detailText.text = "";
            playerHistoryText.text = "あなたの履歴：待機中";
            cpuHistoryText.text = "CPUの履歴：待機中";
            againButton.gameObject.SetActive(false);

            string[] calls = { "最初はグー！", "じゃんけん…", "ぽん！" };
            for (int i = 0; i < calls.Length; i++)
            {
                callText.text = calls[i];
                callText.color = i == 2 ? Red : Yellow;
                sound.Play(i == 2 ? "pon" : "count");
                yield return ScaleBounce(callText.rectTransform, i == 2 ? .36f : .28f);
                if (i < 2) yield return new WaitForSeconds(.14f);
            }

            yield return StartCoroutine(ReplayHistories());
            int outcome = player.HasValue ? Judge(player.Value, cpu) : -1;
            yield return StartCoroutine(DramaticJudgement());
            callText.text = "最終手を公開！";
            callText.color = Yellow;
            if (player.HasValue) playerIcon.Value = player.Value;
            cpuIcon.Value = cpu;
            playerHistoryText.text = player.HasValue ? "最終：" + HandName(player.Value) : "最終：未選択（後出し）";
            cpuHistoryText.text = "最終：" + HandName(cpu);
            if (player.HasValue)
            {
                sound.PlayHand((int)player.Value, .9f);
                yield return new WaitForSecondsRealtime(.16f);
            }
            sound.PlayHand((int)cpu, .9f);
            if (player.HasValue) playerIcon.CrossFadeAlpha(1f, .12f, true);
            cpuIcon.CrossFadeAlpha(1f, .12f, true);
            yield return StartCoroutine(RevealCards());
            ShowResult(outcome, player, cpu);
            yield return ScaleBounce(resultText.rectTransform, .45f);
            if (outcome > 0) StartCoroutine(Confetti());
            againButton.gameObject.SetActive(true);
            yield return ScaleBounce(againButton.GetComponent<RectTransform>(), .28f);
            busy = false;
        }

        private IEnumerator ReplayHistories()
        {
            int cpuVisibleCount = Mathf.Max(0, cpuHandHistory.Count - 1);
            int playerVisibleCount = Mathf.Max(0, playerHandHistory.Count - 1);
            int beatCount = Mathf.Max(2, playerVisibleCount);
            const float totalDuration = 1.56f;
            float stepDuration = totalDuration / beatCount;

            for (int step = 0; step < beatCount; step++)
            {
                callText.text = $"履歴を再生 {step + 1}/{beatCount}";
                callText.color = Cream;

                if (step < playerVisibleCount)
                {
                    JankenIconGraphic.Hand playerPast = playerHandHistory[step];
                    playerIcon.Value = playerPast;
                    playerIcon.canvasRenderer.SetAlpha(1f);
                    playerHistoryText.text = $"あなたの履歴 {step + 1}/{playerVisibleCount}：{HandName(playerPast)}";
                    sound.PlayHand((int)playerPast, .76f);
                    StartCoroutine(ScaleBounce(playerIcon.rectTransform, Mathf.Min(.20f, stepDuration * .70f)));
                }
                else
                {
                    playerIcon.canvasRenderer.SetAlpha(0f);
                    playerHistoryText.text = "あなたの過去履歴：なし";
                }

                int cpuIndex = step == 0 ? 0 : step == beatCount - 1 ? cpuVisibleCount - 1 : -1;
                if (cpuIndex >= 0 && cpuIndex < cpuVisibleCount)
                {
                    JankenIconGraphic.Hand cpuPast = cpuHandHistory[cpuIndex];
                    cpuIcon.Value = cpuPast;
                    cpuIcon.canvasRenderer.SetAlpha(1f);
                    cpuHistoryText.text = $"CPUの履歴 {cpuIndex + 1}/{cpuVisibleCount}：{HandName(cpuPast)}";
                    sound.PlayHand((int)cpuPast, .76f);
                    StartCoroutine(ScaleBounce(cpuIcon.rectTransform, Mathf.Min(.20f, stepDuration * .70f)));
                }
                else
                {
                    cpuIcon.canvasRenderer.SetAlpha(0f);
                    cpuHistoryText.text = "CPU：次の同期拍を待機";
                }

                float stepEnd = Time.unscaledTime + stepDuration;
                while (Time.unscaledTime < stepEnd) yield return null;
            }

            playerIcon.canvasRenderer.SetAlpha(0f);
            cpuIcon.canvasRenderer.SetAlpha(0f);
            callText.text = "履歴再生終了――最終手は判定へ";
            callText.color = Cream;
            playerHistoryText.text = "あなた：履歴再生終了";
            cpuHistoryText.text = "CPU：履歴再生終了";
            yield return new WaitForSecondsRealtime(.35f);
        }

        private IEnumerator DramaticJudgement()
        {
            judgeOverlay.SetActive(true);
            CanvasGroup group = EnsureGroup(judgeOverlay);
            group.alpha = 0f;
            judgeText.text = "判定中…";
            judgeText.color = Cream;
            sound.Play("suspense");
            float elapsed = 0f;
            const float duration = 1.65f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Min(1f, ratio * 5f);
                RectTransform bar = judgeBarFill.rectTransform;
                bar.anchorMax = new Vector2(ratio, 1);
                bar.offsetMax = Vector2.zero;
                int dots = 1 + Mathf.FloorToInt(elapsed * 4f) % 3;
                judgeText.text = elapsed < 1.2f ? "判定中" + new string('・', dots) : "勝負の行方は――";
                float pulse = 1f + Mathf.Sin(elapsed * 15f) * .045f;
                judgeText.rectTransform.localScale = Vector3.one * pulse;
                yield return null;
            }

            sound.Play("reveal");
            Image overlay = judgeOverlay.GetComponent<Image>();
            Color original = overlay.color;
            overlay.color = Cream;
            judgeText.text = "決着！";
            judgeText.color = Navy;
            yield return ScaleBounce(judgeText.rectTransform, .28f);
            overlay.color = original;
            judgeText.color = Cream;
            float fade = 0f;
            while (fade < 1f)
            {
                fade += Time.unscaledDeltaTime * 5f;
                group.alpha = 1f - fade;
                yield return null;
            }
            group.alpha = 1f;
            judgeOverlay.SetActive(false);
        }

        private IEnumerator RevealCards()
        {
            Vector2 pa = playerCard.anchoredPosition;
            Vector2 ca = cpuCard.anchoredPosition;
            float t = 0f;
            playerCard.anchoredPosition = pa + Vector2.left * 400;
            cpuCard.anchoredPosition = ca + Vector2.right * 400;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 5.5f;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                playerCard.anchoredPosition = Vector2.LerpUnclamped(pa + Vector2.left * 400, pa, e);
                cpuCard.anchoredPosition = Vector2.LerpUnclamped(ca + Vector2.right * 400, ca, e);
                yield return null;
            }
            playerCard.anchoredPosition = pa;
            cpuCard.anchoredPosition = ca;
        }

        private void ShowResult(int outcome, JankenIconGraphic.Hand? player, JankenIconGraphic.Hand cpu)
        {
            if (!player.HasValue)
            {
                losses++;
                resultText.text = "後出しで敗北！";
                resultText.color = Red;
                sound.Play("lose");
            }
            else if (outcome > 0)
            {
                wins++;
                resultText.text = "勝ち！";
                resultText.color = Yellow;
                sound.Play("win");
            }
            else if (outcome < 0)
            {
                losses++;
                resultText.text = "負け…！";
                resultText.color = Red;
                sound.Play("lose");
            }
            else
            {
                draws++;
                resultText.text = "あいこ！";
                resultText.color = Cyan;
                sound.Play("draw");
            }
            detailText.text = player.HasValue
                ? $"{HandName(player.Value)} vs {HandName(cpu)}"
                : $"未選択（後出し） vs {HandName(cpu)}";
            scoreText.text = $"{wins} 勝　{losses} 敗　{draws} 分";
        }

        private void BackToChoice()
        {
            if (busy) return;
            sound.Play("choose", .7f);
            StartCoroutine(EnterChoiceScreen(battleScreen));
        }

        // Rock(0) beats Scissors(1), Scissors(1) beats Paper(2), Paper(2) beats Rock(0).
        private static int Judge(JankenIconGraphic.Hand player, JankenIconGraphic.Hand cpu)
        {
            if (player == cpu) return 0;
            return ((int)player + 1) % 3 == (int)cpu ? 1 : -1;
        }

        private static string HandName(JankenIconGraphic.Hand hand) => hand switch
        {
            JankenIconGraphic.Hand.Rock => "グー",
            JankenIconGraphic.Hand.Scissors => "チョキ",
            _ => "パー"
        };

        private IEnumerator Transition(GameObject from, GameObject to)
        {
            busy = true;
            CanvasGroup fromGroup = EnsureGroup(from);
            CanvasGroup toGroup = EnsureGroup(to);
            to.SetActive(true);
            toGroup.alpha = 0f;
            toGroup.interactable = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 4.5f;
                fromGroup.alpha = 1f - t;
                toGroup.alpha = t;
                yield return null;
            }
            from.SetActive(false);
            fromGroup.alpha = 1f;
            toGroup.alpha = 1f;
            toGroup.interactable = true;
            busy = false;
        }

        private IEnumerator ScaleBounce(RectTransform target, float seconds)
        {
            Vector3 original = Vector3.one;
            target.localScale = Vector3.one * .55f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / seconds;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * .16f;
                target.localScale = Vector3.Lerp(Vector3.one * .55f, original * s, Mathf.Clamp01(t * 2.4f));
                yield return null;
            }
            target.localScale = original;
        }

        private IEnumerator Confetti()
        {
            List<RectTransform> pieces = new();
            List<float> fallSpeeds = new();
            List<float> driftPhases = new();
            for (int i = 0; i < 96; i++)
            {
                Image p = UI<Image>("Confetti", root);
                p.color = i % 3 == 0 ? Red : i % 3 == 1 ? Yellow : Cyan;
                p.raycastTarget = false;
                Anchor(p.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(Random.Range(-625f, 625f), Random.Range(310f, 650f)), new Vector2(Random.Range(7, 15), Random.Range(18, 32)));
                pieces.Add(p.rectTransform);
                fallSpeeds.Add(Random.Range(155f, 270f));
                driftPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }
            float t = 0f;
            while (t < 4.6f)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < pieces.Count; i++)
                {
                    RectTransform p = pieces[i];
                    float drift = Mathf.Sin(t * 3.4f + driftPhases[i]) * 58f;
                    p.anchoredPosition += new Vector2(drift, -fallSpeeds[i]) * Time.unscaledDeltaTime;
                    p.Rotate(0, 0, (i % 2 == 0 ? 220 : -220) * Time.unscaledDeltaTime);
                }
                yield return null;
            }
            foreach (RectTransform p in pieces) Destroy(p.gameObject);
        }

        private GameObject Panel(string name)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        private static CanvasGroup EnsureGroup(GameObject go) => go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();

        private void ShowOnly(GameObject shown)
        {
            openingScreen.SetActive(shown == openingScreen);
            choiceScreen.SetActive(shown == choiceScreen);
            battleScreen.SetActive(shown == battleScreen);
        }

        private Text MakeText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            Text text = UI<Text>(name, parent);
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button MakeButton(string name, Transform parent, string label, Color color, Vector2 size)
        {
            Image image = UI<Image>(name, parent);
            image.color = color;
            image.rectTransform.sizeDelta = size;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock cb = button.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, .18f);
            cb.pressedColor = Color.Lerp(color, Color.black, .18f);
            cb.selectedColor = color;
            button.colors = cb;
            if (!string.IsNullOrEmpty(label))
            {
                Text text = MakeText("Text", button.transform, label, 28, FontStyle.Bold, Navy);
                Stretch(text.rectTransform);
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
            }
            return button;
        }

        private static T UI<T>(string name, Transform parent) where T : Graphic
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color result);
            return result;
        }
    }
}
