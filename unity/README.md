# 循光 · Unity 客户端第一版

独立 Unity 客户端源码，目标编辑器为 **Unity 6.0 LTS / 6000.0.60f1**。Android 默认构建为 ARM64、Android 8.0 及以上。

**交付状态：本地和 GitHub Actions 游戏规则测试均已通过，运行时代码通过参考程序集编译检查。GitHub 构建发布流程已提交并运行，因缺少 Unity 激活 Secrets 停在许可预检，尚未进入 Unity 编译；没有新版 APK。本仓库不含旧版原生 APK；可安装版本在独立的 azfiles/schulte-flow 仓库。**

## 首版功能

- 3×3、4×4、5×5 经典舒尔特：从 1 开始依次点按，点错提示、不额外加时。
- 限时挑战：三档分别为 20、35、60 秒；时间耗尽结束，暂停不消耗剩余时间。
- 程序生成的立体倒角卡片、轻微倾斜棋盘、按压回弹、点击光波、连击粒子和通关反馈。
- 连击采用 1.6 秒间隔，误触和暂停会中断；经典和挑战、不同棋盘分别记录最佳成绩。
- 递进音阶、完成和弦、系统触感；支持单独关闭音效、触感及减少动态效果。
- 进入后台停表、暂停遮住棋盘、恢复时手动继续；在本机保存未完成进度和最佳成绩。
- 安全区适配、竖屏布局、随包提供的中文子集字体；运行不依赖云服务、外部模型或素材下载。

## 在 Unity 中运行

1. 用 Unity Hub 安装 6000.0.60f1，勾选 **Android Build Support、Android SDK & NDK Tools、OpenJDK**，完成正常的 Unity 许可激活。
2. Add project，选择本仓库的 **`unity/`** 目录。
3. 首次导入会解析 UGUI 等 Unity 官方包；开发机需能连接 Unity 包服务。项目使用 Built-in Render Pipeline，无第三方插件。
4. 从菜单选择 **Schulte Flow → Open first version**。打开的场景是 `Assets/SchulteFlow/Scenes/Flow.unity`。
5. Game 视图选择竖屏（例如 540×960 或 1080×1920），点击 Play，再点「开始专注」。鼠标可代替单指触控。

场景中的 `FlowApp` 会生成界面和棋盘，不需要手动拖入 Prefab、材质、字体或绑定按钮事件。也可先打开该场景，再直接 Play。首次打开会应用项目的默认构建配置；修改默认配置应在 `Editor/UnityBuild.cs` 中进行。

## Android 构建

在 File → Build Profiles 中选择 Android 并 Switch Platform，再运行 **Schulte Flow → Build Android preview APK**。

默认输出 `unity/Builds/Android/Schulte-Flow-Unity-preview.apk`。初版目标为 ARM64 真机；旧的 x86_64 模拟器不能直接用于此包的验证。

也可从仓库根目录运行：

```bash
export UNITY_EDITOR='/path/to/Unity/Editor/Unity'
bash scripts/build-unity.sh
```

编辑器路径因平台而异；Windows 可在 Unity 菜单中构建，或通过 PowerShell 传入相同的 `-batchmode -quit -projectPath ... -buildTarget Android -executeMethod SchulteFlow.Editor.UnityBuild.Android` 参数。

脚本的成功条件包括 Unity 的 `SCHULTE_BUILD_PASS` 标记；日志在 `work/unity-build/editor.log`。可用 `SCHULTE_BUILD_PATH` 指定输出位置。此处仅提供构建入口，未配置未经验证的自动发布流程。

默认包名 `com.alex.schulteflow.unitypreview`，与原生 v1 并存。本地直接构建使用 Unity 默认调试签名；GitHub 发布流程会换用仓库已有的公开演示签名，便于预览包后续更新。版本号为 `2.0.0-preview.1`，本地 versionCode 默认 `20001`，CI 使用 `20000 + run_number`。

## GitHub Actions 构建与发布

`.github/workflows/unity-android.yml` 提供完整流程：规则测试 → 许可配置检查 → Unity Android 构建 → 包名/ARM64/签名/对齐验证 → GitHub 预发布。为节省构建资源，拆分后仅手动触发。发布标签为 `unity-preview-r<运行编号>`，不会替换原生 v1 的 Release。

Unity 使用固定版本 `6000.0.60f1`，构建器固定到 GameCI v4 的具体提交。仓库 **Settings → Secrets and variables → Actions** 需要配置：

- `UNITY_EMAIL`、`UNITY_PASSWORD`：Unity 激活所用账号。
- `UNITY_SERIAL` 或 `UNITY_LICENSE`：适用于该账号、Unity 版本及 CI 环境的许可材料。

前置检查只检查这些配置是否存在，不输出内容；存在不代表许可有效，实际激活由 Unity 验证。许可获取方式应遵循当前 Unity / GameCI 的官方说明；Personal 许可也需兼容此版本的激活方式。不要把账号密码或许可正文提交到仓库、Issue 或聊天。

配置缺失会使流程明确失败，构建和发布阶段跳过。补齐后可在该次 Actions 页面选择 **Re-run failed jobs**；无需修改游戏代码。所有发布包都会附规则报告、签名报告、包信息与 SHA256。

自动发布仅声称通过构建及包验证；设备交互、画面效果、帧率和迁移验证仍单独记录为待验证。

## 老版成绩迁移

默认并存预览版有独立的 Android 沙箱，**不能读取原生 v1 的成绩**。

准备验证覆盖升级时，设置 `SCHULTE_APP_ID=com.alex.schulteflow`，在 Unity Publishing Settings 中使用与旧 APK 相同的签名密钥。首次启动会尝试读取旧 `schulte_flow` SharedPreferences，迁移三档经典最佳成绩、音效/触感设置、练习次数和未完成棋盘；旧记录不删除。签名与包名均一致才能覆盖安装及访问旧数据。此迁移入口尚未经过设备验证，不应提前删除旧 App 数据。

## 结构与验证

- `Assets/SchulteFlow/Core/Round.cs`：不依赖 Unity 的游戏状态机，沿用原生规则并扩展限时和连击。
- `Runtime/FlowApp.cs`：输入、倒计时、模式切换、前后台生命周期。
- `Runtime/BoardView.cs`：倒角网格、数字卡片、对象池反馈。
- `Runtime/FlowUi.cs`：UGUI、安全区、自适应布局和弹窗。
- `Runtime/SaveStore.cs`：独立记录、进度恢复和旧版迁移入口。
- `Resources/Shaders`：轻量材质与透明光波；`Resources/Fonts`：随包中文字体。
- `Editor/UnityBuild.cs`：编辑器配置、Android/Linux 构建入口。

逻辑测试只需 .NET SDK 8，无 Unity 或 NuGet 外部依赖：

```bash
bash scripts/test-unity-logic.sh
```

已通过 **509 项检查**，覆盖随机棋盘完整性、三档两模式完整通关、误触、连击、暂停恢复、限时边界和损坏存档。GitHub Actions 的 [Unity game rules](https://github.com/azfiles/apkGo/actions/runs/38688718028) 已成功运行，；拆分后在本仓库手动执行。

运行时代码另用 UnityEngine 2021.3.33 / UGUI 2018.3.5.1 参考程序集和 C# 编译器检查，含 Android 条件分支。该检查仅验证语法和所用的较早 Unity API，不等价于目标 Unity 6 的脚本导入、Shader 编译、画面渲染、JNI 或 IL2CPP 构建。Editor 构建脚本也需要在目标 Unity 版本验证。

详细验收步骤见 [首版验收清单](../docs/unity-verification.md)。

## 字体

`FlowSans.otf` 是 Noto Sans SC 的重命名字符子集，保留本版界面所需中文、数字和基本标点；新增文案时需补充字符或换用完整字体。来源与 SIL OFL 1.1 许可见 `Assets/SchulteFlow/Resources/Fonts/NOTICE.txt`、`OFL.txt`。
