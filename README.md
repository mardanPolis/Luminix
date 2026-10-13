# Luminix

A 2D Game Engine in C# with an archetype ECS and data-driven defs, currently using MonoGame.

> [!NOTE]   
> still a work in progress

## 🌟 Features

- **Archetype-Based ECS**
- **Data-Driven Definitions**
- **Game Modding Built-in**

## STRESS TEST, 10k -> 100k -> 1mil entities

<video src="./Sandbox/TestVideos/entityTest.mp4" width="50%" autoplay loop muted playsinline></video>

**Hardware Specs Used:**
- **CPU:** Intel Core i5-14600K
- **GPU:** NVIDIA RTX 4070 Ti Super

## RoadMap

- [x] 2 Threaded: Core and Render Threads
- [x] Archetype ECS
- [x] Defs System
- [x] Logger
- [x] Custom Texture Loader
- [x] Custom Audio Loader
- [ ] Custom Shader Loader
- [ ] Custom Font loader **(Partially done)**
- [x] AudioSystem
- [ ] RenderingSystem **(Partially done)**
- [ ] Remove dependency on MonoGame **(Partially done)** *(Acts like a wrapper for SDL2)*
- [ ] **Serialization System**
  - [x] Data-driven definition loading
  - [ ] Game state saving & loading
- [ ] Physics
- [ ] Particle System
- [ ] Localization

## Project Structure
```text  
Luminix
– Assets            # Engine Embedded Assets
– native            # Native Code
– Src               
– – Core            # Engine Code
– – Collections     #   collection types
– – Debug           # Engine Debug Code
– – Exceptions      # Custom engine exception types
– – Extensions      # Helper extension methods across engine types
– – IO              # asset loading
– – Json            # Json Converters and Json Utility
– – Rendering       # Render pipeline
– – Threading       # Core and Render thread synchronization & tasks
