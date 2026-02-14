# SYSTEM DOCUMENTATION

## Entity System

Registries:
- EntityTagRegistry
- EntityTypeRegistry

Handlers:
- EntityFieldHandler

BaseClasses:
- Entity
- DynamicEntity
- StaticEntity

OtherClasses:
- TypeValues

**Note**

- > **Entity.Type** is a globally registered, immutable definition that links an Entity class to a unique identity and type-level configuration.
- > **EntityFieldHandler** is a system responsible for maintaining reference integrity between entities.
During initialization, it collects and stores all Entity reference fields from every registered Entity.Type.
At runtime, it uses this precomputed field metadata to check those fields and automatically nullifies any reference that points to a destroyed entity.
- > **DynamicEntity** exists and is updated on the CoreThread.
Because rendering occurs on the RenderThread, its render-relevant data is copied from the CoreThread to the RenderThread to ensure thread safety.
- > **StaticEntity** exists as one **Global Instance**, exists and is updated on the CoreThread. Instances exist on the RenderThread, but their data is handled by the global instance.
- > **TypeValues** are static/global values for every registered EntityType. 