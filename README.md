# 🗡️ Aion 2 DPS Meter

A lightweight, **network-based** DPS meter for **Aion 2**. It passively reads game packets off the network interface — it does **not** modify, intercept, or interact with the game client or server in any way.

> **⚠️ Disclaimer:** This tool only reads network traffic on your local machine. It does not inject code, modify memory, or communicate with any external service. Use at your own discretion and in accordance with the game's terms of service.

> **ℹ️ VPN Required:** The meter currently captures traffic only on the loopback interface, so a VPN is required for it to work. Tested and confirmed working with **ExitLag** and **GearUP**.

---

## ✨ Features

### 📶 Ping Monitor
- Reads server heartbeat timing to display your **live ping** in colour-coded form:
  - 🟢 `< 60 ms` — Excellent
  - 🟡 `60–99 ms` — Good
  - 🟠 `100–199 ms` — Mediocre
  - 🔴 `≥ 200 ms` — Bad

### 🎯 Smart Target Tracking
- Detects the **active combat target** based on recent hits.
- Displays the target's **name**, **current HP**, **max HP**, and a live **HP bar** in real time.

### 📊 Real-Time DPS Tracking
- Displays live **DPS** and **total damage** for every player in the session.
- Animated **damage contribution bars** showing each player's share of total damage.
- Automatic session reset between fights.

![Main Window](ReadmeAssets/MainWindow.png)

### 🔍 Detailed Player Statistics
Click any player row to open a detailed breakdown:
- **Total damage** and **DPS**
- **Hit count**
- **Critical rate**, **back-attack rate**, **perfect hit rate**, **double-damage rate**, **parry rate**
- **Damage contribution** percentage relative to the group
- **Combat duration**
- **Skill-by-skill breakdown** — damage, hits, crits, and more per skill
- **Live combat log** (up to 200 most recent hits)

![Player Details](ReadmeAssets/SkillDetails.png)
![Combat Log](ReadmeAssets/CombatLog.png)

### 📜 Combat History
- Every completed fight is automatically saved as a **session snapshot**.
- Browse past sessions in the **History window**, complete with per-player stats and skill breakdowns.
- Configurable **minimum damage threshold** to filter out insignificant or accidental encounters.

![History](ReadmeAssets/CombatHistory.png)
![History Details](ReadmeAssets/HistoryDetails.png)

### 🙈 Nickname Hiding
- Toggle **nickname obfuscation** to mask all player names — useful for streaming or screenshots.

### 💾 Persistent Settings
- Window position, size, and all user preferences are saved automatically between sessions.

---

## 📦 Installation

### Step 1 — Install Npcap (required)

Download and install **Npcap** from [https://npcap.com/#download](https://npcap.com/#download).

> ⚠️ **During installation, you must check "Install Npcap in WinPcap API-compatible Mode".** The meter will not work without this option enabled.

### Step 2 — Download the latest release

Go to the [**Releases**](../../releases) page and download the latest `.zip` archive.

### Step 3 — Extract and run

1. Extract the `.zip` to any folder of your choice.
2. Run **`AionDpsMeter.UI.exe`** — no installation needed.
3. Launch **Aion 2** and start playing. The meter will automatically detect the game traffic and begin tracking.

> **Note:** You may need to run the application as **Administrator** depending on your system's network capture permissions.

---

## ❓ FAQ

**Q: What is the Assassin icon about?**
 - I think it fits better assassin playstyle xdx.

**Q: Why is my nickname displayed as `Player_xxxx`?**
 - The server does not include player nicknames in damage packets. A nickname is only sent in specific events, such as teleporting or entering a dungeon. Until one of those events occurs, the meter displays a placeholder name.

**Q: Why is the total party damage 1–2% lower than the boss's actual HP?**
 - The meter does not record damage dealt by **Theostones**. I didn't want to spend time on this since theostone damage is negligible. It may be added in a future update.

**Q: Why are some summon skills registered as a separate unknown entity?**
 - This is a rare edge case. It happens when a summon is spawned outside your render range, meaning the meter missed the summoning event and could not link the summon's ID to the owning player.

**Q: Will there be support for other languages?**
 - No. It's too much effort, and the translation will not be accurate anyway.

---

## 🛠️ Developer Build Guide

### Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 or later |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) (or Rider) | Latest recommended |
| [Npcap](https://npcap.com/#download) | Latest (WinPcap-compatible mode) |
| Git | Any recent version |

### Clone the repository

```bash
git clone https://github.com/Kuroukihime/af6b8cbd-8503-4627-95d3-aa1e33fa6d0d.git
cd af6b8cbd-8503-4627-95d3-aa1e33fa6d0d
```

### Build from command line

```bash
dotnet restore
dotnet build
```

### Build a self-contained release package

```bash
dotnet publish AionDpsMeter.UI/AionDpsMeter.UI.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -o ./publish
```

The output will be placed in the `./publish` folder, ready to distribute.

### Project structure

```
AionDpsMeter.sln
├── AionDpsMeter.Core        # Domain models, game data (skills, classes, mobs)
├── AionDpsMeter.Services    # Packet capture, TCP stream parsing, session management
└── AionDpsMeter.UI          # WPF front-end, view models, windows
```

### Running in Visual Studio

1. Open `AionDpsMeter.sln`.
2. Set **`AionDpsMeter.UI`** as the startup project.
3. Press **F5** (or run with **Administrator** privileges if packet capture fails to start).

---

## 🙏 Credits

- **Game data reverse engineering** — huge thanks to [**@taengu**](https://github.com/taengu/) for helping reverse-engineer Aion 2 game data.
- Built with [**SharpPcap**](https://github.com/dotpcap/sharppcap) and [**Packet.Net**](https://github.com/dotpcap/packetnet) for packet capture.
- UI built with **WPF** on **.NET 10** using [**CommunityToolkit.Mvvm**](https://github.com/CommunityToolkit/dotnet).

---

## 📄 License

See [LICENSE](LICENSE) for details.
