# TCP/UDP 课表发送器

ClassIsland 2 插件，通过 TCP/UDP 协议将当前课表实时发送到指定设备。

## 功能

- 支持 TCP 和 UDP 两种协议发送课表数据
- 支持 JSON 和纯文本两种格式
- 纯文本格式支持自定义模板，可灵活控制输出内容
- 可设置发送间隔（默认 30 秒）
- 设置页面集成在 ClassIsland 设置中

## 安装

1. 下载 `.cipx` 插件包
2. 在 ClassIsland 中打开「插件」→「安装插件」→ 选择文件
3. 或者手动放入 `data\Plugins\ClassIsland.TcpUdpSender\`

## 设置

进入 ClassIsland 设置 → 「TCP/UDP 发送器」进行配置：

- **启用发送**：开关控制是否发送
- **目标地址**：接收设备的 IP 地址
- **目标端口**：接收设备的端口号
- **协议类型**：TCP 或 UDP
- **发送格式**：JSON 或 纯文本
- **发送间隔**：每次发送的间隔秒数
- **自定义模板**（纯文本格式下可用）：使用占位符自定义输出格式

## 纯文本占位符

| 占位符 | 说明 |
|--------|------|
| `{date}` | 日期（yyyy-MM-dd） |
| `{time}` | 时间（HH:mm:ss） |
| `{subject}` | 科目名称 |
| `{initial}` | 科目简称 |
| `{teacher}` | 教师姓名 |
| `{start}` | 课程开始时间 |
| `{end}` | 课程结束时间 |
| `{index}` | 课程序号 |
| `{isChanged}` | 是否换课（换课显示 `[换]`） |

## JSON 格式示例

```json
{
  "date": "2026-06-27",
  "time": "14:30:00",
  "classPlanName": "默认课表",
  "classes": [
    {
      "index": 1,
      "subject": "数学",
      "initial": "数",
      "teacher": "张老师",
      "start": "08:00",
      "end": "08:45",
      "isChanged": false
    }
  ]
}
```
