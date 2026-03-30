# 2026-CUSGA

Unity 2022.3 的 2D 塔防原型项目。

当前仓库已经做过一次协作精简，只保留了项目开发真正需要的内容：

- `Assets/`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/`

协作规则见：

- `CONTRIBUTING.md`

以下内容不会上传到仓库：

- 本地 AI 记忆文档
- 本地 AI 工具包
- 本地协作者临时说明文件
- 非必要的 TextMesh Pro 示例资源

## 环境要求

- Unity `2022.3.62f3c1`
- Git
- GitHub 私有仓库访问权限

## 第一次拉取仓库

如果你已经被加入仓库协作者，并且本机已经配置好 GitHub SSH：

```powershell
git clone git@github.com:habhab783124-lab/2026-CUSGA.git
cd 2026-CUSGA
```

如果你暂时使用 HTTPS：

```powershell
git clone https://github.com/habhab783124-lab/2026-CUSGA.git
cd 2026-CUSGA
```

## 打开项目

1. 打开 Unity Hub
2. `Add project from disk`
3. 选择仓库目录
4. 用 Unity `2022.3.62f3c1` 打开

首次打开时，Unity 会自动导入依赖和重新生成本地工程文件。

## 日常同步流程

开发前先拉最新：

```powershell
git checkout main
git pull
```

## 推荐开发流程

建议不要长期直接在 `main` 上开发，优先使用分支：

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

然后去 GitHub 发起 Pull Request。

## 如果只是做小改动

```powershell
git status
git add -A
git commit -m "Describe your change clearly"
git push
```

## 常用命令

查看状态：

```powershell
git status
```

查看当前分支：

```powershell
git branch --show-current
```

查看最近提交：

```powershell
git log --oneline -n 10
```

同步远程：

```powershell
git pull
```

上传本地提交：

```powershell
git push
```

## 常见问题

`Repository not found`

- 还没接受 GitHub 仓库邀请
- 当前 GitHub 账号没有仓库权限
- 仓库地址不对

`Permission denied (publickey)`

- SSH key 没加到 GitHub
- 当前电脑没有使用正确的私钥

`failed to push some refs`

- 远程已经有更新，先执行：

```powershell
git pull --rebase
git push
```

## 项目协作约定

- 提交前尽量保证 Unity 能正常编译
- 不要提交 `Library/`、`Temp/`、`Logs/`、`obj/`
- 不要提交本地 AI 文档或本地辅助工具
- 尽量避免多人同时修改同一个大场景文件
- 改动尽量小步提交，提交信息写清楚
