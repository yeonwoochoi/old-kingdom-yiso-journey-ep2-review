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

### **Architecture & Design**
- 🏗️ [ARCHITECTURE.md](docs/ARCHITECTURE.md) - Core architecture, design patterns, and system flows
  - Use when: Understanding Character System, FSM, Ability architecture
  - Use when: Learning module initialization, lifecycle, or interaction patterns

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

### **Game Design Roadmap**
- 🗺️ [ROADMAP.md](docs/ROADMAP.md) - Game design document and future systems
  - Use when: Understanding overall game design vision
  - Use when: Planning Quest, Save, Economy, Portal systems
  - Use when: Checking stage progression and world structure

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

### Key File Paths
```
Character Hub:     Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs
Character Modules: Assets/Scripts/Gameplay/Character/Core/Modules/
FSM Runtime:       Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateMachine.cs
FSM Actions:       Assets/Scripts/Gameplay/Character/StateMachine/Actions/
FSM Decisions:     Assets/Scripts/Gameplay/Character/StateMachine/Decisions/
Abilities:         Assets/Scripts/Gameplay/Character/Abilities/
Ability SOs:       Assets/Scripts/Gameplay/Character/Abilities/Definitions/
```

### Naming Conventions
- **"Yiso" prefix**: All game-specific classes (e.g., `YisoCharacter`, `YisoMovementAbility`)
- **"SO" suffix**: ScriptableObjects for data definitions (e.g., `YisoAbilitySO`, `YisoWeaponDataSO`)
  - **Note**: FSM no longer uses ScriptableObjects (Ver2 uses C# classes)
- **Module naming**: `YisoCharacter[Function]Module` (e.g., `YisoCharacterInputModule`, `YisoCharacterWeaponModule`)

### Design Patterns Used
- **Module Pattern**: Character decomposed into 9 independent modules
- **Facade Pattern**: YisoCharacter provides simple API hiding module complexity
- **Factory Pattern**: AbilitySO creates ability instances
- **Observer Pattern**: Event system for decoupled communication
- **Strategy Pattern**: Abilities as interchangeable behaviors
- **State Pattern**: FSM with C# classes (Ver2)

---

## 📌 Recent Changes

### Ver2 FSM Migration (Commit `8fa6bcf`)
- **Removed**: Ver1 FSM (ScriptableObject-based), BlackboardModule
- **Added**: Ver2 FSM (C# class-based) with Serializable states
- **Breaking Change**: All FSM SO assets became obsolete; states now defined in Inspector

### Face API Addition (Latest)
- **Added**: `Face(FacingDirections)` and `Face(Vector2)` methods to IYisoCharacterContext
- **Purpose**: Enable FSM Actions to control character direction
- **Implementation**: Delegates to YisoOrientationAbility.ForceFace()

### WeaponModule Integration
- **Added**: 9th character module for weapon management
- **Integration**: YisoOrientationAbility reads WeaponAim for aim-based direction priority

---

## 🎯 Getting Started

1. **New to the codebase?** Start with [ARCHITECTURE.md](docs/ARCHITECTURE.md) to understand the system design
2. **Adding new FSM Action/Decision?** Check [API.md](docs/API.md) for IYisoCharacterContext interface
3. **Implementing a game feature?** Check [ROADMAP.md](docs/ROADMAP.md) first, then [IMPLEMENTED.md](docs/IMPLEMENTED.md)
4. **Setting up tests?** See [DEVELOPMENT.md](docs/DEVELOPMENT.md) for testing infrastructure

---

## 📝 Notes

- **Korean Comments**: The team uses Korean comments for internal documentation. Follow existing patterns when adding comments.
- **Original Documentation**: The previous monolithic CLAUDE.md is backed up as `CLAUDE.md.backup` for reference.
