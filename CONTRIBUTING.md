# Contributing

本仓库用于 Unity 2D 塔防原型协作开发。

为了让项目保持可拉取、可打开、可继续迭代，请在提交前遵守下面这些规则。

## 基本流程

开始工作前：

```powershell
git checkout main
git pull
```

新功能或明显独立的修改，优先使用分支：

```powershell
git checkout -b feature/your-feature-name
```

开发完成后：

```powershell
git status
git add -A
git commit -m "Describe your change clearly"
git push -u origin feature/your-feature-name
```

然后在 GitHub 上发起 Pull Request。

如果只是很小的修复，也可以直接在当前分支提交，但仍然建议先同步最新代码。

## 提交规范

提交信息请写清楚，不要只写：

- `update`
- `fix`
- `改一下`

推荐写法：

- `Add tower placement preview`
- `Refactor HUD update flow`
- `Fix enemy path validation`
- `Update buildable area rules`

## Unity 项目协作规则

必须保留并提交：

- `Assets/`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/`
- 所有 `.meta` 文件

不要提交：

- `Library/`
- `Temp/`
- `Logs/`
- `obj/`
- `UserSettings/`
- 自动生成的 `.csproj`
- 自动生成的 `.sln`
- 本地 IDE 缓存
- 本地 AI 文档和 AI 工具目录

## 本仓库特有约定

以下内容只保存在本地，不上传到远程仓库：

- `AGENTS.md`
- `docs/ai-memory/`
- `docs/github-collaborator-setup.md`
- `Packages/SkillsForUnity/`
- `Assets/TextMesh Pro/Documentation/`
- `Assets/TextMesh Pro/Examples & Extras/`

这些内容已经在 `.gitignore` 中处理，不要手动重新加入版本库。

## 场景与资源协作建议

- 尽量避免多人同时修改同一个 `.unity` 场景文件
- 尽量把改动拆到脚本、Prefab 或独立资源，而不是把所有改动都堆进场景
- 修改资源名、对象名、路径前，先确认不会影响现有引用
- 如果不确定是否是项目必需资源，先不要删除

## 提交前最低验证

建议至少做以下检查：

1. `git status`，确认没有误加不该上传的本地文件
2. Unity 能正常打开项目
3. 脚本改动后至少保证可以编译
4. 如果改了运行时逻辑，至少做一次最基本的 Play 检查

如果你在本地使用 `dotnet build` 做额外验证，也很欢迎，但请注意 Unity 自动生成的工程文件有时会滞后，必要时先让 Unity 刷新。

## Pull Request 建议

PR 描述建议回答这几个问题：

1. 这次改了什么
2. 为什么要改
3. 影响了哪些系统
4. 做了哪些验证
5. 还有什么没验证

## 遇到问题时

如果你遇到：

- `Repository not found`
- `Permission denied (publickey)`
- `failed to push some refs`

优先先确认：

1. 你是否已经接受仓库邀请
2. 本机 GitHub SSH 是否配置成功
3. 当前分支是否已经先拉过最新代码

## 一句话原则

小步提交，描述清楚，少传本地文件，尽量别让协作者拉下来就报错。
