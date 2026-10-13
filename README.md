# Luminix

A 2D Game Engine in C# with an archetype ECS and data-driven defs, currently using MonoGame.

> [!Important]   
> still a work in progress, working on it in my free time.

## Features

- [x] 2 Threaded: Core and Render Threads
- [x] Archetype ECS
- [x] Defs System
- [x] Logger
- [x] AudioSystem
- [ ] RenderingSystem **(Partially done)**
  - [x] Texture Rendering
    - [x] Textures
    - [x] Bitmap Fonts
  - [ ] Vector Rendering
    - [ ] Vector Shapes
    - [ ] Vector Based Fonts
- [ ] Remove dependency on MonoGame **(Partially done)** *(Acts like a wrapper for SDL2, Also is used for shader loading, and font asset files)*
  - [x] Custom Texture Loader
  - [x] Custom Audio Loader
  - [ ] SDL3
  - [ ] font .ttf file loading
  - [ ] shader loading
- [ ] **Serialization System**
  - [x] Data-driven definition loading
  - [ ] Game state saving & loading
- [ ] Physics
 - [ ] Box2D
- [ ] Particle System
- [ ] Localization


## STRESS TEST, 10k -> 100k -> 1mil entities

https://github.com/user-attachments/assets/f0d076e5-ea07-4a09-809b-d738fdeb30fe

**Hardware Specs Used:**
- **CPU:** Intel Core i5-14600K
- **GPU:** NVIDIA RTX 4070 Ti Super


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
