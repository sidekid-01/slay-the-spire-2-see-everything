# C# 完整学习笔记

*从零基础到读懂杀戮尖塔2源码*

---

## 📍 当前学习进度

| 阶段 | 内容 | 状态 |
|------|------|------|
| 第一阶段 | 字段、方法、构造函数 | ✅ 已完成 |
| 第二阶段 | 属性、访问修饰符、封装 | ✅ 已完成 |
| 第三阶段 | 继承、多态、抽象类、接口 | ✅ 已完成 |
| 第四阶段 | 静态类、泛型、Lambda | ✅ 已完成 |
| 第五阶段 | LINQ、IEnumerable、惰性求值 | ✅ 已完成 |
| 下一步 | 委托/事件、async/await、反射 | 🔜 进行中 |

---

# 第一部分：C# 基础知识（已掌握）

## 1. ref / out / in 关键字

这三个关键字控制方法参数的传递方式，决定传入的是「副本」还是「原件」。

### out — 输出参数

- 调用前不需要赋值，方法内必须赋值
- 用途：返回多个值，如 TryParse

```csharp
bool success = int.TryParse("123", out int result);
Console.WriteLine(success); // True
Console.WriteLine(result);  // 123
```

### ref — 双向传递

- 调用前必须赋值，方法内可读可改
- 传的是原件，修改会影响原始变量

```csharp
void Double(ref int num) { num *= 2; }
int value = 5;
Double(ref value);
Console.WriteLine(value); // 10
```

### in — 只读传递

- 传的是原件但不允许修改
- 适合大型结构体，避免复制开销

---

## 2. 值类型 vs 引用类型（堆与栈）

### 值类型（栈）— 图纸/副本

- int、bool、float、struct、enum
- 赋值时复制数据，互不影响

```csharp
int a = 100; 
int b = a; 
b = 999; 
// a 还是 100
```

### 引用类型（堆）— 真品/原件

- class、数组、List、string
- 赋值时共享同一份数据

```csharp
int[] a = {1,2,3}; 
int[] b = a; 
b[0] = 999; 
// a[0] 也变成 999
```

---

## 3. 面向对象三大支柱

### 封装（Encapsulation）

private 字段 + public 属性保护数据

```csharp
private int hp;
public int HP { 
    get { return hp; } 
    set { if(value<0) value=0; hp=value; } 
}
```

### 继承（Inheritance）

子类自动拥有父类所有 public/protected 成员

```csharp
class Gilgamesh : Hero { ... }
```

### 多态（Polymorphism）

父类用 virtual，子类用 override 重写方法

```csharp
Hero[] heroes = { new Gilgamesh(), new Archer() };
foreach(Hero h in heroes) h.Attack(); // 各自执行自己的版本
```

---

## 4. 抽象类 vs 接口

| 特性 | 抽象类 abstract | 接口 interface |
|------|----------------|----------------|
| 关系 | 是什么（is-a） | 能做什么（can-do） |
| 数量限制 | 只能继承一个 | 可以实现多个 |
| 有普通方法 | ✅ 可以有 | ❌ 不能有 |
| 有字段 | ✅ 可以有 | ❌ 不能有 |
| 命名习惯 | 正常命名 | I 开头（IFlyable） |

---

## 5. 静态类和静态方法（static）

- 不需要创建对象，直接用类名调用
- 静态字段是所有对象共享的数据

```csharp
static class DamageCalculator {
    public static int CalcDamage(int atk, int def) => atk - def;
}
DamageCalculator.CalcDamage(500, 200); // 300
```

---

## 6. 泛型（Generic）

- List<T> 中的 T 是占位符，使用时指定类型
- List<CardModel> 可以装所有 CardModel 子类

```csharp
List<CardModel> deck = new List<CardModel>();
deck.Add(new StrikeSilent()); // 子类可以装进父类 List
```

---

## 7. Lambda 表达式

=> 就是 return 的简写

```csharp
public int Damage => 6;           // 等价于 get { return 6; }
public bool IsAlive => HP > 0;    // 等价于 get { return HP > 0; }
```

---

# 第二部分：LINQ 与集合操作（本次新学）

## 8. LINQ 基础概念

LINQ（Language Integrated Query）是 C# 最强大的集合操作工具，让你可以轻松筛选、排序、统计数据。

**比喻理解：**
- 没有 LINQ = 自己一张张翻卡牌找打击卡
- 有 LINQ = 喊一声"把所有打击卡拿出来"，自动帮你找好

---

## 9. LINQ 最常用的 6 个方法

### 1️⃣ Where — 筛选符合条件的元素

```csharp
// 找出手牌中所有攻击卡
List<CardModel> hand = GetHandCards();
List<CardModel> attackCards = hand.Where(card => card.Type == CardType.Attack).ToList();

// 找出费用≤2的卡
var cheapCards = hand.Where(card => card.Cost <= 2).ToList();
```

### 2️⃣ Select — 提取/转换元素

```csharp
// 获取所有卡牌的名字列表
List<string> cardNames = deck.Select(card => card.Name).ToList();

// 把卡牌转换成费用+名字的字符串
var cardInfos = hand.Select(card => $"{card.Cost}费 - {card.Name}").ToList();
```

### 3️⃣ OrderBy / OrderByDescending — 排序

```csharp
// 按费用从低到高排序手牌
var sortedHand = hand.OrderBy(card => card.Cost).ToList();

// 按伤害从高到低排序（降序）
var sortedByDamage = attackCards.OrderByDescending(card => card.Damage).ToList();
```

### 4️⃣ Any — 是否存在符合条件的元素

```csharp
// 手牌中有没有攻击卡？
bool hasAttack = hand.Any(card => card.Type == CardType.Attack);

// 敌人中有没有血量<10的？
bool hasLowHP = enemies.Any(enemy => enemy.CurrentHP < 10);
```

### 5️⃣ Count — 统计符合条件的数量

```csharp
// 手牌中有几张攻击卡？
int attackCount = hand.Count(card => card.Type == CardType.Attack);

// 卡组中有几张升级过的卡？
int upgradedCount = deck.Count(card => card.IsUpgraded);
```

### 6️⃣ FirstOrDefault — 获取第一个符合条件的元素

```csharp
// 找到第一张打击卡
CardModel firstStrike = deck.FirstOrDefault(card => card.Tags.Contains(CardTag.Strike));

// ⚠️ 注意：如果找不到，返回 null
if (firstStrike != null) {
    // 使用这张卡
}
```

---

## 10. IEnumerable<T> 接口

### 什么是 IEnumerable？

**IEnumerable<T> = "我是一个可以被遍历（枚举）的集合"**

### 谁实现了 IEnumerable？

几乎所有集合类都实现了这个接口：

- List<T>
- 数组 T[]
- HashSet<T>
- Dictionary<TKey, TValue>
- Queue<T>

### IEnumerable 和 LINQ 的关系

**LINQ 的所有方法都是针对 IEnumerable<T> 的扩展方法！**

这意味着：**只要是 IEnumerable<T>，就能用 LINQ！**

```csharp
List<int> list = new List<int> { 1, 2, 3, 4, 5 };
int[] array = { 1, 2, 3, 4, 5 };
HashSet<int> set = new HashSet<int> { 1, 2, 3, 4, 5 };

// 它们都能用 LINQ，因为都是 IEnumerable<int>
var result1 = list.Where(x => x > 3);   // ✅
var result2 = array.Where(x => x > 3);  // ✅
var result3 = set.Where(x => x > 3);    // ✅
```

---

## 11. 惰性求值（Lazy Evaluation）⭐ 重要！

### 核心概念

```csharp
IEnumerable<CardModel> deck = GetDeck();           // 没执行任何筛选
IEnumerable<CardModel> attacks = deck.Where(...);  // 还没执行筛选！
IEnumerable<CardModel> upgraded = attacks.Where(...); // 还是没执行！

// ⚠️ 重点：上面三行只是"记录了要做什么"，并没有真正执行

List<CardModel> result = upgraded.ToList(); // 现在才真正执行所有筛选！
```

### 比喻理解

- **IEnumerable** = 菜谱（记录步骤）
- **ToList()** = 真正做菜

### 为什么要惰性求值？

#### 原因1：节省内存

```csharp
// ❌ 浪费内存
List<CardModel> all = GetDeck().ToList();           // 占用内存：100张卡
List<CardModel> attacks = all.Where(...).ToList();  // 占用内存：50张卡
List<CardModel> upgraded = attacks.Where(...).ToList(); // 占用内存：10张卡
// 内存中同时存在 160 张卡的数据！

// ✅ 节省内存
IEnumerable<CardModel> all = GetDeck();         // 不占额外内存
IEnumerable<CardModel> attacks = all.Where(...); // 不占额外内存
IEnumerable<CardModel> upgraded = attacks.Where(...); // 不占额外内存
List<CardModel> result = upgraded.ToList();     // 只占用 10 张卡的内存
// 内存中只有最终结果的 10 张卡！
```

#### 原因2：只遍历一次

```csharp
// ❌ 不好的写法（遍历3次）
List<CardModel> step1 = deck.Where(c => c.Type == CardType.Attack).ToList(); // 遍历1次
List<CardModel> step2 = step1.Where(c => c.IsUpgraded).ToList();             // 遍历2次
List<CardModel> step3 = step2.OrderBy(c => c.Cost).ToList();                 // 遍历3次

// ✅ 好的写法（只遍历一次）
IEnumerable<CardModel> query = deck
    .Where(c => c.Type == CardType.Attack)  // 记录步骤1
    .Where(c => c.IsUpgraded)               // 记录步骤2
    .OrderBy(c => c.Cost);                  // 记录步骤3

List<CardModel> result = query.ToList();    // 一次性执行所有步骤，只遍历1次！
```

---

## 12. IEnumerable vs List 对比

| 特性 | IEnumerable<T> | List<T> |
|------|----------------|---------|
| 类型 | 接口 | 具体类 |
| 能遍历 | ✅ | ✅ |
| 能用LINQ | ✅ | ✅ |
| 能用索引 [0] | ❌ | ✅ |
| 能 Add/Remove | ❌ | ✅ |
| 性能 | 惰性求值，节省内存 | 立即执行 |
| 灵活性 | 可以换底层实现 | 固定是 List |

---

## 13. 杀戮尖塔2 实战示例

### 示例1：筛选可打出的卡牌

```csharp
// 找出费用 ≤ 当前能量的卡
int currentEnergy = 3;
var playableCards = hand.Where(card => card.Cost <= currentEnergy).ToList();
```

### 示例2：统计卡组中的攻击卡数量

```csharp
int attackCardCount = deck.Count(card => card.Type == CardType.Attack);
Debug.Log($"卡组中有 {attackCardCount} 张攻击卡");
```

### 示例3：找出血量最低的敌人

```csharp
Enemy weakestEnemy = enemies.OrderBy(enemy => enemy.CurrentHP).FirstOrDefault();
// 优先攻击血最少的敌人
```

### 示例4：检查是否拥有某个遗物

```csharp
bool hasBurningBlood = relics.Any(relic => relic.Name == "Burning Blood");
if (hasBurningBlood) {
    // 战斗结束回血
}
```

### 示例5：组合使用（链式调用）

```csharp
// 找出所有升级过的攻击卡，按伤害降序排序，取前3张
var topAttacks = deck
    .Where(card => card.Type == CardType.Attack && card.IsUpgraded)
    .OrderByDescending(card => card.Damage)
    .Take(3)
    .ToList();
```

---

# 第三部分：学习过程中的疑问与思考

以下是你在学习过程中提出的重要问题，体现了你的深度思考：

## ❓ 问题1：副本的概念是什么？

**💡 答案：** C# 默认传参传的是副本（复印件），原件不受影响。引用类型赋值时共享同一份数据，改一个另一个也会变。

---

## ❓ 问题2：引用类型都有哪些？

**💡 答案：** class、数组、List、Dictionary、string（特殊处理表现像值类型）。

---

## ❓ 问题3：为什么不直接在父类写飞行方法，而要用接口？

**💡 答案：** 因为不是所有子类都能飞，强行写在父类会让不会飞的子类也必须实现，逻辑上说不通。接口只让需要的子类实现。

---

## ❓ 问题4：abstract 类的使用场景是什么？

**💡 答案：** 不只是不知道怎么写才用。更重要的是：强制规范子类必须实现某些方法，以及禁止创建没有意义的对象（比如单纯的 Hero 对象没有意义）。

---

## ❓ 问题5：GetHandCards() 是什么？

**💡 答案：** 是的，它是一个方法（函数）。方法前面的 List<CardModel> 表示返回值的类型。调用时会执行方法内部的代码，然后把结果返回。

---

## ❓ 问题6：为什么中间过程用 IEnumerable，最后才 ToList()？

**💡 答案：** 因为节省内存和提升性能。IEnumerable 使用惰性求值，不创建中间 List，只在最后 ToList() 时才真正执行，只遍历一次集合。

---

# 第四部分：杀戮尖塔2 源码解读

## StrikeSilent 完整源码

```csharp
public sealed class StrikeSilent : CardModel
{
    protected override HashSet<CardTag> CanonicalTags 
        => new HashSet<CardTag> { CardTag.Strike };
    
    protected override IEnumerable<DynamicVar> CanonicalVars 
        => new global::<>z__ReadOnlySingleElementList<DynamicVar>(new DamageVar(6m, ValueProp.Move));
    
    public StrikeSilent()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

## 逐行解读

| 代码片段 | 你能读懂的意思 | 用到的知识 |
|---------|---------------|-----------|
| sealed class : CardModel | 密封类，继承 CardModel，不能被继承 | sealed、继承 |
| HashSet<CardTag> | 不重复的标签集合（泛型） | 泛型、HashSet |
| => new HashSet<CardTag>{...} | Lambda 返回一个新的标签集合 | Lambda、属性 |
| IEnumerable<DynamicVar> | 可枚举的动态变量集合（LINQ可操作） | IEnumerable接口 |
| base(1, CardType.Attack, ...) | 调用父类构造函数，费用1，攻击卡 | 继承、base |
| UpgradeValueBy(3m) | 升级时伤害+3，6→9 | 方法调用 |
| async Task OnPlay | 异步方法（下一步学习内容） | async/await |

---

# 第五部分：未来学习路线

## 📌 你已经掌握了 C# 的核心基础！

以下是你还需要学习的内容：

---

## 🔜 下一步：必学内容

### 1. 委托（Delegate）和事件（Event）

**重要性：** ⭐⭐⭐⭐⭐ 非常重要

**用途：**
- 把方法当参数传递
- 实现卡牌触发效果（如"当你打出攻击卡时..."）
- 战斗事件系统

```csharp
// 示例：卡牌效果触发
onCardPlayed += (card) => {
    if (card.Type == CardType.Attack) {
        GainBlock(1);
    }
};
```

---

### 2. async / await 异步编程

**重要性：** ⭐⭐⭐⭐⭐ 非常重要

**用途：**
- 资源加载（图片、音频、数据）
- 网络请求
- 理解 OnPlay 方法中的 await

```csharp
protected override async Task OnPlay(...)
{
    await DamageCmd.Attack(6).Execute(...);
}
```

---

### 3. 反射（Reflection）

**重要性：** ⭐⭐⭐⭐ 重要

**用途：**
- 运行时动态获取类的信息
- 动态创建对象
- 插件系统、Mod系统

```csharp
// 示例：动态创建卡牌对象
Type cardType = Type.GetType("StrikeSilent");
CardModel card = (CardModel)Activator.CreateInstance(cardType);
```

---

## 🎯 进阶：工作中会用到的内容

### 4. LINQ 高级用法

**重要性：** ⭐⭐⭐⭐ 重要

- GroupBy — 分组统计
- Join — 连接两个集合
- Aggregate — 自定义聚合
- Distinct — 去重

---

### 5. 异常处理（Exception Handling）

**重要性：** ⭐⭐⭐⭐ 重要

- try-catch-finally
- 自定义异常
- 异常传播

---

### 6. 特性（Attribute）

**重要性：** ⭐⭐⭐ 中等

- [Serializable]、[Obsolete] 等
- 自定义特性

---

### 7. 扩展方法（Extension Methods）

**重要性：** ⭐⭐⭐ 中等

- 为现有类添加方法
- LINQ 就是用扩展方法实现的

---

### 8. 可空类型（Nullable）

**重要性：** ⭐⭐⭐⭐ 重要

- int? — 可以为 null 的 int
- ?? 空合并运算符
- ?. 空条件运算符

---

### 9. 序列化（Serialization）

**重要性：** ⭐⭐⭐⭐ 重要

- JSON 序列化/反序列化
- 存档系统

---

### 10. 文件操作（File I/O）

**重要性：** ⭐⭐⭐ 中等

- 读写文本文件
- StreamReader / StreamWriter

---

## 📚 学习优先级建议

| 优先级 | 主题 | 原因 |
|--------|------|------|
| 🔥 最高 | 委托和事件 | 理解游戏事件系统的核心 |
| 🔥 最高 | async/await | 理解异步代码执行 |
| ⭐ 高 | 反射 | 你已经在使用，需要系统学习 |
| ⭐ 高 | LINQ 高级 | 深化数据处理能力 |
| 📖 中 | 异常处理 | 编写健壮代码 |
| 📖 中 | 可空类型 | 防止 NullReferenceException |
| 📖 中 | 序列化 | 存档系统必备 |

---

## 🎯 推荐学习路径

1. **先学委托和事件** — 这是理解游戏架构的关键
2. **再学 async/await** — 理解 OnPlay 方法
3. **然后学反射** — 你已经在用了
4. **深入 LINQ** — GroupBy、Join 等高级用法
5. **学习其他工具** — 异常处理、序列化等

---

# 🎉 学习总结

## ✅ 你已掌握的知识：

- ✅ ref/out/in 参数传递
- ✅ 值类型 vs 引用类型
- ✅ 面向对象三大支柱
- ✅ 抽象类和接口
- ✅ 静态类和方法
- ✅ 泛型
- ✅ Lambda 表达式
- ✅ LINQ 核心方法
- ✅ IEnumerable 接口
- ✅ 惰性求值

---

## 💡 记住吉尔伽美什的比喻：

**王之财宝仓库 = 堆内存（引用类型）**

**图纸副本 = 栈内存（值类型）**

---

**继续加油！💪**
