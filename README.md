STS2 Advisor - 基于反射技术的《杀戮尖塔 2》洗牌预测与战斗增强工具
✅ 实时监测抽牌堆与弃牌堆状态。
✅ 高精度模拟游戏底层洗牌算法。
✅ 多人模式下的本地玩家自动识别。

快速上手
环境要求：.NET 8.0, Godot 4.x
运行命令：dotnet build

sequenceDiagram
    participant UI as UI/外部调用者
    participant Core as AdvisorRuntimeCore
    participant Game as 游戏单例 (CombatManager/RunManager)
    participant Ref as 反射缓存 (_cachedLocalPlayerIdField)

    UI->>Core: TryBuildSnapshot(delta)
    
    Note over Core: 1. 节流检查 (0.25s)
    Core->>Game: 获取当前战斗状态 (DebugOnlyGetState)
    Game-->>Core: 返回 CombatState

    Note over Core: 2. 玩家排序 (探测本地 ID)
    Core->>Ref: 检查是否已缓存“搜查令”?
    alt 未缓存
        Core->>Game: typeof(RewardSynchronizer).GetField("_localPlayerId")
        Game-->>Ref: 存入 FieldInfo 盒子
    end
    Ref-->>Core: 提供 FieldInfo
    Core->>Game: GetValue(获取具体的 ulong ID)
    Game-->>Core: 返回本地玩家 ID

    Note over Core: 3. 循环处理每个玩家数据
    loop 每个玩家 (Player)
        Core->>Core: 计算牌堆指纹 (BuildPileSignature)
        Core->>Core: 比较指纹，确定 Changed 状态
        
        opt 存在随机数种子 (RNG)
            Note over Core: 4. 洗牌预测 (模拟未来)
            Core->>Core: 克隆影子 RNG (peek)
            Core->>Core: 执行 StableShuffle (排序 + 随机)
        end
    end

    Core-->>UI: 输出 PanelSnapshot (包含所有玩家牌堆和预测结果)

🛠️ 设计架构与技术
本项目展示了在受限的单线程环境（Godot）中，如何高效地进行底层数据探测与逻辑模拟。

1. 反射缓存设计 (Reflection Performance Optimization)
为了读取内部私有的 _localPlayerId，项目避免了高能耗的逐帧内存搜索：
缓存策略：引入 FieldInfo 静态缓存。仅在初始化时定位字段偏移量，后续通过指针级的 GetValue 快速提取数据。
安全降级：通过完善的异常处理机制，确保在游戏版本更新导致字段变动时，Mod 能安全失效而不会导致游戏闪退。

2. 状态监测与性能节流 (State Throttling)
通过双重机制确保极致的性能表现：
0.25s 刷新窗口：基于 delta 时间的节流算法，平衡了 UI 实时性与 CPU 占用。
指纹检测 (Data Signature)：为牌堆生成轻量级指纹，只有当数据发生实质性变化时，才触发昂贵的 UI 重绘逻辑。

3. “影子 RNG” 模拟逻辑 (Shadow RNG Simulation)
项目实现了对游戏随机性序列的“无损预测”：
隔离性：通过克隆游戏当前的 Rng 种子与计数器（Counter），在独立的影子实例中运行预测。
算法还原：100% 还原了游戏原生的 Fisher–Yates 乱序算法（StableShuffle），实现精准预测且不污染游戏的原始随机序列。
