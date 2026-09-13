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
        private Text scoreText;
        private Text callText;
        private Text resultText;
        private Text detailText;
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
            Anchor(title.rectTransform, new Vector2(.5f, .57f), new Vector2(.5f, .57f), Vector2.zero, new Vector2(900, 120));
            title.alignment = TextAnchor.MiddleCenter;

            Text sub = MakeText("Subtitle", panel.transform, "最初はグー。タイミングよく手を選ぼう。", 24, FontStyle.Normal, Muted);
            Anchor(sub.rectTransform, new Vector2(.5f, .43f), new Vector2(.5f, .43f), Vector2.zero, new Vector2(800, 60));
            sub.alignment = TextAnchor.MiddleCenter;

            Button start = MakeButton("StartButton", panel.transform, "はじめる", Red, new Vector2(300, 78));
            Anchor(start.GetComponent<RectTransform>(), new Vector2(.5f, .27f), new Vector2(.5f, .27f), Vector2.zero, new Vector2(300, 78));
            start.onClick.AddListener(BeginGame);
            return panel;
        }

        private GameObject BuildChoice()
        {
            GameObject panel = Panel("Choice");
            Text prompt = MakeText("Prompt", panel.transform, "手を選んでね", 48, FontStyle.Bold, Cream);
            Anchor(prompt.rectTransform, new Vector2(.5f, .76f), new Vector2(.5f, .76f), Vector2.zero, new Vector2(700, 70));
            prompt.alignment = TextAnchor.MiddleCenter;
            Text hint = MakeText("Hint", panel.transform, "クリックすると勝負スタート！", 20, FontStyle.Normal, Muted);
            Anchor(hint.rectTransform, new Vector2(.5f, .68f), new Vector2(.5f, .68f), Vector2.zero, new Vector2(600, 40));
            hint.alignment = TextAnchor.MiddleCenter;

            CreateHandButton(panel.transform, JankenIconGraphic.Hand.Rock, "グー", new Vector2(.25f, .39f), Red);
            CreateHandButton(panel.transform, JankenIconGraphic.Hand.Scissors, "チョキ", new Vector2(.5f, .39f), Yellow);
            CreateHandButton(panel.transform, JankenIconGraphic.Hand.Paper, "パー", new Vector2(.75f, .39f), Cyan);
            return panel;
        }

        private void CreateHandButton(Transform parent, JankenIconGraphic.Hand hand, string label, Vector2 anchor, Color accent)
        {
            Button button = MakeButton("Choose_" + hand, parent, "", Navy2, new Vector2(250, 260));
            Anchor(button.GetComponent<RectTransform>(), anchor, anchor, Vector2.zero, new Vector2(250, 260));
            ColorBlock cb = button.colors;
            cb.normalColor = Navy2;
            cb.highlightedColor = new Color(accent.r * .42f, accent.g * .42f, accent.b * .42f, 1);
            cb.pressedColor = accent;
            cb.selectedColor = Navy2;
            cb.fadeDuration = .12f;
            button.colors = cb;

            JankenIconGraphic icon = UI<JankenIconGraphic>("Icon", button.transform);
            Anchor(icon.rectTransform, new Vector2(.5f, .58f), new Vector2(.5f, .58f), Vector2.zero, new Vector2(150, 150));
            icon.Value = hand;
            icon.color = accent;
            icon.raycastTarget = false;
            Text text = MakeText("Label", button.transform, label, 31, FontStyle.Bold, Cream);
            Anchor(text.rectTransform, new Vector2(.5f, .15f), new Vector2(.5f, .15f), Vector2.zero, new Vector2(210, 55));
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            button.onClick.AddListener(() => Choose(hand));
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
            againButton = MakeButton("Again", panel.transform, "もう一回", Cyan, new Vector2(240, 64));
            Anchor(againButton.GetComponent<RectTransform>(), new Vector2(.5f, .055f), new Vector2(.5f, .055f), Vector2.zero, new Vector2(240, 64));
            againButton.onClick.AddListener(BackToChoice);
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
            StartCoroutine(Transition(openingScreen, choiceScreen));
        }

        private void Choose(JankenIconGraphic.Hand hand)
        {
            if (busy) return;
            sound.Play("choose");
            StartCoroutine(PlayRound(hand));
        }

        private IEnumerator PlayRound(JankenIconGraphic.Hand player)
        {
            busy = true;
            yield return Transition(choiceScreen, battleScreen);
            JankenIconGraphic.Hand cpu = (JankenIconGraphic.Hand)Random.Range(0, 3);
            playerIcon.Value = player;
            cpuIcon.Value = cpu;
            playerIcon.canvasRenderer.SetAlpha(0f);
            cpuIcon.canvasRenderer.SetAlpha(0f);
            resultText.text = "";
            detailText.text = "";
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

            playerIcon.CrossFadeAlpha(1f, .12f, true);
            cpuIcon.CrossFadeAlpha(1f, .12f, true);
            yield return StartCoroutine(RevealCards());
            int outcome = Judge(player, cpu);
            ShowResult(outcome, player, cpu);
            yield return ScaleBounce(resultText.rectTransform, .45f);
            if (outcome > 0) StartCoroutine(Confetti());
            againButton.gameObject.SetActive(true);
            yield return ScaleBounce(againButton.GetComponent<RectTransform>(), .28f);
            busy = false;
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

        private void ShowResult(int outcome, JankenIconGraphic.Hand player, JankenIconGraphic.Hand cpu)
        {
            if (outcome > 0)
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
            detailText.text = $"{HandName(player)} vs {HandName(cpu)}";
            scoreText.text = $"{wins} 勝　{losses} 敗　{draws} 分";
        }

        private void BackToChoice()
        {
            if (busy) return;
            sound.Play("choose", .7f);
            StartCoroutine(Transition(battleScreen, choiceScreen));
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
            for (int i = 0; i < 36; i++)
            {
                Image p = UI<Image>("Confetti", root);
                p.color = i % 3 == 0 ? Red : i % 3 == 1 ? Yellow : Cyan;
                p.raycastTarget = false;
                Anchor(p.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(Random.Range(-80, 80), 80), new Vector2(Random.Range(7, 15), Random.Range(18, 32)));
                pieces.Add(p.rectTransform);
            }
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < pieces.Count; i++)
                {
                    RectTransform p = pieces[i];
                    p.anchoredPosition += new Vector2(Mathf.Sin(t * 8f + i) * 2.5f, (290f - t * 440f) * Time.unscaledDeltaTime);
                    p.Rotate(0, 0, (i % 2 == 0 ? 250 : -250) * Time.unscaledDeltaTime);
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
