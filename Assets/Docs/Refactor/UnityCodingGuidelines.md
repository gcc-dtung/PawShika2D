# Unity Coding Guidelines

> Bộ quy tắc dùng chung cho mọi dự án Unity (C#).
> Gồm: triết lý, nguyên tắc thiết kế (SOLID), pattern, kiến trúc, quy ước đặt tên, quy tắc riêng của Unity, commit và checklist review.
> Không phụ thuộc dự án cụ thể. Dự án nào cần khác đi thì ghi phần ngoại lệ vào file riêng của dự án (ví dụ `ProjectConventions.md`), không sửa file này.

---

## Mục lục

1. [Triết lý: vừa đủ, không over-engineering](#1-triết-lý-vừa-đủ-không-over-engineering)
2. [Nguyên tắc thiết kế (SOLID)](#2-nguyên-tắc-thiết-kế-solid)
3. [Design Pattern: khi nào dùng, khi nào không](#3-design-pattern-khi-nào-dùng-khi-nào-không)
4. [Kiến trúc](#4-kiến-trúc)
5. [Cấu trúc thư mục](#5-cấu-trúc-thư-mục)
6. [Quy ước đặt tên](#6-quy-ước-đặt-tên)
7. [Bố cục một file / class](#7-bố-cục-một-file--class)
8. [Quy tắc riêng của Unity](#8-quy-tắc-riêng-của-unity)
9. [Asset và tối ưu](#9-asset-và-tối-ưu)
10. [Prefab và UI](#10-prefab-và-ui)
11. [Animation, async, thời gian](#11-animation-async-thời-gian)
12. [Lỗi, log, comment](#12-lỗi-log-comment)
13. [Test](#13-test)
14. [Git và commit](#14-git-và-commit)
15. [Checklist trước khi commit](#15-checklist-trước-khi-commit)
16. [Code dùng lại giữa các dự án](#16-code-dùng-lại-giữa-các-dự-án)
17. [Nguồn tham khảo](#17-nguồn-tham-khảo)

---

## 1. Triết lý: vừa đủ, không over-engineering

> *"Architecture là công cụ, không phải tôn giáo."*

| Quy tắc | Ý nghĩa |
| --- | --- |
| **Giải quyết vấn đề đang có** | Không viết code cho tính năng "có thể sau này cần" (YAGNI). |
| **Luật ba lần** (Rule of Three) | Lặp lại lần 1, 2: chấp nhận. Lần 3: mới tách thành hàm / class / pattern chung. |
| **Nhỏ thì đơn giản, lớn thì có cấu trúc** | Tính năng 1 script 50 dòng không cần Model + View + Presenter + Interface. |
| **Đơn giản trước, refactor sau** | Make it work → make it right → make it fast. Refactor khi có lý do cụ thể (khó thêm tính năng, khó sửa bug, lặp code). |
| **Mỗi lớp trừu tượng phải trả lời được "để làm gì"** | Interface, event, factory... mà không trả lời được câu này thì xóa đi. |
| **Ưu tiên công cụ sẵn có của Unity** | Inspector, ScriptableObject, Prefab, UnityEvent trước khi tự xây framework. |

### Dấu hiệu đang over-engineering

- Interface chỉ có đúng 1 class cài đặt và không bao giờ có class thứ hai (trừ khi cần mock để test).
- Đọc một luồng đơn giản phải nhảy qua 4–5 file.
- Có "Manager của Manager", "Factory của Factory".
- Dùng DI framework (Zenject, VContainer) cho dự án 1–2 người, dưới vài chục hệ thống.
- Viết State pattern cho enum chỉ có 3–5 trạng thái và ít logic mỗi trạng thái.
- Tạo event cho việc chỉ có đúng 1 bên nghe và 2 bên vốn đã biết nhau.

### Dấu hiệu đang thiếu cấu trúc

- Class > 500 dòng hoặc có > 5 lý do để sửa.
- `switch` theo loại (type / enum) lặp lại ở nhiều nơi.
- Sửa tính năng A làm hỏng tính năng B không liên quan.
- Không test được logic nếu không mở scene.
- `.Instance` của nhiều Manager rải khắp nơi.

---

## 2. Nguyên tắc thiết kế (SOLID)

SOLID là kết quả của 4 tư duy dưới đây. Hiểu tư duy thì tự áp dụng được SOLID.

| Tư duy | SOLID | Dẫn tới pattern |
| --- | --- | --- |
| Tách phần hay thay đổi (Encapsulate What Varies) | **O** – Open/Closed | Strategy, State |
| Ưu tiên kết hợp hơn kế thừa (Composition over Inheritance) | **L** – Liskov, **I** – Interface Segregation | Component, Decorator |
| Phụ thuộc vào trừu tượng (Program to Abstraction) | **D** – Dependency Inversion | Factory |
| Ít phụ thuộc, tập trung một việc (Low Coupling, High Cohesion) | **S** – Single Responsibility | Observer, Command |

### S – Single Responsibility

- Mỗi class chỉ có **một lý do để sửa**.
- Dấu hiệu vi phạm: tên class có "And", "Manager" làm mọi thứ, file > 300–500 dòng.
- Cách tách phổ biến trong Unity: **dữ liệu + luật** (C# thuần) / **hiển thị** (MonoBehaviour) / **điều phối** (nối hai bên).

### O – Open/Closed

- Thêm loại mới (booster, enemy, vũ khí...) **bằng cách thêm class**, không sửa `switch` cũ.
- Chỉ áp dụng khi **đã có ≥ 3 loại** hoặc chắc chắn sẽ thêm. 2 loại thì `if/else` vẫn ổn.

### L – Liskov Substitution

- Class con phải dùng thay được class cha mà không làm sai hành vi.
- Class con phải `throw NotImplementedException` hay override thành rỗng → sai thiết kế, tách interface.
- Kế thừa tối đa **1–2 tầng**.

### I – Interface Segregation

- Interface nhỏ, theo vai trò: `IDamageable`, `IInteractable`, không làm `IEntity` 20 method.
- Class cài nhiều interface nhỏ thay vì một interface lớn.

### D – Dependency Inversion

- Code cấp cao (luật game) không gọi thẳng thứ cấp thấp có thể thay đổi: SDK quảng cáo, IAP, lưu file, thời gian server, analytics, nền tảng.
- **Phân biệt:** DIP là nguyên tắc (phụ thuộc interface), DI là kỹ thuật (truyền dependency vào qua constructor / `Init()` / Inspector).
- **Chỉ tạo interface khi** có ≥ 2 cài đặt (thật / giả lập / mock test) hoặc là ranh giới với SDK bên ngoài.

```csharp
// Đúng: luật game không biết SDK nào đang chạy
public interface IAdsService
{
    void ShowRewarded(Action<bool> onDone);
}

public class FakeAdsService : IAdsService { /* giả lập trong Editor */ }
public class AdMobAdsService : IAdsService { /* SDK thật */ }
```

---

## 3. Design Pattern: khi nào dùng, khi nào không

> Pattern là công cụ, không phải mục tiêu. Gặp vấn đề trước, chọn pattern sau, đặt tên cuối cùng.

| Pattern | Dùng khi | **Không** dùng khi | Ghi chú Unity |
| --- | --- | --- | --- |
| **Strategy** | Nhiều biến thể của cùng một hành vi, ≥ 3 loại, có thể đổi lúc chạy | Chỉ 2 biến thể đơn giản | Class C# thuần hoặc ScriptableObject làm strategy |
| **Observer** (event) | Một nơi phát, **nhiều nơi nghe**, bên phát không cần biết bên nghe | Chỉ 1 bên nghe và 2 bên vốn liên quan trực tiếp → gọi thẳng | Luôn hủy đăng ký (xem mục 8) |
| **Object Pool** | Tạo / hủy liên tục (đạn, VFX, ô lưới, chữ nổi) | Object tạo 1 lần khi load | Dùng `UnityEngine.Pool.ObjectPool<T>` có sẵn |
| **Factory** | Tạo object theo loại / dữ liệu, ẩn việc chọn class cụ thể | Chỉ `new` một class duy nhất | Thường đi cùng Strategy: `Factory.Create(type)` |
| **State** | Mỗi trạng thái có **nhiều logic riêng** (Enter / Update / Exit), chuyển trạng thái phức tạp | Enum 3–5 trạng thái, logic mỗi trạng thái vài dòng → giữ `enum` + `switch` | AI, nhân vật, game flow lớn |
| **Command** | Cần Undo / Redo, replay, hàng đợi lệnh | Không có tính năng trên | Input buffer, Undo nước đi |
| **Singleton** | Hạ tầng **thật sự duy nhất, ít state**: Audio, Log, Input, Analytics | **Game logic** (Level, Board, Player, Inventory, Booster...) | Xem quy tắc dưới |

### Quy tắc Singleton

1. Chỉ cho **hạ tầng**. Game logic tuyệt đối không.
2. Không tự tạo khi chưa có (`new GameObject` trong getter); thiếu thì `LogError` rõ ràng.
3. Xóa tham chiếu trong `OnDestroy`.
4. Không gọi `.Instance` của Singleton khác trong `Awake` (lỗi thứ tự khởi tạo).
5. Thay thế ưu tiên theo thứ tự: **kéo tham chiếu trong Inspector** → **truyền vào qua `Init()` từ Bootstrap** → event.

```csharp
public class AudioService : MonoBehaviour
{
    private static AudioService _instance;

    public static AudioService Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("AudioService chưa có trong scene.");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
```

### Pattern Unity hay dùng ngoài 7 pattern trên

| Pattern | Dùng khi |
| --- | --- |
| **ScriptableObject làm dữ liệu / config** | Số liệu cân bằng, database item, level. Designer sửa không cần code |
| **Component** | Ghép hành vi bằng nhiều MonoBehaviour nhỏ thay vì cây kế thừa |
| **Composition Root / Bootstrap** | Một chỗ duy nhất tạo service và nối dependency khi vào scene |

---

## 4. Kiến trúc

### 4.1 Module và chiều phụ thuộc

Dự án chia thành **module theo tính năng** (feature, mỗi feature là một "lát dọc": có đủ luật, hiển thị, dữ liệu, asset của nó), đặt trên một nền **dùng chung**.

```text
                      ┌──────────────┐
                      │     Game     │  Nối mọi thứ: Bootstrap, luồng màn hình
                      └──────┬───────┘
          ┌──────────────────┼──────────────────┐
          ▼                  ▼                  ▼
   ┌────────────┐     ┌────────────┐     ┌────────────┐
   │ Feature A  │  ✕  │ Feature B  │  ✕  │ Feature C  │   Không biết nhau
   └─────┬──────┘     └─────┬──────┘     └─────┬──────┘
         └──────────────────┼──────────────────┘
                            ▼
                      ┌──────────────┐
                      │    Shared    │  UI widget, PopupView, art / font / âm thanh chung
                      └──────┬───────┘
                             ▼
              ┌──────────────────────────────┐
              │ Core                         │
              │  ├── Contracts  (định nghĩa) │  Event, interface, enum, data dùng chung
              │  └── Foundation (hạ tầng)    │  EventBus, Save, Audio, Pool, Util, service SDK
              └──────────────────────────────┘

Mũi tên = "được phép phụ thuộc vào". Không có mũi tên = cấm.
```

| Module | Chứa | Được biết | **Không được biết** |
| --- | --- | --- | --- |
| **Core / Foundation** | Hạ tầng không dính tới game này: `EventBus`, Save, Audio, Pool, Util, interface + cài đặt service SDK (`IAdsService`, `IIAPService`, `IClock`) | Unity, thư viện ngoài | Mọi thứ còn lại |
| **Core / Contracts** | **Chỉ định nghĩa**, không có logic: struct event dùng giữa các feature, interface giữa các feature, enum / id / data nhỏ dùng chung (`BoosterType`, `RewardData`) | Foundation | Shared, Features, Game |
| **Shared** | Thứ nhiều feature cùng dùng nhưng thuộc về giao diện / asset: widget UI, `PopupView`, font, shader, icon chung, SFX chung | Core | Features, Game |
| **Features / \<Tên\>** | Toàn bộ một tính năng: Model, View, Presenter, Config, Prefab, Art, Editor tool, Test của nó | Core, Shared | **Feature khác**, Game |
| **Game** | Composition root: `GameBootstrap`, luồng màn hình / game flow, prefab màn hình tổng ghép từ prefab của các feature | Tất cả | — |
| **Editor** (trong từng module) | Tool chỉ chạy trong Editor | Module chứa nó + những gì module đó được biết | — |

**Quy tắc cứng:**

1. **Feature không biết feature khác.** Không `using` namespace, không tham chiếu class, prefab, ScriptableObject, scene của feature khác. Kể cả trong Inspector.
2. **Feature chỉ giao tiếp với nhau qua Core / Contracts** (event hoặc interface, mục 4.2).
3. **Core / Contracts chỉ chứa định nghĩa**, không chứa luật. Thấy `if`, vòng lặp, tính toán trong Contracts → sai chỗ, chuyển về feature sở hữu.
4. **Game là nơi duy nhất biết mọi feature.** Mọi việc "nối A với B" nằm ở đây.
5. Phụ thuộc chỉ đi **một chiều** theo mũi tên. Không gọi ngược lên.

**Feature phải đủ lớn (chia thô, không chia mịn):**

Cách ly chỉ có lợi giữa những phần **thật sự độc lập**. Ép những phần vốn dính chặt nói chuyện qua event chỉ sinh ra hàng chục event một-đổi-một → over-engineering.

- **Feature** = một nhóm có luật, dữ liệu và giao diện riêng, có thể **tắt / xóa mà phần còn lại vẫn chạy** (Shop, Daily Reward, Lucky Wheel, Profile...).
- Những phần **luôn đi cùng nhau, gọi nhau liên tục** (board, level, input, booster, timer, tutorial trong game puzzle) là **một** feature, ví dụ `Gameplay`. Bên trong chia thư mục con; các thư mục con **được gọi thẳng nhau** (quy tắc nội bộ, mục 4.4).
- Quá nhỏ (1–2 script, không có dữ liệu riêng) → gộp vào feature gần nhất hoặc vào Shared.
- Không chắc nên tách hay gộp → **gộp trước**, tách khi có lý do thật (feature lớn lên, hay bị thay đổi riêng, muốn mang sang dự án khác).

```text
Features/
├── Gameplay/          Một feature: Board/, Level/, Input/, Booster/, Timer/, Tutorial/ gọi thẳng nhau
├── Economy/           Ví tiền, năng lượng
├── Shop/
├── DailyReward/
├── LuckyWheel/
└── Profile/
```

**Mức áp dụng theo quy mô dự án:**

| Quy mô | Chia module | Quy tắc cách ly (1–5) | `.asmdef` |
| --- | --- | --- | --- |
| Prototype / game jam | Chỉ chia thư mục | Không bắt buộc | Không |
| **Puzzle / casual nhỏ** (1–3 người) | Lõi màn chơi = **một** feature; meta chia theo feature | **Có** | **Theo nhóm** (~5 file, bảng dưới) |
| Trung bình, có live-ops | Lõi màn chơi 2–3 feature lớn; meta theo feature | Có | Mỗi feature lớn một file |
| Lớn / nhiều team | Feature mịn | Có | Mỗi feature một file |

**Bảo vệ bằng `.asmdef`** (dự án nhỏ, theo nhóm):

| Assembly | Chứa | Reference được |
| --- | --- | --- |
| `<Project>.Core.Foundation` | `Core/Foundation` | (không) |
| `<Project>.Core.Contracts` | `Core/Contracts` | Foundation |
| `<Project>.Shared` | `Shared` | Foundation, Contracts |
| `<Project>.Gameplay` | `Features/Gameplay` | Foundation, Contracts, Shared |
| `<Project>.Meta` | Mọi feature meta (`Features/Shop`, `Features/DailyReward`...) | Foundation, Contracts, Shared |
| `<Project>.Game` | `Game` | Tất cả |
| `<…>.Editor` | Thư mục `Editor/` của từng nhóm | Assembly runtime cùng nhóm (+ những gì nó được biết). Platform: **Editor only** |
| `<…>.UnitTests` / `.IntegrationTests` | Thư mục `Tests/` (mục 13) | Assembly cần test + Test Runner |

- Compiler chặn được: Gameplay ↔ Meta, Core / Shared → feature, mọi thứ → Game.
- Compiler **không** chặn được các feature meta tham chiếu nhau (chung một assembly) → kiểm tra bằng namespace: trong `Features/Shop` không được có `using <Project>.Features.DailyReward`. Thêm vào checklist review.
- Thư mục `Editor/` nằm trong vùng của một `.asmdef` runtime **không** tự thành Editor-only. Mỗi thư mục `Editor/` cần `.asmdef` riêng (Platform: Editor only), nếu không bản build sẽ lỗi vì `using UnityEditor`.
- Tách `.asmdef` riêng cho một feature khi: nó lớn hẳn lên, hay vi phạm cách ly, hoặc muốn mang sang dự án khác.

**Dấu hiệu đang cách ly quá mức → gộp lại, đừng thêm event:**
- Một event chỉ có đúng một bên nghe, và bên phát thực chất đang "ra lệnh" cho bên nghe.
- Một contract interface chỉ có một nơi dùng, method chép y nguyên class cài đặt.
- Sửa một hành vi phải sửa 3 file ở 3 feature khác nhau.
- Số event giữa các feature tăng nhanh hơn số tính năng.

**Kiểm tra:** xóa thư mục một feature (ví dụ `LuckyWheel/`) → chỉ phải sửa vài dòng ở `Game/` (Bootstrap, flow); **không feature nào khác báo lỗi**. Chưa đạt → feature đang bị dính vào nơi khác.

### 4.2 Giao tiếp giữa các feature

Có **đúng hai kênh**, cả hai đều nằm trong Core / Contracts:

| Kênh | Dùng khi | Ví dụ |
| --- | --- | --- |
| **1. Event** (mặc định) | Báo "**đã xảy ra**", bên phát không cần câu trả lời, không cần biết ai nghe | `LevelCompletedEvent`, `CoinsChangedEvent`, `BoosterUsedEvent` |
| **2. Contract interface** | Cần **hỏi hoặc ra lệnh và nhận kết quả ngay** (trả về giá trị, `bool` thành công / thất bại) | `ICurrencyWallet.TrySpend(amount)`, `IBoosterInventory.GetCount(type)` |

#### Kênh 1: Event

- Struct event dùng giữa các feature đặt trong `Core/Contracts/Events/`. Event chỉ dùng **bên trong** một feature thì giữ trong feature đó.
- Đặt tên ở thì quá khứ (đã xảy ra): `LevelCompletedEvent`, không `CompleteLevelEvent`.
- Event mang **dữ liệu**, không mang tham chiếu tới object của feature phát (`int level, int stars`, không `LevelController sender`).
- Bên phát không biết ai nghe; **không ai nghe thì game vẫn chạy đúng**.
- Muốn feature khác **làm gì đó** mà không cần kết quả → event yêu cầu, tên `…RequestedEvent`: `ShopOpenRequestedEvent`. Feature sở hữu (hoặc Game) nghe và xử lý.

```csharp
// Core/Contracts/Events/LevelCompletedEvent.cs
public readonly struct LevelCompletedEvent : IGameEvent
{
    public readonly int Level;
    public readonly int Stars;
    public LevelCompletedEvent(int level, int stars) { Level = level; Stars = stars; }
}

// Features/Level: phát, không biết ai nghe
EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent(level, stars));

// Features/Economy: nghe, không biết Level tồn tại
private void OnEnable()  => EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);
private void OnDisable() => EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
```

#### Kênh 2: Contract interface

- Interface đặt trong `Core/Contracts/Interfaces/`.
- **Một** feature cài đặt nó (feature "sở hữu" dữ liệu đó). Feature khác nhận interface qua `Init(...)` do **Game** truyền vào.
- Feature dùng interface **không biết** feature nào cài đặt.
- Interface nhỏ, theo vai trò (mục 2, "I"): `ICurrencyWallet` chỉ có `Balance`, `TrySpend`, `Add`; không gom mọi thứ của Economy vào một interface.
- Chỉ tạo contract khi **thật sự có feature khác cần gọi**. Không tạo sẵn "cho chắc".

```csharp
// Core/Contracts/Interfaces/ICurrencyWallet.cs
public interface ICurrencyWallet
{
    int Balance { get; }
    bool TrySpend(int amount);
    void Add(int amount);
}

// Features/Economy: sở hữu và cài đặt
public class CoinWallet : ICurrencyWallet { /* ... */ }

// Features/Shop: chỉ biết interface
public class ShopService
{
    private readonly ICurrencyWallet _wallet;
    public ShopService(ICurrencyWallet wallet) => _wallet = wallet;
}

// Game/GameBootstrap: nơi duy nhất biết cả hai
var wallet = new CoinWallet(save);
var shop = new ShopService(wallet);
```

#### Dữ liệu dùng chung

- Enum / id / struct nhỏ mà nhiều feature cùng cần (`BoosterType`, `CurrencyType`, `RewardData`) → `Core/Contracts/Types/`.
- Dữ liệu chỉ một feature dùng → giữ trong feature, kể cả khi "có thể sau này cần" (chuyển sang Contracts khi thật sự có feature thứ hai cần).

#### Bị cấm giữa các feature

| Cấm | Thay bằng |
| --- | --- |
| `using <Project>.Features.Khác` | Event / contract trong Core |
| Kéo tham chiếu tới object / prefab / ScriptableObject của feature khác trong Inspector | Game truyền contract vào qua `Init(...)` |
| Feature A mở popup của feature B | Phát `…RequestedEvent`; B hoặc Game mở |
| Gọi `.Instance` của feature khác | Contract interface |
| Cờ `static` để feature khác đọc | Contract interface (hỏi) hoặc event (báo thay đổi) |
| Đặt logic vào Contracts để "dùng chung" | Logic ở feature sở hữu, chia sẻ qua contract |

#### Khi feature B "cần" thứ gì đó của feature A

```text
B cần biết A đã làm gì?             → A phát event (Contracts), B nghe
B cần hỏi / yêu cầu A và chờ kết quả? → interface trong Contracts, A cài, Game truyền vào B
B cần dùng cùng một enum / data?    → chuyển type đó sang Contracts/Types
B cần dùng cùng widget / sprite?    → chuyển sang Shared
B cần dùng cùng luật / thuật toán?  → có thể B và A thật ra là một feature → gộp
```

### 4.3 Tách Model / View (/ Presenter)

| Thành phần | Là gì | Quy tắc |
| --- | --- | --- |
| **Model** | Class C# thuần (`…Model`) | Chứa dữ liệu + luật. Không `using UnityEngine` nếu tránh được (cho phép `Vector2Int`, `Mathf`). Báo thay đổi bằng C# event. Test được không cần scene |
| **View** | MonoBehaviour (`…View`) | Chỉ hiển thị và chuyển input lên trên. **Không quyết định luật** |
| **Presenter / Controller** | C# thuần hoặc MonoBehaviour (`…Presenter`, `…Controller`) | Nghe Model → cập nhật View; nghe View → gọi Model |

**Mức áp dụng (tránh over-engineering):**

| Trường hợp | Làm |
| --- | --- |
| Logic phức tạp, cần test (board, kinh tế, phần thưởng) | Model riêng + View riêng + Presenter hoặc Controller nối |
| Popup / UI có logic (shop, phần thưởng, cài đặt) | Model + View, Presenter nếu logic đủ nhiều |
| UI chỉ hiển thị một giá trị (label coin, thanh máu) | Một View nghe event của Model là đủ |
| Hiệu ứng, trang trí | Một MonoBehaviour, xong |

`IView` interface chỉ tạo khi thật sự viết test cho Presenter.

### 4.4 Giao tiếp bên trong một feature

Bên trong một feature, các class được biết nhau. Chọn theo thứ tự ưu tiên, **dừng ở cách đầu tiên phù hợp**:

| # | Cách | Khi nào |
| --- | --- | --- |
| 1 | **Tham chiếu trực tiếp qua Inspector** (`[SerializeField]`) | Hai object cùng scene / prefab, quan hệ cha – con hoặc cố định |
| 2 | **Truyền vào qua `Init(...)` / constructor** | Object tạo lúc chạy (spawn, pool), class C# thuần |
| 3 | **C# event trên Model / Service** | Một nguồn, vài bên nghe trong cùng feature |
| 4 | **Singleton** | Chỉ hạ tầng trong Core / Foundation (mục 3) |

`EventBus` toàn game dành cho giao tiếp **giữa** feature (mục 4.2); trong nội bộ feature ưu tiên gọi trực tiếp hoặc C# event.

**Cấm (ở mọi nơi):**
- Tự đi tìm / lấy / gắn component lúc chạy: `Find…`, `GetComponent…`, `AddComponent`, `Camera.main` (chi tiết mục 8.3).
- Cờ `static bool` để nơi khác đọc trạng thái (`static bool IsPaused`) → dùng event hoặc contract.
- Gọi ngược chiều phụ thuộc (mục 4.1).

### 4.5 Bootstrap (Composition Root)

Một MonoBehaviour trong `Game/` tạo service, tạo các feature và nối contract giữa chúng. Mọi chỗ "ai tạo cái gì, ai biết ai" nằm ở đây.

```csharp
namespace MyGame.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelController levelController;   // Features/Level
        [SerializeField] private ShopPopup shopPopup;               // Features/Shop
        [SerializeField] private CoinView coinView;                 // Features/Economy

        private void Awake()
        {
            // Core / Foundation
            ISaveService save = new PlayerPrefsSaveService();
            IAdsService ads = Application.isEditor ? new FakeAdsService() : new AdMobAdsService();

            // Feature sở hữu dữ liệu, lộ ra qua contract
            ICurrencyWallet wallet = new CoinWallet(save);

            // Feature khác chỉ nhận contract
            levelController.Init(ads);
            shopPopup.Init(new ShopService(wallet));
            coinView.Init(wallet);
        }
    }
}
```

- Không cần DI framework. Bootstrap viết tay là đủ cho dự án vừa và nhỏ.
- Chỉ dùng Zenject / VContainer / Reflex khi team > 3 người hoặc dự án live-service nhiều năm.
- Bootstrap dài quá (> ~150 dòng) → tách thành vài `…Installer` theo nhóm feature, Bootstrap gọi lần lượt.

---

## 5. Cấu trúc thư mục

**Chia theo tính năng (feature), không chia theo loại file.** Mỗi feature là một thư mục tự đủ, khớp với module ở mục 4.1.

```text
Assets/
├── _Project/                       Mọi thứ của dự án (dấu _ để nằm trên cùng)
│   ├── Core/
│   │   ├── Foundation/             EventBus, Save, Audio, Pool, Util, Services (IAds, IIAP, IClock + cài đặt)
│   │   │   └── <Project>.Core.Foundation.asmdef
│   │   └── Contracts/
│   │       ├── Events/             Struct event dùng giữa các feature
│   │       ├── Interfaces/         Interface giữa các feature (ICurrencyWallet...)
│   │       ├── Types/              Enum, id, data nhỏ dùng chung
│   │       └── <Project>.Core.Contracts.asmdef
│   │
│   ├── Shared/
│   │   ├── Scripts/                PopupView, script widget, helper UI
│   │   ├── Prefabs/                Widgets/, PopupFrame
│   │   ├── Art/                    Icon chung, khung, nút + Shared.spriteatlas
│   │   ├── Fonts/  Shaders/  Materials/
│   │   ├── Audio/                  SFX chung (bấm nút, mở popup), nhạc nền
│   │   └── <Project>.Shared.asmdef
│   │
│   ├── Features/
│   │   ├── Gameplay/               Lõi màn chơi = một feature (mục 4.1)
│   │   │   ├── Scripts/            Board/, Level/, Input/, Booster/, Timer/... (thư mục con gọi thẳng nhau)
│   │   │   ├── Prefabs/  Art/  Audio/  Configs/  Editor/  Tests/  Docs/
│   │   │   └── <Project>.Gameplay.asmdef
│   │   │
│   │   ├── <FeatureMeta>/          Ví dụ: Economy, Shop, DailyReward, LuckyWheel, Profile
│   │   │   ├── Scripts/            Model, View, Presenter, Service của feature
│   │   │   ├── Prefabs/
│   │   │   ├── Art/                Sprite riêng + <TênFeature>.spriteatlas
│   │   │   ├── Audio/
│   │   │   ├── Configs/            ScriptableObject cấu hình / dữ liệu của feature
│   │   │   ├── Editor/             Tool riêng của feature
│   │   │   ├── Tests/              Editor/ (unit test) + Runtime/ (integration) (mục 13)
│   │   │   └── Docs/README.md      Tài liệu của feature (xem dưới)
│   │   │
│   │   └── <Project>.Meta.asmdef   Một assembly chung cho mọi feature meta (dự án nhỏ)
│   │
│   ├── Game/
│   │   ├── Scripts/                GameBootstrap, GameFlow, ScreenFlow, Installer
│   │   ├── Prefabs/                Màn hình tổng (MainMenu, HUD) ghép từ prefab của các feature
│   │   └── <Project>.Game.asmdef
│   │
│   ├── Scenes/                     Scene toàn game (Boot, Main...)
│   ├── Settings/                   URP, Preset import, Input, Quality
│   └── Resources/                  Giữ tối thiểu (mục 9.3)
│
├── ThirdParty/                     Asset mua / tải về, giữ nguyên cấu trúc, không sửa
├── Plugins/                        SDK, thư viện (DOTween...)
└── Docs/                           Tài liệu dự án
```

### Đặt file ở đâu

| Câu hỏi | Đặt ở |
| --- | --- |
| Chỉ một feature dùng? | `Features/<Tên>/…` |
| Là event / interface / enum mà ≥ 2 feature cần? | `Core/Contracts/…` |
| Là hạ tầng không dính tới game này (mang sang game khác được)? | `Core/Foundation/` (sau này tách thành package, mục 16) |
| Là widget, popup base, art, font, âm thanh mà ≥ 2 feature dùng? | `Shared/` |
| Nối nhiều feature, điều khiển luồng màn hình, Bootstrap? | `Game/` |
| Asset tải về chưa chỉnh sửa? | `ThirdParty/` (sửa thì copy sang `_Project/`) |

- **Chuyển lên Shared / Contracts khi feature thứ hai thật sự cần**, không chuyển trước "cho chắc".
- Mỗi feature có cùng bộ thư mục con như trên; thư mục nào không có nội dung thì **không tạo**.
- Tối đa **4 tầng** thư mục tính từ `_Project/`.
- Cấm thư mục `Misc/`, `Temp/`, `Old/`, `Backup/`, `New Folder/`. Đã có git.
- Tạo / đổi tên / di chuyển file và thư mục **trong Unity**, không ngoài Explorer, để `.meta` (GUID) đi theo.

### Tài liệu của feature

Feature có hơn vài script → **bắt buộc** có `Docs/README.md`, gồm:

1. **Tóm tắt:** feature làm gì (1–3 câu).
2. **Phụ thuộc:** dùng contract / event nào trong `Core/Contracts`, phát event nào, cài đặt interface nào.
3. **Class chính:** điểm vào (`ShopService`, `ShopPopup`...) và vai trò.
4. **Cách dùng:** đoạn code ngắn cho thấy Game nối feature này thế nào.
5. **Ghi chú kiến trúc:** pattern dùng bên trong, quyết định không hiển nhiên.

Cần thêm thì tách file PascalCase cùng thư mục: `Architecture.md`, `HowToAddItem.md`.

### Namespace

Namespace trùng thư mục (bỏ `Scripts`):

| Thư mục | Namespace |
| --- | --- |
| `Core/Foundation/Save/` | `<Project>.Core.Foundation.Save` |
| `Core/Contracts/Events/` | `<Project>.Core.Contracts` (gộp chung cho dễ `using`) |
| `Shared/Scripts/UI/` | `<Project>.Shared.UI` |
| `Features/Shop/Scripts/` | `<Project>.Features.Shop` |
| `Game/Scripts/` | `<Project>.Game` |

Thấy `using <Project>.Features.X` trong một file thuộc `Features/Y` → **vi phạm** mục 4.1.

---

## 6. Quy ước đặt tên

### 6.1 Code C#

| Thành phần | Quy ước | Ví dụ |
| --- | --- | --- |
| Namespace | PascalCase, theo thư mục, dạng **block** (mục 7.1) | `MyGame.Features.Booster` |
| Class, struct, enum | PascalCase, danh từ | `BoardModel`, `GameState` |
| Interface | `I` + PascalCase, tính từ hoặc vai trò | `IDamageable`, `IAdsService` |
| Method | PascalCase, động từ | `LoadLevel()`, `TrySpend()` |
| Method trả về bool có thể thất bại | `Try…` + `out` | `TryGetCell(pos, out cell)` |
| Property | PascalCase | `CurrentLevel`, `IsActive` |
| Field private | `_camelCase` | `_currentHealth` |
| Field `[SerializeField] private` | `camelCase` (hiển thị đẹp trong Inspector) | `moveSpeed` |
| Hằng số `const` / `static readonly` | PascalCase | `MaxLives` |
| Tham số, biến cục bộ | camelCase | `levelIndex` |
| Bool (field, property, method) | **Bắt buộc** bắt đầu bằng `Is` / `Has` / `Can` / `Should`. Tránh `Dead`, `Active`, `Flag` | `IsPlaying`, `HasShield`, `CanMove()` |
| Method bất đồng bộ | Hậu tố `Async`, trả về `Task` / `Task<T>` (hoặc `UniTask`). Không `async void` trừ hàm xử lý event | `LoadLevelAsync()` |
| Event (C# `event`) | Cụm động từ chỉ thay đổi trạng thái, thì quá khứ hoặc tiếp diễn. **Không** tiền tố `On` | `LevelCompleted`, `DoorOpening`, `Changed` |
| Hàm xử lý event | `On` + tên event | `OnLevelCompleted()`, `OnChanged()` |
| Struct event cho event bus | Thì quá khứ + `…Event` | `LevelLoadedEvent` |
| List, mảng, set, dictionary | Danh từ **số nhiều** | `enemies`, `spawnPoints` |
| Enum | Số ít; enum cờ (`[Flags]`) số nhiều | `BoosterType`, `CellFlags` |

> Field private luôn `_camelCase`, field `[SerializeField] private` luôn `camelCase`. Static private cũng `_camelCase` (`_instance`). Không trộn kiểu khác.

### 6.2 Hậu tố theo vai trò

| Hậu tố | Vai trò |
| --- | --- |
| `…Model` | Dữ liệu + luật, C# thuần |
| `…View` | MonoBehaviour hiển thị |
| `…Presenter` / `…Controller` | Nối Model và View |
| `…Service` | Hạ tầng, thường có interface (Save, Ads, IAP, Analytics) |
| `…Manager` | Điều phối nhiều object cùng loại trong scene. **Hạn chế dùng**, tên mơ hồ |
| `…Config` | ScriptableObject cấu hình số liệu |
| `…Database` | ScriptableObject chứa danh sách dữ liệu |
| `…Data` | Class / struct dữ liệu thuần, thường `[Serializable]` |
| `…Factory` | Tạo object theo loại |
| `…Event` | Struct sự kiện |
| `…Builder` / `…Window` | Công cụ Editor |

### 6.3 Asset

| Loại | Quy ước | Ví dụ |
| --- | --- | --- |
| Prefab | PascalCase | `PlacedBlock.prefab` |
| Scene | PascalCase | `Gameplay.unity`, `Boot.unity` |
| ScriptableObject | PascalCase, có hậu tố loại | `EconomyConfig.asset` |
| Sprite / texture | `loai_ten_bienthe` (snake_case) | `btn_play_pressed.png`, `icon_coin.png` |
| Audio | `sfx_…`, `bgm_…` | `sfx_block_place.wav` |
| Material | `M_` + PascalCase | `M_Outline.mat` |
| Animation clip | `Doituong_Hanhdong` | `Cat_Idle.anim` |

- Không dấu tiếng Việt, không khoảng trắng trong tên file.

### 6.4 Code Editor

| Thành phần | Quy ước | Ví dụ |
| --- | --- | --- |
| Custom Inspector | `<TênClass>Editor` | `LevelDefinitionEditor` |
| Editor Window | `<TênFeature / việc>Window` | `LevelEditorWindow` |
| Property Drawer | `<TênType>Drawer` | `RewardDataDrawer` |
| Tool dựng asset / UI | `<Việc>Builder` | `ShopPopupBuilder` |
| Menu | `Tools/<Feature>/<Hành động>` | `Tools/Level/Validate All Levels` |

---

## 7. Bố cục một file / class

### 7.1 Định dạng code

| Quy tắc | Đúng | Sai |
| --- | --- | --- |
| **Một class / interface / struct / enum mỗi file**, tên file trùng tên type | `ShopService.cs` chứa `ShopService` | Một file chứa nhiều class |
| **Luôn ghi rõ access modifier** (`private`, `public`, `protected`, `internal`), kể cả khi mặc định là `private` | `private void Awake()` | `void Awake()` |
| **Ngoặc kiểu Allman**: `{` xuống dòng riêng | xem ví dụ dưới | `if (x) {` |
| **Luôn có `{}`**, kể cả `if` / `for` một dòng | `if (x)` ⏎ `{` ⏎ `return;` ⏎ `}` | `if (x) return;` |
| **Namespace dạng block**, không dạng file-scoped (Unity 6 có lỗi không nhận MonoBehaviour trong file-scoped namespace) | `namespace A.B { … }` | `namespace A.B;` |
| `var` chỉ khi type **thấy rõ** ở vế phải | `var cells = new List<CellView>();` | `var x = GetData();` |
| Property / method một biểu thức được viết dạng `=>` | `public int Lives => _lives;` | — |

### 7.2 Thứ tự trong class

```csharp
namespace MyGame.Features.Level
{
    public class LevelController : MonoBehaviour
    {
        // 1. Hằng số
        private const float StateChangeDelay = 0.8f;

        // 2. Field Inspector ([SerializeField] private), nhóm bằng [Header]
        [Header("References")]
        [SerializeField] private BoardView boardView;

        // 3. Field private
        private BoardModel _board;

        // 4. Event
        public event Action<int> LevelCompleted;

        // 5. Property
        public int CurrentLevel { get; private set; }

        // 6. Unity lifecycle theo thứ tự gọi: Awake, OnEnable, Start, Update, OnDisable, OnDestroy
        private void OnEnable()
        {
            _board.Solved += OnBoardSolved;
        }

        private void OnDisable()
        {
            _board.Solved -= OnBoardSolved;
        }

        // 7. Method public
        public void Init(ILevelSource source)
        {
            if (source == null)
            {
                Debug.LogError($"[{name}] Thiếu ILevelSource", this);
                return;
            }
            // ...
        }

        // 8. Method private (gồm hàm xử lý event)
        private void OnBoardSolved()
        {
            LevelCompleted?.Invoke(CurrentLevel);
        }
    }
}
```

### 7.3 Kích thước

- Giới hạn tham khảo: class ≤ **300 dòng** (> 500 bắt buộc tách), method ≤ **40 dòng**, tham số ≤ **4** (nhiều hơn thì gom vào struct).
- Lồng `if` tối đa 3 tầng. Dùng return sớm (guard clause).

---

## 8. Quy tắc riêng của Unity

### 8.1 Lifecycle

| Hàm | Làm gì |
| --- | --- |
| `Awake` | Khởi tạo **bản thân** từ field đã gán sẵn. Không gọi sang object khác, không `GetComponent` (mục 8.3) |
| `OnEnable` | **Đăng ký** event |
| `Start` | Dùng object khác (lúc này mọi `Awake` đã chạy) |
| `OnDisable` | **Hủy đăng ký** event, dừng tween / coroutine |
| `OnDestroy` | Giải phóng tài nguyên, xóa tham chiếu static |

- Đăng ký ở `OnEnable` thì hủy ở `OnDisable`. Đăng ký ở `Awake` thì hủy ở `OnDestroy`. **Luôn cặp đôi.**
- Class C# thuần nghe event → cài `IDisposable`, hủy đăng ký trong `Dispose()`.
- Không dựa vào thứ tự `Awake` giữa các object. Cần thứ tự → Bootstrap gọi `Init()` theo thứ tự rõ ràng. `[DefaultExecutionOrder]` chỉ dùng cho hạ tầng (Bootstrap chạy trước), không dùng để vá lỗi thứ tự giữa các feature.
- **Xóa hàm lifecycle rỗng** (`Update`, `Start`, `Awake`, `FixedUpdate`...): Unity vẫn gọi chúng và tốn chi phí dù rỗng.
- **Không viết constructor cho MonoBehaviour.** Unity gọi constructor cả lúc serialize trong Editor. Khởi tạo bằng `Awake` hoặc `Init(...)`.
- Object cần component khác trên cùng GameObject → khai báo `[RequireComponent(typeof(…))]` **và** vẫn gán qua `[SerializeField]` (mục 8.3).
- `Update` chỉ cho việc thật sự phải làm mỗi frame. Việc theo thời gian → tween / coroutine; việc theo sự kiện → event. Component không cần chạy thì `enabled = false`.

| Hàm | Dùng cho |
| --- | --- |
| `Update` | Input, logic mỗi frame không dính vật lý |
| `FixedUpdate` | Vật lý, di chuyển `Rigidbody` |
| `LateUpdate` | Camera đi theo, chỉnh sau khi mọi `Update` đã chạy |

### 8.2 Inspector và serialize

- Dùng `[SerializeField] private`, **không** `public` field chỉ để hiện trong Inspector.
- Property cần đọc từ ngoài: `public int Lives => lives;`.
- Đổi tên field đã serialize → thêm `[FormerlySerializedAs("tenCu")]` để không mất dữ liệu.
- Kiểm tra tham chiếu thiếu và giá trị sai trong `OnValidate` và `LogError` / `LogWarning` rõ ràng, đừng để `NullReferenceException` lúc chạy.
- Sắp xếp Inspector bằng `[Header]`, `[Tooltip]`, `[Range]`, `[Min]`.
- Property cần hiện trong Inspector: `[field: SerializeField] public float Speed { get; private set; }`.
- `[SerializeReference]` **chỉ** khi cần đa hình (field kiểu interface / class cha giữ được class con) hoặc cần giá trị `null`. Đổi tên type / namespace sau đó sẽ làm hỏng dữ liệu → thêm `[MovedFrom]`.
- **Unity không serialize:** field `static` / `const` / `readonly`, `Dictionary`, mảng nhiều chiều (`[,]`), mảng lồng (`[][]`), list lồng (`List<List<T>>`). Cần thì bọc trong class `[Serializable]` hoặc dùng list cặp key–value.
- `[NonSerialized]` để loại một field `public` khỏi serialize; `[HideInInspector]` chỉ ẩn, **vẫn** serialize.

### 8.3 Không tìm, không lấy, không gắn component lúc chạy

> **Quy tắc:** code chạy trong game (MonoBehaviour, class C# thuần, mọi lúc: `Awake`, `Start`, `Update`, khi spawn, khi bấm nút...) **không được** tự đi tìm hay tự gắn component / object.
> Mọi tham chiếu phải **có sẵn trước khi object chạy**: gán trong Inspector, hoặc được truyền vào qua `Init(...)`.

**Lý do:**
- **Phụ thuộc bị ẩn:** đọc field không biết class cần gì; thiếu thì chỉ lỗi lúc chạy (`NullReferenceException`), không lỗi lúc gán.
- **Dễ gãy:** đổi tên object, đổi cấu trúc con, đổi tag là hỏng mà compiler không báo.
- **Chậm:** `Find…` duyệt cả scene; `GetComponent` / `GetComponentInChildren` duyệt component và cây con; `AddComponent` cấp phát bộ nhớ và gây GC.
- **Khó test:** class tự tìm dependency thì không truyền bản giả vào được.

#### Danh sách API bị cấm trong code runtime

| Nhóm | API |
| --- | --- |
| Tìm trong scene | `GameObject.Find`, `GameObject.FindWithTag`, `GameObject.FindGameObjectsWithTag`, `Object.FindObjectOfType(s)`, `FindAnyObjectByType`, `FindFirstObjectByType`, `FindObjectsByType`, `Resources.FindObjectsOfTypeAll` |
| Tìm trong cây con | `transform.Find`, `transform.GetChild(i)` để lấy object cụ thể theo vị trí |
| Lấy component | `GetComponent`, `GetComponents`, `GetComponentInChildren(s)`, `GetComponentInParent(s)`, `TryGetComponent` |
| Gắn / gỡ component | `AddComponent`, `Destroy(component)` để đổi hành vi |
| Gián tiếp | `Camera.main` (bên trong là tìm theo tag), `SendMessage`, `BroadcastMessage` |

#### Thay bằng gì

| Cần | Làm |
| --- | --- |
| Component trên **cùng object** | `[SerializeField]` field, gán trong Inspector (tự điền bằng `Reset()`, xem dưới) |
| Component trên **object con** | `[SerializeField]` field / mảng trỏ thẳng vào object con |
| Object khác **trong scene** | `[SerializeField]` kéo trong Inspector, hoặc Bootstrap truyền vào qua `Init(...)` |
| Object **spawn lúc chạy** | Factory / Pool truyền dependency vào qua `Init(...)` ngay sau khi tạo |
| **Tất cả** object cùng loại | Nơi spawn tự giữ danh sách (`List<T>`), không đi tìm lại |
| Component từ **kết quả raycast / va chạm** | Registry `Dictionary<Collider2D, T>` do nơi spawn đăng ký lúc tạo, gỡ lúc trả về pool |
| **Thêm hành vi** lúc chạy | Prefab đã có sẵn component, bật / tắt (`enabled`, `SetActive`), hoặc đổi strategy (mục 3) |
| `Camera.main` | `[SerializeField] Camera cam` |

#### Tự điền tham chiếu trong Editor (không tốn gì lúc chạy)

`GetComponent` **được phép** trong `Reset()` và `OnValidate()` vì hai hàm này chạy trong Editor lúc chỉnh sửa, kết quả được **lưu vào scene / prefab**. Lúc chạy game không gọi lại.

```csharp
public class BlockView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Collider2D hitBox;

    public void Init(BlockModel model) { /* nhận dependency từ ngoài */ }

#if UNITY_EDITOR
    // Chạy khi gắn component hoặc bấm Reset trong Inspector
    private void Reset()
    {
        body = GetComponent<SpriteRenderer>();
        hitBox = GetComponent<Collider2D>();
    }

    // Báo lỗi ngay trong Editor nếu quên gán
    private void OnValidate()
    {
        if (!body)
        {
            Debug.LogError($"[{name}] Thiếu body", this);
        }
    }
#endif
}
```

#### Registry thay cho `GetComponent` trên kết quả raycast

```csharp
public class CellSpawner : MonoBehaviour
{
    private readonly Dictionary<Collider2D, CellView> _cellsByCollider = new();

    private CellView Spawn(CellModel model)
    {
        var view = _pool.Get();
        view.Init(model);
        _cellsByCollider[view.HitBox] = view;      // đăng ký 1 lần khi tạo
        return view;
    }

    private void Despawn(CellView view)
    {
        _cellsByCollider.Remove(view.HitBox);      // gỡ khi trả về pool
        _pool.Release(view);
    }

    public bool TryGetCell(Collider2D col, out CellView view) => _cellsByCollider.TryGetValue(col, out view);
}
```

#### Chỗ duy nhất được dùng

| Nơi | Được dùng | Điều kiện |
| --- | --- | --- |
| Script trong thư mục `Editor/` (tool, builder, window) | Tất cả | — |
| `Reset()`, `OnValidate()` bọc `#if UNITY_EDITOR` | `GetComponent…` | Chỉ để điền / kiểm tra field serialize |
| Unit test | Tất cả | Dựng scene test |

Ngoài bảng trên **không có ngoại lệ**. Gặp trường hợp tưởng như bắt buộc phải dùng → thiết kế lại theo bảng "Thay bằng gì"; vẫn không được thì ghi lý do vào `ProjectConventions.md` của dự án.

**Cách kiểm tra:** tìm trong code (trừ thư mục `Editor/` và `Tests/`) các từ `Find`, `GetComponent`, `TryGetComponent`, `AddComponent`, `Camera.main`, `SendMessage`. Mọi kết quả phải nằm trong `Reset` / `OnValidate`.

### 8.4 Hiệu năng

- **Mục tiêu GC: < 1 KB / frame** khi đang chơi, lý tưởng là 0 (xem cột *GC Alloc* trong Profiler).
- Không cấp phát bộ nhớ trong `Update` / vòng lặp nóng: không LINQ, không `new List`, không nối chuỗi, không lambda bắt biến. Cần ghép chuỗi thường xuyên → `StringBuilder` dùng lại.
- Tránh **boxing**: không truyền value type vào chỗ nhận `object` (`string.Format`, `ArrayList`, interface trên struct); dùng generic.
- Dùng API **NonAlloc** / nhận buffer: `Physics2D.OverlapCircleNonAlloc`, `Physics2D.Raycast(…, results)`.
- Tính toán dùng lại trong vòng lặp → tính một lần bên ngoài.
- Dữ liệu tạm, ngắn hạn, nhỏ → `struct` thay vì `class`.
- Không tìm / lấy / gắn component lúc chạy (mục 8.3).
- Spawn / despawn liên tục → Object Pool.
- So sánh tag bằng `CompareTag("Enemy")`, không `tag == "Enemy"`.
- Cập nhật UI theo event (khi giá trị đổi), không cập nhật mỗi frame.
- Cần thì đo bằng Profiler trước khi tối ưu.

### 8.5 ScriptableObject

- Dùng làm **dữ liệu chỉ đọc** lúc chạy (config, database).
- **Không ghi** vào ScriptableObject lúc chạy: trong Editor nó lưu lại thật, trên máy thì mất khi tắt app. Trạng thái chơi để trong Model / SaveData.

### 8.6 Null của Unity

- Object Unity đã `Destroy` vẫn `!= null` với `?.` và `??`. Với `UnityEngine.Object` dùng `if (obj)` hoặc `obj != null`, **không** dùng `?.` / `??`.

### 8.7 Khác

- Hạn chế `Resources/` (xem mục 9.3).
- `DontDestroyOnLoad` chỉ cho object hạ tầng, đặt trong scene Boot.
- Không dùng `SendMessage`, `Invoke("TenHam")` (dựa vào chuỗi, refactor không bắt được).
- Chuỗi dùng nhiều lần (key save, tên animation, tag) → hằng số hoặc `Animator.StringToHash`.

---

## 9. Asset và tối ưu

> Đo trước, tối ưu sau. Mọi thay đổi ở mục này phải so sánh được số liệu trước / sau (Frame Debugger, Profiler, Memory Profiler, Build Report).

### 9.1 Sprite Atlas: nên dùng

**Lợi ích:** sprite cùng atlas, cùng material vẽ chung một draw call (batch). Lợi nhất cho UI nhiều icon / nút và các object lặp lại nhiều (ô lưới, item).

**Chia atlas theo "cái gì xuất hiện cùng lúc trên màn hình"**, không gom tất cả vào một atlas. Unity nạp **cả texture atlas** khi chỉ cần một sprite trong đó, nên atlas trộn lẫn làm tốn RAM.

| Atlas mẫu | Chứa |
| --- | --- |
| `UI_Common` | Nút, khung, icon tiền tệ dùng ở mọi màn hình |
| `UI_<Màn hình>` | Thứ chỉ có ở một màn hình (Menu, Shop, Popup...) |
| `Gameplay` | Sprite trong màn chơi (ô, khối, vật cản, hiệu ứng nhỏ) |
| `<TínhNăng>` | Tính năng mở riêng, ít dùng (vòng quay, sự kiện) |

**Không đưa vào atlas:**
- Spine / animation có atlas riêng của nó.
- Background lớn (gần full màn hình): để texture riêng.
- Sprite đã được đóng sẵn thành sprite sheet.
- Sprite rất lớn làm atlas phình (> 1/4 kích thước atlas).

**Thiết lập:**

| Thiết lập | UI (`Image`) | World (`SpriteRenderer`) |
| --- | --- | --- |
| Allow Rotation | **Tắt** (UI không hỗ trợ sprite xoay) | Bật được |
| Tight Packing | **Tắt** (UI vẽ theo khung chữ nhật, có thể lộ sprite bên cạnh) | Bật được |
| Padding | 2–4 px | 2–4 px |
| Generate Mip Maps | Tắt | Tắt (game 2D) |
| Max Texture Size | 2048 (mobile) | 2048 (mobile) |

- Sprite dùng 9-slice vẫn đưa vào atlas được; set border trong Sprite Editor như bình thường.
- Một sprite chỉ nằm trong **một** atlas.
- Kiểm tra: Frame Debugger / Stats → số **Batches** phải giảm. Không giảm → xem lại cách chia hoặc thứ tự vẽ (sprite khác atlas chen giữa làm vỡ batch).

### 9.2 Texture: import và nén

| Thiết lập | Quy tắc |
| --- | --- |
| Texture Type | `Sprite (2D and UI)` cho sprite; `Default` cho texture 3D / shader |
| Max Size | Vừa đủ kích thước hiển thị thật trên màn hình lớn nhất, không để mặc định 2048 cho icon 64 px |
| Compression (Android / iOS) | **ASTC** (6x6 cân bằng; 4x4 cho UI cần nét; 8x8 cho background) |
| Mip Maps | Tắt cho UI và game 2D camera cố định |
| Read/Write | Tắt (bật làm tốn gấp đôi RAM), trừ khi code đọc pixel |
| Kích thước | Nên là bội của 4 để nén ASTC / ETC không bị lỗi viền |
| Filter Mode | Bilinear; Point chỉ cho pixel art |

- Đặt quy tắc chung bằng **Preset** (Project Settings → Preset Manager) theo thư mục để không phải chỉnh tay từng file.
- Audio: nhạc nền `Streaming`, SFX ngắn `Decompress On Load`; nén Vorbis; mono cho SFX nếu không cần stereo.

### 9.3 `Resources/`: hạn chế tối đa

- **Mọi thứ** trong `Resources/` luôn bị đưa vào build, dù có dùng hay không.
- Nạp một asset trong `Resources/` kéo theo **mọi asset nó tham chiếu**. Một config gốc nằm trong `Resources/` mà trỏ tới sprite / prefab nặng thì toàn bộ bị nạp lúc khởi động.
- Chỉ đặt vào `Resources/` thứ phải nạp bằng tên mà không có cách khác (config gốc nhỏ, settings của plugin). Config gốc chỉ trỏ tới config con nhẹ, không trỏ thẳng tới asset nặng.
- Còn lại: tham chiếu qua Inspector / ScriptableObject.

### 9.4 Addressables: khi nào dùng

Addressables là hệ thống nạp asset theo địa chỉ, bất đồng bộ, có thể tải từ server và tự giải phóng.

| ✅ Dùng khi | ❌ Chưa cần khi |
| --- | --- |
| Có nội dung tải từ server (DLC, cập nhật level / sự kiện không cần ra bản mới) | Không có server / nội dung tải về |
| Bản build quá lớn, cần tách phần nội dung để tải sau (giới hạn store) | Build nhỏ (< ~150 MB) |
| Nhiều nội dung nặng chỉ cần lúc dùng rồi giải phóng (skin, theme, chương, map) | Một hoặc vài scene, asset gần như luôn nằm trong RAM |
| Nhiều scene lớn cần quản lý bộ nhớ chặt | Prototype, game jam, dự án nhỏ |

**Cái giá phải trả:** mọi chỗ nạp asset chuyển sang bất đồng bộ (`await` / callback), phải tự `Release` đúng lúc (quên là rò bộ nhớ, release sớm là mất hình), thêm bước build content, debug khó hơn.

**Cách áp dụng nếu dùng:**
- Bắt đầu từ **một phạm vi nhỏ, rõ ràng** (ví dụ: background theo theme, level pack), không chuyển cả dự án một lần.
- Bọc trong một service (`IAssetLoader`) để phần còn lại của code không biết là Addressables hay tham chiếu trực tiếp.
- Mỗi `LoadAssetAsync` có đúng một `Release` tương ứng, đặt ở nơi sở hữu asset đó.
- Sprite Atlas vẫn dùng chung được với Addressables (đưa cả atlas vào cùng group với sprite của nó).

### 9.5 Kiểm tra dung lượng và bộ nhớ

- **Build Report:** sau khi build, xem `Editor.log` (mục *Build Report*) để biết asset nào chiếm dung lượng thật. Asset không được tham chiếu thì không vào build, nên thư mục nặng trong project chưa chắc làm nặng build.
- **Memory Profiler** (package): xem texture nào chiếm RAM lúc chạy.
- **Frame Debugger:** xem số draw call và vì sao batch bị vỡ.
- Kiểm tra trên **máy thật** (mobile tầm thấp), không chỉ trong Editor.

---

## 10. Prefab và UI

### 10.1 Cái gì nên là prefab

> Mặc định: **mọi UI có cấu trúc và có thể sửa riêng** đều là prefab. Scene chỉ chứa khung (Camera, Canvas gốc, EventSystem, Bootstrap) và **instance** của các prefab.

| Nhóm | Ví dụ | Bắt buộc? |
| --- | --- | --- |
| **Object sinh ra lúc chạy** | Mục trong danh sách (thẻ shop, ô phần thưởng, ô ngày), object gameplay, VFX | **Bắt buộc** |
| **Widget dùng chung** | Nút chính / phụ / đóng, ô hiện tiền tệ, toggle, tab, badge, thanh tiến độ | **Bắt buộc**, làm đầu tiên |
| **Popup** | Win, Lose, Settings, Shop, Reward, Notice... | Nên (mỗi popup 1 prefab) |
| **Màn hình / tab** | Home, Shop, Event, Profile... | Nên (mỗi màn hình 1 prefab) |
| **HUD** | Thanh thời gian, mạng, hàng nút kỹ năng / booster | Nên |
| **Nhóm hệ thống** | Bootstrap + các service | Nên nếu có **nhiều scene** dùng chung |

**Không cần prefab:**
- Thứ chỉ có một, đơn giản và gắn với scene: Camera, Canvas gốc, EventSystem, light.
- Container chỉ để sắp xếp layout (một `VerticalLayoutGroup` rỗng).

**Lợi ích:** mỗi UI là một file riêng (diff gọn, ít conflict scene), sửa trong Prefab Mode không phải mò Hierarchy dài, sửa widget một chỗ thì mọi nơi cập nhật, mang sang scene / dự án khác được.

### 10.2 Nested Prefab và Prefab Variant

| Dùng | Khi |
| --- | --- |
| **Nested** (prefab lồng prefab) | Ghép widget vào popup / màn hình. Ví dụ `ShopPopup` chứa `CloseButton`, `CurrencyPill` |
| **Variant** | Cùng cấu trúc, **khác hình thức**: `Button_Primary` → `Button_Green`, `Button_Red`; `PopupFrame` → `PopupFrame_Small` |
| **Prefab mới** | Khác cấu trúc hoặc khác logic |

- Variant chỉ khác **hình thức / số liệu**, không dùng Variant để có logic khác.
- Lồng tối đa **3 tầng** (màn hình → nhóm → widget). Sâu hơn khó biết sửa ở đâu.
- **Override trên instance càng ít càng tốt.** Instance trong scene chỉ nên override: vị trí, tham chiếu ra ngoài, bật / tắt. Phải override nhiều → tạo Variant hoặc sửa prefab gốc.
- Sửa prefab gốc thì sửa trong **Prefab Mode**, không sửa instance rồi `Apply All` (dễ đẩy nhầm override lên).

### 10.3 Prefab phải tự đủ

- Tham chiếu `[SerializeField]` **bên trong** prefab chỉ trỏ tới object con của chính nó.
- Thứ **bên ngoài** (service, model, popup khác) được truyền vào qua `Init(...)` hoặc do nơi quản lý gán; không tự tìm (mục 8.3).
- Script View chính đặt ở **object gốc** của prefab, tên prefab trùng tên script: `ShopPopup.prefab` ↔ `ShopPopup.cs`.
- Mở prefab một mình trong Prefab Mode phải không có tham chiếu thiếu (`None`) nào ngoài những field được ghi rõ là "gán từ ngoài".

### 10.4 Đặt sẵn hay Instantiate

| Cách | Khi |
| --- | --- |
| **Đặt sẵn instance trong scene, tắt; mở thì bật** | Popup / màn hình **dùng thường xuyên**, số lượng ít, dự án một vài scene. Đơn giản, không trễ lần mở đầu. **Mặc định** |
| **Instantiate khi mở, Destroy hoặc giữ lại khi đóng** | Popup **hiếm dùng và nặng** (nhiều ảnh lớn), hoặc số popup rất nhiều. Tiết kiệm RAM, đổi lại trễ lần đầu |
| **Pool** | Mục trong danh sách, object gameplay lặp lại |

### 10.5 Canvas

- Mỗi khi một phần tử UI đổi (vị trí, màu, chữ, bật / tắt), Unity **dựng lại cả Canvas** chứa nó.
- Tách **Canvas con** (nested Canvas) cho:
  - Mỗi popup / màn hình.
  - Phần thay đổi liên tục (đồng hồ, thanh thời gian, số chạy) tách khỏi phần tĩnh.
- Tắt **Raycast Target** trên ảnh / chữ không cần bấm.
- Canvas Scaler: `Scale With Screen Size`, chọn một độ phân giải tham chiếu cho cả dự án, `Match` cố định.
- Ẩn popup bằng `SetActive(false)` hoặc tắt `Canvas` component; `CanvasGroup.alpha = 0` vẫn tốn chi phí vẽ và raycast nếu không tắt `blocksRaycasts`.

### 10.6 Base Popup

Các popup có chung hành vi (mở / đóng, hiệu ứng, chặn bấm xuyên, âm thanh) → dùng **một class cha** cho popup. Đây là chỗ kế thừa hợp lý (mục 2, "L"): quan hệ IS-A rõ, dùng chung code, chỉ 1 tầng.

**Base popup chịu trách nhiệm:**
- `Open()` / `Close()` / `CloseImmediate()`, trạng thái `IsOpen`.
- Hiệu ứng mở / đóng mặc định (chạy theo thời gian thực, không phụ thuộc `timeScale`).
- Chặn bấm xuyên (nền mờ, `CanvasGroup.blocksRaycasts`).
- Hook cho lớp con: `OnOpened()`, `OnClosed()`.
- Event `Closed` để nơi mở biết popup đã đóng.

**Base popup không được biết:**
- Luật game, level, tiền, booster... (đó là việc của lớp con hoặc của nơi quản lý popup).
- Popup khác.

**Lớp con:**
- Có hàm riêng nhận dữ liệu rồi gọi `Open()`: `winPopup.Show(stars, reward)`. Không làm `PopupView<TData>` generic trừ khi thật sự cần.
- Không override `Open()` / `Close()`; dùng hook.
- Tối đa **1 tầng** kế thừa dưới base (không `BaseRewardPopup` → `DailyRewardPopup` → ...). Phần dùng chung giữa vài popup → tách thành widget / component.

```csharp
public abstract class PopupView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;          // gán sẵn, không GetComponent
    [SerializeField] private RectTransform panel;

    public bool IsOpen { get; private set; }
    public event Action<PopupView> Closed;

    public void Open()  { /* bật, hiệu ứng mở, IsOpen = true, OnOpened() */ }
    public void Close() { /* hiệu ứng đóng, tắt, IsOpen = false, OnClosed(), Closed?.Invoke(this) */ }

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }

#if UNITY_EDITOR
    private void Reset() => group = GetComponent<CanvasGroup>();
#endif
}

public class WinPopup : PopupView
{
    [SerializeField] private StarRowView stars;

    public void Show(int starCount, int coinReward)
    {
        stars.Set(starCount);
        Open();
    }
}
```

**Hiệu ứng khác nhau giữa các popup:** khi đã có ≥ 2–3 kiểu, tách hiệu ứng thành component riêng (`PopupScaleAnim`, `PopupSlideAnim`) mà base gọi tới (Strategy), không làm thêm tầng kế thừa. Chỉ một kiểu thì để trong base.

### 10.7 Quản lý popup (khi nào cần)

| Cần | Làm |
| --- | --- |
| Vài popup, không chồng lên nhau | Nơi điều khiển (flow / screen controller) giữ tham chiếu và gọi `Show(...)` trực tiếp. **Không cần manager** |
| Popup chồng lên nhau, nút Back (Android) đóng popup trên cùng | Một `PopupStack` nhỏ: `Push` khi mở, `Pop` khi đóng, Back đóng popup trên cùng |
| Nhiều popup cùng muốn hiện (thưởng ngày + lên cấp + sự kiện) | Hàng đợi: hiện lần lượt, cái sau chờ cái trước `Closed` |

Bắt đầu từ dòng đầu; chỉ thêm stack / hàng đợi khi thật sự gặp vấn đề tương ứng.

### 10.8 Thư mục prefab

Prefab nằm theo module sở hữu nó (mục 5):

| Prefab | Đặt ở |
| --- | --- |
| Widget dùng chung (nút, ô tiền tệ, toggle, tab, badge), `PopupFrame` | `Shared/Prefabs/Widgets/` |
| Popup, màn hình / tab, mục danh sách, object gameplay, VFX **của một feature** | `Features/<Tên>/Prefabs/` (chia tiếp `Popups/`, `Items/`, `VFX/` nếu nhiều) |
| Màn hình tổng ghép từ nhiều feature (MainMenu, HUD màn chơi) | `Game/Prefabs/` |

- Prefab của feature A **không lồng** prefab của feature B (vi phạm mục 4.1). Màn hình cần cả hai → ghép ở `Game/Prefabs/`.
- Prefab của feature được lồng widget từ `Shared/`.

### 10.9 UI sinh bằng code Editor

Nếu dùng script Editor để dựng UI (builder): builder phải **xuất ra prefab** (`PrefabUtility.SaveAsPrefabAsset`) rồi scene dùng instance, không dựng thẳng vào scene. Như vậy chỉnh tay trong prefab sau đó vẫn giữ được, và chạy lại builder không phá scene.

---

## 11. Animation, async, thời gian

- Chọn **một** cách làm chính cho animation code trong dự án (DOTween **hoặc** Coroutine **hoặc** async) và thống nhất.
- Nếu dùng DOTween:
  - Giữ tween trong field (`Tween` / `Sequence`), gắn `SetLink(gameObject)`.
  - `Kill()` trong `OnDisable`, khi phát lại, khi trả về pool.
  - Trạng thái cần trả về gốc khi bị hủy → xử lý trong `OnKill`.
  - UI chạy cả khi pause → `SetUpdate(true)`.
- Coroutine: giữ `Coroutine` trong field để `StopCoroutine`; cache `WaitForSeconds` dùng lại.
- Thời gian dựa vào giờ thật (phần thưởng ngày, hồi năng lượng) → đi qua một `IClock` / service thời gian, không gọi `DateTime.Now` rải rác.

---

## 12. Lỗi, log, comment

### Lỗi và log

- Lỗi cấu hình (thiếu tham chiếu, thiếu dữ liệu) → `Debug.LogError` kèm tên object: `Debug.LogError($"[{name}] Thiếu boardView", this)`.
- Không nuốt exception bằng `catch { }` rỗng.
- Log gắn tiền tố hệ thống: `[Save]`, `[Ads]`.
- Log debug nhiều → bọc trong `[Conditional("DEBUG")]` hoặc tắt ở bản build.

### Comment

- Code tự giải thích bằng tên. Comment giải thích **tại sao**, không giải thích **làm gì**.
- Bắt buộc comment cho: quyết định không hiển nhiên, workaround bug Unity / SDK, con số "ma thuật".
- `// TODO:` phải kèm việc cần làm cụ thể. Không để code cũ bị comment lại, đã có git.
- Ngôn ngữ comment: thống nhất một ngôn ngữ trong cả dự án.

---

## 13. Test

- Ưu tiên test **logic C# thuần** (Model, solver, kinh tế, sinh level) bằng Unity Test Framework (EditMode). Không cần scene, chạy nhanh.
- Không cố test View / MonoBehaviour trừ khi thật cần.
- Đây là lý do chính để tách Model ra khỏi MonoBehaviour và để có interface cho service.
- Bug đã sửa ở logic → thêm 1 test cho nó.

### Vị trí và assembly

```text
Features/<Tên>/Tests/
├── Editor/        Unit test (EditMode)       → <Assembly>.<Tên>.UnitTests.asmdef         (Editor only)
└── Runtime/       Integration test (PlayMode) → <Assembly>.<Tên>.IntegrationTests.asmdef  (Any Platform)
```

- `<Assembly>` là assembly chứa code được test (`<Project>.Gameplay`, `<Project>.Meta`...), ví dụ `MyGame.Meta.Shop.UnitTests`.
- Assembly test reference assembly cần test + `UnityEngine.TestRunner` (+ `UnityEditor.TestRunner` cho EditMode).
- Chỉ tạo `Runtime/` khi thật sự có test cần chạy qua nhiều frame / cần scene.

### Đặt tên

| Thành phần | Quy ước | Ví dụ |
| --- | --- | --- |
| Class test | `<TênClass>Tests` | `CoinWalletTests` |
| Method test | `<Method>_<TìnhHuống>_<KếtQuảMongĐợi>` | `TrySpend_NotEnoughCoins_ReturnsFalse` |
| Đối tượng được test | `sut` (system under test) | `var sut = new CoinWallet(save);` |

### Cách viết

- Chia 3 đoạn **Arrange / Act / Assert** (chuẩn bị / làm / kiểm tra).
- Ưu tiên `[Test]`; chỉ dùng `[UnityTest]` khi phải chờ qua frame.
- Dependency bên ngoài (save, thời gian, quảng cáo) → truyền bản giả qua interface.

```csharp
[Test]
public void TrySpend_NotEnoughCoins_ReturnsFalse()
{
    // Arrange
    var sut = new CoinWallet(new FakeSaveService());
    sut.Add(50);

    // Act
    bool result = sut.TrySpend(100);

    // Assert
    Assert.IsFalse(result);
    Assert.AreEqual(50, sut.Balance);
}
```

---

## 14. Git và commit

### Nhánh

- `main`: luôn chạy được.
- Làm việc trên nhánh: `<loai>/<mo-ta-ngan>`, cùng bộ loại với commit: `feat/booster-freeze`, `fix/timer-pause`, `refactor/remove-level-singleton`, `chore/update-dotween`, `docs/coding-guidelines`.

### Commit message

```text
<loai>(<pham-vi>): <mo ta ngan>
```

| Loại | Dùng khi |
| --- | --- |
| `feat` | Tính năng mới |
| `fix` | Sửa bug |
| `refactor` | Đổi cấu trúc, không đổi hành vi |
| `perf` | Tối ưu hiệu năng |
| `docs` | Tài liệu |
| `style` | Chỉ format code, không đổi logic |
| `test` | Thêm / sửa test |
| `chore` | Cấu hình, package, việc vặt |
| `art` / `audio` | Chỉ đổi asset |

**Phạm vi (scope):** tên feature (`shop`, `booster`, `level`) hoặc nhóm chung: `core`, `shared`, `game`, `ui`, `audio`, `gfx`, `scene`, `editor`.

Ví dụ: `feat(booster): thêm booster Freeze`, `refactor(level): bỏ Singleton LevelManager`.

Commit lớn → thêm thân message (sau một dòng trống) giải thích **tại sao**:

```text
feat(economy): giới hạn năng lượng hồi tự động ở 20

Người chơi treo máy qua đêm được quá nhiều năng lượng.
EnergyService kiểm tra trần trước khi cộng phần hồi theo thời gian.
```

### Quy tắc

- Một commit = một thay đổi có ý nghĩa. **Không trộn refactor với tính năng mới** trong cùng commit.
- Luôn commit file `.meta` đi kèm asset.
- Unity: Version Control mode = **Visible Meta Files**, Asset Serialization = **Force Text**.
- Tạo / đổi tên / di chuyển asset **trong Unity** để `.meta` (GUID) đi theo; thiếu `.meta` là mất tham chiếu.
- Cấu hình **UnityYAMLMerge** (Smart Merge) làm công cụ merge cho `.unity`, `.prefab`, `.asset` để giảm conflict.

**`.gitignore` tối thiểu:**

```text
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
*.csproj
*.sln
*.suo
*.user
*.userprefs
.DS_Store
.vscode/
.idea/
```

**Git LFS** cho file nhị phân lớn (khoảng > 500 KB): `*.psd`, `*.png`, `*.jpg`, `*.tga`, `*.exr`, `*.wav`, `*.mp3`, `*.ogg`, `*.mp4`, `*.mov`, `*.fbx`, `*.blend`, `*.pdf`, `*.dll`. Bật LFS **từ đầu dự án**; chuyển sau khi đã có lịch sử thì phải viết lại lịch sử.

---

## 15. Checklist trước khi commit

**Kiểm tra tự động** (làm một lần khi tạo dự án, sau đó chạy mỗi lần build):
- Cài package **Microsoft.Unity.Analyzers** + file `.editorconfig` ở gốc dự án để IDE cảnh báo vi phạm quy tắc (access modifier, ngoặc, đặt tên, hàm Unity rỗng...).
- **Project Auditor** (package của Unity) để rà script, asset, project settings.
- `.asmdef` (mục 4.1) bắt lỗi feature tham chiếu nhau ngay lúc compile.

**Thiết kế**
- [ ] Class mới có đúng một trách nhiệm?
- [ ] Thêm loại mới có phải sửa `switch` cũ không? Nếu ≥ 3 loại → cân nhắc Strategy.
- [ ] Feature này có `using` / tham chiếu / lồng prefab của feature khác không? (Không được, mục 4.1. Feature meta chung assembly nên compiler không bắt, phải tự kiểm tra.)
- [ ] Giao tiếp giữa feature chỉ qua event hoặc interface trong `Core/Contracts` (mục 4.2)?
- [ ] `Core/Contracts` chỉ có định nghĩa, không có logic?
- [ ] File mới nằm đúng module theo bảng "Đặt file ở đâu" (mục 5)?
- [ ] Có thêm `.Instance` mới cho game logic không? (Không được.)
- [ ] Interface / event / factory mới trả lời được "để làm gì" không? (Không → bỏ.)

**Unity**
- [ ] Mọi event đăng ký đều có hủy đăng ký tương ứng?
- [ ] Tween / coroutine được dừng khi disable?
- [ ] Không có `Find…` / `GetComponent…` / `AddComponent` / `Camera.main` trong code runtime (chỉ trong `Editor/`, `Reset`, `OnValidate`)?
- [ ] Không ghi vào ScriptableObject lúc chạy?
- [ ] Field Inspector là `[SerializeField] private`?
- [ ] Đổi tên field serialize có `FormerlySerializedAs`?

**Asset**
- [ ] Sprite mới đã nằm đúng atlas (hoặc có lý do không cần atlas)?
- [ ] Texture mới có Max Size / nén / mipmap / Read-Write đúng mục 9.2?
- [ ] Không thêm asset nặng vào `Resources/` hoặc vào config nằm trong `Resources/`?
- [ ] UI mới là prefab (mục 10.1), tự đủ tham chiếu (10.3), override trên instance chỉ ở mức tối thiểu (10.2)?

**Sạch sẽ**
- [ ] Không còn `Debug.Log` thử nghiệm, code bị comment, file tạm.
- [ ] Tên đúng quy ước mục 6; định dạng đúng mục 7.1 (access modifier, `{}`, namespace block).
- [ ] Không còn hàm lifecycle rỗng.
- [ ] Logic mới có test nếu là class C# thuần quan trọng (mục 13).
- [ ] Console không có warning / error mới.
- [ ] Scene / prefab bị đổi đúng là thứ mình định đổi.
- [ ] Đã chạy thử luồng chính trong Play mode.

---

## 16. Code dùng lại giữa các dự án

> Mục tiêu: những gì đã viết tốt một lần (event bus, pool, save, audio, popup base...) **không phải viết lại** ở dự án sau, và sửa bug một chỗ thì mọi dự án được hưởng.

### 16.1 Cái gì mang đi được

Một đoạn code **mang đi được** khi trả lời "có" cho cả 3 câu:

1. Không biết gì về game này (không `using` Contracts, Gameplay, Meta, Game; không nhắc tới level, booster, coin...)?
2. Dự án game khác (thể loại khác) cũng sẽ cần nó gần như nguyên vẹn?
3. Cấu hình được từ ngoài (Inspector, tham số, ScriptableObject) thay vì sửa code?

| ✅ Thường mang đi được | ❌ Không mang đi |
| --- | --- |
| Event bus, Object Pool, Save (`ISaveService` + cài đặt), Audio service, đồng hồ / thời gian server, Haptics, Frame rate, Util / Extension | `Core/Contracts` (event, type riêng của game) |
| **Hành vi** UI: popup base, safe area, hiệu ứng bấm nút, scroll lồng, chữ hiện dần | **Prefab và art** UI (style riêng từng game) |
| Cheat panel / debug console, service giả lập Ads / IAP / Analytics | Luật game, solver, sinh level, config cân bằng |
| Tool Editor chung (kiểm tra tham chiếu thiếu, dựng Sprite Atlas, đổi tên hàng loạt) | Builder dựng UI / level riêng của game |

### 16.2 Hai bước: thư mục trước, package sau

| Bước | Khi nào | Làm gì |
| --- | --- | --- |
| **1. Thư mục cách ly** | **Ngay từ đầu** mọi dự án | Đặt trong `Core/Foundation/` với `.asmdef` riêng, không reference assembly nào của game (mục 4.1). Sửa trực tiếp như code thường |
| **2. Package UPM** | Khi **dự án thứ hai thật sự cần** | Tách ra git repo riêng thành package; các dự án cài qua `manifest.json` |

- **Không tách package ngay từ đầu:** chưa có dự án thứ hai thì chưa biết phần nào thật sự dùng chung; sửa code trong package chậm hơn (đổi phiên bản, cập nhật ở dự án).
- Bước 1 gần như miễn phí vì quy tắc phụ thuộc một chiều (mục 4.1) đã bắt buộc Foundation không biết game.
- Code chỉ "có thể sau này dùng lại" nhưng đang dính game → **để nguyên trong game**, đừng cố tổng quát hóa trước (luật ba lần, mục 1).

### 16.3 Cấu trúc package

```text
unity-foundation/                       Git repo riêng
├── package.json
├── README.md                           Cài đặt, danh sách module, ví dụ dùng
├── CHANGELOG.md                        Thay đổi theo từng phiên bản
├── LICENSE.md
├── Runtime/
│   ├── Core/                           Event bus, pool, save, util  → <Org>.Foundation.asmdef
│   ├── UI/                             Popup base, safe area...      → <Org>.Foundation.UI.asmdef
│   └── DOTween/                        Code cần DOTween              → <Org>.Foundation.DOTween.asmdef
├── Editor/                             → <Org>.Foundation.Editor.asmdef (Editor only)
├── Tests/
│   ├── Editor/                         → <Org>.Foundation.UnitTests.asmdef
│   └── Runtime/
└── Samples~/                           Scene / script mẫu (dấu ~ để Unity không import, người dùng tự thêm qua Package Manager)
```

```json
{
  "name": "com.<org>.foundation",
  "version": "1.0.0",
  "displayName": "<Org> Foundation",
  "unity": "2022.3",
  "description": "Event bus, pool, save, audio, UI helpers dùng chung.",
  "dependencies": {}
}
```

- Tên package: `com.<tên-bạn>.<tên-module>`, chữ thường, gạch ngang.
- Namespace: `<Org>.Foundation`, `<Org>.Foundation.UI`... **không** dùng tên dự án game.
- Package **không phụ thuộc** dự án nào. Dự án phụ thuộc package, không có chiều ngược lại.
- **Thư viện không phải UPM** (DOTween, SDK quảng cáo...): code dùng chúng tách vào assembly con riêng; trong `.asmdef` dùng **Define Constraints** (ví dụ `DOTWEEN`) hoặc **Version Defines** để assembly đó chỉ compile khi dự án có thư viện. Dự án không cài DOTween vẫn dùng được phần còn lại.
- Package không chứa art / prefab mang style riêng; chỉ prefab trung tính trong `Samples~`.

### 16.4 Cài đặt và phiên bản

Cài vào dự án qua `Packages/manifest.json`, **luôn ghim phiên bản bằng git tag**:

```json
"com.<org>.foundation": "https://github.com/<user>/unity-foundation.git#v1.2.0"
```

Đánh phiên bản theo **SemVer** `MAJOR.MINOR.PATCH`:

| Tăng | Khi |
| --- | --- |
| `PATCH` (1.2.0 → 1.2.1) | Sửa bug, không đổi API |
| `MINOR` (1.2.0 → 1.3.0) | Thêm tính năng, code cũ vẫn chạy |
| `MAJOR` (1.2.0 → 2.0.0) | Đổi / xóa API, dự án dùng phải sửa code |

- Mỗi phiên bản: cập nhật `version` trong `package.json` + ghi `CHANGELOG.md` + tạo git tag `vX.Y.Z`.
- Không trỏ thẳng vào nhánh (`#main`): dự án sẽ tự đổi code khi package đổi, khó tái hiện bug.

**Sửa package trong lúc làm dự án:**
- Clone repo package ra cạnh dự án, trong `manifest.json` đổi tạm sang đường dẫn local: `"com.<org>.foundation": "file:../../unity-foundation"`. Sửa, test ngay trong dự án.
- Xong → commit + tag trong repo package → trả `manifest.json` về URL git với tag mới. **Không commit** `manifest.json` đang trỏ `file:`.

### 16.5 Checklist trước khi đưa code vào package

- [ ] Trả lời "có" cả 3 câu ở mục 16.1?
- [ ] Không còn `using` tới namespace của game?
- [ ] Mọi giá trị riêng của game (âm thanh, thời gian, màu...) đã thành tham số / field Inspector?
- [ ] Có test cho phần logic (mục 13)?
- [ ] Có ví dụ dùng trong `README.md` hoặc `Samples~`?
- [ ] Đã dùng thật ở ít nhất 2 dự án (hoặc chắc chắn sẽ dùng ngay ở dự án thứ hai)?

---

## 17. Nguồn tham khảo

| Nguồn | Dùng cho |
| --- | --- |
| *Head First Design Patterns* (Freeman) | Nguyên tắc thiết kế, Strategy, Observer, Factory, State, Command, Singleton |
| *Game Programming Patterns* (Robert Nystrom) – gameprogrammingpatterns.com | Pattern riêng cho game: Component, Object Pool, Game Loop, Event Queue; phê bình Singleton |
| *Clean Code* (Robert C. Martin) | Đặt tên, hàm ngắn, SOLID |
| Unity e-book *Level up your code with design patterns and SOLID* | Áp dụng SOLID và pattern trong Unity |
| Unity e-book *Create a C# style guide* | Quy ước đặt tên và format C# trong Unity |
| Microsoft *C# Coding Conventions* | Quy ước C# chuẩn |
| Unity Manual: *Creating custom packages*, *Assembly definitions* | Cấu trúc package UPM, `.asmdef`, Define Constraints |
| semver.org | Đánh phiên bản package |
