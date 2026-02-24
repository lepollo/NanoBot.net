# nanobot.NET

**nanobot.NET** 是 [nanobot](https://github.com/HKUDS/nanobot) 项目的 .NET 10 复刻版本。它是一个超轻量级的个人 AI 助手，以 DLL 的形式提供核心功能，并附带一个功能齐全的 CLI 工具。

## 🚀 特性

*   **超轻量级**: 保持了原始项目的精简架构。
*   **多模型支持**: 通过 OpenAI SDK 完美支持 OpenAI、OpenRouter 及其兼容接口。
*   **股票 & 天气**: **无需 API Key** 即可获取实时股价（美股/港股/A股）和天气。
*   **强大的工具系统**: 内置文件系统、Shell 执行、Web 搜索 (Brave)、Web 抓取等。
*   **缺省 Chat 模式**: 启动即进入交互对话，极致便捷。
*   **环境变量优先**: 支持全系统变量配置，无需在文件中存储密钥。

## 🛠️ 配置说明 (环境变量优先)

nanobot.NET 优先从 Windows 系统变量中读取配置。建议在终端中设置以下变量：

| 变量名 | 说明 | 示例 |
| :--- | :--- | :--- |
| `OPENAI_API_KEY` | **(必填)** 你的 API 密钥 | `sk-xxxx...` |
| `OPENAI_MODEL` | 使用的模型名称 (默认: `gpt-4o`) | `gpt-4-turbo` / `deepseek-chat` |
| `OPENAI_API_BASE` | API 代理/基础地址 | `https://openrouter.ai/api/v1` |
| `GITHUB_TOKEN` | GitHub 技能授权码 (可选) | `ghp_xxxx...` |

> **提示**: 如果环境变量未设置，程序将回退读取 `~/.nanobot/config.json` 配置文件。

## 📖 使用指南

### 1. 启动对话 (默认模式)
直接运行编译后的程序（或使用 `dotnet run`）即可进入交互式对话：
```bash
# 启动交互式聊天
nanobot
# 或
dotnet run --project Nanobot.CLI
```

### 2. 初始化环境 (首次使用)
```bash
nanobot onboard
```

### 3. 单次指令模式
```bash
nanobot agent -m "AAPL 现在的股价是多少？"
```

### 4. 启动网关 (Telegram)
```bash
nanobot gateway
```

### 5. 管理定时任务
```bash
nanobot cron add --name "daily_check" --message "检查今日天气和股市摘要" --cron "0 9 * * *"
nanobot cron list
```

## 🏗️ 项目结构

*   `Nanobot.Core`: 核心逻辑库，包含 LLM 提供商、工具注册表、记忆系统等。
*   `Nanobot.CLI`: 命令行界面工具，已集成缺省聊天循环。
*   `Nanobot.Tests`: 单元测试项目。

## 📄 许可证

本项目采用 MIT 许可证。
