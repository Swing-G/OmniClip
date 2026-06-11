# OmniClip Phase 3 Execution Plan — AI 核心 (v0.2.0)

> **基于设计规格说明书 §10 开发路线图 Phase 3**
> **日期**: 2026-06-11
> **预计工期**: 2-3 周

---

## 目标

实现 AI 语义搜索引擎和自动分类系统，让 OmniClip 从"被动记录"升级为"主动理解"。

核心能力：
- 用自然语言搜索剪贴板（不再依赖关键词精确匹配）
- 每条内容自动打上分类标签（代码/文章/链接/截图/密钥/待办等）

---

## 技术栈

| 组件 | 选型 | 理由 |
|------|------|------|
| **推理引擎** | ONNX Runtime 1.18+ | 跨平台，GPU 加速可选，C# 一等公民 |
| **嵌入模型** | `all-MiniLM-L6-v2` (multilingual) | 384维，~80MB，10ms/次，支持中英混合 |
| **向量存储** | SQLite BLOB 列 | 无需额外数据库，1万条约15MB |
| **相似度计算** | 余弦相似度 (暴力搜索) | <1万条时 <50ms，够用 |

---

## 任务分解

### Task 1: ONNX 模型准备

**目标**: 下载模型文件，集成 ONNX Runtime NuGet，创建推理服务骨架。

**操作步骤**:

1. 添加 NuGet 包: `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.OnnxRuntime.Managed`
2. 下载 `all-MiniLM-L6-v2` ONNX 模型文件（从 HuggingFace 转换或直接下载 ONNX 版本）
3. 创建 `EmbeddingService` 类:
   - 加载模型 `InferenceSession`
   - Tokenize 输入文本（使用 BertTokenizer 或简单 whitespace）
   - 运行推理 → 384 维 float32 向量
   - 池化策略: mean pooling
4. 单元测试: 相似语义句子余弦相似度 > 0.8，不相关句子 < 0.5

**期望产出**: `src/OmniClip.Core/Services/EmbeddingService.cs`

---

### Task 2: 向量数据库

**目标**: 在 SQLite 中存储和管理向量，实现快速相似度检索。

**操作步骤**:

1. 创建 `vectors` 表:
   ```sql
   CREATE TABLE IF NOT EXISTS vectors (
       entry_id TEXT PRIMARY KEY,
       vector BLOB NOT NULL,
       model_name TEXT NOT NULL,
       FOREIGN KEY (entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
   );
   ```
2. `VectorStore` 类:
   - `InsertVectorAsync(entryId, float[], model)` — 写 BLOB
   - `GetVectorAsync(entryId)` — 读 BLOB → float[]
   - `SearchSimilarAsync(queryVector, topK)` — 全量计算余弦相似度，返回 TopK
   - `DeleteByEntryIdAsync(entryId)`
3. 优化: 超过 5k 条时，先按时间粗筛（最近30天），再精确排序
4. 单元测试

**期望产出**: `src/OmniClip.Core/Services/VectorStore.cs`

---

### Task 3: 内容处理管道（AI Pipeline）

**目标**: 每条新内容到达时，自动完成向量化和分类。

**操作步骤**:

1. 创建 `AiPipelineManager`:
   ```
   新内容到达
     ↓
   文本提取（已有 PlainText）
     ↓
   向量化（EmbeddingService）→ 存入 vectors 表
     ↓
   自动分类（规则引擎 + 嵌入匹配）→ 更新 tags 字段
     ↓
   完成
   ```
2. 修改 `App.OnClipboardChanged`: 插入数据库后触发异步 AI 管道
3. 管道不阻塞 UI 线程（Task.Run）
4. 向量化失败不丢失记录（fallback: tags=空，仍可浏览）

**期望产出**: `src/OmniClip.Core/Services/AiPipelineManager.cs`

---

### Task 4: 语义搜索

**目标**: 用户输入自然语言，返回语义匹配的剪贴板历史。

**操作步骤**:

1. 在 `DatabaseService` 或新 `SearchService` 中添加:
   - `SemanticSearchAsync(query, topK=20)` 
   - 流程: query → embedding → VectorStore.SearchSimilar → 取 entry IDs → 查 clipboard_entries → 返回
2. 在 MainWindow 搜索框中支持语义搜索:
   - 关键词搜索（当前）: `WHERE plain_text LIKE '%xx%'`
   - 语义搜索: 当关键词匹配结果 < 3 条时自动触发语义搜索
   - 或添加搜索模式切换按钮
3. 结果显示时标注相似度百分比
4. 混合排序: 语义相似度(70%) + 时间衰减(20%) + 类型匹配(10%)

**期望产出**: `src/OmniClip.Core/Services/SearchService.cs`

---

### Task 5: 自动分类引擎

**目标**: 每条内容自动打上分类标签。

**操作步骤**:

1. **规则引擎**（覆盖 ~80% 场景）:
   - URL 正则 → `链接`
   - 代码特征({, =>, import, class 等) → `代码`
   - 密码/密钥模式 → `密钥`
   - 文件路径模式 → `文件路径`
   - 邮件/手机号正则 → `联系信息`
   - 日期时间密集 → `日程`
   - Markdown 格式 → `文档`

2. **嵌入引擎**（覆盖剩余 ~20%）:
   - 预设标签向量（代码/文章/待办/数据等）
   - 内容向量 vs 标签向量 → 余弦相似度 > 0.5 → 匹配
   
3. 分类结果写入 `tags` 字段（JSON 数组格式: `["代码","文档"]`）
4. 在 MainWindow 左侧栏显示标签及计数，支持标签筛选

**期望产出**: `src/OmniClip.Core/Services/TagClassifier.cs`

---

### Task 6: 标签管理 UI

**目标**: 用户可查看、筛选、管理标签。

**操作步骤**:

1. 左侧导航栏显示标签列表 + 计数
2. 点击标签 → Feed 仅显示该标签条目
3. 右键标签 → 重命名/删除
4. 用户可手动为条目添加/删除标签（在预览面板）
5. 标签筛选与类型筛选互斥或组合

**期望产出**: 修改 `MainWindow.xaml` 侧边栏

---

### Task 7: AI Insights 入口

**目标**: 激活侧边栏 "AI Insights" 按钮，展示 AI 功能入口。

**操作步骤**:

1. 点击 "AI Insights" → 弹出面板:
   - "Summarize today" - 总结今天的剪贴内容
   - "Semantic search" - 切换到语义搜索模式
   - "Smart tags" - 查看/管理自动标签
2. 当前阶段（无 LLM），可显示统计: 今日复制次数、最活跃来源、内容类型分布

**期望产出**: 修改 `MainWindow.xaml.cs` AiInsights_Click

---

## 数据量预估

| 规模 | 向量存储 | 检索延迟 |
|------|----------|----------|
| 1,000 条 | ~1.5 MB | <5ms |
| 10,000 条 | ~15 MB | <50ms |
| 100,000 条 | ~150 MB | <500ms（需粗筛优化） |

---

## 风险 & 缓解

| 风险 | 缓解 |
|------|------|
| ONNX 模型与 .NET 8 兼容性 | 使用最新 ONNX Runtime，优先测试模型加载 |
| 嵌入推理速度慢 | 文本截断到 512 token，英文模型转 multilingual 版本 |
| 向量 BLOB 读写性能 | 批量操作，异步写入 |
| 标签分类不准 | 规则引擎先覆盖常见场景，嵌入引擎作为补充 |

---

## 验收标准

- [ ] ONNX 模型成功加载，推理不报错
- [ ] "Redis 缓存" 和 "memcached 配置" 语义搜索能互相找到
- [ ] 代码片段自动标记为 `代码`
- [ ] URL 自动标记为 `链接`
- [ ] 语义搜索 < 100ms (1万条内)
- [ ] 向量化和分类不阻塞 UI
