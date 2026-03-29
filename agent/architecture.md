# /agent/architecture.md

## Architecture

The project follows a **Feature-first Clean Architecture**.

Structure:

Assets/Scripts/Features/<FeatureName>/
Domain/
Application/
Presentation/
Infrastructure/
<FeatureName>Setup.cs      # Bootstrap: composition root
<FeatureName>Bootstrap.cs  # Bootstrap: scene-level wiring

Assets/Scripts/Shared/

Each feature must be self-contained.

Rules:

* Do not move feature-specific code into Shared.
* Shared must only contain reusable cross-feature utilities.
* Infrastructure implements Application ports.
* Domain must stay framework-independent.
* Bootstrap (Setup/Bootstrap classes at feature root) handles composition and wiring between layers.
* Scene-owned features must define a scene contract in their README.
* Networked features must define one explicit initialization path and late-join behavior in their README.
* Runtime fallback creation must not replace missing scene/prefab setup.

Features should grow independently.
Only split features when a concept gains an independent lifecycle.
Never move feature-specific code into Shared.

## Scene Contract

Every scene-owned feature must document:

* required GameObjects/components
* required serialized references
* runtime-created objects
* forbidden runtime-created replacements
* allowed runtime lookup exceptions
* initialization order
* late-join / reconnect behavior for networked objects
