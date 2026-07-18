# 待實作 & 待討論清單

> 記錄目前實作**尚未涵蓋**的需求,以及需要對齊的設計問題。
> 用途:先把缺口盤點清楚,等其他需求補齊後一起討論、一次規劃。
> (實作現況:FSM 核心 / composite plugin / bot DSL / Waterball 基本三狀態已完成,測試 15 綠。)

---

## A. 知識王（KnowledgeKing）完整答題流程 — 目前是簡化假模型

### 現況
`KnowledgeKing` 目前只是佔位版:
- 用 `elapsed` 事件推進題號（`CurrentQuestionIndex++`），**不是真的答題**。
- `Questioning.onEnter` 只發 `Question {index}`。
- `ThanksForJoining.onEnter` 只發一句佔位 `Thanks for joining!`。
- **沒有分數、沒有答對錯判斷、沒有 timeout、沒有結果公布。**

### 需求 vs 現況差距表
| 需求 | 現況 | 缺什麼 |
|------|------|--------|
| 答題要 @bot、答對才計分 | ❌ 只有 `elapsed` 推進 | `answer` 事件 + 對錯判斷 |
| **每題第一位答對者 +1 分** | ❌ 無分數概念 | 計分資料結構 + 「本題是否已有人答對」旗標 |
| 答錯 → 靜默忽略（機器人不回應） | ⚠️ 靠 guard 精神但無答題邏輯 | 答錯 → guard 不過 → 不回應 |
| 未標記機器人的訊息不視為答題 | ⚠️ 同上 | 答題 guard 需含 tagsBot |
| 答對 → 發 `Congrats! you got the answer!`（標記獲勝者）→ 出下一題 | ❌ 無 | 答對 transition + action（含 @標記） |
| 3 題全答完 → 進 ThanksForJoining | ⚠️ 有，但靠 `elapsed` 非「答完」 | 改成「答完最後一題」觸發 |
| **1 小時未答完 → 立即中斷進 ThanksForJoining** | ❌ 無 | timeout 事件 + transition |
| 20 秒後知識王結束回 Normal | ⚠️ 只有手動 `king-stop` 指令 | timeout 事件（20s）→ 回 Normal |

### ThanksForJoining 結果公布（完全未實作）
進入 ThanksForJoining 時:
- **分兩種情況**（依 ctx 有無人廣播）:
  - 沒人廣播 → 機器人**廣播**，用**語音訊息**公布結果（`Messenger.GoBroadcasting` + `Speak`）。
  - 有人廣播 → 用**聊天訊息**公布結果（`Messenger.SendChat`）。
  - → 這是 composite `initialLeafStateResolver` 或 entry 分支的活;需確認放哪。
- **結果格式**:
  - 依分數多寡決定獲勝者，分數最多者獲勝。
  - 多人同分 → 平手。
  - 無人獲勝（都 0 分？）→ `Tie!`
  - 有獲勝者 → `The winner is <獲勝者 Id>`

---

## B. 錄音（Record）完整流程 — 目前是簡化假模型

### 現況
[DefineRecordState](../src/Application/WaterballBot.cs) 只有:
- `Waiting --go broadcasting--> Recording`（進場發一句佔位話）。
- `Record --stop-recording--> Normal`（離開整個 Record）。
- **沒有錄音累積、沒有 Record Replay 輸出、沒有講者結束回 Waiting、沒有循環。**

### 需求（未實作）
錄音中狀態（Recording）:
1. 有講者（Speaker）**傳 Speak 語音訊息**時，把**每一筆語音訊息的文字記錄下來**（累積）。
2. 該講者**結束廣播**後:
   - 把整段錄下的所有語音訊息，以 **Record Replay 格式**輸出（格式見設計/需求第五項）。
   - **標記錄音者**、傳訊到**聊天室**。
   - 回到 **Waiting** 狀態。
3. 若之後**又有新講者開始廣播** → 再次進入 Recording，如此**循環**。
4. 直到錄音者下 **`stop-recording`** 指令 → 離開整個 Record 回 Normal。

### 需要的子狀態/轉移（目前缺）
- **Recording 自我響應**:`speak` 事件 → 把該筆語音文字**累積進 buffer**（handle 或自我 transition，不離開 Recording）。
- **講者結束廣播事件**（例如 `end broadcasting` / speaker 停止）→ 觸發:
  - 輸出 Record Replay（累積內容 + 標記錄音者 → 聊天室）。
  - 清空 buffer。
  - `Recording --> Waiting`。
- 需求描述的狀態鏈:`Recording --speak--> (累積)`、`Recording --講者結束--> Record Replay 輸出 --> Waiting`。
  - 待確認:「Record Replay」是一個**獨立狀態**，還是「講者結束」transition 的 **action**（輸出後直接回 Waiting）？（傾向後者:輸出是轉移副作用，不是常駐狀態。）

### 待決策（Record）
1. **錄音 buffer 放哪**:Recording 這個 state 私有？還是 ctx？（只有 Recording 用 → 傾向 state 私有，但跨「講者結束」transition 要讀，可能得放 ctx。）
2. **「講者結束廣播」怎麼進 FSM**:一個 `end broadcasting` 事件？payload 帶錄音者 id？
3. **Record Replay 格式**:需求第五項的確切格式（`[Record Replay]` + 逐筆語音 + @錄音者）待補。
4. **speak 事件 payload**:內容 + 講者 id。EventParser 未支援。

---

## C. 計分 / Player — 目前完全沒有

### 現況
[BotContext](../src/Application/BotContext.cs) 只有 `CurrentQuestionIndex`，**無任何分數欄位**。

### 需要新增（待討論選型）
- 每個 userId → 分數的映射。
- 每題「是否已有人答對」的旗標（只有**第一位**答對者得分）。
- 正確答案是什麼（判斷答對錯）。

### 待決策
1. **資料結構**:`Dictionary<int,int>`（userId→score，最輕量）vs `Player` 物件（Id+Score，可能還有名字，較好擴充）。
2. **正確答案來源**（需求未定）:
   - 寫死一組題庫（題目+答案清單，屬 application 層）。
   - 或簡化:每題正解都是同一個固定字串，專注驗證計分/流程。

---

## D. 需要新增的事件（目前 EventParser 未支援）
- `answer`（帶 authorId、內容、tagsBot 的答題訊息）— 可能沿用 `new message` + 內容判斷，或獨立事件。
- `speak`（帶講者 id、語音文字）— Recording 累積用。
- **講者結束廣播**（例如 `end broadcasting`）— 觸發 Record Replay 輸出 + 回 Waiting。
- 知識王 **1 小時 timeout**（未答完中斷）。
- 知識王 **20 秒 timeout**（結束回 Normal）。
- → 這些 timeout 怎麼進 FSM?（目前 `elapsed` 是通用計時事件，需釐清語意:一個 elapsed 還是多種計時事件？）

---

## E. 其他既知的小缺口
- `Messenger.Speak` / `GoBroadcasting` / `StopBroadcasting` / `CommentPost` 介面已定義，但**尚未全部被 application 用到**（結果公布、Record Replay、Forum 留言等場景未接）。
- Forum（貼文）輸入管線未實作 — 目前 EventParser 只 parse chat 訊息，`ChatMessage` payload 沒有 forum post（帶 postId/tags）型別。
- 三頻道（ChatRoom / Forum / Broadcast）目前壓成 event name + parser，無獨立物件（設計文件認為可省，但 forum 輸入 payload 待補）。

---

## F. 待對齊的設計問題（等其他需求補齊後一起討論）
1. 計分用 `Dictionary` 還是 `Player` 物件？
2. 正確答案來源（題庫 vs 固定字串）？
3. timeout 事件的模型（單一 `elapsed` vs 多種具名計時事件 `elapsed-20s` / `elapsed-1h`）？
4. 結果公布的「廣播 vs 聊天」分支放 resolver 還是 entry？
5. Forum 輸入要不要補（`ForumPost` payload）？
6. Record Replay 是**獨立狀態**還是「講者結束」transition 的 **action**？（傾向 action）
7. 錄音 buffer 放 Recording state 私有還是 ctx？（跨「講者結束」transition 要讀 → 可能得放 ctx）
8. Record Replay 的確切輸出格式（需求第五項）？
9. I/O 格式（見 G 區）要現在做還是等其他需求一起？（風險:payload 之後可能為計分/Record 再動）

---

## G. 輸入 / 輸出格式 — 目前只支援簡化格式，需改成正式規格

### 現況
- [EventParser](../src/Application/EventParser.cs) 只 parse 簡化的 `msg <id> ...` / `login` / `elapsed` 等，**不是正式的 `[name] {json}` 格式**。
- payload 的 id 目前是 **int**（正式規格是 **string**，且 `"bot"` 也是合法 tag）。
- 無 `started` / `end` / `new post` / `speak` / `stop broadcasting` 事件。
- [ConsoleMessenger](../src/Application/ConsoleMessenger.cs) 的輸出格式是自訂的，**不符正式輸出規格**（💬/📢/🤖 comment 等）。

### 正式輸入格式
每行一個事件:`[<event name>] <payload JSON>`（有些事件無 payload）。

| 事件 | payload 欄位 | 範例 |
|------|-------------|------|
| `started` | `time`(YYYY-MM-DD HH:mm:ss), `quota`(int>0) | `[started] {"time":"2023-08-07 00:00:00","quota":10}` |
| `login` | `userId`(string), `isAdmin`(bool) | `[login] {"userId":"1","isAdmin":true}` |
| `logout` | `userId`(string) | `[logout] {"userId":"1"}` |
| `<n> <unit> elapsed` | 無 payload；unit ∈ {seconds, minutes, hours} | `[10 seconds elapsed]` |
| `new message` | `authorId`(string), `content`(≤1000), `tags`(string[]) | `[new message] {"authorId":"5","content":"...","tags":["1","3","bot"]}` |
| `new post` | `id`, `authorId`, `title`(≤50), `content`(≤1000), `tags`(string[]) | `[new post] {"id":"1","authorId":"8","title":"...","content":"...","tags":["1","2","3"]}` |
| `go broadcasting` | `speakerId`(string) | `[go broadcasting] {"speakerId":"4"}` |
| `speak` | `speakerId`(string), `content`(string) | `[speak] {"speakerId":"4","content":"大家早安"}` |
| `stop broadcasting` | `speakerId`(string) | `[stop broadcasting] {"speakerId":"4"}` |
| `end` | 無 payload，程式終止 | `[end]` |

### 正式輸出格式（只有這些事件會被記載）
| 情境 | 格式 | 範例 |
|------|------|------|
| 時間流逝 | `🕑 <n> <unit> elapsed...` | `🕑 10 seconds elapsed...` |
| 聊天·成員 | `💬 <id>: <content> <tags>` | `💬 3: 哈哈 @1, @2, @4` |
| 聊天·機器人 | `🤖: <content> <tags>` | `🤖: thank you @3, @4` |
| 論壇·成員發文 | `<id>: 【<title>】<content> <tags>` | `4: 【標題】內文 @1, @2` |
| 論壇·機器人留言 | `🤖 comment in post <post id>: <content> <tags>` | `🤖 comment in post 1: Nice post @2` |
| 廣播·成員開始 | `📢 <id> is broadcasting...` | `📢 4 is broadcasting...` |
| 廣播·機器人開始 | `🤖 go broadcasting...` | — |
| 廣播·成員語音 | `📢 <id>: <speak message>` | `📢 3: 這個世界上有 10 種人` |
| 廣播·機器人語音 | `🤖 speaking: <speak message>` | `🤖 speaking: The winner is 2` |
| 廣播·成員停止 | `📢 <id> stop broadcasting` | `📢 4 stop broadcasting` |
| 廣播·機器人停止 | `🤖 stop broadcasting...` | — |

- **tags 格式**:每個 id 前標 `@`，id 間以「逗號 + 一個空白」分隔（`@3, @4, @bot`）。

### 結構決策（已對齊）
**輸入**——一個「分派器 + 每事件一個小 parser」，**不做頻道繼承**:
```
EventParser（分派器）: 讀 [name] 外殼、抽 json → 依 name 找對應小 parser
  ├─ NewMessageParser / NewPostParser
  ├─ GoBroadcastingParser / SpeakParser / StopBroadcastingParser
  ├─ LoginParser / LogoutParser / ElapsedParser
  └─ StartedParser / EndParser
```
- 理由:事件是 `[name]{json}` 統一格式、依 name 分派;`login`/`elapsed`/`started` 無頻道，硬塞頻道父類會職責重疊、違反 is-a。
- 想按頻道分組 → 用**資料夾/命名空間**（`Parsing/ChatRoom/`…），不是繼承。**分類 ≠ 繼承。**

**輸出**——把 `IMessenger` 拆成三個格式器（頻道差異在輸出端是真的）:
- `ChatRoomView`：`💬` / `🤖:`
- `ForumView`：`<id>:【..】` / `🤖 comment in post`
- `BroadcastView`：`📢 ..broadcasting` / `🤖 speaking:` / `🤖 stop broadcasting`

**Program.cs**——把範例事件列表**寫死**在 Program.cs 裡跑（demo 用），不從 stdin 讀。

### 連帶要改的既有東西
- payload id：`int` → **string**（`ChatMessage`、`BotContext`、guards、EventParser、`adminUserIds` 全受影響）。
- `isAdmin` 改由 `login` 事件的 payload 帶（目前靠 `adminUserIds` 集合硬編）。
- `started` 事件帶初始 quota → 取代 Program.cs 寫死的 `initialTokenQuota: 100`。
- `elapsed` 事件要解析 `<n> <unit>` → 對應知識王 20s / 1h timeout（見 D 區 timeout 模型問題）。

---

## H. 重構決策（可讀性，非新功能）

### H1. Guard 改「具名 class」寫法（已決定，待實作）
**動機**:目前 guard 用 `PredicateGuard`（通用殼）+ `.And()`/`.Or()` extension，判斷邏輯藏在別處建立的 lambda 裡（`Test` 只寫 `_predicate(...)`），跳過去看不到判斷內容，不直覺。

**改法**——每種判斷一個具名 class，`Test` 裡就寫明判斷:
```
// 單一判斷（Test 裡就是白話判斷,不再靠外部 lambda）
IsAdminGuard<C>    : Test => ctx.IsCurrentUserAdmin
HasQuotaGuard<C>   : Test => ctx.TokenQuota >= amount
CommandIsGuard<C>  : Test => payload is ChatMessage m && m.TagsBot && m.Content == keyword
AlwaysTrueGuard<C> : Test => true          // 保留（無條件 transition 的預設）

// 組合:收「一串」而非二元 left/right,Test 用 foreach「全過才 true」
AndGuard<C>(params IGuard<C>[] guards)
    Test: foreach g → if (!g.Test) return false; return true;
```
呼叫端:`new AndGuard<C>(commandIs, isAdmin, hasQuota)`（平舖、想加幾個加幾個、不巢狀）。

**要拿掉的**:
- `PredicateGuard<C>`（通用 lambda 殼）。
- `.And()` / `.Or()` extension methods（`GuardExtensions`）。
- **`OrGuard` 不做**（YAGNI）:目前 0 處用到 OR;guard 疊加的自然語意就是 AND（一條 transition 的條件全過才轉）。看似 OR 的需求多半其實是「兩條不同 transition」。需要「任一即可」時再加 `OrGuard`。

**連帶影響**:`BotGuards.CommandIs/IsAdmin/HasQuota`（目前回 `PredicateGuard`）改成回對應具名 class;`BotBuilder` 裡 `guard.And(...)` 改成收集成 list 再 `new AndGuard(...)`;`TransitionBuilder.When`（收 `Func`）需決定保留 lambda 入口或也改 class。

### H2. FSM 委派（`Fire` → `Current.Handle` → 子 FSM `Fire`）
**決定:保留,不改。** 它看起來像遞迴，實則是**委派**（每層是不同的 FSM 物件，非同一方法自呼）。這是難點二「主 FSM 一行不改就支援子狀態機」的核心——若改成 `if (Current is CompositeState)` 反而破壞 OCP。
- **可做**:在 `Fire` 的 `Current.Handle` 那行加註解，明說「這裡是委派非遞迴，composite 會往內層再跑一層」。

### H3. Action 比照 H1，但只「部分」改（已決定）
Action 與 Guard 結構同構，可比照 H1，**但只改可重用的，一次性領域 lambda 保留**。

**判準:看這段邏輯在程式碼裡「重不重複 / 被不被重用」，不是「執行幾次」。**
（`ctx.OnlineCount++` 執行時會跑很多次，但在 codebase 只出現在 login 那一條 transition → 不重用 → 不抽 class。）

| 對象 | 改法 | 理由 |
|------|------|------|
| **可重用** `DeductQuota(n)` / `SendChat(msg)` | 改**具名 class**（`DeductQuotaAction`/`SendChatAction`），`Execute` 寫明副作用 | 被多條 transition 共用（只是參數不同），值得抽 |
| **一次性領域** `does: (_,ctx)=>ctx.OnlineCount++` 這類 | **保留 `DelegateAction`（lambda 殼）** | 只綁一條特定 transition、程式裡只出現一處、不被重用 → 抽 class 是過度設計（多命名/檔案/跳轉，卻沒換到重用好處） |
| `TransitionAction`（收一串依序跑） | 不動，已是好寫法 | — |
| `NoOpAction` | 不動 | 預設空物件，對的 |

**關鍵差別（Action vs Guard）**:Guard 的判斷幾乎全可重用（admin/quota/關鍵字）→ `PredicateGuard` 幾乎可全拿掉;Action 有一半是 application 的**一次性領域副作用**（歸零題號、人數±1、標記廣播中）→ **`DelegateAction` 必須保留**當它們的載體，不能像 `PredicateGuard` 那樣全砍。

**具體:** `BotActions.DeductQuota/SendChat` 從「回 `DelegateAction` 包 lambda」改成「回具名 class」（與 H1 的 `BotGuards` 對稱）;`DelegateAction` 留著給 `WaterballBot` 的一次性 `does:` 用。

---

## 其他需求（使用者待補）
> （此區留給使用者補上尚未提出的需求，補齊後一起規劃實作。）
