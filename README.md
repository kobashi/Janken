# じゃんけん！ — Unity 6 ゲームプログラミング教材

日本式の「最初はグー、じゃんけんぽん！」を、マウス操作で遊ぶ2Dゲームです。画像・音声素材をダウンロードせず、アイコン描画と効果音合成をすべてC#で行います。

## 遊び方

1. 「はじめる」をクリックします。
2. 4秒間、6枚の手を何度でもクリックします。同じ手が2枚あり、片方はフェイクです。
3. 本物を押すと現在の手が更新され、押すたびにカードとフェイクの配置が変わります。
4. 掛け声、手札登場、「判定中」の演出を経て勝敗が表示されます。
5. 「もう一回」で続けます。勝・敗・引き分け数は画面上部に残ります。

本物とフェイクは同じクリック音と押下アニメーションを使うため、音だけで見分けることはできません。制限時間内に本物を一度も押せなかった場合は、手がランダムに自動選択されます。

日本式のルールは次の通りです。

- グーはチョキに勝つ
- チョキはパーに勝つ
- パーはグーに勝つ
- 同じ手なら「あいこ」

## Unityで開く

必要環境はUnity 6（プロジェクト作成時は `6000.5.3f1`）とWebGL Build Supportです。

1. Unity Hubで「Add project from disk」を選び、このフォルダを指定します。
2. `Assets/Scenes/Main.unity` を開き、Playを押します。
3. Gameビューは16:9（1280 × 720推奨）で確認します。

シーンは意図的に空です。`RuntimeInitializeOnLoadMethod` が `JankenGame` を生成し、UIをコードから組み立てます。Hierarchyを手作業する方法と比較しやすい教材構成です。

## WebGLビルド

Unityメニューから **Janken > Build WebGL** を実行すると、`Builds/WebGL` に出力されます。ローカルで確認する場合は、ファイルを直接開かずHTTPサーバーを使います。

```bash
python3 -m http.server 8000 --directory Builds/WebGL
```

その後 `http://localhost:8000` を開きます。GitHub Pagesで特別なレスポンスヘッダーが不要になるよう、WebGL圧縮は無効にしています。

## GitHub Pagesで公開

`.github/workflows/webgl-pages.yml` が `main` ブランチへのpush時にWebGLをビルドしてPagesへ公開します。

1. GitHubで空のリポジトリを作り、このプロジェクトをpushします。
2. Unity Personal LicenseをGameCIの手順でアクティベートし、Repository secretsに `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD` を登録します。
3. GitHubの **Settings > Pages > Build and deployment > Source** を **GitHub Actions** にします。
4. Actionsの「Build and deploy WebGL」が成功すると公開URLが表示されます。

## 教材として読む順番

| ファイル | 学べること |
| --- | --- |
| `Assets/Scripts/JankenGame.cs` | 状態遷移、制限時間、連打とフェイク、コルーチン、乱数、勝敗判定、UI生成 |
| `Assets/Scripts/JankenIconGraphic.cs` | `Graphic` の継承、頂点と三角形による2D描画 |
| `Assets/Scripts/SynthSound.cs` | PCM、周波数、エンベロープ、ノイズ、情報を漏らさない共通効果音 |
| `Assets/Plugins/WebGL/JankenWebAudio.jslib` | Web Audio API、自動再生制限、ブラウザー向け音声合成 |
| `Assets/Editor/WebGLBuild.cs` | Editor拡張、再現可能なWebGLビルド |

勝敗判定の中心は次の考え方です。手を `グー=0`、`チョキ=1`、`パー=2` と並べると、「自分の次の番号が相手」なら勝ちになります。

```csharp
if (player == cpu) return 0;
return ((int)player + 1) % 3 == (int)cpu ? 1 : -1;
```

## 発展課題

- 3本先取モードを追加する
- CPUの手を完全な乱数ではなく、プレイヤーの履歴から決める
- ミュートボタンと音量スライダーを追加する
- UIをPrefab化し、コード生成版と比較する
- スマートフォン向けに縦画面レイアウトを作る

## ライセンス

このプロジェクトのオリジナルのソースコード、ドキュメント、コード生成アイコンは [MIT License](LICENSE) です。著作権表示とライセンス文を残せば、授業、改造、再配布、商用利用に使えます。

日本語表示に同梱している Noto Sans CJK JP は第三者著作物であり、MIT Licenseの対象外です。このフォントには SIL Open Font License 1.1 が適用され、ライセンス全文は [`ThirdPartyNotices/NotoSansCJK-LICENSE.txt`](ThirdPartyNotices/NotoSansCJK-LICENSE.txt) にあります。
