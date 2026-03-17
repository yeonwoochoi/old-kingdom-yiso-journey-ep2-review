# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Old Kingdom Yiso Journey Episode 2** is a Unity 2D top-down stage-based story RPG built with Unity 6000.0.62f1.

The codebase follows a **modular architecture** with:
- **C# class-based FSM (Ver2)** for AI/player state management
- **ScriptableObject-driven abilities system** separating data from logic
- **9 independent character modules** coordinated through facade pattern

The development team uses Korean comments throughout the codebase.

---

## 📚 Documentation Structure

This documentation is split into specialized files for better context management. Choose the right document based on your task:

### **Game Design (기획서)**
- 🎮 [GAME_DESIGN.md](docs/GAME_DESIGN.md) - 게임 기획서 (월드 구조, 성장 시스템, 저장 로직, 무한 도장)
  - Use when: Understanding overall game design vision
  - Use when: Checking world structure, chapter flow, save/reset rules
  - Use when: Planning stage entry/completion process

### **Architecture & Design**
- 🏗️ [ARCHITECTURE.md](docs/ARCHITECTURE.md) - 전체 시스템 레이어 구조 + Character System 아키텍처
  - Use when: Understanding the 4-layer initialization order (BootStrapper → Core → Infra → World)
  - Use when: Understanding Character System, FSM, Ability architecture
  - Use when: Learning module initialization, lifecycle, or interaction patterns

### **System Details**
- 🔧 [SYSTEMS.md](docs/SYSTEMS.md) - 각 시스템별 역할 및 책임범위 상세
  - Use when: Implementing or modifying any specific system (Core, Infra, World, Combat, Player, Economy, UI)
  - Use when: Checking which system owns a particular responsibility

### **API Reference**
- 📖 [API.md](docs/API.md) - Interface contracts and API documentation
  - Use when: Developing FSM Actions or Decisions
  - Use when: Creating new Abilities
  - Use when: Querying IYisoCharacterContext methods

### **Development Guide**
- 💻 [DEVELOPMENT.md](docs/DEVELOPMENT.md) - Development environment, testing, and build setup
  - Use when: Setting up development environment
  - Use when: Writing unit tests
  - Use when: Managing dependencies or building the project

### **Implemented Systems**
- ✅ [IMPLEMENTED.md](docs/IMPLEMENTED.md) - Currently working game systems (snapshot)
  - Use when: Checking what features are already implemented
  - Use when: Understanding Weapon, Combat, FOV systems
  - Use when: Verifying system integration details

### **Scripting System**
- 📝 [SCRIPTING.md](docs/SCRIPTING.md) - `.yiso` 커스텀 DSL 스크립팅 시스템 설계
  - Use when: Implementing ScriptingSystem (Lexer/Parser/Runner)
  - Use when: Adding new script block types or ScriptAPI
  - Use when: Working on the Unity EditorWindow tool

### **Development Roadmap**
- 🗺️ [ROADMAP.md](docs/ROADMAP.md) - 개발 우선순위 및 Phase별 구현 계획
  - Use when: Planning which system to implement next
  - Use when: Checking implementation status of a system
  - Use when: Understanding the core game loop

---

## ⚡ Quick Reference

### Common Commands
```bash
# Run Tests
Unity Editor → Window → General → Test Runner → EditMode tab

# Build Project
Unity Editor → File → Build Settings

# Unity Version
6000.0.62f1
```

### Key File Paths (현재 기존 코드 위치)
```
Character:    Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs
FSM Runtime:  Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateMachine.cs
FSM Actions:  Assets/Scripts/Gameplay/Character/StateMachine/Actions/
Abilities:    Assets/Scripts/Gameplay/Character/Abilities/
Health:       Assets/Scripts/Gameplay/Health/
EventBus:     Assets/Scripts/Gameplay/Tools/Event/YisoEventManager.cs
MapSystem:    Assets/Scripts/Gameplay/Map/YisoMapController.cs
```

### Naming Conventions
- **"Yiso" prefix**: All game-specific classes
- **"SO" suffix**: ScriptableObjects for static data
- **"I" prefix**: Interfaces (`IEventSystem`, `ISaveSystem`)
- **"System" suffix**: System classes (`YisoQuestSystem`)
- **Korean comments**: 팀 내부 주석은 한국어 사용

---

## 🎯 Getting Started

1. **전체 구조 파악** → [ARCHITECTURE.md](docs/ARCHITECTURE.md) — 4레이어 시스템 구조
2. **게임 기획 이해** → [GAME_DESIGN.md](docs/GAME_DESIGN.md) — 기획서 전체
3. **시스템 구현 시** → [SYSTEMS.md](docs/SYSTEMS.md) → [API.md](docs/API.md) 순서로 확인
4. **기존 코드 활용** → [IMPLEMENTED.md](docs/IMPLEMENTED.md) — 통합 상태 확인 후 진행
5. **우선순위 확인** → [ROADMAP.md](docs/ROADMAP.md) — Phase별 구현 계획

---

## 📝 Notes

- **설계 우선:** 전체 시스템 큰 틀을 기준으로 작업한다. 기존 Character/FSM 코드는 새 시스템에 맞게 통합할 예정.
- **Korean Comments**: 팀 내부 코드 주석은 한국어. 기존 스타일 유지.
- **기존 코드 처리:** 새 시스템과 충돌하는 기존 구현은 일부 또는 전부를 재편한다. 무조건 유지하지 않음.
