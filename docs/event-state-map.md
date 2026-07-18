# Event ↔ State 對照表

> 本文件整理 Waterball bot 支援的**所有 event 訊息**,以及每個 event 最終**由哪個 state 處理、怎麼處理**。
> 資料源:[EventParser 分派器](../src/Application/Parsing/EventParser.cs) 與 [WaterballBot 狀態機定義](../src/Application/WaterballBot.cs)。

---

## 一、共有幾種 event?

輸入格式為 `[<name>] <json>`(見 [G 區規格](todo-and-open-questions.md#L135))。共 **10 種 event**,依「是否驅動 FSM」分兩類:

| # | Event name | payload | 驅動 FSM? |
|---|-----------|---------|:--------:|
| 1 | `new message` | `authorId, content, tags[]` | ✅ |
| 2 | `elapsed`(`<n> <unit> elapsed`) | 換算後 seconds | ✅ |
| 3 | `go broadcasting` | `speakerId` | ✅ |
| 4 | `speak` | `speakerId, content` | ✅ |
| 5 | `stop broadcasting` | `speakerId` | ✅ |
| 6 | `login` | `userId, isAdmin` | ✅(留 Normal)|
| 7 | `logout` | `userId` | ✅(留 Normal)|
| 8 | `new post` | `id, authorId, title, content, tags[]` | ❌(僅回顯/設發話者)|
| 9 | `started` | `time, quota` | ❌(設初始 quota)|
| 10 | `end` | 無 | ❌(終止程式)|

> `elapsed` 特例:秒數在外殼 `[10 seconds elapsed]` 裡(非 json),由分派器換算成 seconds 放進 payload。

---

## 二、每個 event 由哪個 state 處理

### A. 驅動 FSM 的 event(進 `fsm.Fire`)

| Event | 處理的 State | 觸發條件 / 處理方式 | 結果 |
|-------|-------------|---------------------|------|
| **new message** | `Normal` | 指令 `king`(admin + costs 5) | → KnowledgeKing |
| | `Normal` | 指令 `record`(costs 3,記錄音者) | → Record |
| | `KnowledgeKing`(外層) | 指令 `king-stop`(admin) | → Normal |
| | `Questioning` | tag bot + 答對 + 首答 | 計分 + Congrats,跨題 / 進 Thanks |
| | `ThanksForJoining` | 指令 `play again` | 歸零重開 → Questioning |
| | `Record`(外層) | 指令 `stop-recording`(限錄音者) | 輸出 Replay(若錄音中)→ Normal |
| **elapsed** | `Questioning`(onHandle) | 每個 elapsed | 累計 `ElapsedSecondsInQuestion` / `InGame` |
| | `Questioning` | `ElapsedSecondsInGame >= 3600`(宣告最前,優先) | → ThanksForJoining(1h 強制) |
| | `Questioning` | `ElapsedSecondsInQuestion >= 20` | 跨題 / 進 Thanks(20s 到) |
| | `ThanksForJoining`(onHandle) | 每個 elapsed | 累計 `ElapsedSecondsInThanks` |
| | `KnowledgeKing`(外層) | `ElapsedSecondsInThanks >= 20` | → Normal(Thanks 結束) |
| **go broadcasting** | `Waiting` | — | → Recording(`SomeoneIsBroadcasting=true`)|
| **speak** | `Recording`(onHandle) | — | 累計進 `RecordBuffer`(**不轉移**,冒泡靜默)|
| **stop broadcasting** | `Recording` | — | 輸出 Record Replay + 清 buffer → Waiting(循環)|
| **login** | `Normal` | — | `OnlineCount++`(留 Normal,重選 Default/Interacting)|
| **logout** | `Normal` | — | `OnlineCount--`(留 Normal)|

### B. 不進 FSM、由「前置層」處理的 event

| Event | 處理者 | 做什麼 |
|-------|-------|-------|
| **new post** | [EventContextBinder](../src/Application/Parsing/EventContextBinder.cs) + [InputEcho](../src/Application/Output/InputEcho.cs) | 設當前發話者;回顯 `<id>:【title】content @tags`(**無 FSM transition**)|
| **started** | EventContextBinder | 設初始 quota(`ShowInitialQuota`)|
| **end** | [Program.cs](../src/Application/Program.cs) 主迴圈 | `break` 終止程式 |

> `login` / `started` 也會先經 EventContextBinder(建使用者表 / 設額度),再進 FSM。
> 所有成員事件(message/post/broadcast/elapsed)另由 InputEcho 在 `fsm.Fire` **之前**回顯成正式格式。

---

## 三、狀態階層 × 收哪些 event

```
Normal (composite)                    ← new message(king/record)、login、logout
├─ Default        (輪播 good to hear…)
└─ Interacting    (輪播 nice to see you…)   ← OnlineCount >= 10 時進此

KnowledgeKing (composite)             ← new message(king-stop,外層)、elapsed(1h/Thanks-20s,外層)
├─ Questioning       ← new message(答題)、elapsed(onHandle 累計 + 20s 跨題 + 1h 強制)
└─ ThanksForJoining  ← new message(play again)、elapsed(onHandle 累計,20s 由外層轉 Normal)

Record (composite)                    ← new message(stop-recording,外層)
├─ Waiting           ← go broadcasting
└─ Recording         ← speak(onHandle 累計)、stop broadcasting(→ Replay + 回 Waiting)
```

---

## 四、三個關鍵 pattern

1. **`new message` 是最忙的 event** —— 5 個 state 都處理它,靠 `content`(指令關鍵字)+ guard 分流:
   `king` / `record` / `king-stop` / 答案代號 / `play again` / `stop-recording`。

2. **`elapsed` 靠 ctx 累計欄位分流** —— 同一事件,`Questioning` 看 20s、外層看 1h 與 Thanks-20s。
   累計用 **onHandle**(非 self-loop transition)—— 避免 re-entry 被 onEnter 歸零(見
   [批四〈累計 tick〉](plans/waterball-full-implementation.md))。

3. **`speak` 只被 `Recording` 處理**,且用 onHandle 累計、**不轉移**;其他 state 收到 speak → 冒泡靜默(無對應 transition)。

---

## 五、內層 / 外層 transition 的目標限制

一條 transition 的 `stateTo` **只能是同一層 FSM 認得的 state**:

- **1h → ThanksForJoining**:目標是內層 state → 放**內層**(`Questioning`),宣告在 20s 之前故優先。
- **king-stop / Thanks-20s → Normal**:目標是外層 state → 放**外層**(`KnowledgeKing`)。
  內層 `ThanksForJoining` 對 elapsed 無對應 transition → 冒泡到外層被 `KnowledgeKing --elapsed--> Normal` 接住。
- **stop-recording → Normal**:目標是外層 → 放**外層**(`Record`);任意子狀態(Waiting/Recording)冒泡離開。
