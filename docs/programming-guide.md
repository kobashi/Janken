# じゃんけんゲームで学ぶ開発・プログラミング基礎

この資料は、このプロジェクトの制作・修正・公開で登場した用語と、その修正を理解するための前提知識をまとめたものです。対象はUnityやGitを学び始めた人です。説明は公開済みの **v1.6.0** を基準とし、途中で採用・削除された仕様は末尾で区別します。

まず [ゲームを遊ぶ](https://kobashi.github.io/Janken/) と、画面・入力・音・判定の順番を観察してください。実装は [JankenGame.cs](../Assets/Scripts/JankenGame.cs)、[SynthSound.cs](../Assets/Scripts/SynthSound.cs)、[JankenWebAudio.jslib](../Assets/Plugins/WebGL/JankenWebAudio.jslib) を中心に読めます。

## 1. プロジェクトと実行のしくみ

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| Unity 6 / 2Dプロジェクト | Unityはゲームを作る開発環境。2Dは主に平面上でUIや図形を描く構成。この教材では3Dモデルではなく、UI上に手のアイコンを表示する。 |
| シーン（Scene） | ゲームに読み込まれる画面・オブジェクトの単位。[Main.unity](../Assets/Scenes/Main.unity) は意図的にほぼ空で、実行時に画面を組み立てる。 |
| GameObject / Component | GameObjectが入れ物で、Componentが振る舞い。`new GameObject("JankenGame").AddComponent<JankenGame>()` は入れ物を作り、ゲーム進行の機能を付ける。 |
| `MonoBehaviour` | Unityから`Awake`やコルーチンなどを使えるようにする基本クラス。`JankenGame`はこれを継承する。 |
| アトリビュート | `[ ... ]`で宣言に付ける追加情報。処理そのものではなく、Unityへ「このメソッドをいつ呼ぶか」などを伝える。 |
| ライフサイクル | オブジェクトの生成・初期化・破棄の流れ。いつ`Awake`が呼ばれ、どのシーンにオブジェクトが属するかを意識する。 |

### 起動時に作ることと、シーン変更後も残すことは別

[JankenGame.cs の `Bootstrap`](../Assets/Scripts/JankenGame.cs) には次の指定があります。

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void Bootstrap()
{
    if (FindAnyObjectByType<JankenGame>() == null)
        new GameObject("JankenGame").AddComponent<JankenGame>();
}
```

`AfterSceneLoad`は「シーン読み込み後に`Bootstrap`を実行する」という**起動タイミング**の指定です。`FindAnyObjectByType`で既存の`JankenGame`を探し、重複生成を避けています。これだけでオブジェクトがシーン変更後も残るわけではありません。

一方、同じファイルの`BuildEventSystem`にある`DontDestroyOnLoad(go)`は、生成した **EventSystemをシーン読み込み時に破棄しない**指定です。`EventSystem`はUIボタンへのマウス入力などを処理し、`StandaloneInputModule`が入力を取り込みます。既存のEventSystemがあれば新しく作りません。なお、`DontDestroyOnLoad`を呼んでいるのはEventSystemだけで、ここで作る`JankenGame`やCanvas全体ではありません。

## 2. 画面と操作

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| Canvas | Unity UIの描画面。オープニング、選択、勝負の画面をこの上に作る。 |
| `RectTransform` / Anchor | UI要素の位置・大きさと、親に対する配置基準。画面サイズが変わっても相対的な場所を保つ。 |
| `CanvasScaler` | 基準解像度に対するUIの拡大縮小を扱う。ここでは1280×720を基準とする。 |
| `GraphicRaycaster` | Canvas上のUIに対してポインターが当たる対象を調べる。クリック可能なボタンに必要。 |
| Button / `onClick` | 押せるUIと、押されたときに呼ぶ処理。難易度選択、手の選択、再挑戦に使う。 |
| 状態遷移 | ゲームの状態が「オープニング→選択→勝負→結果」へ変わること。画面切替と入力可否を同じ状態に合わせる。 |
| 入力のガード | `choiceActive`や`busy`のような真偽値で、時間外の入力・演出中の二重操作を防ぐ。 |
| コード生成UI / Prefab | この教材はUIをC#から組み立てる。PrefabはUnityエディター上で再利用できるオブジェクトのひな型で、別の設計方法。 |

手の絵は外部画像ではありません。[JankenIconGraphic.cs](../Assets/Scripts/JankenIconGraphic.cs) がUnityの`Graphic`を継承し、`OnPopulateMesh`で頂点と三角形を追加します。`SetVerticesDirty()`は手の値が変わったときに描画し直す合図です。小さなアイコンでも「形を頂点データとして表現する」2D描画の基本を学べます。

## 3. 時間、CPU、履歴、勝敗

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| コルーチン / `IEnumerator` | 複数フレームにまたがる処理を書く方法。カウントダウン、画面遷移、履歴再生、判定演出を順番に進める。`yield return null`は次のフレームまで待つ。 |
| `Time.unscaledTime` | ゲームの時間倍率に左右されない経過時刻。CPUの拍と入力締切を同じ開始時刻から計算する。 |
| `WaitForSecondsRealtime` | 実時間で一定時間待つコルーチン命令。演出の待機に使う。 |
| 締切と境界条件 | `経過時間 < 締切`なら入力可能、締切以後は無効、と明確に分ける。ボタンの`interactable`も締切後に切る。 |
| 乱数 | CPUがグー・チョキ・パーを選ぶ際、`Random.Range(0, 3)`で0、1、2のいずれかを得る。乱数は先読みできない動きの教材になる。 |
| `List<T>` / 履歴 | 可変長の配列。プレイヤーの全タップとCPUの3回の選択を、それぞれ押した順に保持する。 |
| `enum` | 取り得る値を名前で列挙する型。`Hand.Rock`、`Hand.Scissors`、`Hand.Paper`なら数字だけより読みやすい。 |
| nullable（`Hand?`） | 手が決まっていない状態も表せる型。未選択は無理にランダムな手へ置き換えず、敗北として扱う。 |
| 剰余演算（`%`） | 割った余り。3種類の手を循環させる勝敗判定に使う。 |

CPUは選択開始から約1秒、2.5秒、4秒の「ジャン・ケン・ポン」で手を決めます。最後の発音後もプレイヤーは入力でき、その猶予を難易度で0.5秒／1.0秒／1.5秒に設定します。したがって入力終了は開始から4.5秒／5.0秒／5.5秒です。詳細は[JankenGame.cs の選択処理](../Assets/Scripts/JankenGame.cs)を参照してください。

勝敗は同じ手なら引き分け、それ以外は `((int)player + 1) % 3 == (int)cpu` ならプレイヤー勝利です。これは列挙順を「グー→チョキ→パー」に固定した場合の式です。順番を変えるなら判定式も見直す必要があります。

履歴演出では**最終選択を再生しません**。プレイヤーのそれ以前の履歴は全件を一定時間に均等配置します。CPUの最終選択以前の2件は、プレイヤー側の最初と最後の拍で表示・発音します。最終手は判定演出後に初めて公開するため、結果を先に見せない設計です。コルーチン、リストの末尾、同期点の扱いがこの修正の前提知識になります。

## 4. 音をコードで作る

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| PCM / サンプルレート | 音の波形を時刻ごとの数値として並べる方式。Unity版は1秒当たり44,100サンプルを`AudioClip`にします。 |
| 周波数 / ピッチ | 1秒間の振動回数（Hz）と、聞こえる高さ。グーは低いド（C3）、チョキはソ（G3）、パーは高いド（C4）を基準にする。 |
| 矩形波 / ノコギリ波 / サイン波 | 基本的な波形。矩形波はブザー的、ノコギリ波はざらついた、サイン波は丸い音色になる。 |
| エンベロープ | 音量の時間変化。アタックで立ち上げ、減衰・持続・リリースで終わらせ、クリックノイズや不自然な途切れを抑える。 |
| ピッチ変化 | 音高を時間とともに動かすこと。チョキはソから一度下がり、また上がる輪郭を持つ。 |
| ノイズ / 子音 | 不規則な波形を短く足して、破裂音や摩擦音らしい立ち上がりを作る。これは人声そのものではなく擬音表現。 |
| モーラ / 濁音・拗音・半濁音 | 日本語の拍と音の種類。「グー」「チョキ」「パー」の長さや立ち上がりの違いを、合成音で近似する着想に使った。 |
| ポリフォニー / 同時再生 | 音が重なって鳴ること。連打やCPUとの同時発音で前の音を切らないよう、Unity版は10個の`AudioSource`、WebGL版は10個の`AudioContext`を用意する。 |

[SynthSound.cs](../Assets/Scripts/SynthSound.cs) はUnity用のPCM波形を生成します。[JankenWebAudio.jslib](../Assets/Plugins/WebGL/JankenWebAudio.jslib) はブラウザー用のWeb Audio APIで同じ意図の音を作ります。両者は実装方式が異なるので、音色変更時には**両方**を更新・試聴する必要があります。

ブラウザーには、ページを開いただけでは音声再生を許可しない場合があります。このためWebGL版は最初の`pointerdown`などのユーザー操作で`AudioContext`を再開します。`AudioContext`はWeb Audioの処理環境で、`OscillatorNode`が周期波、`GainNode`が音量、ノイズ用バッファが非周期音を担当します。複数コンテキストを用意する意図は連打時の独立した再生ですが、実際の同時発音数はブラウザーや端末の制約も受けます。

## 5. ビルド、公開、バージョン管理

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| ビルド | Unityのプロジェクトを遊べる形式に変換すること。[WebGLBuild.cs](../Assets/Editor/WebGLBuild.cs) が出力条件を指定し、`Builds/WebGL`へ書き出す。 |
| WebGL / WebAssembly | ブラウザー上でゲームを動かす配布形式。生成されたHTML・JavaScript・`.wasm`・データファイルをまとめて公開する。 |
| HTTPサーバー | WebGLはローカルファイルを直接開くのではなく、HTTP経由で試す。例: `python3 -m http.server 8000 --directory Builds/WebGL`。 |
| Git / リポジトリ | 変更履歴を管理する仕組みとその保存場所。GitHubはリモートのリポジトリを置くサービス。 |
| コミット / ブランチ / タグ | コミットは変更の記録、ブランチは開発の系列、タグは特定のコミットに付ける版名。例: `main`と`v1.6.0`。 |
| GitHub Release | タグに説明文を付けて利用者へ公開する版の案内。ソースの履歴上の「リリース」を示す。 |
| GitHub Pages | 静的ファイルをWeb配信する機能。このプロジェクトのゲームは`gh-pages`ブランチから配信し、最新版をルート、旧版を`versions/vX.Y.Z/`に保存する。 |
| CI / GitHub Actions | pushなどをきっかけに自動でビルドや検証を行う仕組み。[ワークフロー](../.github/workflows/webgl-pages.yml)にはUnityビルド手順があるが、実行にはUnityライセンスのSecrets設定が必要。現在の公開済みWebGL版は`gh-pages`ブランチに置かれている。 |
| 再現可能なビルド | バージョン、対象シーン、圧縮方式などをコードで指定し、同じ条件で作り直しやすくする考え方。 |

**ソースをGitHubへpushすること、Releaseを作ること、Pagesで遊べるようにすることは別の操作です。** リリース確認では、(1)タグとRelease、(2)Pagesのビルド状態、(3)配信HTMLのバージョン、(4)旧版URLが残ること、をそれぞれ点検します。[README](../README.md)と[CHANGELOG](../CHANGELOG.md)は利用方法と変更内容を伝える資料です。

## 6. ライセンスと素材

| 用語 | 意味と、このゲームでの使い方 |
| --- | --- |
| MIT License | プロジェクト独自のコード・文書・コード生成アイコンの利用条件。再利用時は著作権表示とライセンス文を残す。全文は[LICENSE](../LICENSE)。 |
| SIL Open Font License 1.1（OFL） | 同梱するNoto Sans CJK JPに適用されるフォントの利用条件。独自コードのMITとは別扱いで、全文は[フォントのライセンス](../ThirdPartyNotices/NotoSansCJK-LICENSE.txt)にある。 |
| 第三者著作物 | 自分たちが作っていない素材。コード全体をMITで公開しても、同梱フォントまでMITになるわけではない。 |

## 7. 修正・検証の読み方

仕様変更では、まず「何をいつ許すか」を決めます。たとえば後出し猶予なら、CPU最終発音の時刻を固定し、`終了時刻 = CPU最終発音時刻 + 難易度の猶予`とします。次にUI文言、入力条件、履歴、音、READMEを同じ仕様へ合わせます。最後にUnityのコンパイル、WebGLビルド、ブラウザー操作、音声ログ、旧版URLを確認します。**ビルド成功だけでは、画面の読みやすさや音の聞こえ方までは保証できません。**

過去の版には「フェイクの手」「シャッフル選択」「未選択時の自動ランダム手」がありましたが、現行のv1.6.0では削除または変更済みです。[CHANGELOG](../CHANGELOG.md)と[旧版プレイ一覧](https://kobashi.github.io/Janken/versions/)を比較すると、仕様変更とバージョン管理の関係を追えます。

### 学習用チェック問題

1. `RuntimeInitializeOnLoadMethod`と`DontDestroyOnLoad`は、何を決める点が違いますか。
2. 「ポン」の時刻を4秒に固定したまま猶予を1.5秒へ変えたら、入力締切は何秒ですか。
3. プレイヤーが4回押した場合、履歴演出で表示するのは何件ですか。最後の1件はいつ表示しますか。
4. WebGLだけ手の音を変更したら、Unityエディターで鳴る音も変わりますか。
5. `main`へソースをpushしただけで、既存の`gh-pages`公開物とGitHub Releaseが必ず更新されますか。
