# Schulte Flow · Unity 原型

独立保存的循光 Unity 客户端原型，目标 Unity **6000.0.60f1**。源码来自 `azfiles/apkGo` 的 `codex/unity-client-v1`，快照 `228156918890fed46473e6e108d6e8b438aa2c79`。

[完整开发说明](unity/README.md) · [验收范围](docs/unity-verification.md) · [可直接安装的原生 / Godot 舒尔特表](https://github.com/azfiles/schulte-flow)

Unity Hub 导入 `unity/` 目录，打开 `Assets/SchulteFlow/Scenes/Flow.unity`。纯规则测试：

```bash
bash scripts/test-unity-logic.sh
```

**这是源码原型，尚无经验证的 Unity APK。** 历史规则检查为 509 项；完整构建依赖 Unity 合法激活，Secrets 不随拆分迁移。需要构建时按开发说明自行配置，再手动运行 Android 工作流。

这里只保留 Unity 源码、字体许可证、规则测试和构建工具，不包含原生旧 APK。后续 3D 主项目在 [azfiles/apkGo](https://github.com/azfiles/apkGo)。项目自有代码为 MIT；字体使用 `unity/Assets/SchulteFlow/Resources/Fonts/` 内的原有许可，Unity Engine 使用其自身条款。
