# FSM — 社群機器人引擎 × 有限狀態機框架

依 `fsm-bot-design-discussion.md` 的設計實作。核心是**嚴格三層**：

| 層 | 專案 | 職責 | 不知道什麼 |
|----|------|------|-----------|
| **FSM 模組** | `src/Fsm.Core` | 宣告狀態 + 轉移規則，餵事件照規則跑 | 不知道「社群機器人」是什麼 |
| **子狀態機 plugin** | `src/Fsm.Composite` | 在 FSM 之上加 Composite 巢狀能力 | 主 FSM 一行不改（OCP） |
| **bot 模組** | `src/Bot` | Facade / DSL，封裝指令 / 額度 / 權限 / 輪播 | 不知道某隻機器人的具體值 |
| **application 層** | `src/Application` | 用 bot DSL 宣告 Waterball 機器人 | 不知道狀態機底層 |

## 專案結構

```
src/
  Fsm.Core/        State<C> / Event / Guard<C> / Action<C> / Transition<C>
                   FiniteStateMachine<C> (fire→FireResult) / LeafState<C>
  Fsm.Composite/   CompositeState<C>  (implements State + has-a FSM，冒泡 + initialResolver)
  Bot/             IMessenger / IBotContext / BotBuilder<C> DSL / Rotate<C>
  Application/      BotContext / ConsoleMessenger / WaterballBot / EventParser / Program
tests/
  Waterball.Tests/ 核心 fire 順序、靜默失敗、冒泡、resolver、play again 等
```

## 設計要點對照

- **先響應再轉移**：`fire()` 先 `current.Handle()`（輪播），再查 transition 表。
- **exit → action → entry**：轉移執行順序，需求白紙黑字規定。
- **靜默失敗**：guard 不過 → 不進 matched → 不轉移，但輪播（handle）照發。
- **宣告式轉移**：`Transition<C>` 純資料，FSM 統一查表（`from + on(name) + guard`），衝突取宣告順序第一條。
- **子狀態機冒泡**：`CompositeState.Handle` 委派內層；內層吃掉→Consumed，沒吃到→冒泡外層。核心無任何 `if (composite)`。
- **initialResolver**：`(C)=>stateId` 把「依 ctx 選初始子狀態」的領域判斷從框架搬到 application（Record 看廣播、Normal 看人數）。
- **play again 陷阱**：開場白綁 transition action（因路徑而異），出題放 entry（共同）。
- **Context 泛型 `<C>`**：核心只認得型別參數 C，`BotContext` 定義在 application，強型別。
- **Messenger 依賴反轉**：介面在 bot 層，`ConsoleMessenger` 在 application 注入；`Action.Execute(): void`。

## Waterball DSL 範例（`WaterballBot.Define()`）

```csharp
bot.State("Normal")
   .SubStates(sub => { sub.State("Default").OnMessage(new Rotate<BotContext>("good to hear", ...)); ... })
   .StartWith(ctx => ctx.OnlineCount < 10 ? "Default" : "Interacting")
   .Command("king").AdminOnly().Costs(5).Replies("KnowledgeKing is started!").SwitchTo("KnowledgeKing")
   .Command("record").Costs(3).SwitchTo("Record");
```

## 建置與執行

```bash
dotnet build
dotnet test

# 互動執行（每行一個事件）
dotnet run --project src/Application
```

### 輸入語法（示範）

```
login | logout | go broadcasting | stop-recording | elapsed
msg <authorId> [@bot ]<content>          # 一則聊天訊息；authorId=1 為管理員
```

範例 transcript：

```
msg 2 @bot king      # 非管理員 → 靜默失敗，只輪播
msg 1 @bot king      # 管理員 → 輪播 + "KnowledgeKing is started!" + Question 0
elapsed
elapsed
elapsed               # → Thanks for joining!
msg 1 @bot play again # → "KnowledgeKing is gonna start again!" + Question 0
```

## 備註

- `nuget.config`（repo 內）清掉機器層的私有 feed，只用 nuget.org，讓乾淨簽出可離線還原（套件已在快取時）。
- Target framework：`net10.0`；測試框架：xUnit。
