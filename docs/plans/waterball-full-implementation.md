# Waterball Bot 完整實作計畫

> 本計畫把 [todo-and-open-questions.md](../todo-and-open-questions.md) 盤點的缺口,
> 依「先鎖地基、再疊功能、最後重構」的順序,拆成六個可獨立驗證的批次。
> 每批 TDD-first(先寫失敗測試),每批結束 `dotnet test` 必須全綠才進下一批。
>
> **決策已全數對齊**(見文末〈決策記錄〉)。本計畫據此展開,不再重開設計問題。
> **使用者已提供**:知識王題庫(3 題選擇題)、Record Replay 確切格式(換行分隔 + @錄音者)—— 均已實作。

---

## 測試指令(每批驗證用)

```bash
cd d:/titan/Waterball/FSM
dotnet test --nologo -v q      # 全綠才進下一批
dotnet build --nologo          # 型別重構批另外確認編譯
```

基線:目前 **15 綠**(2026-07 盤點)。

---

## 依賴關係總圖

```
批一  id:int→string 純重構 + User 物件        ← 地基,影響面最廣
  │
批二  I/O 正式格式(分派器+小parser / 三View / Program寫死)
  │
批三  事件模型 + timeout(elapsed→seconds)+ 計分結構
  │        ├──────────────┐
批四  知識王完整流程 (A)   批五  錄音完整流程 (B)   ← 建在批一~三之上,彼此獨立
  │        └──────────────┘
批六  Guard/Action 具名 class 重構 (H)         ← 純可讀性,排最後
```

**關鍵風險**(文件 F9):若先做 A/B/C 再回頭改 I/O 格式,payload(id 型別、事件名)會被推翻重來。
→ 故 **批一(id→string)最先**,地基穩了才疊功能。

---

## 批一:id `int → string` 純重構 + 引進 `User` 物件

**目標**:純結構重構,不加任何新功能,測試維持全綠(僅需調整測試中的 id 字面值)。

### 為什麼先做
正式輸入規格的 id 是 `string`(且 `"bot"` 是合法 tag)。int 型別滲透到
`ChatMessage` / `BotContext` / guards / `EventParser` / `adminUserIds`。先換乾淨,
後面新功能直接用 string 寫,不必寫完再改。

### User 物件(放 **Bot 專案** 的 `Domain/` 資料夾,命名空間 `Bot`)
```csharp
// src/Bot/Domain/User.cs
namespace Bot;

public sealed class User
{
    public string Id { get; }
    public bool IsAdmin { get; }
    public int Score { get; set; }      // 知識王計分(批三/四用)
    public bool IsOnline { get; set; }  // 預留;OnlineCount 仍用 int 計數器
    public User(string id, bool isAdmin) { Id = id; IsAdmin = isAdmin; }
}
```
- `User` 放在 **`Bot` 專案的 `Domain/` 子資料夾**(命名空間 `Bot`,**不另開 csproj**):
  `User` 被 `IBotContext`(Bot 層)與 Application 層共用,依 csproj 依賴方向(Application → Bot),共用型別放在兩者共同下層 `Bot` 最自然。
  用 `Domain/` 資料夾標示它是「純領域模型」,但編譯上仍屬 `Bot.dll`。
- 與 `TokenQuota` 同屬 Bot 層但語意不同:`TokenQuota` 是 bot 執行期配額狀態;`User` 是領域實體,故獨立資料夾。
- 未來若 Domain 長大(多實體 + 領域規則),再抽成獨立 `Domain.csproj` 插在 `Fsm.*` 與 `Bot` 之間。

### 改動清單
| 檔案 | 改動 |
|------|------|
| `src/Bot/ChatMessage.cs` | `AuthorId` int → **string** |
| `src/Bot/Domain/User.cs` | **新增**(如上,命名空間 `Bot`) |
| `src/Bot/IBotContext.cs` | `bool IsCurrentUserAdmin` → **`User? CurrentUser`**;`Users` 表(見下) |
| `src/Bot/BotGuards.cs` | `IsAdmin` 讀 `ctx.CurrentUser?.IsAdmin == true` |
| `src/Application/BotContext.cs` | 加 `Dictionary<string, User> Users`;`CurrentUser` 屬性;`IsCurrentUserAdmin` 欄位改成 `CurrentUser` |
| `src/Application/EventParser.cs` | `authorId` int → string;`ctx.CurrentUser = ...` 取代 `ctx.IsCurrentUserAdmin = ...` |
| `tests/**` | 測試中 `Admin = 1` → `"1"` 等字面值調整;`ctx.IsCurrentUserAdmin = true` → 設 `CurrentUser` |

> 註:批一暫時保留 `adminUserIds` 集合(改成 `IReadOnlySet<string>`);批二 `login` 事件帶 `isAdmin` 後才移除。

### TDD 步驟
1. 先改測試中 id 字面值為 string(測試會編譯失敗 → red)。
2. 逐檔改型別直到編譯過 + 15 綠(green)。
3. 無 refactor 空間(本批本身就是重構)。

### 驗收
- `dotnet build` 過;`dotnet test` 15 綠。
- 全 codebase 無 `int authorId` / `int AuthorId` 殘留(grep 確認)。

---

## 批二:I/O 正式格式

**目標**:輸入改成正式 `[<name>] <json>` 格式;輸出改成正式規格(💬/📢/🤖 comment…);
`login` 帶 `isAdmin`、`started` 帶 `quota`。

### 輸入:分派器 + 每事件一個小 parser(**不做頻道繼承**)
```
src/Application/Parsing/
  EventParser.cs          分派器:讀 [name] 外殼、抽 json → 依 name 找對應小 parser
  NewMessageParser.cs     {authorId, content, tags[]}   → ChatMessage(含 TagsBot = tags 含 "bot")
  NewPostParser.cs        {id, authorId, title, content, tags[]}
  GoBroadcastingParser.cs {speakerId}
  SpeakParser.cs          {speakerId, content}
  StopBroadcastingParser.cs {speakerId}
  LoginParser.cs          {userId, isAdmin}  → 建/更新 ctx.Users[userId]
  LogoutParser.cs         {userId}
  ElapsedParser.cs        "[<n> <unit> elapsed]" 無 payload → 換算 seconds(見批三)
  StartedParser.cs        {time, quota}      → 設初始 quota
  EndParser.cs            無 payload
```
- JSON 用 **`System.Text.Json`** 反序列化成對應 record。
- 分類用**資料夾/命名空間**(`Parsing/`),不是繼承。分類 ≠ 繼承。
- `login` 的 `isAdmin` → 建 `User(userId, isAdmin)` 入 `ctx.Users`;**移除** `adminUserIds` 集合。
- `started` 的 `quota` → 取代 Program.cs 寫死的 `initialTokenQuota`。

#### 分派機制:註冊表式 Strategy(對擴充開放,對修改封閉)
「一 name 一 parser」本質就是 Strategy:每個小 parser 是一個 concrete strategy,`EventParser` 是依 `name` 挑 strategy 的 context。**選擇邏輯用註冊表(Dictionary),不用 `switch`** —— 未來新增事件種類只要「新增一個 class」,分派器一行都不用改。

- 共同介面:每個 parser 自己宣告負責的 `Name`,並統一產出 `Fsm.Core.Event`(FSM `Fire` 直接吃的型別)。
  ```csharp
  public interface IEventParser
  {
      string Name { get; }         // 我負責哪個 [name],例:"login"
      Event Parse(string json);    // 吃自己那包 json → 產出 Event(name + 強型別 payload record)
  }
  ```
- **統一回傳型別 = `Fsm.Core.Event`**:FSM `Fire(Event)` 本就吃 `Event(name, payload)`,故 parser 直接產出它,
  不再多一層 `IDomainEvent` 包裝(payload 的強型別 record —— `ChatMessage`、`NewPost`、`SpeakInfo`… —— 本身就是「領域事件」,放進 `Event.Payload`)。
  下游(guard / action)再用 `payload is ChatMessage m` 之類依具體型別分流。
  - `ElapsedParser` 是特例:name 是 `elapsed`,但秒數在 `[<n> <unit> elapsed]` 外殼裡(無 json),
    由分派器解析外殼、把換算後的 seconds 帶入(見批三)。
- 分派器 `EventParser`:啟動時把所有 `IEventParser` 收進 `Dictionary<string, IEventParser>`,依 name 查表分派。
  ```csharp
  public sealed class EventParser(IEnumerable<IEventParser> parsers)
  {
      private readonly Dictionary<string, IEventParser> _parsers =
          parsers.ToDictionary(p => p.Name);

      public Event Parse(string name, string json) =>
          _parsers.TryGetValue(name, out var p)
              ? p.Parse(json)
              : throw new UnknownEventException(name);   // 未知 name 明確報錯,不靜默吞掉
  }
  ```
- **新增一個事件種類的成本**:新增一個實作 `IEventParser` 的 class(宣告 `Name` + `Parse`)→ 加進 parser 清單即生效,**`EventParser` / 既有 parser 皆不動**。

### 輸出:`IMessenger` 拆三個格式器(頻道差異在輸出端是真的)
```
src/Application/Output/
  ChatRoomView.cs    💬 <id>: ... / 🤖: ...
  ForumView.cs       <id>:【..】 / 🤖 comment in post <id>: ...
  BroadcastView.cs   📢 <id> is broadcasting / 🤖 speaking: / 🤖 stop broadcasting
```
- tags 格式:每 id 前標 `@`,以「逗號+空白」分隔(`@3, @4, @bot`)。
- `ConsoleMessenger` 改成組合三個 View(或三 View 各自實作對應 IMessenger 方法)。
- `SpyMessenger` 對應更新(測試斷言字串隨正式格式調整)。

### Program.cs
- 範例事件列表**寫死**在 Program.cs 跑(demo),不從 stdin 讀。

### 輸入回顯(`InputEcho`,批二後追加)
- 把 incoming 成員事件 echo 成正式格式(在 `fsm.Fire` **之前**印,先顯示成員動作、機器人回應接在後):
  - `💬 <id>: <content> <tags>`(new message)/ `<id>:【<title>】<content> <tags>`(new post)
  - `📢 <id> is broadcasting...` / `📢 <id>: <content>` / `📢 <id> stop broadcasting`
  - `🕑 <n> <unit> elapsed...`(用**原始外殼**,非換算後 seconds)
  - login/logout/started/end 不回顯。
- 為此 `ChatMessage` 加 `Tags`(原始 tag 清單,`NewMessageParser` 帶入),回顯 `@1, @2` 用;`TagsBot` 仍為衍生旗標。

### TDD 步驟(每個小 parser 一組測試)
1. 每個 parser:實作 `IEventParser`;給 json → 斷言 `Parse` 產出正確 `Event`(name + payload record 型別/欄位)(red→green)。
2. 每個 parser 的 `Name` → 斷言等於對應的 `[name]`(如 `LoginParser.Name == "login"`)。
3. 每個 View:給 content+tags → 斷言輸出字串符合正式規格。
4. 分派器(註冊表):
   - 給 `[login]{...}` → 斷言分派到 `LoginParser`(產出 `LoginEvent`)。
   - 給未知 name → 斷言丟 `UnknownEventException`。
   - 塞一組假 `IEventParser` 進建構子 → 斷言依 `Name` 建表且能正確查表分派(驗證「加新 parser 免改分派器」)。
5. 既有 15 測試隨新輸入/輸出格式調整。

### 驗收
- 正式格式範例(文件 G 區表格每一列)都有對應綠測試。
- `adminUserIds` 集合已移除;admin 身份來自 `login`。
- 所有小 parser 皆實作 `IEventParser` 並統一產出 `Fsm.Core.Event`;分派器以 `Dictionary` 查表、無 `switch`/`if-else` 硬編 name。
- 未知 name 有明確例外(`UnknownEventException`),非靜默忽略。

---

## 批三:事件模型 + timeout + 計分結構

**目標**:補齊 timeout 的 seconds 累計模型與計分欄位,為批四/五鋪路。本批不含 A/B 的狀態轉移(那是批四/五),只加**共享的 ctx 欄位與累計機制**。

### timeout 模型(選 A:單一 `elapsed` + ctx 累計秒)
- `ElapsedParser` 把 `<n> <unit>`(unit ∈ seconds/minutes/hours)換算成 **seconds**,
  發統一 `elapsed` 事件,payload 帶 `int seconds`。
- ctx 新增累計欄位(供批四 guard 讀):
  ```csharp
  int ElapsedSecondsInQuestion;  // 本題累計(答對/進新題歸零)
  int ElapsedSecondsInGame;      // 全場累計(進 KnowledgeKing 歸零)
  int ElapsedSecondsInThanks;    // ThanksForJoining 累計(進場歸零)
  ```
- **累計點**:批四會在知識王相關狀態掛累計(見批四〈累計 tick〉)。本批先加欄位 + 換算邏輯 + 測試。

> FSM 分流機制(批四用):同一 `elapsed` 事件,20s 放 `Questioning` 內層 transition、
> 1h 放 `KnowledgeKing` 外層 transition,靠 guard 讀不同累計欄位分流。
> 這是 FSM 分層委派天然做到的(內層先試、內層不吃才輪外層)。

### 計分結構(用 `User.Score`)
- 分數即 `ctx.Users[id].Score`,不另開表(批一已備妥 User)。
- ctx 新增首答旗標:`string? FirstCorrectAnswerer`(null=本題尚無人答對)。
  - 一石二鳥:非 null 即「已有人答對」旗標 + 提供 @標記對象。
- 題庫介面(維持不變 —— 選擇題也套得進來):
  ```csharp
  public interface IQuizBank
  {
      string QuestionAt(int index);   // 題目內容(發問用,含題幹 + 四個選項)
      bool IsCorrect(int index, string answer);  // 判對錯(比對選項代號)
      int Count { get; }
  }
  ```
- **題庫已由使用者提供:選擇題(題幹 + 四選項 A/B/C/D + 正解代號)**。實作為 `ChoiceQuizBank`,取代原 `StubQuizBank`。
  - 題目 model:
    ```csharp
    public record QuizQuestion(string Stem, string[] Options, char CorrectOption);
    // Options 依序對應 A、B、C、D;CorrectOption 為 'A'..'D'
    ```
  - **`QuestionAt` 把題幹 + 選項格式化成一段可發問的字串**,例:
    ```
    請問哪個 SQL 語句用於選擇所有的行?
    A) SELECT *
    B) SELECT ALL
    C) SELECT ROWS
    D) SELECT DATA
    ```
  - **`IsCorrect` 只接受「字母代號」、Trim + 忽略大小寫**(玩家回 `A` 或 `a` 都算對;回選項內容不算)。
    ```csharp
    public bool IsCorrect(int index, string answer)
    {
        var a = answer?.Trim();
        if (string.IsNullOrEmpty(a) || a.Length != 1) return false;
        return char.ToUpperInvariant(a[0]) == _questions[index].CorrectOption;
    }
    ```
  - 題庫資料(內建 3 題,正解:第0題 A、第1題 C、第2題 A):

    | index | 題幹 | A | B | C | D | 正解 |
    |-------|------|---|---|---|---|------|
    | 0 | 請問哪個 SQL 語句用於選擇所有的行? | SELECT * | SELECT ALL | SELECT ROWS | SELECT DATA | **A** |
    | 1 | 請問哪個 CSS 屬性可用於設置文字的顏色? | text-align | font-size | color | padding | **C** |
    | 2 | 請問在計算機科學中,「XML」代表什麼? | Extensible Markup Language | Extensible Modeling Language | Extended Markup Language | Extended Modeling Language | **A** |

    > 之後要加題只需往資料陣列補 `QuizQuestion`,`Count` 自動反映、流程不動。

### TDD 步驟
1. `ElapsedParser`:`[20 seconds elapsed]`→seconds=20;`[1 hours elapsed]`→3600(red→green)。
2. `IQuizBank` + `ChoiceQuizBank`:
   - `Count == 3`。
   - `IsCorrect(0, "A")` / `IsCorrect(0, "a")` → true;`IsCorrect(0, "B")` → false。
   - `IsCorrect(1, "C")` → true;`IsCorrect(2, "A")` → true。
   - 邊界:`IsCorrect(0, " a ")` → true(Trim);`IsCorrect(0, "SELECT *")` → false(只收代號);`IsCorrect(0, "")` / null → false。
   - `QuestionAt(0)` 含題幹與四個選項標記(斷言字串包含 `A)`、`B)`、`C)`、`D)` 與題幹)。
3. ctx 新欄位的預設值/歸零行為單元測試。

### 驗收
- unit 換算三種(seconds/minutes/hours)都綠。
- ctx 具備 `ElapsedSecondsIn*` / `FirstCorrectAnswerer` / `Users[].Score` 且有預設值測試。
- `ChoiceQuizBank` 實作 `IQuizBank`,內建 3 題(正解 A/C/A);`IsCorrect` 只收代號、Trim、忽略大小寫;`QuestionAt` 含題幹 + 四選項。

---

## 批四:知識王完整答題流程 (A)

**目標**:把佔位版知識王換成真的答題流程(計分、首答、timeout、結果公布)。

### 狀態機(已定案)
```
KnowledgeKing (composite, 初始 → Questioning)
│ [外層 transition]
│ ├─ king-stop (admin) ───────────────────────► Normal
│ └─ elapsed && ElapsedSecondsInGame >= 3600 ──► ThanksForJoining   (1h 強制中斷)
│
├─ Questioning
│   onEnter: 發題目 QuizBank.QuestionAt(index)
│            + 提示玩家回代號:「請 @bot 並回覆選項代號(A/B/C/D)作答」
│            + ElapsedSecondsInQuestion = 0
│            + FirstCorrectAnswerer = null
│   │
│   ├─ new message && TagsBot && IsCorrect(index, content) && FirstCorrectAnswerer == null
│   │    action: FirstCorrectAnswerer=authorId; Users[authorId].Score++;
│   │            SendChat("Congrats! you got the answer!", @[authorId])
│   │    ├─ index < Total-1 → Questioning (index++)
│   │    └─ index == Total-1 → ThanksForJoining
│   │
│   ├─ elapsed && ElapsedSecondsInQuestion >= 20
│   │    ├─ index < Total-1 → Questioning (index++)      (20s 到,沒答對也跨題)
│   │    └─ index == Total-1 → ThanksForJoining
│   │
│   └─ (答錯 / 未 TagsBot / 已有人答對 → guard 全不過 → 靜默不回應)
│
└─ ThanksForJoining
    onEnter: ElapsedSecondsInThanks = 0
             + 公布結果:
               SomeoneIsBroadcasting ? Messenger.SendChat(result)
                                     : Messenger.Speak(result)   // 無人廣播→語音
             result = 依 Users[].Score:
               最高分唯一 → "The winner is <id>"
               多人同分 / 全 0 分 → "Tie!"
    ├─ play again ─────────────────────────────► Questioning  (分數+index 歸零)
    └─ elapsed && ElapsedSecondsInThanks >= 20 ─► Normal       (結束回 Normal)
```

### 累計 tick(批三欄位在此掛上)—— 實作用 `onHandle`,非 self-loop
- **實作決定:累計放狀態的 `onHandle`(不是 self-loop transition)。**
  原因:self-loop `Questioning --elapsed--> Questioning` 會 **re-enter**,觸發 `OnEnterQuestioning` 把 `ElapsedSecondsInQuestion` 歸零 → 累計永遠被沖掉。
  `onHandle` 在 `Fire` 的 transition 查表**之前**跑、且不 exit/entry,所以:(1) 累計不被歸零;(2) 同一個 elapsed 累計後,20s guard 立刻看得到最新秒數(無 off-by-one)。
  - 為此 `BotBuilder.AddLeafState` 加了 `onHandle` 參數(與輪播 `onHandle` 並存時兩者依序跑)。
  - Questioning.onHandle 累加 `ElapsedSecondsInQuestion += s` 與 `ElapsedSecondsInGame += s`;ThanksForJoining.onHandle 累加 `ElapsedSecondsInThanks += s`。
- `ResetGame`(king / play again)清 `ElapsedSecondsInGame/InQuestion/InThanks` + 分數 + index,避免上一場殘留誤觸下一場 timeout。

### 內層 / 外層 transition 的目標狀態限制(實作發現)
- **一條 transition 的 `stateTo` 只能是同一層 FSM 認得的狀態**(內層 FSM 不認得外層 `Normal`,外層不認得內層 `ThanksForJoining`)。故:
  - **1h → ThanksForJoining**:目標是內層狀態 → 放**內層**(`Questioning --elapsed--> ThanksForJoining`,guard `ElapsedSecondsInGame>=3600`,宣告在 20s 之前 → 優先)。
  - **king-stop / Thanks-20s → Normal**:目標是外層狀態 → 放**外層**(`KnowledgeKing --...--> Normal`)。Thanks-20s 靠 `ElapsedSecondsInThanks>=20` 當「人在 Thanks」的代理條件;內層 Thanks 無對應 elapsed transition → 冒泡到外層被接住。

### 「答對出下一題」與「20s 出下一題」共用目標
兩條 transition 目標狀態/後續完全相同(index++ 進下一題 or 進 ThanksForJoining),
差別只在「有無加分 + 有無 Congrats」。照 [WaterballBot.cs:78-85](../../src/Application/WaterballBot.cs#L78-L85)
既有「兩條同事件、guard 互補(`<Total-1` vs `>=Total-1`)」手法展開。

### TDD 步驟
1. 答對計分:首答者 +1、發 Congrats @首答者、進下一題(red→green)。
2. 同題第二人答對:不計分、靜默(FirstCorrectAnswerer 已非 null)。
3. 答錯 / 未 tagBot:靜默、無輸出。
4. 20s timeout 無人答對:跨到下一題。
5. 最後一題答對/20s:進 ThanksForJoining。
6. 1h timeout:任意題強制進 ThanksForJoining。
7. 結果公布:無人廣播→Speak、有人廣播→SendChat;唯一最高→winner、同分/全0→Tie。
8. play again:分數+index 歸零重開。
9. 結束 20s:回 Normal。

### 驗收
- 上述 9 組情境全綠。題庫用 `ChoiceQuizBank`(3 題選擇題,正解 A/C/A)。

---

## 批五:錄音完整流程 (B)

**目標**:把佔位版錄音換成真的累積 + Record Replay 輸出 + 講者循環。

### 狀態機(已定案)
```
Record (composite, resolver: SomeoneIsBroadcasting ? Recording : Waiting)
│ [外層] stop-recording ──────────────────────► Normal  (離開整個 Record)
│
├─ Waiting
│   onEnter: SendChat("[Record] waiting for a broadcaster...")
│   └─ go broadcasting ──► Recording (SomeoneIsBroadcasting=true)
│
└─ Recording
    onEnter: Messenger.GoBroadcasting()
    ├─ speak ──► onHandle: RecordBuffer.Add(speak.content)     ← 累積,不轉移
    │            (LeafState.Handle 回 NotConsumed → 冒泡,外層無 speak transition → 靜默結束)
    └─ stop broadcasting ──► action: 輸出 Record Replay(RecordBuffer + @錄音者 → 聊天室);
                                     RecordBuffer.Clear()
                          ──► Waiting                            ← 循環
```

### buffer(放 **ctx**)
```csharp
List<string> RecordBuffer;   // 累積每筆 speak 文字;stop broadcasting 輸出後 Clear
string? RecorderId;          // 錄音者(下 record 指令者)→ Record Replay @標記對象
```
- 放 ctx:因 `stop broadcasting` 的 action 要讀它輸出,而 action 只拿得到 `(event, ctx)`。

### speak 累積用 `onHandle`(不需 self-transition)
- [LeafState.cs:32-37](../../src/Fsm.Core/LeafState.cs#L32-L37):`Handle` 做完響應**一定回 NotConsumed**。
  → Recording 用 `onHandle` 累積 buffer,累完冒泡,外層沒有 speak transition → 靜默結束。效果正確且比 timeout 累積乾淨。

### Record Replay 格式(已由使用者確認)
- 格式:`[Record Replay] ` + 各筆 speak 以**換行(`\n`)** 分隔,結尾 ` @<錄音者>`,傳聊天室(`SendChat` + tag 錄音者)。
  ```
  🤖: [Record Replay] 大家好,我是小華!
  歡迎來到小華脫口秀 @3
  ```
- **錄音者 = 下 `record` 指令者**,整個 Record session 不變(與「誰在廣播 speakerId」無關)。範例中講者換人(4→3),Replay 仍標 `@3`。
- **觸發時機(兩處都輸出)**:
  - `stop broadcasting`(錄音中回 Waiting):輸出 Replay + 清 buffer。
  - `stop-recording`(指令,**限錄音者**,離開整個 Record 回 Normal):若在錄音中(buffer 有內容)輸出 Replay + 清;等待中則不輸出(沒錄到東西)。

### TDD 步驟
1. Recording 收 speak:buffer 累積、狀態不變、不消化冒泡。
2. 多筆 speak:依序累積。
3. stop broadcasting:輸出 Replay(含累積+@錄音者)、buffer 清空、回 Waiting。
4. 循環:回 Waiting 後再 go broadcasting → 再進 Recording、buffer 從空開始。
5. stop-recording:任意子狀態離開整個 Record 回 Normal。
6. 其他狀態收 speak:靜默(不累積、無輸出)。

### 驗收
- 上述 6 組情境全綠。Record Replay 用確認格式(換行分隔 + @錄音者);`stop broadcasting` 與 `stop-recording`(限錄音者)兩處都測 Replay 輸出。

---

## 批六:Guard / Action 具名 class 重構 (H)

**目標**:純可讀性重構,**不改行為**,測試維持全綠。功能都完成後才做,故新功能直接沿用重構後寫法。

### H1 — Guard 改具名 class
```
IsAdminGuard<C>    : Test => ctx.CurrentUser?.IsAdmin == true
HasQuotaGuard<C>   : Test => ctx.TokenQuota >= amount
CommandIsGuard<C>  : Test => payload is ChatMessage m && m.TagsBot && m.Content == keyword
AlwaysTrueGuard<C> : Test => true
AndGuard<C>(params IGuard<C>[]) : foreach g → if(!g.Test) return false; return true
```
- 移除:`.And()/.Or()` extension(`GuardExtensions`)。改用 `AndGuard`。
- **保留 `PredicateGuard<C>`**:對稱 H3「一次性 `does:` 保留 `DelegateAction`」—— 一次性 `when:` lambda(知識王答對/timeout 等十幾條,application 專屬、不重用)保留用 `PredicateGuard` 包。只把**可重用**的 guard 改具名 class。
- **不做 `OrGuard`**(YAGNI;0 處用到 OR)。
- `BotGuards.CommandIs/IsAdmin/HasQuota` 回對應具名 class;`BotBuilder` 收集成 list → `new AndGuard(...)`。

### H2 — FSM 委派(`Fire`→`Current.Handle`→子 FSM `Fire`)
- **保留不改**。是委派非遞迴(每層不同 FSM 物件)。
- 僅在 [FiniteStateMachine.cs](../../src/Fsm.Core/FiniteStateMachine.cs) 的 `Current.Handle` 那行**加註解**說明「委派非遞迴,composite 往內層再跑一層」。

### H3 — Action 部分改(只改可重用的)
| 對象 | 改法 |
|------|------|
| 可重用 `DeductQuota(n)` / `SendChat(msg)` | 改具名 class(`DeductQuotaAction`/`SendChatAction`) |
| 一次性領域 `does: (_,ctx)=>ctx.OnlineCount++` 類 | **保留 `DelegateAction`**(只綁一條、不重用) |
| `TransitionAction`(收一串依序跑) | 不動 |
| `NoOpAction` | 不動 |
- 判準:**在 codebase 裡重不重複/被不被重用**,不是執行幾次。
- `BotActions.DeductQuota/SendChat` 從「回 DelegateAction 包 lambda」改「回具名 class」;`DelegateAction` 留給 WaterballBot 一次性 `does:` 用。

### TDD 步驟
- 本批不加新行為,靠既有全部測試維綠證明重構無回歸。
- 逐項重構後每步 `dotnet test` 確認綠。

### 驗收
- 全部測試綠;`PredicateGuard`/`GuardExtensions` 已移除;無 `OrGuard`。

---

## 風險與注意

| 風險 | 緩解 |
|------|------|
| 批一型別重構滲透廣,易漏 | 靠編譯器(int→string 不相容會編譯失敗)+ 15 綠兜底 |
| 題庫/Replay 格式未定 | 介面預留(`IQuizBank`/Replay stub),使用者提供後只換實作,不動流程 |
| timeout 累計點放錯 → 20s/1h 分流失準 | 批四明確定「累計 self-loop 宣告在 20s-timeout 之後」,靠宣告順序 |
| speak 冒泡誤被外層吃掉 | 確認 Record/外層無 speak transition(批五測試 6 專驗) |

## 成功標準(全計畫)
- 六批全綠,無回歸。
- 知識王:計分/首答/20s跨題/1h中斷/結果公布/play again/結束回Normal 全實作且測試涵蓋。
- 錄音:累積/Replay輸出/講者循環/stop-recording 全實作且測試涵蓋。
- I/O 為正式格式;id 為 string;admin 由 login 帶;guard/action 為具名 class。
- 真題庫、Record Replay 確切格式:使用者已提供並實作完成。

---

## 決策記錄(對齊結果,不再重開)

### G 區 + User
- id `int → string`,批一單獨做。
- JSON 用 `System.Text.Json`。
- 引進 `User { Id, IsAdmin, Score, IsOnline }`(放 **`Bot` 專案 `Domain/` 資料夾**,命名空間 `Bot`,不另開 csproj);ctx 持 `Dictionary<string,User> Users`。
- admin 身份由 `login` 事件帶,建 User 入表;移除 `adminUserIds`。
- ctx 保留 `CurrentUser`(取代 bool `IsCurrentUserAdmin`);guard 讀 `CurrentUser.IsAdmin`。
- `OnlineCount` 保留 int 計數器。
- 輸入:分派器+小parser,不做頻道繼承。輸出:拆 ChatRoomView/ForumView/BroadcastView。Program.cs 寫死事件。

### D 區
- timeout 模型選 **A**:單一 `elapsed`,parser 做 unit→seconds,payload 帶 seconds。
- 兩種 timeout 靠 FSM 分層委派分流(20s 內層、1h 外層,guard 讀不同累計欄位)。
- 20s 歸零時機:答對時歸零(進新題也歸零)。
- answer 沿用 `new message` + 判斷,不另立事件。

### C 區
- 分數放 `User.Score`。
- 首答旗標放 ctx,用 `string? FirstCorrectAnswerer`(兼旗標與 @對象)。
- 題庫來源:使用者另外提供,先用 `IQuizBank` + StubQuizBank 介面預留。

### A 區
- 20s 未答對:**跨題**(20s = 每題限時)。
- 首答旗標:記首答者 userId(`string?`)。
- 結束 20s:獨立計時器,進 ThanksForJoining 歸零。
- 公布分支:`ThanksForJoining.onEnter` 內判 `SomeoneIsBroadcasting`(無人→Speak、有人→SendChat)。

### B 區
- Record Replay:`stop broadcasting` transition 的 action(非獨立狀態)。
- buffer 放 ctx;`stop broadcasting` 輸出後清空。
- speak 只在 Recording 累積(用 onHandle);其他狀態冒泡靜默。

### H 區
- 排最後(功能完成後再重構)。
- H1 具名 guard class + AndGuard;移除 GuardExtensions(.And/.Or);**保留 PredicateGuard** 給一次性 when: lambda 用(對稱一次性 does: 用 DelegateAction);不做 OrGuard。
- H2 FSM 委派保留,僅加註解。
- H3 action 只改可重用的(DeductQuota/SendChat),一次性 `does:` 保留 DelegateAction。
