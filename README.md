# nanobot.NET

**nanobot.NET** 是 [nanobot](https://github.com/HKUDS/nanobot) 项目的 .NET 10 复刻版本。它是一个超轻量级的个人 AI 助手，以 DLL 的形式提供核心功能，并附带一个功能齐全的 CLI 工具。

## 🚀 特性

*   **超轻量级**: 保持了原始项目的精简架构。
*   **多模型支持**: 通过 OpenAI SDK 完美支持 OpenAI、OpenRouter 及其兼容接口。
*   **强大的工具系统**: 内置文件系统、Shell 执行、Web 搜索 (Brave)、Web 抓取、天气预报等工具。
*   **记忆系统**: 基于文件的持久化记忆。
*   **多通道支持**: 支持通过 Telegram 机器人进行交互。
*   **自动化**: 支持 Cron 定时任务和 Heartbeat 主动唤醒。

## 🛠️ 安装与运行

### 1. 初始化

```bash
dotnet run --project Nanobot.CLI onboard
```

这将在 `~/.nanobot` 目录下创建配置文件和工作区。

### 2. 配置

编辑 `~/.nanobot/config.json`，添加你的 API 密钥：

```json
{
  "providers": {
    "openai": {
      "apiKey": "YOUR_OPENAI_KEY"
    },
    "webSearch": {
      "apiKey": "YOUR_BRAVE_SEARCH_KEY"
    }
  }
}
```

### 3. 使用

*   **对话**:
    ```bash
    dotnet run --project Nanobot.CLI agent -m "你好"
    ```
*   **启动网关 (Telegram)**:
    ```bash
    dotnet run --project Nanobot.CLI gateway
    ```
*   **管理定时任务**:
    ```bash
    dotnet run --project Nanobot.CLI cron list
    ```

## 🏗️ 项目结构

*   `Nanobot.Core`: 核心逻辑库，包含 LLM 提供商、工具注册表、记忆系统等。
*   `Nanobot.CLI`: 命令行界面工具。
*   `Nanobot.Tests`: 单元测试项目。

## 📄 许可证

本项目采用 MIT 许可证。
