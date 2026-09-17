![.NET](https://img.shields.io/badge/.NET-11.0.preview6-blue?logo=dotnet)
![MAUI](https://img.shields.io/badge/MAUI-11.0.preview6-brightgreen?logo=dotnet)
![xUnit](https://img.shields.io/badge/xUnit-tests-orange)
![License](https://img.shields.io/badge/License-MIT-green)
![Seq](https://img.shields.io/badge/Seq-Structured%20Logging-5A67D8)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)


# XyloType

Do you want to learn dactylo or improve your typing precision/speed ?  
XyloType have been design in this very way  
Select you exercice, type then look at your stats.  


---
## Exercices

You can design your own exercices  
You will have to select the letters you want, text you want or dynamically generate pseudo words based on these letters.  


---
## Roadmap
- [ ] Add user database for exercice's stats  
  - [ ] Local 
  - [ ] Online (not the priority)
- [ ] From stat page, Add buttons: redo, or next exercice
- [ ] Exercices settings: reorder exercice

---

# Architectue
MVVM  
Clean Architecture

```mermaid
flowchart LR

Domain["Domain"]
Application["Application"]
UI["Maui"]
ViewModels["ViewModels"]
Infrastucture["Infrastructure"]

Infrastucture --> Application
UI ---> Application
UI --> ViewModels
ViewModels --> Application
Application --> Domain

```

---
## Technos
.NET11 preview5  
MAUI11.0.0-preview.5  
EF Core 11.0.0-preview.5  
Sqlite 11.0.0-preview.5  
CommunityToolkit.Mvvm 8.4.2  
CommunityToolkit.Maui 14.2.0  
Google.Protobuf 3.35.1  
Grpc.Tools 2.81.1  
Serilog — application logging  
Seq — structured log visualization and analysis  
🐳 Docker — runs the local Seq instance  

----

## 📝 Logging

The application uses Serilog for logging and Seq for structured log visualization and analysis.

### Seq visualization
Details about how to use [Seq](ReadmeSeq.md)

----

## Licence  
Mit